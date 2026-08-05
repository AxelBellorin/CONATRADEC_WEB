window.conatradecMapaInteligenteMejoras = (() => {
    const api = window.conatradecMapaInteligente;

    const respuestasClima = new Map();
    const metricasActivas = new Map();
    const capasTerritoriales = new Set();

    const METRICAS = {
        temperatura: {
            propiedad: "temperatura",
            nombre: "Temperatura",
            etiqueta: "Temperatura promedio",
            unidad: "°C",
            minimo: 15,
            maximo: 40,
            decimales: 1,
            icono: "fa-solid fa-temperature-half",
            clase: "temperature",
            descripcion: valor => {
                if (valor < 20) return "Fresco";
                if (valor < 25) return "Agradable";
                if (valor < 30) return "Cálido";
                if (valor < 34) return "Caluroso";
                return "Muy caluroso";
            }
        },

        humedad: {
            propiedad: "humedadRelativa",
            nombre: "Humedad relativa",
            etiqueta: "Humedad promedio",
            unidad: "%",
            minimo: 20,
            maximo: 100,
            decimales: 1,
            icono: "fa-solid fa-droplet",
            clase: "humidity",
            descripcion: valor => {
                if (valor < 40) return "Baja";
                if (valor < 60) return "Moderada";
                if (valor < 80) return "Alta";
                return "Muy alta";
            }
        },

        lluvia: {
            propiedad: "precipitacion",
            nombre: "Precipitación",
            etiqueta: "Precipitación estimada",
            unidad: "mm",
            minimo: 0,
            maximo: 20,
            decimales: 2,
            icono: "fa-solid fa-cloud-rain",
            clase: "rain",
            descripcion: valor => {
                if (valor <= 0.05) return "Sin lluvia";
                if (valor < 1) return "Lluvia ligera";
                if (valor < 5) return "Lluvia moderada";
                if (valor < 15) return "Lluvia fuerte";
                return "Lluvia intensa";
            }
        },

        viento: {
            propiedad: "velocidadViento",
            nombre: "Velocidad del viento",
            etiqueta: "Viento promedio",
            unidad: "km/h",
            minimo: 0,
            maximo: 60,
            decimales: 1,
            icono: "fa-solid fa-wind",
            clase: "wind",
            descripcion: valor => {
                if (valor < 5) return "Calma";
                if (valor < 15) return "Viento suave";
                if (valor < 30) return "Viento moderado";
                if (valor < 50) return "Viento fuerte";
                return "Viento muy fuerte";
            }
        }
    };

    const CLAVES_CLIMA = Object.keys(METRICAS);

    if (!api || api.__conatradecMejorasAplicadas) {
        return {
            cerrarPopups: () => { }
        };
    }

    optimizarMarcadoresMasivos();
    deshabilitarCapasDeCalor();
    envolverGeoJsonTerritorial();

    const mostrarClimaOriginal =
        api.mostrarClima.bind(api);

    const aplicarFiltroOriginal =
        api.aplicarFiltroTerritorial.bind(api);

    const aplicarCapasOriginal =
        api.aplicarCapas.bind(api);

    const cambiarCapaOriginal =
        api.cambiarCapa.bind(api);

    const destruirOriginal =
        api.destruir.bind(api);

    /**
     * Reduce el trabajo inicial cuando existen miles de terrenos.
     *
     * - Los anillos de alerta ya no crean un objeto gráfico por terreno.
     *   El color del marcador y los filtros siguen comunicando el nivel.
     * - El objeto Popup se crea únicamente al primer clic del marcador.
     * - Los grupos se cargan por bloques sin animaciones costosas.
     */
    function optimizarMarcadoresMasivos() {
        if (typeof L === "undefined" ||
            L.__conatradecMarcadoresOptimizados) {
            return;
        }

        if (typeof L.markerClusterGroup === "function") {
            const clusterOriginal =
                L.markerClusterGroup.bind(L);

            L.markerClusterGroup = opciones =>
                clusterOriginal({
                    ...opciones,
                    animate: false,
                    animateAddingMarkers: false,
                    chunkedLoading: true,
                    chunkInterval: 45,
                    chunkDelay: 12,
                    removeOutsideVisibleBounds: true
                });
        }

        if (L.Marker?.prototype?.bindPopup) {
            const bindPopupOriginal =
                L.Marker.prototype.bindPopup;

            L.Marker.prototype.bindPopup = function (
                contenido,
                opciones) {
                const esTerreno =
                    this?.options?.pane ===
                    "geoMarkerPane";

                if (!esTerreno) {
                    return bindPopupOriginal.call(
                        this,
                        contenido,
                        opciones);
                }

                this.__conatradecPopupPendiente = {
                    contenido,
                    opciones
                };

                if (!this.__conatradecPopupLazyRegistrado) {
                    this.__conatradecPopupLazyRegistrado = true;

                    this.on("click", function () {
                        const pendiente =
                            this.__conatradecPopupPendiente;

                        if (!pendiente) return;

                        this.__conatradecPopupPendiente = null;

                        bindPopupOriginal.call(
                            this,
                            pendiente.contenido,
                            pendiente.opciones);

                        this.openPopup();
                    });
                }

                return this;
            };
        }

        if (typeof L.circleMarker === "function") {
            const circleMarkerOriginal =
                L.circleMarker.bind(L);

            let alertaCompartida = null;

            L.circleMarker = (
                latlng,
                opciones = {}) => {
                const clase =
                    String(opciones.className || "");

                const esAnilloMasivo =
                    opciones.pane === "geoAlertPane" &&
                    clase.includes("geo-alert-ring");

                if (!esAnilloMasivo) {
                    return circleMarkerOriginal(
                        latlng,
                        opciones);
                }

                if (!alertaCompartida) {
                    alertaCompartida = L.layerGroup();
                    alertaCompartida.options =
                        alertaCompartida.options || {};
                    alertaCompartida.__conatradecAlertaAgrupada =
                        true;
                }

                return alertaCompartida;
            };
        }

        L.__conatradecMarcadoresOptimizados = true;
    }

    /**
     * Leaflet conserva los objetos de las capas climáticas para que el
     * estado, las leyendas y las fichas sigan funcionando, pero no dibuja
     * ninguna superficie de calor sobre el mapa.
     */
    function deshabilitarCapasDeCalor() {
        if (typeof L === "undefined" ||
            typeof L.heatLayer !== "function" ||
            L.heatLayer.__conatradecSoloTarjetas) {
            return;
        }

        const heatLayerOriginal = L.heatLayer.bind(L);

        const heatLayerInvisible = (
            puntos,
            opciones = {}) => {
            const grupo = L.layerGroup();

            grupo.__conatradecClima = {
                puntos: Array.isArray(puntos)
                    ? puntos
                    : [],
                opciones
            };

            return grupo;
        };

        heatLayerInvisible.__conatradecSoloTarjetas = true;
        heatLayerInvisible.__original = heatLayerOriginal;

        L.heatLayer = heatLayerInvisible;
    }

    /**
     * Registra las capas departamentales, municipales y del resumen
     * territorial. En el resumen territorial conserva el color de estado,
     * pero limita el relleno para que el mapa base y los terrenos continúen
     * siendo legibles.
     */
    function envolverGeoJsonTerritorial() {
        if (typeof L === "undefined" ||
            typeof L.geoJSON !== "function" ||
            L.geoJSON.__conatradecClimaTarjetas) {
            return;
        }

        const geoJsonOriginal = L.geoJSON.bind(L);

        const geoJsonConClima = (
            geojson,
            opciones = {}) => {
            const pane = String(opciones?.pane || "");
            const esTerritorial = [
                "geoDepartmentPane",
                "geoMunicipalityPane",
                "geoTerritorialSummaryPane"
            ].includes(pane);

            if (!esTerritorial) {
                return geoJsonOriginal(
                    geojson,
                    opciones);
            }

            const onEachFeatureOriginal =
                opciones.onEachFeature;

            const styleOriginal =
                opciones.style;

            const opcionesAjustadas = {
                ...opciones,

                onEachFeature: (
                    feature,
                    layer) => {
                    if (typeof onEachFeatureOriginal ===
                        "function") {
                        onEachFeatureOriginal(
                            feature,
                            layer);
                    }

                    registrarCapaTerritorial(
                        feature,
                        layer,
                        pane);

                    configurarOpacidadResumen(
                        layer,
                        pane);
                }
            };

            if (styleOriginal) {
                opcionesAjustadas.style = feature => {
                    const estiloBase =
                        typeof styleOriginal === "function"
                            ? styleOriginal(feature)
                            : styleOriginal;

                    return limitarEstiloTerritorial(
                        estiloBase,
                        pane,
                        false);
                };
            }

            const resultado = geoJsonOriginal(
                geojson,
                opcionesAjustadas);

            return resultado;
        };

        geoJsonConClima.__conatradecClimaTarjetas = true;
        geoJsonConClima.__original = geoJsonOriginal;

        L.geoJSON = geoJsonConClima;
        L.geoJson = geoJsonConClima;
    }

    function limitarEstiloTerritorial(
        estilo,
        pane,
        resaltado) {
        const base = estilo || {};
        const esResumen =
            pane === "geoTerritorialSummaryPane";

        const rellenoNormal = esResumen
            ? 0.028
            : 0.035;

        return {
            ...base,
            weight: resaltado
                ? 2.2
                : Math.min(
                    Number(base.weight) || 1.5,
                    1.7),
            opacity: resaltado
                ? 0.96
                : Math.min(
                    Number(base.opacity) || 0.82,
                    0.86),
            fillOpacity: resaltado
                ? 0.09
                : Math.min(
                    Number(base.fillOpacity) || 0,
                    rellenoNormal)
        };
    }

    function configurarOpacidadResumen(
        layer,
        pane) {
        if (!layer?.setStyle) {
            return;
        }

        layer.setStyle(
            limitarEstiloTerritorial(
                layer.options,
                pane,
                false));

        /*
         * Los manejadores originales siguen mostrando tooltip y popup.
         * Este manejador se registra después y limita el relleno para que
         * el mapa base, los puntos y las delimitaciones sigan legibles.
         */
        layer.on("mouseover", evento => {
            evento.target.setStyle(
                limitarEstiloTerritorial(
                    evento.target.options,
                    pane,
                    true));
        });

        layer.on("mouseout", evento => {
            evento.target.setStyle(
                limitarEstiloTerritorial(
                    evento.target.options,
                    pane,
                    false));
        });
    }

    function registrarCapaTerritorial(
        feature,
        layer,
        pane) {
        const registro = {
            feature,
            layer,
            pane,
            baseTooltip: null,
            basePopup: null
        };

        capasTerritoriales.add(registro);

        capturarContenidoBase(registro);
        actualizarRegistro(registro);

        layer.on("add", () => {
            setTimeout(() => {
                capturarContenidoBase(registro);
                actualizarRegistro(registro);
            }, 0);
        });

        layer.on("mouseover", () => {
            setTimeout(() => {
                capturarContenidoBase(registro);
                actualizarRegistro(registro);
            }, 0);
        });

        layer.on("click", () => {
            setTimeout(() => {
                capturarContenidoBase(
                    registro,
                    true);
                actualizarRegistro(registro);
                actualizarPopupsTerreno();
            }, 0);
        });

        /*
         * No se elimina el registro al ocultar temporalmente una capa.
         * Leaflet reutiliza la misma instancia cuando el usuario vuelve
         * a habilitar departamentos o municipios.
         */
    }

    function capturarContenidoBase(
        registro,
        forzarPopup = false) {
        const tooltip =
            registro.layer.getTooltip?.();

        if (tooltip) {
            const contenido =
                contenidoComoTexto(
                    tooltip.getContent?.());

            if (contenido &&
                !contenido.includes(
                    "geo-climate-card")) {
                registro.baseTooltip =
                    contenido;
            }
        }

        const popup =
            registro.layer.getPopup?.();

        if (popup) {
            const contenido =
                contenidoComoTexto(
                    popup.getContent?.());

            if (contenido &&
                !contenido.includes(
                    "geo-climate-card") &&
                (forzarPopup ||
                 registro.basePopup === null)) {
                registro.basePopup =
                    contenido;
            }
        }
    }

    function contenidoComoTexto(contenido) {
        if (typeof contenido === "string") {
            return contenido;
        }

        if (contenido instanceof HTMLElement) {
            return contenido.outerHTML;
        }

        return "";
    }

    api.mostrarClima = (
        elementId,
        respuesta) => {
        respuestasClima.set(
            elementId,
            respuesta);

        const resultado =
            mostrarClimaOriginal(
                elementId,
                respuesta);

        actualizarTodo(elementId);
        return resultado;
    };

    api.aplicarFiltroTerritorial = async (
        elementId,
        departamento,
        municipio,
        ajustarVista = true) => {
        const resultado =
            await aplicarFiltroOriginal(
                elementId,
                departamento,
                municipio,
                ajustarVista);

        actualizarTodo(elementId);
        return resultado;
    };

    api.aplicarCapas = (
        elementId,
        estadoCapas) => {
        actualizarMetricaActiva(
            elementId,
            estadoCapas);

        const resultado =
            aplicarCapasOriginal(
                elementId,
                estadoCapas);

        actualizarTodo(elementId);
        return resultado;
    };

    api.cambiarCapa = (
        elementId,
        clave,
        activa) => {
        const claveNormalizada =
            String(clave || "")
                .toLowerCase();

        if (CLAVES_CLIMA.includes(
            claveNormalizada)) {
            metricasActivas.set(
                elementId,
                activa
                    ? claveNormalizada
                    : "");
        }

        const resultado =
            cambiarCapaOriginal(
                elementId,
                clave,
                activa);

        actualizarTodo(elementId);
        return resultado;
    };

    api.destruir = elementId => {
        respuestasClima.delete(elementId);
        metricasActivas.delete(elementId);

        for (const registro of
            [...capasTerritoriales]) {
            if (idMapaDeLayer(
                registro.layer) ===
                elementId) {
                capasTerritoriales.delete(
                    registro);
            }
        }

        return destruirOriginal(elementId);
    };

    api.__conatradecMejorasAplicadas = true;

    function actualizarMetricaActiva(
        elementId,
        estadoCapas) {
        const activa =
            CLAVES_CLIMA.find(clave =>
                Boolean(
                    estadoCapas?.[clave])) ||
            "";

        metricasActivas.set(
            elementId,
            activa);
    }

    function actualizarTodo(elementId) {
        setTimeout(() => {
            actualizarCapasTerritoriales(
                elementId);

            actualizarPopupsTerreno(
                elementId);

            actualizarDescripcionesClima();
        }, 0);
    }

    function actualizarCapasTerritoriales(
        elementId) {
        for (const registro of
            capasTerritoriales) {
            const id =
                idMapaDeLayer(
                    registro.layer);

            if (!elementId ||
                !id ||
                id === elementId) {
                actualizarRegistro(
                    registro,
                    elementId);
            }
        }
    }

    function idMapaDeLayer(layer) {
        return layer?._map
            ?.getContainer?.()
            ?.id || "";
    }

    function actualizarRegistro(
        registro,
        elementIdAlternativo = "") {
        const elementId =
            idMapaDeLayer(registro.layer) ||
            elementIdAlternativo;

        if (!elementId) return;

        const metricaClave =
            metricasActivas.get(elementId) ||
            "";

        const respuesta =
            respuestasClima.get(elementId);

        const tooltip =
            registro.layer.getTooltip?.();

        if (tooltip &&
            registro.baseTooltip !== null) {
            tooltip.setContent(
                contenidoTerritorial(
                    registro.baseTooltip,
                    registro,
                    respuesta,
                    metricaClave,
                    true));
        }

        const popup =
            registro.layer.getPopup?.();

        if (popup &&
            registro.basePopup !== null) {
            popup.setContent(
                contenidoTerritorial(
                    registro.basePopup,
                    registro,
                    respuesta,
                    metricaClave,
                    false));
        }
    }

    function contenidoTerritorial(
        base,
        registro,
        respuesta,
        metricaClave,
        compacto) {
        const limpio =
            quitarTarjetaClima(base);

        if (!metricaClave ||
            !respuesta?.disponible) {
            return limpio;
        }

        const calculo =
            calcularClimaTerritorial(
                registro,
                respuesta,
                metricaClave);

        if (!calculo) return limpio;

        return limpio +
            crearTarjetaClima(
                metricaClave,
                calculo,
                respuesta,
                compacto);
    }

    function quitarTarjetaClima(html) {
        return String(html || "")
            .replace(
                /<section class="geo-climate-card[\s\S]*?<\/section>/g,
                "");
    }

    function calcularClimaTerritorial(
        registro,
        respuesta,
        metricaClave) {
        const metrica =
            METRICAS[metricaClave];

        const puntos =
            puntosClimaticosValidos(
                respuesta,
                metrica.propiedad);

        if (puntos.length === 0) {
            return null;
        }

        const internos =
            puntos.filter(punto =>
                puntoDentroFeature(
                    punto.longitud,
                    punto.latitud,
                    registro.feature));

        if (internos.length > 0) {
            const valores =
                internos.map(punto =>
                    punto.valor);

            return {
                valor: promedio(valores),
                minimo: Math.min(...valores),
                maximo: Math.max(...valores),
                puntos: internos.length,
                estimado: false
            };
        }

        const centro =
            centroDeRegistro(registro);

        if (!centro) return null;

        const cercano = [...puntos]
            .sort((a, b) =>
                distanciaCuadrada(
                    a,
                    centro) -
                distanciaCuadrada(
                    b,
                    centro))[0];

        if (!cercano) return null;

        return {
            valor: cercano.valor,
            minimo: cercano.valor,
            maximo: cercano.valor,
            puntos: 1,
            estimado: true
        };
    }

    function puntosClimaticosValidos(
        respuesta,
        propiedad) {
        const puntos =
            Array.isArray(respuesta?.puntos)
                ? respuesta.puntos
                : [];

        return puntos
            .map(punto => ({
                latitud:
                    numero(punto.latitud),
                longitud:
                    numero(punto.longitud),
                valor:
                    numero(punto[propiedad])
            }))
            .filter(punto =>
                punto.latitud !== null &&
                punto.longitud !== null &&
                punto.valor !== null);
    }

    function centroDeRegistro(registro) {
        const centro =
            registro.layer
                ?.getBounds?.()
                ?.getCenter?.();

        if (centro &&
            Number.isFinite(centro.lat) &&
            Number.isFinite(centro.lng)) {
            return {
                latitud: centro.lat,
                longitud: centro.lng
            };
        }

        return null;
    }

    function distanciaCuadrada(
        punto,
        centro) {
        return Math.pow(
            punto.latitud -
                centro.latitud,
            2) +
            Math.pow(
                punto.longitud -
                centro.longitud,
            2);
    }

    function promedio(valores) {
        if (!Array.isArray(valores) ||
            valores.length === 0) {
            return 0;
        }

        return valores.reduce(
            (total, valor) =>
                total + valor,
            0) / valores.length;
    }

    function puntoDentroFeature(
        longitud,
        latitud,
        feature) {
        const geometria =
            feature?.geometry;

        if (!geometria) return false;

        if (geometria.type ===
            "Polygon") {
            return puntoDentroPoligono(
                longitud,
                latitud,
                geometria.coordinates);
        }

        if (geometria.type ===
            "MultiPolygon") {
            return (
                geometria.coordinates || [])
                .some(poligono =>
                    puntoDentroPoligono(
                        longitud,
                        latitud,
                        poligono));
        }

        return false;
    }

    function puntoDentroPoligono(
        longitud,
        latitud,
        anillos) {
        if (!Array.isArray(anillos) ||
            anillos.length === 0) {
            return false;
        }

        if (!puntoDentroAnillo(
            longitud,
            latitud,
            anillos[0])) {
            return false;
        }

        for (
            let indice = 1;
            indice < anillos.length;
            indice++) {
            if (puntoDentroAnillo(
                longitud,
                latitud,
                anillos[indice])) {
                return false;
            }
        }

        return true;
    }

    function puntoDentroAnillo(
        longitud,
        latitud,
        anillo) {
        if (!Array.isArray(anillo) ||
            anillo.length < 3) {
            return false;
        }

        let dentro = false;
        let anterior =
            anillo.length - 1;

        for (
            let actual = 0;
            actual < anillo.length;
            actual++) {
            const puntoActual =
                anillo[actual];

            const puntoAnterior =
                anillo[anterior];

            const xActual =
                Number(puntoActual?.[0]);

            const yActual =
                Number(puntoActual?.[1]);

            const xAnterior =
                Number(puntoAnterior?.[0]);

            const yAnterior =
                Number(puntoAnterior?.[1]);

            if (![xActual, yActual,
                  xAnterior, yAnterior]
                .every(Number.isFinite)) {
                anterior = actual;
                continue;
            }

            const cruza =
                ((yActual > latitud) !==
                 (yAnterior > latitud)) &&
                (longitud <
                    (xAnterior - xActual) *
                    (latitud - yActual) /
                    ((yAnterior - yActual) ||
                        Number.EPSILON) +
                    xActual);

            if (cruza) dentro = !dentro;
            anterior = actual;
        }

        return dentro;
    }

    function crearTarjetaClima(
        metricaClave,
        calculo,
        respuesta,
        compacto) {
        const metrica =
            METRICAS[metricaClave];

        const valor =
            limitar(
                calculo.valor,
                metrica.minimo,
                metrica.maximo);

        const posicion =
            ((valor - metrica.minimo) /
             Math.max(
                metrica.maximo -
                    metrica.minimo,
                0.0001)) * 100;

        const valorTexto =
            formatearValor(
                calculo.valor,
                metrica);

        const fuente =
            calculo.estimado
                ? "Estimación del punto meteorológico más cercano"
                : calculo.puntos === 1
                    ? "Dato meteorológico dentro del territorio"
                    : `Promedio de ${calculo.puntos} puntos meteorológicos`;

        const rango =
            !calculo.estimado &&
            calculo.puntos > 1
                ? `${formatearValor(
                    calculo.minimo,
                    metrica)} – ${formatearValor(
                    calculo.maximo,
                    metrica)}`
                : "";

        const actualizado =
            fechaClima(respuesta);

        return `
            <section class="geo-climate-card ${metrica.clase} ${compacto ? "compact" : ""}">
                <div class="geo-climate-card-heading">
                    <span class="geo-climate-card-icon">
                        <i class="${metrica.icono}"></i>
                    </span>

                    <span class="geo-climate-card-copy">
                        <small>${metrica.etiqueta}</small>
                        <strong>${valorTexto}</strong>
                    </span>

                    <b>${metrica.descripcion(calculo.valor)}</b>
                </div>

                <div class="geo-climate-gradient"
                     style="--climate-position:${limitar(posicion, 0, 100).toFixed(2)}%">
                    <i></i>
                </div>

                <div class="geo-climate-scale">
                    <span>${formatearEscala(
                        metrica.minimo,
                        metrica)}</span>
                    <span>${formatearEscala(
                        metrica.maximo,
                        metrica)}</span>
                </div>

                ${compacto
                    ? ""
                    : `
                        <div class="geo-climate-card-meta">
                            <small>${fuente}</small>
                            ${rango
                                ? `<small>Rango observado: ${rango}</small>`
                                : ""}
                            ${actualizado
                                ? `<small>Actualizado: ${actualizado}</small>`
                                : ""}
                        </div>
                    `}
            </section>`;
    }

    function formatearValor(
        valor,
        metrica) {
        const decimales =
            metrica.clase === "rain" &&
            Math.abs(valor) < 1
                ? 2
                : metrica.decimales;

        return `${Number(valor)
            .toFixed(decimales)} ${metrica.unidad}`;
    }

    function formatearEscala(
        valor,
        metrica) {
        return `${Number(valor)
            .toFixed(0)} ${metrica.unidad}`;
    }

    function fechaClima(respuesta) {
        if (!respuesta?.actualizadoUtc) {
            return "";
        }

        const fecha =
            new Date(
                respuesta.actualizadoUtc);

        if (Number.isNaN(
            fecha.getTime())) {
            return "";
        }

        return fecha.toLocaleString(
            "es-NI",
            {
                day: "2-digit",
                month: "2-digit",
                hour: "2-digit",
                minute: "2-digit"
            });
    }

    function numero(valor) {
        const convertido =
            Number(valor);

        return Number.isFinite(
            convertido)
            ? convertido
            : null;
    }

    function limitar(
        valor,
        minimo,
        maximo) {
        return Math.min(
            maximo,
            Math.max(
                minimo,
                valor));
    }

    /**
     * Los popups de terrenos ya contienen los cuatro indicadores.
     * Se muestra únicamente el indicador activado y se incorpora el mismo
     * degradado informativo usado en las fichas territoriales.
     */
    function actualizarPopupsTerreno(
        elementId = "") {
        const raiz =
            elementId
                ? document.getElementById(
                    elementId)
                : document;

        if (!raiz) return;

        const metricaClave =
            elementId
                ? (metricasActivas.get(
                    elementId) || "")
                : metricaActivaGlobal();

        const metrica =
            METRICAS[metricaClave];

        raiz.querySelectorAll(
            ".geo-popup-climate")
            .forEach(contenedor => {
                if (!metrica) {
                    contenedor.style.display =
                        "none";
                    return;
                }

                contenedor.style.display =
                    "";

                const elementos =
                    [...contenedor
                        .querySelectorAll(
                            ":scope > span")];

                const indice =
                    CLAVES_CLIMA.indexOf(
                        metricaClave);

                elementos.forEach(
                    (elemento, posicion) => {
                        elemento.style.display =
                            posicion === indice
                                ? ""
                                : "none";
                    });

                contenedor
                    .querySelector(
                        ".geo-climate-inline-gradient")
                    ?.remove();

                const activo =
                    elementos[indice];

                const texto =
                    activo
                        ?.querySelector("b")
                        ?.textContent || "";

                const valor =
                    numero(
                        texto.replace(
                            ",",
                            ".")
                            .match(
                                /-?\d+(?:\.\d+)?/)
                            ?.[0]);

                if (valor === null) return;

                contenedor.insertAdjacentHTML(
                    "beforeend",
                    crearGradienteTerreno(
                        metrica,
                        valor));
            });
    }

    function crearGradienteTerreno(
        metrica,
        valor) {
        const posicion =
            limitar(
                ((valor - metrica.minimo) /
                 Math.max(
                    metrica.maximo -
                        metrica.minimo,
                    0.0001)) * 100,
                0,
                100);

        return `
            <div class="geo-climate-inline-gradient ${metrica.clase}">
                <div style="--climate-position:${posicion.toFixed(2)}%">
                    <i></i>
                </div>
                <span>
                    <small>${formatearEscala(
                        metrica.minimo,
                        metrica)}</small>
                    <b>${metrica.descripcion(valor)}</b>
                    <small>${formatearEscala(
                        metrica.maximo,
                        metrica)}</small>
                </span>
            </div>`;
    }

    function metricaActivaGlobal() {
        return [...metricasActivas.values()]
            .find(Boolean) || "";
    }

    function actualizarDescripcionesClima() {
        const descripciones = {
            Temperatura:
                "Muestra la temperatura en las fichas de departamentos, municipios y terrenos.",
            "Humedad relativa":
                "Muestra la humedad relativa en las fichas del mapa.",
            Precipitación:
                "Muestra la precipitación estimada en las fichas del mapa.",
            "Velocidad del viento":
                "Muestra la velocidad del viento en las fichas del mapa."
        };

        document
            .querySelectorAll(
                ".geo-layer-item")
            .forEach(item => {
                const nombre =
                    item.querySelector("strong")
                        ?.textContent
                        ?.trim();

                if (!nombre ||
                    !descripciones[nombre]) {
                    return;
                }

                const descripcion =
                    item.querySelector(
                        ".geo-layer-copy small");

                if (descripcion) {
                    descripcion.textContent =
                        descripciones[nombre];
                }
            });
    

        document
            .querySelectorAll(
                ".owner-control-panel .geo-section-title p")
            .forEach(parrafo => {
                const textoActual =
                    parrafo.textContent
                        ?.trim()
                        ?.toLowerCase() || "";

                if (textoActual.includes(
                    "mapa de calor")) {
                    parrafo.textContent =
                        "Muestra el indicador seleccionado en las fichas de sus terrenos, sin cubrir el mapa.";
                }
            });

        document
            .querySelectorAll(
                ".owner-control-panel .owner-clear-layer")
            .forEach(boton => {
                const textoActual =
                    boton.textContent
                        ?.replace(/\s+/g, " ")
                        ?.trim() || "";

                if (textoActual ===
                    "Quitar capa meteorológica") {
                    const nodoTexto =
                        [...boton.childNodes]
                            .find(nodo =>
                                nodo.nodeType ===
                                Node.TEXT_NODE &&
                                nodo.textContent
                                    ?.includes(
                                        "Quitar capa meteorológica"));

                    if (nodoTexto) {
                        nodoTexto.textContent =
                            " Quitar indicador meteorológico";
                    }
                }
            });
    }

    function cerrarPopups(elementId) {
        const contenedor =
            document.getElementById(
                elementId);

        if (!contenedor) return;

        contenedor
            .querySelectorAll(
                ".leaflet-popup-close-button")
            .forEach(boton =>
                boton.click());
    }

    let observacionPendiente = false;

    function actualizarInterfaz() {
        document
            .querySelectorAll(
                ".geo-map-stage")
            .forEach(escenario => {
                const detalleAbierto =
                    Boolean(
                        escenario.querySelector(
                            ".geo-detail-panel"));

                escenario.classList.toggle(
                    "has-detail-panel",
                    detalleAbierto);
            });

        actualizarDescripcionesClima();
        actualizarPopupsTerreno();
    }

    function mutacionRelevante(mutaciones) {
        const selectores = [
            ".geo-detail-panel",
            ".geo-popup-climate",
            ".geo-layer-item",
            ".leaflet-popup-content"
        ].join(",");

        return mutaciones.some(mutacion =>
            [...mutacion.addedNodes].some(nodo => {
                if (!(nodo instanceof Element)) {
                    return false;
                }

                return nodo.matches?.(selectores) ||
                    Boolean(nodo.querySelector?.(selectores));
            }));
    }

    const observador =
        new MutationObserver(mutaciones => {
            if (observacionPendiente ||
                !mutacionRelevante(mutaciones)) {
                return;
            }

            observacionPendiente = true;

            requestAnimationFrame(() => {
                observacionPendiente = false;
                actualizarInterfaz();
            });
        });

    observador.observe(
        document.body,
        {
            childList: true,
            subtree: true
        });

    actualizarInterfaz();

    return {
        cerrarPopups
    };
})();
