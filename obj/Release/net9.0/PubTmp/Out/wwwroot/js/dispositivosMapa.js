window.conatradecDispositivosMapa = (() => {
    const mapas = new Map();
    const cacheUbicaciones = new Map();
    let departamentosPromise = null;

    const departamentosUrl =
        "https://gis.unicef.org/server/rest/services/" +
        "Departamentos_Nicaragua_MIL1/MapServer/5/query" +
        "?where=1%3D1&outFields=ADM1_ES&returnGeometry=true" +
        "&outSR=4326&f=geojson";

    function inicializar(elementId) {
        destruir(elementId);

        const elemento = document.getElementById(elementId);
        if (!elemento || typeof L === "undefined") {
            return false;
        }

        const mapa = L.map(elementId, {
            center: [12.8654, -85.2072],
            zoom: 7,
            minZoom: 5,
            zoomControl: true
        });

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(mapa);

        const marcadores = typeof L.markerClusterGroup === "function"
            ? L.markerClusterGroup({
                showCoverageOnHover: false,
                maxClusterRadius: 52
            })
            : L.layerGroup();

        mapa.addLayer(marcadores);

        const contexto = {
            mapa,
            marcadores,
            departamentos: null,
            dispositivos: []
        };

        mapas.set(elementId, contexto);
        cargarDepartamentos(contexto);

        setTimeout(() => mapa.invalidateSize(), 150);
        return true;
    }

    async function cargarDepartamentos(contexto) {
        try {
            const geojson = await obtenerDepartamentos();

            if (!geojson) {
                return;
            }

            contexto.departamentos = L.geoJSON(geojson, {
                style: {
                    color: "#3B655B",
                    weight: 1.4,
                    opacity: 0.8,
                    fillColor: "#3B655B",
                    fillOpacity: 0.045
                },
                onEachFeature: (feature, layer) => {
                    const nombre =
                        feature?.properties?.ADM1_ES ||
                        feature?.properties?.adm1_es ||
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
                "No fue posible cargar los departamentos de Nicaragua.",
                error);
        }
    }

    function obtenerDepartamentos() {
        if (departamentosPromise) {
            return departamentosPromise;
        }

        departamentosPromise = fetch(departamentosUrl)
            .then(respuesta => {
                if (!respuesta.ok) {
                    throw new Error("No fue posible descargar los límites.");
                }

                return respuesta.json();
            })
            .catch(error => {
                console.warn(error);
                return null;
            });

        return departamentosPromise;
    }

    async function mostrar(elementId, dispositivos) {
        let contexto = mapas.get(elementId);

        if (!contexto) {
            inicializar(elementId);
            contexto = mapas.get(elementId);
        }

        if (!contexto) {
            return;
        }

        contexto.dispositivos = dispositivos || [];
        contexto.marcadores.clearLayers();

        const limites = [];

        for (const dispositivo of contexto.dispositivos) {
            const latitud = Number(dispositivo.latitud);
            const longitud = Number(dispositivo.longitud);

            if (!Number.isFinite(latitud) || !Number.isFinite(longitud)) {
                continue;
            }

            const conectado = dispositivo.conectado === true;
            const simulada = dispositivo.ubicacionSimulada === true;
            const claseEstado = conectado
                ? "device-map-marker-online"
                : "device-map-marker-offline";

            const iconoPlataforma =
                iconoParaPlataforma(dispositivo.plataforma);

            const icono = L.divIcon({
                className: "device-map-marker-wrapper",
                html: `
                    <span class="device-map-marker ${claseEstado}">
                        <i class="${iconoPlataforma}"></i>
                        ${simulada
                            ? `<small class="device-map-warning">
                                   <i class="fa-solid fa-triangle-exclamation"></i>
                               </small>`
                            : ""}
                    </span>`,
                iconSize: [42, 48],
                iconAnchor: [21, 45],
                popupAnchor: [0, -40]
            });

            const marcador = L.marker(
                [latitud, longitud],
                { icon: icono });

            marcador.bindPopup(
                construirPopup(
                    dispositivo,
                    {
                        departamento: "Identificando...",
                        municipio: "Identificando..."
                    }));

            contexto.marcadores.addLayer(marcador);
            limites.push([latitud, longitud]);

            resolverUbicacion(latitud, longitud)
                .then(ubicacion => {
                    marcador.setPopupContent(
                        construirPopup(dispositivo, ubicacion));
                })
                .catch(() => {
                    marcador.setPopupContent(
                        construirPopup(
                            dispositivo,
                            {
                                departamento: "No identificado",
                                municipio: "No identificado"
                            }));
                });
        }

        if (limites.length === 1) {
            contexto.mapa.setView(limites[0], 15);
        } else if (limites.length > 1) {
            contexto.mapa.fitBounds(limites, {
                padding: [45, 45],
                maxZoom: 14
            });
        } else {
            contexto.mapa.setView([12.8654, -85.2072], 7);
        }

        setTimeout(() => contexto.mapa.invalidateSize(), 180);
    }

    async function resolverUbicacion(latitud, longitud) {
        const clave = `${latitud.toFixed(5)},${longitud.toFixed(5)}`;

        if (cacheUbicaciones.has(clave)) {
            return cacheUbicaciones.get(clave);
        }

        const url =
            "https://nominatim.openstreetmap.org/reverse" +
            `?format=jsonv2&lat=${encodeURIComponent(latitud)}` +
            `&lon=${encodeURIComponent(longitud)}` +
            "&zoom=14&addressdetails=1&accept-language=es";

        const promesa = fetch(url, {
            headers: {
                "Accept": "application/json"
            }
        })
            .then(respuesta => {
                if (!respuesta.ok) {
                    throw new Error("No fue posible identificar la ubicación.");
                }

                return respuesta.json();
            })
            .then(datos => {
                const direccion = datos?.address || {};

                return {
                    departamento:
                        direccion.state ||
                        direccion.region ||
                        direccion.state_district ||
                        "No identificado",
                    municipio:
                        direccion.city ||
                        direccion.town ||
                        direccion.municipality ||
                        direccion.county ||
                        direccion.village ||
                        direccion.hamlet ||
                        "No identificado"
                };
            })
            .catch(() => ({
                departamento: "No identificado",
                municipio: "No identificado"
            }));

        cacheUbicaciones.set(clave, promesa);
        return promesa;
    }

    function construirPopup(dispositivo, ubicacion) {
        const conectado = dispositivo.conectado === true;
        const simulada = dispositivo.ubicacionSimulada === true;

        const claseEstado = conectado
            ? "device-map-marker-online"
            : "device-map-marker-offline";

        const iconoPlataforma =
            iconoParaPlataforma(dispositivo.plataforma);

        const nombreDispositivo =
            dispositivo.nombreDispositivo ||
            dispositivo.modelo ||
            "Dispositivo sin nombre";

        const precision = dispositivo.precisionMetros == null
            ? "No disponible"
            : `${Number(dispositivo.precisionMetros).toFixed(0)} m`;

        const fechaUbicacion =
            formatearFecha(dispositivo.fechaUbicacionUtc);

        const ultimoLatido =
            formatearFecha(dispositivo.ultimoLatidoUtc);

        const googleMapsUrl =
            "https://www.google.com/maps/dir/?api=1" +
            `&destination=${encodeURIComponent(dispositivo.latitud)}` +
            `%2C${encodeURIComponent(dispositivo.longitud)}` +
            "&travelmode=driving";

        return `
            <div class="device-map-popup">
                <div class="device-map-popup-header">
                    <span class="device-map-popup-icon ${claseEstado}">
                        <i class="${iconoPlataforma}"></i>
                    </span>
                    <div>
                        <h3>${escapar(nombreDispositivo)}</h3>
                        <span>
                            ${escapar(
                                dispositivo.plataforma ||
                                "Plataforma no informada")}
                        </span>
                    </div>
                </div>

                <div class="device-map-location">
                    <p>
                        <i class="fa-solid fa-map"></i>
                        <span>
                            <strong>Departamento</strong>
                            ${escapar(ubicacion.departamento)}
                        </span>
                    </p>
                    <p>
                        <i class="fa-solid fa-location-dot"></i>
                        <span>
                            <strong>Municipio</strong>
                            ${escapar(ubicacion.municipio)}
                        </span>
                    </p>
                </div>

                <p>
                    <strong>Usuario:</strong>
                    ${escapar(
                        dispositivo.usuarioNombre ||
                        "No disponible")}
                </p>
                <p>
                    <strong>Modelo:</strong>
                    ${escapar(
                        dispositivo.modelo ||
                        "No disponible")}
                </p>
                <p>
                    <strong>Estado:</strong>
                    ${conectado ? "Conectado" : "Desconectado"}
                </p>
                <p>
                    <strong>Precisión:</strong>
                    ${escapar(precision)}
                </p>
                <p>
                    <strong>Última ubicación:</strong>
                    ${escapar(fechaUbicacion)}
                </p>
                <p>
                    <strong>Último latido:</strong>
                    ${escapar(ultimoLatido)}
                </p>
                <p>
                    <strong>Permiso:</strong>
                    ${escapar(
                        dispositivo.estadoPermisoUbicacion ||
                        "No informado")}
                </p>

                ${simulada
                    ? `<div class="device-map-simulated">
                           <i class="fa-solid fa-triangle-exclamation"></i>
                           Ubicación reportada como simulada
                       </div>`
                    : ""}

                <a class="device-google-maps-button"
                   href="${googleMapsUrl}"
                   target="_blank"
                   rel="noopener noreferrer">
                    <i class="fa-solid fa-route"></i>
                    Cómo llegar con Google Maps
                </a>
            </div>
        `;
    }

    function invalidar(elementId) {
        const contexto = mapas.get(elementId);

        if (!contexto) {
            return;
        }

        setTimeout(() => contexto.mapa.invalidateSize(), 100);
    }

    function destruir(elementId) {
        const contexto = mapas.get(elementId);

        if (!contexto) {
            return;
        }

        contexto.mapa.remove();
        mapas.delete(elementId);
    }

    function iconoParaPlataforma(plataforma) {
        const valor = String(plataforma || "").toLowerCase();

        if (valor.includes("android")) {
            return "fa-brands fa-android";
        }

        if (valor.includes("windows") || valor.includes("winui")) {
            return "fa-brands fa-windows";
        }

        return "fa-solid fa-mobile-screen-button";
    }

    function formatearFecha(valor) {
        if (!valor) {
            return "No disponible";
        }

        const fecha = new Date(valor);

        if (Number.isNaN(fecha.getTime())) {
            return String(valor);
        }

        return fecha.toLocaleString("es-NI", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
    }

    function escapar(valor) {
        return String(valor ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    return {
        inicializar,
        mostrar,
        invalidar,
        destruir
    };
})();
