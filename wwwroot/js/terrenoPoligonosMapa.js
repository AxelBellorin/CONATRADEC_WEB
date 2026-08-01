/*
 * Superpone la delimitación opcional sobre los mapas existentes.
 * Los marcadores de punto continúan siendo creados por mapaInteligente.js.
 */
window.conatradecTerrenoPoligonosMapa =
    window.conatradecTerrenoPoligonosMapa || (() => {
        const L = window.L;
        const mapas = new Map();
        const terrenosPorMapa = new Map();
        const gruposPorMapa = new Map();
        const visibilidadPorMapa = new Map();
        const poligonos = new Map();

        if (!L) {
            return {
                establecerPoligonos: () => {},
                redibujar: () => {},
                limpiar: () => {}
            };
        }

        function propiedad(objeto, camel, pascal) {
            if (!objeto) return null;

            return Object.prototype.hasOwnProperty.call(
                objeto,
                camel)
                ? objeto[camel]
                : objeto[pascal];
        }

        function numero(valor) {
            const resultado = Number(valor);
            return Number.isFinite(resultado)
                ? resultado
                : null;
        }

        function escapar(valor) {
            return String(valor ?? "")
                .replaceAll("&", "&amp;")
                .replaceAll("<", "&lt;")
                .replaceAll(">", "&gt;")
                .replaceAll('"', "&quot;")
                .replaceAll("'", "&#039;");
        }

        function idTerreno(item) {
            return numero(
                propiedad(
                    item,
                    "terrenoId",
                    "TerrenoId"));
        }

        function vertices(item) {
            const valores =
                propiedad(
                    item,
                    "vertices",
                    "Vertices");

            if (!Array.isArray(valores))
                return [];

            return valores
                .map(vertice => {
                    const latitud =
                        numero(
                            propiedad(
                                vertice,
                                "latitud",
                                "Latitud"));

                    const longitud =
                        numero(
                            propiedad(
                                vertice,
                                "longitud",
                                "Longitud"));

                    return latitud === null ||
                           longitud === null
                        ? null
                        : [latitud, longitud];
                })
                .filter(Boolean);
        }

        function registrarMapasLeaflet() {
            if (L.__conatradecRegistroMapasPoligono)
                return;

            const crearMapa = L.map;

            L.map = function (elemento, opciones) {
                const mapa =
                    crearMapa.call(
                        this,
                        elemento,
                        opciones);

                const id =
                    typeof elemento === "string"
                        ? elemento
                        : elemento?.id;

                if (id) {
                    mapas.set(id, mapa);
                    visibilidadPorMapa.set(id, true);

                    mapa.on("unload", () => {
                        mapas.delete(id);
                        terrenosPorMapa.delete(id);
                        gruposPorMapa.delete(id);
                        visibilidadPorMapa.delete(id);
                    });
                }

                return mapa;
            };

            L.__conatradecRegistroMapasPoligono = true;
        }

        function asegurarPane(mapa) {
            if (mapa.getPane("geoTerrainPolygonPane"))
                return;

            const pane =
                mapa.createPane(
                    "geoTerrainPolygonPane");

            /*
             * Debajo de alertas y marcadores, pero encima de las capas
             * territoriales y de calor.
             */
            pane.style.zIndex = "405";
            pane.style.pointerEvents = "auto";
        }

        function colorTerreno(terreno) {
            const nivel =
                String(
                    propiedad(
                        terreno,
                        "nivelAlerta",
                        "NivelAlerta") ?? "")
                    .toUpperCase();

            if (nivel === "CRITICA" ||
                nivel === "CRITICO")
                return "#EF4444";

            if (nivel === "ATENCION")
                return "#F59E0B";

            if (nivel === "NORMAL" ||
                nivel === "ESTABLE")
                return "#3B655B";

            return "#64748B";
        }

        function limpiar(elementId) {
            const mapa = mapas.get(elementId);
            const grupo = gruposPorMapa.get(elementId);

            if (mapa && grupo) {
                try {
                    mapa.removeLayer(grupo);
                } catch {
                }
            }

            gruposPorMapa.delete(elementId);
        }

        function dibujar(elementId, ajustarVista = false) {
            const mapa = mapas.get(elementId);
            const terrenos = terrenosPorMapa.get(elementId) || [];
            const visible = visibilidadPorMapa.get(elementId) !== false;

            limpiar(elementId);

            if (!mapa || !visible)
                return;

            asegurarPane(mapa);

            const grupo = L.featureGroup();
            const bounds = L.latLngBounds([]);
            let total = 0;

            for (const terreno of terrenos) {
                const id = idTerreno(terreno);
                const poligono = poligonos.get(id);

                if (!poligono)
                    continue;

                const puntos = vertices(poligono);

                if (puntos.length < 3)
                    continue;

                const color = colorTerreno(terreno);
                const capa = L.polygon(
                    puntos,
                    {
                        pane: "geoTerrainPolygonPane",
                        color,
                        weight: 2.5,
                        opacity: 0.96,
                        fillColor: color,
                        fillOpacity: 0.17,
                        interactive: true,
                        smoothFactor: 0.4
                    });

                const codigo = escapar(
                    propiedad(
                        poligono,
                        "codigoTerreno",
                        "CodigoTerreno") ??
                    propiedad(
                        terreno,
                        "codigo",
                        "Codigo"));

                const area =
                    numero(
                        propiedad(
                            poligono,
                            "areaManzanasCalculada",
                            "AreaManzanasCalculada")) ?? 0;

                const puntoDentro =
                    propiedad(
                        poligono,
                        "puntoDentroPoligono",
                        "PuntoDentroPoligono") !== false;

                capa.bindTooltip(
                    `<div class="terrain-polygon-tooltip">
                        <strong>${codigo}</strong>
                        <span>${area.toLocaleString(
                            "es-NI",
                            {
                                minimumFractionDigits: 2,
                                maximumFractionDigits: 2
                            })} Mz delimitadas</span>
                        ${puntoDentro
                            ? ""
                            : "<small>El punto principal está fuera del polígono.</small>"}
                    </div>`,
                    {
                        sticky: true,
                        direction: "top"
                    });

                capa.addTo(grupo);
                capa.getBounds().getNorthEast() &&
                    bounds.extend(capa.getBounds());

                total++;
            }

            if (total === 0)
                return;

            grupo.addTo(mapa);
            gruposPorMapa.set(elementId, grupo);

            if (ajustarVista && bounds.isValid()) {
                for (const terreno of terrenos) {
                    const latitud =
                        numero(
                            propiedad(
                                terreno,
                                "latitud",
                                "Latitud"));

                    const longitud =
                        numero(
                            propiedad(
                                terreno,
                                "longitud",
                                "Longitud"));

                    if (latitud !== null &&
                        longitud !== null) {
                        bounds.extend([latitud, longitud]);
                    }
                }

                mapa.fitBounds(
                    bounds,
                    {
                        padding: [35, 35],
                        maxZoom: 17
                    });
            }
        }

        function establecerPoligonos(items) {
            poligonos.clear();

            for (const item of Array.isArray(items) ? items : []) {
                const id = idTerreno(item);

                if (id)
                    poligonos.set(id, item);
            }

            for (const elementId of mapas.keys())
                dibujar(elementId, false);
        }

        function envolverModuloMapa() {
            const modulo =
                window.conatradecMapaInteligente;

            if (!modulo ||
                modulo.__poligonosTerrenoIntegrados)
                return;

            if (typeof modulo.mostrarTerrenos === "function") {
                const mostrarOriginal =
                    modulo.mostrarTerrenos.bind(modulo);

                modulo.mostrarTerrenos =
                    function (
                        elementId,
                        terrenos,
                        ajustarVista) {
                        terrenosPorMapa.set(
                            elementId,
                            Array.isArray(terrenos)
                                ? terrenos
                                : []);

                        const resultado =
                            mostrarOriginal(
                                elementId,
                                terrenos,
                                ajustarVista);

                        setTimeout(
                            () => dibujar(
                                elementId,
                                Boolean(ajustarVista)),
                            0);

                        return resultado;
                    };
            }

            if (typeof modulo.aplicarCapas === "function") {
                const aplicarOriginal =
                    modulo.aplicarCapas.bind(modulo);

                modulo.aplicarCapas =
                    function (
                        elementId,
                        capas) {
                        const terrenosVisibles =
                            propiedad(
                                capas,
                                "terrenos",
                                "Terrenos") !== false;

                        visibilidadPorMapa.set(
                            elementId,
                            terrenosVisibles);

                        const resultado =
                            aplicarOriginal(
                                elementId,
                                capas);

                        setTimeout(
                            () => dibujar(
                                elementId,
                                false),
                            0);

                        return resultado;
                    };
            }

            modulo.__poligonosTerrenoIntegrados = true;
        }

        registrarMapasLeaflet();
        envolverModuloMapa();

        return {
            establecerPoligonos,
            redibujar:
                (elementId, ajustarVista) =>
                    dibujar(
                        elementId,
                        Boolean(ajustarVista)),
            limpiar
        };
    })();
