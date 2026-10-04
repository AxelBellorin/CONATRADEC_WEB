window.conatradecTerrenoEditor =
    window.conatradecTerrenoEditor || (() => {
        const instancias = new Map();
        const METROS_POR_MANZANA = 7042.25;

        function propiedad(objeto, camel, pascal) {
            if (!objeto) return null;

            return Object.prototype.hasOwnProperty.call(
                objeto,
                camel)
                ? objeto[camel]
                : objeto[pascal];
        }

        function numero(valor, respaldo = 0) {
            const resultado = Number(valor);
            return Number.isFinite(resultado)
                ? resultado
                : respaldo;
        }

        function leerVertices(datos) {
            const valores =
                propiedad(
                    datos,
                    "vertices",
                    "Vertices");

            if (!Array.isArray(valores))
                return [];

            return valores
                .map(item => [
                    numero(
                        propiedad(
                            item,
                            "latitud",
                            "Latitud")),
                    numero(
                        propiedad(
                            item,
                            "longitud",
                            "Longitud"))
                ])
                .filter(item =>
                    Number.isFinite(item[0]) &&
                    Number.isFinite(item[1]));
        }

        function puntoDentroPoligono(punto, vertices) {
            if (!punto || vertices.length < 3)
                return true;

            const y = punto[0];
            const x = punto[1];
            let dentro = false;

            for (let i = 0, j = vertices.length - 1;
                 i < vertices.length;
                 j = i++) {
                const yi = vertices[i][0];
                const xi = vertices[i][1];
                const yj = vertices[j][0];
                const xj = vertices[j][1];

                const intersecta =
                    ((yi > y) !== (yj > y)) &&
                    (x <
                        (xj - xi) *
                        (y - yi) /
                        ((yj - yi) || Number.EPSILON) +
                        xi);

                if (intersecta)
                    dentro = !dentro;
            }

            return dentro;
        }

        function calcularArea(vertices) {
            if (vertices.length < 3) {
                return {
                    areaMetrosCuadrados: 0,
                    areaHectareas: 0,
                    areaManzanas: 0
                };
            }

            const puntos =
                vertices.map(item =>
                    L.latLng(
                        item[0],
                        item[1]));

            const metros =
                L.GeometryUtil &&
                typeof L.GeometryUtil.geodesicArea === "function"
                    ? L.GeometryUtil.geodesicArea(puntos)
                    : 0;

            return {
                areaMetrosCuadrados:
                    Math.round(metros * 100) / 100,
                areaHectareas:
                    Math.round(metros / 10000 * 10000) / 10000,
                areaManzanas:
                    Math.round(
                        metros /
                        METROS_POR_MANZANA *
                        10000) /
                    10000
            };
        }

        function obtenerVertices(instancia) {
            if (!instancia?.poligono)
                return [];

            const puntos =
                instancia.poligono
                    .getLatLngs()?.[0] || [];

            return puntos.map(item => [
                Number(item.lat.toFixed(8)),
                Number(item.lng.toFixed(8))
            ]);
        }

        function reemplazarPoligono(instancia, vertices) {
            if (instancia.poligono) {
                instancia.grupoEditable.removeLayer(
                    instancia.poligono);

                instancia.poligono = null;
            }

            if (!Array.isArray(vertices) ||
                vertices.length < 3) {
                return;
            }

            instancia.poligono =
                L.polygon(
                    vertices,
                    {
                        color: "#3B655B",
                        weight: 3,
                        opacity: 0.96,
                        fillColor: "#3B655B",
                        fillOpacity: 0.19,
                        smoothFactor: 0.2
                    });

            instancia.grupoEditable.addLayer(
                instancia.poligono);
        }

        async function notificar(instancia) {
            if (!instancia?.dotNetRef)
                return;

            const punto =
                instancia.marcador.getLatLng();

            const vertices =
                obtenerVertices(instancia);

            const areas =
                calcularArea(vertices);

            await instancia.dotNetRef.invokeMethodAsync(
                "GeometriaTerrenoActualizada",
                {
                    latitud:
                        Number(punto.lat.toFixed(8)),
                    longitud:
                        Number(punto.lng.toFixed(8)),
                    vertices:
                        vertices.map(item => ({
                            latitud: item[0],
                            longitud: item[1]
                        })),
                    areaMetrosCuadrados:
                        areas.areaMetrosCuadrados,
                    areaHectareas:
                        areas.areaHectareas,
                    areaManzanas:
                        areas.areaManzanas,
                    puntoDentroPoligono:
                        puntoDentroPoligono(
                            [punto.lat, punto.lng],
                            vertices)
                });
        }

        function inicializar(elementId, dotNetRef, datos) {
            destruir(elementId);

            const elemento =
                document.getElementById(elementId);

            if (!elemento ||
                !window.L) {
                return false;
            }

            const latitud =
                numero(
                    propiedad(
                        datos,
                        "latitud",
                        "Latitud"),
                    12.8654);

            const longitud =
                numero(
                    propiedad(
                        datos,
                        "longitud",
                        "Longitud"),
                    -85.2072);

            const mapa =
                L.map(
                    elemento,
                    {
                        center: [latitud, longitud],
                        zoom: 15,
                        maxZoom: 20
                    });

            const calles =
                L.tileLayer(
                    "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
                    {
                        maxZoom: 20,
                        attribution:
                            "&copy; OpenStreetMap contributors"
                    })
                    .addTo(mapa);

            const satelite =
                L.tileLayer(
                    "https://server.arcgisonline.com/ArcGIS/rest/services/" +
                    "World_Imagery/MapServer/tile/{z}/{y}/{x}",
                    {
                        maxZoom: 20,
                        attribution: "Tiles &copy; Esri"
                    });

            L.control.layers(
                {
                    "Calles": calles,
                    "Satélite": satelite
                },
                null,
                {
                    position: "topright"
                })
                .addTo(mapa);

            const grupoEditable =
                new L.FeatureGroup()
                    .addTo(mapa);

            const marcador =
                L.marker(
                    [latitud, longitud],
                    {
                        draggable: true,
                        title: "Punto principal del terreno"
                    })
                    .addTo(mapa);

            marcador.bindTooltip(
                "Punto principal",
                {
                    direction: "top"
                });

            const instancia = {
                elementId,
                mapa,
                dotNetRef,
                marcador,
                grupoEditable,
                poligono: null,
                seleccionarPunto: false,
                opcionesDibujo: {
                    allowIntersection: false,
                    showArea: true,
                    shapeOptions: {
                        color: "#3B655B",
                        fillColor: "#3B655B",
                        fillOpacity: 0.19
                    }
                }
            };

            instancias.set(elementId, instancia);

            reemplazarPoligono(
                instancia,
                leerVertices(datos));

            if (typeof L.Control.Draw === "function") {
                const control =
                    new L.Control.Draw({
                        position: "topleft",
                        draw: {
                            polygon:
                                instancia.opcionesDibujo,
                            polyline: false,
                            rectangle: false,
                            circle: false,
                            circlemarker: false,
                            marker: false
                        },
                        edit: {
                            featureGroup: grupoEditable,
                            remove: false
                        }
                    });

                mapa.addControl(control);

                mapa.on(
                    L.Draw.Event.CREATED,
                    async evento => {
                        const nuevos =
                            evento.layer
                                .getLatLngs()[0]
                                .map(item => [
                                    item.lat,
                                    item.lng
                                ]);

                        reemplazarPoligono(
                            instancia,
                            nuevos);

                        await notificar(instancia);
                    });

                mapa.on(
                    L.Draw.Event.EDITED,
                    async () =>
                        await notificar(instancia));
            }

            marcador.on(
                "dragend",
                async () =>
                    await notificar(instancia));

            mapa.on(
                "click",
                async evento => {
                    if (!instancia.seleccionarPunto)
                        return;

                    instancia.seleccionarPunto = false;
                    marcador.setLatLng(evento.latlng);
                    elemento.classList.remove("selecting-point");

                    await notificar(instancia);
                });

            const verticesIniciales =
                obtenerVertices(instancia);

            if (verticesIniciales.length >= 3) {
                const bounds =
                    L.latLngBounds(verticesIniciales);

                bounds.extend(
                    marcador.getLatLng());

                mapa.fitBounds(
                    bounds,
                    {
                        padding: [35, 35],
                        maxZoom: 18
                    });
            }

            setTimeout(
                () => mapa.invalidateSize(),
                120);

            notificar(instancia);
            return true;
        }

        function dibujarPoligono(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia ||
                typeof L.Draw?.Polygon !== "function") {
                return;
            }

            if (instancia.poligono) {
                reemplazarPoligono(
                    instancia,
                    []);
            }

            const herramienta =
                new L.Draw.Polygon(
                    instancia.mapa,
                    instancia.opcionesDibujo);

            herramienta.enable();
        }

        function editarPoligono(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia?.poligono ||
                !instancia.poligono.editing) {
                return;
            }

            instancia.poligono.editing.enable();

            instancia.poligono.once(
                "edit",
                async () => {
                    await notificar(instancia);
                });
        }

        function seleccionarPunto(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            instancia.seleccionarPunto = true;

            document
                .getElementById(elementId)
                ?.classList.add("selecting-point");
        }

        function establecerPunto(
            elementId,
            latitud,
            longitud,
            centrar = true) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            const punto =
                L.latLng(
                    numero(latitud),
                    numero(longitud));

            instancia.marcador.setLatLng(punto);

            if (centrar) {
                instancia.mapa.setView(
                    punto,
                    Math.max(
                        instancia.mapa.getZoom(),
                        16));
            }

            notificar(instancia);
        }

        function eliminarPoligono(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            reemplazarPoligono(instancia, []);
            notificar(instancia);
        }

        function centrarPunto(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            instancia.mapa.setView(
                instancia.marcador.getLatLng(),
                17);
        }

        function centrarPoligono(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia?.poligono)
                return;

            instancia.mapa.fitBounds(
                instancia.poligono.getBounds(),
                {
                    padding: [35, 35],
                    maxZoom: 18
                });
        }

        function destruir(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            try {
                instancia.mapa.remove();
            } catch {
            }

            instancias.delete(elementId);
        }

        return {
            inicializar,
            dibujarPoligono,
            editarPoligono,
            seleccionarPunto,
            establecerPunto,
            eliminarPoligono,
            centrarPunto,
            centrarPoligono,
            destruir
        };
    })();
