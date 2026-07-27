window.conatradecMapaInteligente = (() => {
    const mapas = new Map();

    function inicializar(elementId) {
        destruir(elementId);

        const elemento = document.getElementById(elementId);
        if (!elemento || typeof L === "undefined") {
            return false;
        }

        const mapa = L.map(elementId, {
            center: [12.8654, -85.2072],
            zoom: 7,
            minZoom: 5
        });

        L.tileLayer(
            "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
            {
                maxZoom: 19,
                attribution: "&copy; OpenStreetMap contributors"
            })
            .addTo(mapa);

        const marcadores =
            typeof L.markerClusterGroup === "function"
                ? L.markerClusterGroup({
                    showCoverageOnHover: false,
                    maxClusterRadius: 55
                })
                : L.layerGroup();

        mapa.addLayer(marcadores);

        mapas.set(elementId, {
            mapa,
            marcadores,
            departamentos: null
        });

        cargarDepartamentos(elementId);
        setTimeout(() => mapa.invalidateSize(), 120);

        return true;
    }

    async function cargarDepartamentos(elementId) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        const url =
            "https://gis.unicef.org/server/rest/services/" +
            "Departamentos_Nicaragua_MIL1/MapServer/5/query" +
            "?where=1%3D1&outFields=ADM1_ES&returnGeometry=true" +
            "&outSR=4326&f=geojson";

        try {
            const respuesta = await fetch(url);
            if (!respuesta.ok) return;

            const geojson = await respuesta.json();

            contexto.departamentos = L.geoJSON(geojson, {
                style: {
                    color: "#3B655B",
                    weight: 1.4,
                    opacity: 0.75,
                    fillColor: "#3B655B",
                    fillOpacity: 0.035
                },
                onEachFeature: (feature, layer) => {
                    const nombre =
                        feature?.properties?.ADM1_ES ||
                        "Departamento";

                    layer.bindTooltip(nombre, {
                        sticky: true,
                        direction: "top",
                        className: "department-tooltip"
                    });
                }
            }).addTo(contexto.mapa);

            contexto.departamentos.bringToBack();
        } catch (error) {
            console.warn(
                "No se cargaron los departamentos.",
                error);
        }
    }

    function mostrar(elementId, terrenos) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        contexto.marcadores.clearLayers();
        const limites = [];

        (terrenos || []).forEach(terreno => {
            const latitud = Number(terreno.latitud);
            const longitud = Number(terreno.longitud);

            if (!Number.isFinite(latitud) ||
                !Number.isFinite(longitud)) {
                return;
            }

            const clase =
                clasePorNivel(terreno.nivelAlerta);

            const icono = L.divIcon({
                className: "smart-marker-wrapper",
                html: `
                    <span class="smart-marker ${clase}">
                        <i class="fa-solid fa-seedling"></i>
                    </span>`,
                iconSize: [38, 45],
                iconAnchor: [19, 42],
                popupAnchor: [0, -38]
            });

            const marcador = L.marker(
                [latitud, longitud],
                { icon: icono });

            marcador.bindPopup(
                construirPopup(terreno));

            contexto.marcadores.addLayer(marcador);
            limites.push([latitud, longitud]);
        });

        if (limites.length === 1) {
            contexto.mapa.setView(limites[0], 15);
        } else if (limites.length > 1) {
            contexto.mapa.fitBounds(limites, {
                padding: [40, 40],
                maxZoom: 14
            });
        } else {
            contexto.mapa.setView(
                [12.8654, -85.2072],
                7);
        }

        setTimeout(
            () => contexto.mapa.invalidateSize(),
            150);
    }

    function construirPopup(terreno) {
        const alertas = Array.isArray(terreno.alertas)
            ? terreno.alertas
            : [];

        const listaAlertas = alertas.length === 0
            ? `<span class="smart-popup-ok">
                   <i class="fa-solid fa-circle-check"></i>
                   Sin alertas agrícolas
               </span>`
            : `<ul>${alertas
                .map(item => `<li>${escapar(item)}</li>`)
                .join("")}</ul>`;

        const fecha = terreno.fechaUltimoAnalisis
            ? new Date(terreno.fechaUltimoAnalisis)
                .toLocaleDateString("es-NI")
            : "Sin análisis";

        return `
            <div class="smart-map-popup">
                <div class="smart-popup-header">
                    <div>
                        <strong>${escapar(terreno.codigo)}</strong>
                        <span>${escapar(terreno.estado)}</span>
                    </div>
                </div>

                <p>
                    <strong>Propietario:</strong>
                    ${escapar(terreno.productor)}
                </p>
                <p>
                    <strong>Ubicación:</strong>
                    ${escapar(terreno.municipio)},
                    ${escapar(terreno.departamento)}
                </p>
                <p>
                    <strong>Extensión:</strong>
                    ${numero(terreno.extensionManzanas)} Mz
                </p>
                <p>
                    <strong>Producción:</strong>
                    ${numero(terreno.produccionQuintalesOro)} QQ oro
                </p>
                <p>
                    <strong>Último análisis:</strong>
                    ${escapar(fecha)}
                </p>

                <div class="smart-popup-values">
                    <span>
                        <b>pH</b>
                        ${valor(terreno.ultimoPh)}
                    </span>
                    <span>
                        <b>Materia orgánica</b>
                        ${valor(terreno.materiaOrganica, "%")}
                    </span>
                    <span>
                        <b>Acidez</b>
                        ${valor(terreno.acidezTotal)}
                    </span>
                </div>

                <div class="smart-popup-alerts">
                    ${listaAlertas}
                </div>

                <a href="${escapar(terreno.googleMapsUrl)}"
                   target="_blank"
                   rel="noopener noreferrer"
                   class="device-google-maps-button">
                    <i class="fa-solid fa-route"></i>
                    Cómo llegar con Google Maps
                </a>
            </div>`;
    }

    async function obtenerMunicipios(rutaRelativa) {
        try {
            const respuesta = await fetch(rutaRelativa, {
                credentials: "same-origin"
            });

            if (!respuesta.ok) return [];
            return await respuesta.json();
        } catch {
            return [];
        }
    }

    function clasePorNivel(nivel) {
        switch (String(nivel || "").toUpperCase()) {
            case "CRITICA":
                return "critical";
            case "ATENCION":
                return "attention";
            case "NORMAL":
                return "normal";
            default:
                return "without-analysis";
        }
    }

    function valor(dato, sufijo = "") {
        if (dato === null ||
            dato === undefined ||
            Number.isNaN(Number(dato))) {
            return "—";
        }

        return `${Number(dato).toFixed(2)}${sufijo}`;
    }

    function numero(dato) {
        if (dato === null ||
            dato === undefined ||
            Number.isNaN(Number(dato))) {
            return "0.00";
        }

        return Number(dato).toFixed(2);
    }

    function escapar(valor) {
        return String(valor ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function destruir(elementId) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        contexto.mapa.remove();
        mapas.delete(elementId);
    }

    return {
        inicializar,
        mostrar,
        destruir,
        obtenerMunicipios
    };
})();
