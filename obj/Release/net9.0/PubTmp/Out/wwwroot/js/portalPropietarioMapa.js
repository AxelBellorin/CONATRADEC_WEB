window.conatradecPortalPropietarioMapa =
    window.conatradecPortalPropietarioMapa || (() => {
        const mapas = new Map();

        function escapar(valor) {
            return String(valor ?? "")
                .replaceAll("&", "&amp;")
                .replaceAll("<", "&lt;")
                .replaceAll(">", "&gt;")
                .replaceAll('"', "&quot;")
                .replaceAll("'", "&#039;");
        }

        function numero(valor) {
            const resultado = Number(valor);
            return Number.isFinite(resultado)
                ? resultado
                : 0;
        }

        function obtenerPropiedad(objeto, camel, pascal) {
            if (!objeto) {
                return null;
            }

            if (Object.prototype.hasOwnProperty.call(
                objeto,
                camel)) {
                return objeto[camel];
            }

            return objeto[pascal];
        }

        function destruir(elementId) {
            const existente = mapas.get(elementId);

            if (existente) {
                existente.remove();
                mapas.delete(elementId);
            }
        }

        function render(elementId, terrenos) {
            destruir(elementId);

            const elemento =
                document.getElementById(elementId);

            if (!elemento ||
                typeof window.L === "undefined") {
                return;
            }

            const mapa = window.L.map(
                elemento,
                {
                    zoomControl: true,
                    attributionControl: true
                });

            window.L.tileLayer(
                "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
                {
                    maxZoom: 19,
                    attribution:
                        "&copy; OpenStreetMap contributors"
                })
                .addTo(mapa);

            const limites = [];

            for (const terreno of terrenos ?? []) {
                const latitud = numero(
                    obtenerPropiedad(
                        terreno,
                        "latitud",
                        "Latitud"));

                const longitud = numero(
                    obtenerPropiedad(
                        terreno,
                        "longitud",
                        "Longitud"));

                if (latitud === 0 ||
                    longitud === 0) {
                    continue;
                }

                const codigo = escapar(
                    obtenerPropiedad(
                        terreno,
                        "codigoTerreno",
                        "CodigoTerreno"));

                const direccion = escapar(
                    obtenerPropiedad(
                        terreno,
                        "direccion",
                        "Direccion"));

                const municipio = escapar(
                    obtenerPropiedad(
                        terreno,
                        "municipio",
                        "Municipio"));

                const departamento = escapar(
                    obtenerPropiedad(
                        terreno,
                        "departamento",
                        "Departamento"));

                const extension = numero(
                    obtenerPropiedad(
                        terreno,
                        "extensionManzanas",
                        "ExtensionManzanas"));

                const plantas = numero(
                    obtenerPropiedad(
                        terreno,
                        "cantidadPlantas",
                        "CantidadPlantas"));

                const analisis = numero(
                    obtenerPropiedad(
                        terreno,
                        "totalAnalisis",
                        "TotalAnalisis"));

                const marcador =
                    window.L.marker(
                        [latitud, longitud])
                        .addTo(mapa);

                marcador.bindPopup(
                    `<div class="owner-map-popup">
                        <strong>${codigo}</strong>
                        <span>${direccion}</span>
                        <span>${municipio}${municipio && departamento ? ", " : ""}${departamento}</span>
                        <span>${extension.toLocaleString("es-NI", {
                            minimumFractionDigits: 2,
                            maximumFractionDigits: 2
                        })} Mz · ${plantas.toLocaleString("es-NI")} plantas</span>
                        <span>${analisis.toLocaleString("es-NI")} análisis</span>
                    </div>`);

                limites.push([latitud, longitud]);
            }

            if (limites.length === 1) {
                mapa.setView(limites[0], 15);
            } else if (limites.length > 1) {
                mapa.fitBounds(
                    window.L.latLngBounds(limites),
                    {
                        padding: [35, 35],
                        maxZoom: 16
                    });
            } else {
                mapa.setView(
                    [12.8654, -85.2072],
                    7);
            }

            mapas.set(elementId, mapa);

            window.setTimeout(
                () => mapa.invalidateSize(),
                120);
        }

        return {
            render,
            dispose: destruir
        };
    })();
