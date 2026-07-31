window.conatradecClimaTimeline = (() => {
    const contextos = new Map();

    const variables = {
        temperatura: {
            propiedad: "temperatura",
            unidad: "°C",
            titulo: "Temperatura",
            min: 14,
            max: 36,
            gradient: {
                0.0: "#2563eb",
                0.35: "#22c55e",
                0.68: "#facc15",
                1.0: "#ef4444"
            }
        },
        lluvia: {
            propiedad: "probabilidadLluvia",
            unidad: "%",
            titulo: "Probabilidad de lluvia",
            min: 0,
            max: 100,
            gradient: {
                0.0: "#dbeafe",
                0.45: "#38bdf8",
                0.72: "#2563eb",
                1.0: "#312e81"
            }
        },
        precipitacion: {
            propiedad: "precipitacion",
            unidad: "mm",
            titulo: "Precipitación",
            min: 0,
            max: 20,
            gradient: {
                0.0: "#e0f2fe",
                0.4: "#38bdf8",
                0.75: "#0284c7",
                1.0: "#0c4a6e"
            }
        },
        viento: {
            propiedad: "viento",
            unidad: "km/h",
            titulo: "Viento",
            min: 0,
            max: 50,
            gradient: {
                0.0: "#dcfce7",
                0.45: "#4ade80",
                0.75: "#facc15",
                1.0: "#f97316"
            }
        },
        humedad: {
            propiedad: "humedad",
            unidad: "%",
            titulo: "Humedad",
            min: 30,
            max: 100,
            gradient: {
                0.0: "#fde68a",
                0.42: "#6ee7b7",
                0.72: "#38bdf8",
                1.0: "#1d4ed8"
            }
        }
    };

    function inicializar(
        elementId,
        pasos,
        variable,
        dotNetReference,
        indiceInicial = 0) {
        destruir(elementId);

        const elemento = document.getElementById(elementId);

        if (!elemento ||
            typeof L === "undefined") {
            return false;
        }

        const datos = Array.isArray(pasos)
            ? pasos
            : [];

        const mapa = L.map(elementId, {
            center: [12.8654, -85.2072],
            zoom: 7,
            minZoom: 6,
            maxZoom: 18,
            zoomControl: false,
            preferCanvas: true
        });

        L.tileLayer(
            "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
            {
                maxZoom: 19,
                attribution:
                    "&copy; OpenStreetMap contributors"
            })
            .addTo(mapa);

        L.control.zoom({
            position: "bottomright"
        }).addTo(mapa);

        mapa.createPane("clTimelineHeatPane");
        mapa.getPane("clTimelineHeatPane").style.zIndex = "350";
        mapa.getPane("clTimelineHeatPane").style.pointerEvents = "none";

        mapa.createPane("clTimelineMarkerPane");
        mapa.getPane("clTimelineMarkerPane").style.zIndex = "430";

        const contexto = {
            elementId,
            elemento,
            mapa,
            pasos: datos,
            variable: normalizarVariable(variable),
            indice: limitarIndice(
                indiceInicial,
                datos.length),
            dotNetReference,
            marcadores: L.layerGroup(),
            calor: null,
            intervalo: null,
            control: crearControl(mapa)
        };

        contexto.marcadores.addTo(mapa);
        contextos.set(elementId, contexto);

        mostrarPasoInterno(
            contexto,
            contexto.indice,
            true);

        setTimeout(() => mapa.invalidateSize(), 160);
        return true;
    }

    function mostrarPaso(elementId, indice) {
        const contexto = contextos.get(elementId);
        if (!contexto) return;

        mostrarPasoInterno(
            contexto,
            limitarIndice(
                indice,
                contexto.pasos.length),
            false);
    }

    function cambiarVariable(elementId, variable) {
        const contexto = contextos.get(elementId);
        if (!contexto) return;

        contexto.variable =
            normalizarVariable(variable);

        mostrarPasoInterno(
            contexto,
            contexto.indice,
            false);
    }

    function reproducir(elementId) {
        const contexto = contextos.get(elementId);

        if (!contexto ||
            contexto.pasos.length <= 1) {
            return;
        }

        pausar(elementId);

        contexto.intervalo = window.setInterval(() => {
            const siguiente =
                (contexto.indice + 1) %
                contexto.pasos.length;

            mostrarPasoInterno(
                contexto,
                siguiente,
                false);
        }, 1200);
    }

    function pausar(elementId) {
        const contexto = contextos.get(elementId);
        if (!contexto) return;

        if (contexto.intervalo) {
            window.clearInterval(
                contexto.intervalo);

            contexto.intervalo = null;
        }
    }

    function mostrarPasoInterno(
        contexto,
        indice,
        ajustarVista) {
        if (!contexto ||
            contexto.pasos.length === 0) {
            return;
        }

        contexto.indice = limitarIndice(
            indice,
            contexto.pasos.length);

        const paso =
            contexto.pasos[contexto.indice];

        const puntos =
            Array.isArray(paso?.puntos)
                ? paso.puntos.filter(punto =>
                    coordenadaValida(
                        Number(punto.latitud),
                        Number(punto.longitud)))
                : [];

        contexto.marcadores.clearLayers();

        if (contexto.calor &&
            contexto.mapa.hasLayer(contexto.calor)) {
            contexto.mapa.removeLayer(
                contexto.calor);
        }

        contexto.calor = null;

        const configuracion =
            variables[contexto.variable];

        const valores = puntos
            .map(punto =>
                convertirNumero(
                    punto[
                        configuracion.propiedad]))
            .filter(Number.isFinite);

        const minimo = valores.length > 0
            ? Math.min(...valores)
            : configuracion.min;

        const maximo = valores.length > 0
            ? Math.max(...valores)
            : configuracion.max;

        const rango = Math.max(
            maximo - minimo,
            0.0001);

        const limites = [];
        const calor = [];

        puntos.forEach(punto => {
            const latitud =
                Number(punto.latitud);

            const longitud =
                Number(punto.longitud);

            const valor =
                convertirNumero(
                    punto[
                        configuracion.propiedad]);

            const intensidad =
                Number.isFinite(valor)
                    ? Math.min(
                        1,
                        Math.max(
                            0.08,
                            (valor - minimo) /
                            rango))
                    : 0.08;

            const color =
                colorPorIntensidad(
                    intensidad,
                    configuracion.gradient);

            const icono = L.divIcon({
                className:
                    "cl-timeline-marker-wrapper",
                html: `
                    <div class="cl-timeline-marker"
                         style="--cl-marker-color:${color}">
                        <i class="fa-solid fa-seedling"></i>
                        <strong>
                            ${escapar(
                                formatearValor(
                                    valor,
                                    configuracion.unidad))}
                        </strong>
                    </div>`,
                iconSize: [54, 58],
                iconAnchor: [27, 54],
                popupAnchor: [0, -48]
            });

            const marcador = L.marker(
                [latitud, longitud],
                {
                    icon: icono,
                    pane: "clTimelineMarkerPane",
                    title:
                        punto.codigo ||
                        "Terreno"
                });

            marcador.bindPopup(
                construirPopup(
                    punto,
                    paso,
                    configuracion,
                    valor),
                {
                    maxWidth: 320,
                    minWidth: 260,
                    className:
                        "cl-timeline-leaflet-popup"
                });

            contexto.marcadores.addLayer(
                marcador);

            limites.push(
                [latitud, longitud]);

            calor.push([
                latitud,
                longitud,
                intensidad
            ]);
        });

        if (typeof L.heatLayer === "function" &&
            calor.length > 1) {
            contexto.calor = L.heatLayer(
                calor,
                {
                    pane: "clTimelineHeatPane",
                    radius: 46,
                    blur: 32,
                    minOpacity: 0.22,
                    maxZoom: 11,
                    max: 1,
                    gradient:
                        configuracion.gradient
                });

            contexto.calor.addTo(
                contexto.mapa);
        }

        actualizarControl(
            contexto,
            paso,
            configuracion,
            valores);

        if (ajustarVista &&
            limites.length > 0) {
            if (limites.length === 1) {
                contexto.mapa.setView(
                    limites[0],
                    14,
                    {
                        animate: false
                    });
            } else {
                contexto.mapa.fitBounds(
                    L.latLngBounds(limites),
                    {
                        padding: [45, 45],
                        maxZoom: 12,
                        animate: false
                    });
            }
        }

        if (contexto.dotNetReference) {
            contexto.dotNetReference
                .invokeMethodAsync(
                    "ActualizarPasoTimeline",
                    contexto.indice)
                .catch(() => {
                });
        }
    }

    function construirPopup(
        punto,
        paso,
        configuracion,
        valor) {
        const probabilidad =
            numeroOGuion(
                punto.probabilidadLluvia,
                "%");

        const precipitacion =
            numeroOGuion(
                punto.precipitacion,
                " mm");

        const viento =
            numeroOGuion(
                punto.viento,
                " km/h");

        const humedad =
            numeroOGuion(
                punto.humedad,
                "%");

        return `
            <div class="cl-timeline-popup">
                <span>
                    ${escapar(
                        paso?.etiqueta ||
                        "Pronóstico")}
                </span>

                <h3>
                    ${escapar(
                        punto.codigo ||
                        "Terreno")}
                </h3>

                <p>
                    ${escapar(
                        punto.ubicacion ||
                        "Ubicación no registrada")}
                </p>

                <div class="cl-timeline-popup-main">
                    <small>
                        ${escapar(
                            configuracion.titulo)}
                    </small>
                    <strong>
                        ${escapar(
                            formatearValor(
                                valor,
                                configuracion.unidad))}
                    </strong>
                    <b>
                        ${escapar(
                            punto.condicion ||
                            "Sin descripción")}
                    </b>
                </div>

                <div class="cl-timeline-popup-grid">
                    <span>
                        <small>Lluvia</small>
                        <strong>${probabilidad}</strong>
                    </span>
                    <span>
                        <small>Precipitación</small>
                        <strong>${precipitacion}</strong>
                    </span>
                    <span>
                        <small>Viento</small>
                        <strong>${viento}</strong>
                    </span>
                    <span>
                        <small>Humedad</small>
                        <strong>${humedad}</strong>
                    </span>
                </div>

                <footer>
                    Riesgo de tormenta:
                    <strong>
                        ${escapar(
                            punto.riesgoTormenta ||
                            "BAJO")}
                    </strong>
                </footer>
            </div>`;
    }

    function crearControl(mapa) {
        const control = L.control({
            position: "topleft"
        });

        control.onAdd = () => {
            const contenedor =
                L.DomUtil.create(
                    "div",
                    "cl-timeline-map-control");

            L.DomEvent.disableClickPropagation(
                contenedor);

            control.contenedor =
                contenedor;

            return contenedor;
        };

        control.addTo(mapa);
        return control;
    }

    function actualizarControl(
        contexto,
        paso,
        configuracion,
        valores) {
        const contenedor =
            contexto.control?.contenedor;

        if (!contenedor) return;

        const promedio =
            valores.length === 0
                ? null
                : valores.reduce(
                    (total, valor) =>
                        total + valor,
                    0) /
                  valores.length;

        contenedor.innerHTML = `
            <span>
                ${escapar(
                    paso?.etiqueta ||
                    "Pronóstico")}
            </span>

            <strong>
                ${escapar(
                    configuracion.titulo)}
            </strong>

            <small>
                Promedio:
                ${escapar(
                    formatearValor(
                        promedio,
                        configuracion.unidad))}
            </small>`;
    }

    function colorPorIntensidad(
        intensidad,
        gradient) {
        const paradas = Object.entries(
            gradient)
            .map(([posicion, color]) => [
                Number(posicion),
                color
            ])
            .sort((a, b) =>
                a[0] - b[0]);

        let color = paradas[0]?.[1] ||
            "#3b655b";

        for (const parada of paradas) {
            if (intensidad >= parada[0]) {
                color = parada[1];
            }
        }

        return color;
    }

    function normalizarVariable(variable) {
        const clave =
            String(variable || "")
                .trim()
                .toLowerCase();

        return Object.prototype.hasOwnProperty.call(
            variables,
            clave)
                ? clave
                : "temperatura";
    }

    function limitarIndice(
        indice,
        total) {
        if (total <= 0)
            return 0;

        const numero = Number(indice);

        if (!Number.isFinite(numero))
            return 0;

        return Math.max(
            0,
            Math.min(
                total - 1,
                Math.trunc(numero)));
    }

    function coordenadaValida(
        latitud,
        longitud) {
        return Number.isFinite(latitud) &&
            Number.isFinite(longitud) &&
            latitud >= 10.4 &&
            latitud <= 15.4 &&
            longitud >= -88.2 &&
            longitud <= -82.2;
    }

    function convertirNumero(valor) {
        if (valor === null ||
            valor === undefined ||
            valor === "") {
            return Number.NaN;
        }

        const numero = Number(valor);

        return Number.isFinite(numero)
            ? numero
            : Number.NaN;
    }

    function formatearValor(
        valor,
        unidad) {
        const numero =
            convertirNumero(valor);

        if (!Number.isFinite(numero))
            return "—";

        return `${numero.toFixed(1)}${unidad}`;
    }

    function numeroOGuion(
        valor,
        unidad) {
        const numero =
            convertirNumero(valor);

        return Number.isFinite(numero)
            ? `${numero.toFixed(1)}${unidad}`
            : "—";
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
        const contexto =
            contextos.get(elementId);

        if (!contexto)
            return;

        pausar(elementId);
        contexto.mapa.remove();
        contextos.delete(elementId);
    }

    return {
        inicializar,
        mostrarPaso,
        cambiarVariable,
        reproducir,
        pausar,
        destruir
    };
})();
