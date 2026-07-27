window.conatradecMapa = (() => {
    const mapas = new Map();

    const coloresDepartamentos = {
        "boaco": "#7DA27E",
        "carazo": "#C9875B",
        "chinandega": "#D9B44A",
        "chontales": "#62A6A6",
        "esteli": "#D87A45",
        "granada": "#8A7CC2",
        "jinotega": "#8DAA56",
        "leon": "#C86A6A",
        "madriz": "#5E91C8",
        "managua": "#AA6FA8",
        "masaya": "#D39B52",
        "matagalpa": "#4E8B73",
        "nueva segovia": "#7894B8",
        "rio san juan": "#4BA3A7",
        "rivas": "#B78258",
        "region autonoma de la costa caribe norte": "#5D9C76",
        "region autonoma de la costa caribe sur": "#5B8FA8"
    };

    function inicializar(elementId) {
        destruir(elementId);

        const elemento = document.getElementById(elementId);
        if (!elemento) return;

        const mapa = L.map(elementId, {
            center: [12.8654, -85.2072],
            zoom: 7,
            minZoom: 6
        });

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(mapa);

        const marcadores = L.markerClusterGroup({
            showCoverageOnHover: false,
            maxClusterRadius: 55
        });

        mapa.addLayer(marcadores);
        const contexto = { mapa, marcadores, departamentos: null, terrenos: [] };
        mapas.set(elementId, contexto);

        cargarDepartamentos(contexto);
        setTimeout(() => mapa.invalidateSize(), 100);
    }

    async function cargarDepartamentos(contexto) {
        const url = "https://gis.unicef.org/server/rest/services/Departamentos_Nicaragua_MIL1/MapServer/5/query" +
            "?where=1%3D1&outFields=ADM1_ES&returnGeometry=true&outSR=4326&f=geojson";

        try {
            const respuesta = await fetch(url);
            if (!respuesta.ok) throw new Error("No fue posible descargar los límites.");
            const geojson = await respuesta.json();

            contexto.departamentos = L.geoJSON(geojson, {
                style: feature => estiloDepartamento(nombreDepartamento(feature)),
                onEachFeature: (feature, layer) => {
                    const nombre = nombreDepartamento(feature);

                    layer.on({
                        mouseover: evento => {
                            evento.target.setStyle({ weight: 3.5, fillOpacity: 0.32 });
                            evento.target.bringToFront();
                        },
                        mouseout: evento => {
                            contexto.departamentos.resetStyle(evento.target);
                        },
                        click: evento => {
                            const cantidad = contexto.terrenos.filter(
                                terreno => normalizar(terreno.departamento) === normalizar(nombre)).length;

                            evento.target.bindPopup(`
                                <div class="department-popup">
                                    <h3>${escapar(nombre)}</h3>
                                    <strong>${cantidad}</strong>
                                    <span>${cantidad === 1 ? "terreno registrado" : "terrenos registrados"}</span>
                                </div>
                            `).openPopup();
                        }
                    });

                    layer.bindTooltip(nombre, {
                        sticky: true,
                        direction: "top",
                        className: "department-tooltip"
                    });
                }
            }).addTo(contexto.mapa);

            contexto.departamentos.bringToBack();
        } catch (error) {
            console.warn("No se pudieron cargar los límites departamentales:", error);
        }
    }

    function mostrarTerrenos(elementId, terrenos) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        contexto.terrenos = terrenos || [];
        contexto.marcadores.clearLayers();
        const limites = [];

        contexto.terrenos.forEach(terreno => {
            if (!Number.isFinite(terreno.latitud) || !Number.isFinite(terreno.longitud)) return;

            const estado = (terreno.estado || "Normal").toLowerCase();
            const color = estado.includes("crít")
                ? "#EF4444"
                : estado.includes("atenci") || estado.includes("sin análisis")
                    ? "#FF9800"
                    : "#3B655B";

            const icono = L.divIcon({
                className: "custom-marker-wrapper",
                html: `<span class="custom-marker" style="--marker-color:${color}">
                           <i class="fa-solid fa-location-dot"></i>
                       </span>`,
                iconSize: [34, 42],
                iconAnchor: [17, 40],
                popupAnchor: [0, -36]
            });

            const marcador = L.marker([terreno.latitud, terreno.longitud], { icon: icono });
            const extension = terreno.extensionManzanas == null ? "No registrada" : `${terreno.extensionManzanas} Mz`;
            const ph = terreno.ultimoPh == null ? "No disponible" : terreno.ultimoPh;

            marcador.bindPopup(`
                <div class="map-popup">
                    <span class="map-popup-code">${escapar(terreno.codigo)}</span>
                    <h3>${escapar(terreno.nombre)}</h3>
                    <p><strong>Productor:</strong> ${escapar(terreno.productor)}</p>
                    <p><strong>Ubicación:</strong> ${escapar(terreno.municipio || "")}, ${escapar(terreno.departamento || "")}</p>
                    <p><strong>Extensión:</strong> ${escapar(extension)}</p>
                    <p><strong>Último pH:</strong> ${escapar(ph)}</p>
                    <span class="map-popup-status" style="--status-color:${color}">${escapar(terreno.estado || "Normal")}</span>
                </div>
            `);

            contexto.marcadores.addLayer(marcador);
            limites.push([terreno.latitud, terreno.longitud]);
        });

        if (contexto.departamentos) contexto.departamentos.bringToBack();

        if (limites.length === 1) {
            contexto.mapa.setView(limites[0], 14);
        } else if (limites.length > 1) {
            contexto.mapa.fitBounds(limites, { padding: [40, 40], maxZoom: 13 });
        } else {
            contexto.mapa.setView([12.8654, -85.2072], 7);
        }

        setTimeout(() => contexto.mapa.invalidateSize(), 100);
    }

    function estiloDepartamento(nombre) {
        return {
            color: "#33473F",
            weight: 1.6,
            opacity: 0.9,
            fillColor: coloresDepartamentos[normalizar(nombre)] || "#8AA79B",
            fillOpacity: 0.20
        };
    }

    function nombreDepartamento(feature) {
        const propiedades = feature?.properties || {};
        return propiedades.ADM1_ES || propiedades.NAME_1 || propiedades.nombre || "Departamento";
    }

    function normalizar(valor) {
        return String(valor || "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .replace(/\s+/g, " ")
            .trim();
    }

    function destruir(elementId) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;
        contexto.mapa.remove();
        mapas.delete(elementId);
    }

    function escapar(valor) {
        return String(valor ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    return { inicializar, mostrarTerrenos, destruir };
})();
