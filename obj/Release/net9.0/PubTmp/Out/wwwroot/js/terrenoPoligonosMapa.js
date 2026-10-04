/*
 * CONATRADEC
 * Delimitaciones de terrenos bajo demanda.
 *
 * Reglas del Centro Geoespacial nacional:
 * - La capa de polígonos es independiente de los puntos.
 * - Inicia apagada.
 * - Con territorio seleccionado puede mostrarse a cualquier zoom.
 * - Sin territorio seleccionado requiere el zoom mínimo configurado.
 * - Solo solicita los polígonos necesarios para la vista actual.
 */
window.conatradecTerrenoPoligonosMapa =
    window.conatradecTerrenoPoligonosMapa || (() => {
        const L = window.L;
        const mapas = new Map();
        const terrenosPorMapa = new Map();
        const poligonosPorMapa = new Map();
        const gruposPorMapa = new Map();
        const estadosPorMapa = new Map();
        let poligonosGlobales = [];

        const ZOOM_MINIMO_PREDETERMINADO = 13;
        const DEMORA_RECARGA_MS = 420;

        if (!L) {
            return apiVacia();
        }

        function apiVacia() {
            return {
                establecerPoligonos: () => { },
                establecerPoligonosParaMapa: () => { },
                configurarCargaDinamica: () => false,
                desconfigurarCargaDinamica: () => { },
                establecerVisibilidad: () => { },
                redibujar: () => { },
                limpiar: () => { }
            };
        }

        function estado(elementId) {
            if (!estadosPorMapa.has(elementId)) {
                estadosPorMapa.set(elementId, {
                    dinamico: false,
                    visible: true,
                    zoomMinimo: ZOOM_MINIMO_PREDETERMINADO,
                    referenciaDotNet: null,
                    temporizador: null,
                    secuencia: 0,
                    claveSolicitud: "",
                    cargando: false,
                    mensaje: "",
                    observadorInterfaz: null
                });
            }

            return estadosPorMapa.get(elementId);
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
            return numero(propiedad(
                item,
                "terrenoId",
                "TerrenoId"));
        }

        function vertices(item) {
            const valores = propiedad(
                item,
                "vertices",
                "Vertices");

            if (!Array.isArray(valores)) return [];

            return valores
                .map(vertice => {
                    const latitud = numero(propiedad(
                        vertice,
                        "latitud",
                        "Latitud"));
                    const longitud = numero(propiedad(
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
            if (L.__conatradecRegistroMapasPoligonoV2) {
                return;
            }

            const crearMapa = L.map;

            L.map = function (elemento, opciones) {
                const mapa = crearMapa.call(
                    this,
                    elemento,
                    opciones);

                const elementId =
                    typeof elemento === "string"
                        ? elemento
                        : elemento?.id;

                if (elementId) {
                    mapas.set(elementId, mapa);
                    estado(elementId);

                    mapa.on("zoomend moveend", () => {
                        redibujar(elementId, false);
                        programarCarga(elementId);
                    });

                    mapa.on("unload", () => {
                        desconfigurarCargaDinamica(elementId);
                        mapas.delete(elementId);
                        terrenosPorMapa.delete(elementId);
                        poligonosPorMapa.delete(elementId);
                        gruposPorMapa.delete(elementId);
                        estadosPorMapa.delete(elementId);
                    });
                }

                return mapa;
            };

            L.__conatradecRegistroMapasPoligonoV2 = true;
        }

        function asegurarPane(mapa) {
            if (mapa.getPane("geoTerrainPolygonPane")) {
                return;
            }

            const pane = mapa.createPane(
                "geoTerrainPolygonPane");

            pane.style.zIndex = "405";
            pane.style.pointerEvents = "auto";
        }

        function colorTerreno(terreno) {
            const nivel = String(propiedad(
                terreno,
                "nivelAlerta",
                "NivelAlerta") ?? "")
                .toUpperCase();

            if (nivel === "CRITICA" ||
                nivel === "CRITICO") {
                return "#EF4444";
            }

            if (nivel === "ATENCION") {
                return "#F59E0B";
            }

            if (nivel === "NORMAL" ||
                nivel === "ESTABLE") {
                return "#3B655B";
            }

            return "#64748B";
        }

        function terrenoPorId(elementId, terrenoId) {
            return (terrenosPorMapa.get(elementId) || [])
                .find(item => idTerreno(item) === terrenoId);
        }

        function limpiar(elementId) {
            const mapa = mapas.get(elementId);
            const grupo = gruposPorMapa.get(elementId);

            if (mapa && grupo) {
                try {
                    mapa.removeLayer(grupo);
                } catch {
                    // El mapa pudo ser destruido durante una navegación.
                }
            }

            gruposPorMapa.delete(elementId);
        }

        function territorioSeleccionado(elementId) {
            const mapaElemento = document.getElementById(elementId);
            const escenario = mapaElemento?.closest(
                ".geo-map-stage");
            const breadcrumb = escenario?.querySelector(
                ".geo-territorial-breadcrumb");

            const departamento = breadcrumb
                ?.querySelector("span")
                ?.textContent
                ?.trim() || "";

            const municipio = breadcrumb
                ?.querySelector("strong")
                ?.textContent
                ?.trim() || "";

            return {
                departamento,
                municipio,
                seleccionado: Boolean(
                    departamento || municipio)
            };
        }

        function puedeDibujar(elementId) {
            const mapa = mapas.get(elementId);
            const config = estado(elementId);

            if (!mapa || !config.visible) {
                return false;
            }

            const territorio = territorioSeleccionado(
                elementId);

            return territorio.seleccionado ||
                mapa.getZoom() >= config.zoomMinimo;
        }

        function redibujar(elementId, ajustarVista = false) {
            const mapa = mapas.get(elementId);
            const config = estado(elementId);

            limpiar(elementId);

            if (!mapa || !config.visible ||
                !puedeDibujar(elementId)) {
                actualizarControl(elementId);
                return;
            }

            const poligonos = poligonosPorMapa.get(
                elementId) || [];

            if (poligonos.length === 0) {
                actualizarControl(elementId);
                return;
            }

            asegurarPane(mapa);

            const grupo = L.featureGroup();
            const bounds = L.latLngBounds([]);
            const renderer = L.canvas({
                padding: 0.35,
                tolerance: 4
            });

            const territorio = territorioSeleccionado(
                elementId);
            const interactivo = territorio.seleccionado ||
                mapa.getZoom() >= config.zoomMinimo + 1;

            let total = 0;

            const idsTerrenosVisibles = new Set(
                (terrenosPorMapa.get(elementId) || [])
                    .map(idTerreno)
                    .filter(Boolean));

            for (const poligono of poligonos) {
                const terrenoId = idTerreno(poligono);
                const puntos = vertices(poligono);

                if (idsTerrenosVisibles.size > 0 &&
                    !idsTerrenosVisibles.has(terrenoId)) {
                    continue;
                }

                if (!terrenoId || puntos.length < 3) {
                    continue;
                }

                const terreno = terrenoPorId(
                    elementId,
                    terrenoId) || poligono;
                const color = colorTerreno(terreno);

                const capa = L.polygon(puntos, {
                    pane: "geoTerrainPolygonPane",
                    renderer,
                    color,
                    weight: interactivo ? 2.2 : 1.35,
                    opacity: interactivo ? 0.92 : 0.72,
                    fillColor: color,
                    fillOpacity: interactivo ? 0.035 : 0.018,
                    interactive: interactivo,
                    smoothFactor: 0.8,
                    bubblingMouseEvents: false
                });

                if (interactivo) {
                    const codigo = escapar(propiedad(
                        poligono,
                        "codigoTerreno",
                        "CodigoTerreno") ??
                        propiedad(
                            terreno,
                            "codigo",
                            "Codigo"));

                    const area = numero(propiedad(
                        poligono,
                        "areaManzanasCalculada",
                        "AreaManzanasCalculada")) || 0;

                    capa.bindTooltip(`
                        <div class="terrain-polygon-tooltip">
                            <strong>${codigo}</strong>
                            <span>${area.toLocaleString(
                                "es-NI",
                                {
                                    minimumFractionDigits: 2,
                                    maximumFractionDigits: 2
                                })} Mz delimitadas</span>
                        </div>`, {
                        sticky: true,
                        direction: "top"
                    });

                    capa.on({
                        mouseover: evento => {
                            evento.target.setStyle({
                                weight: 2.4,
                                opacity: 0.98,
                                fillOpacity: 0.09
                            });
                        },

                        mouseout: evento => {
                            evento.target.setStyle({
                                weight: 2.2,
                                opacity: 0.92,
                                fillOpacity: 0.035
                            });
                        }
                    });
                }

                capa.addTo(grupo);
                bounds.extend(capa.getBounds());
                total++;
            }

            if (total === 0) {
                actualizarControl(elementId);
                return;
            }

            grupo.addTo(mapa);
            gruposPorMapa.set(elementId, grupo);

            if (ajustarVista && bounds.isValid()) {
                mapa.fitBounds(bounds, {
                    padding: [35, 35],
                    maxZoom: 16
                });
            }

            config.mensaje = `${total} delimitaciones visibles`;
            actualizarControl(elementId);
        }

        function configurarCargaDinamica(
            elementId,
            referenciaDotNet,
            zoomMinimo = ZOOM_MINIMO_PREDETERMINADO) {
            const config = estado(elementId);

            config.dinamico = true;
            config.visible = false;
            config.referenciaDotNet = referenciaDotNet;
            config.zoomMinimo = Number.isFinite(Number(zoomMinimo))
                ? Number(zoomMinimo)
                : ZOOM_MINIMO_PREDETERMINADO;
            config.claveSolicitud = "";
            config.mensaje = "Capa desactivada";

            establecerPoligonosParaMapa(elementId, []);
            observarControl(elementId);
            esperarMapa(elementId);

            return true;
        }

        function esperarMapa(elementId, intento = 0) {
            if (mapas.has(elementId)) {
                inyectarControl(elementId);
                actualizarControl(elementId);
                return;
            }

            if (intento >= 80) {
                return;
            }

            setTimeout(
                () => esperarMapa(elementId, intento + 1),
                100);
        }

        function desconfigurarCargaDinamica(elementId) {
            const config = estadosPorMapa.get(elementId);

            if (!config) return;

            if (config.temporizador) {
                clearTimeout(config.temporizador);
                config.temporizador = null;
            }

            config.observadorInterfaz?.disconnect();
            config.observadorInterfaz = null;
            config.referenciaDotNet = null;
            config.dinamico = false;
            config.cargando = false;
            config.secuencia++;

            document
                .querySelectorAll(
                    `.geo-polygons-dynamic-layer[data-map-id="${elementId}"]`)
                .forEach(item => item.remove());

            limpiar(elementId);
        }

        function observarControl(elementId) {
            const config = estado(elementId);

            config.observadorInterfaz?.disconnect();

            config.observadorInterfaz =
                new MutationObserver(mutaciones => {
                    const relevante = mutaciones.some(mutacion =>
                        [...mutacion.addedNodes].some(nodo =>
                            nodo instanceof Element &&
                            (nodo.matches?.(".geo-layer-list, .geo-layer-item") ||
                             nodo.querySelector?.(".geo-layer-list, .geo-layer-item"))));

                    if (relevante) {
                        inyectarControl(elementId);
                    }
                });

            config.observadorInterfaz.observe(document.body, {
                childList: true,
                subtree: true
            });

            inyectarControl(elementId);
        }

        function inyectarControl(elementId) {
            const existente = document.querySelector(
                `.geo-polygons-dynamic-layer[data-map-id="${elementId}"]`);

            if (existente) {
                actualizarControl(elementId);
                return existente;
            }

            const mapaElemento = document.getElementById(elementId);
            const workspace = mapaElemento?.closest(".geo-workspace");

            if (!workspace) return null;

            const items = [...workspace.querySelectorAll(
                ".geo-layer-item")];

            const nombresCapaTerreno = new Set([
                "Terrenos registrados",
                "Mis terrenos"
            ]);

            const terrenoItem = items.find(item =>
                nombresCapaTerreno.has(
                    item.querySelector("strong")
                        ?.textContent
                        ?.trim() || ""));

            if (!terrenoItem) return null;

            const control = document.createElement("label");
            control.className =
                "geo-layer-item geo-polygons-dynamic-layer";
            control.dataset.mapId = elementId;
            control.title =
                "Carga delimitaciones únicamente al acercarse o seleccionar un territorio. Los puntos permanecen visibles.";

            control.innerHTML = `
                <span class="geo-layer-icon agricola">
                    <i class="fa-solid fa-draw-polygon"></i>
                </span>
                <span class="geo-layer-copy">
                    <strong>Delimitaciones de terrenos</strong>
                    <small class="geo-polygons-status">Capa desactivada</small>
                </span>
                <span class="geo-layer-loading geo-polygons-loading"
                      title="Cargando delimitaciones">
                    <i class="fa-solid fa-spinner fa-spin"></i>
                </span>
                <input type="checkbox"
                       aria-label="Mostrar delimitaciones de terrenos" />
                <span class="geo-switch" aria-hidden="true"></span>`;

            const checkbox = control.querySelector(
                "input[type='checkbox']");

            checkbox.addEventListener("change", () => {
                establecerVisibilidad(
                    elementId,
                    checkbox.checked);
            });

            terrenoItem.insertAdjacentElement(
                "afterend",
                control);

            actualizarControl(elementId);
            return control;
        }

        function establecerVisibilidad(elementId, visible) {
            const config = estado(elementId);
            config.visible = Boolean(visible);
            config.claveSolicitud = "";

            if (!config.visible) {
                config.secuencia++;
                config.cargando = false;
                config.mensaje = "Capa desactivada";
                establecerPoligonosParaMapa(elementId, []);
                actualizarControl(elementId);
                return;
            }

            programarCarga(elementId, true);
            actualizarControl(elementId);
        }

        function programarCarga(elementId, inmediata = false) {
            const config = estado(elementId);

            if (!config.dinamico || !config.visible) {
                return;
            }

            if (config.temporizador) {
                clearTimeout(config.temporizador);
            }

            config.temporizador = setTimeout(
                () => solicitarPoligonos(elementId),
                inmediata ? 0 : DEMORA_RECARGA_MS);
        }

        async function solicitarPoligonos(elementId) {
            const mapa = mapas.get(elementId);
            const config = estado(elementId);

            if (!mapa || !config.visible ||
                !config.referenciaDotNet) {
                return;
            }

            const territorio = territorioSeleccionado(elementId);
            const zoom = mapa.getZoom();

            if (!territorio.seleccionado &&
                zoom < config.zoomMinimo) {
                config.cargando = false;
                config.mensaje =
                    `Acérquese a zoom ${config.zoomMinimo} o seleccione un territorio`;
                establecerPoligonosParaMapa(elementId, []);
                actualizarControl(elementId);
                return;
            }

            const bounds = mapa.getBounds();
            const clave = [
                territorio.departamento,
                territorio.municipio,
                zoom,
                bounds.getSouth().toFixed(4),
                bounds.getNorth().toFixed(4),
                bounds.getWest().toFixed(4),
                bounds.getEast().toFixed(4)
            ].join("|");

            if (config.claveSolicitud === clave &&
                poligonosPorMapa.has(elementId)) {
                redibujar(elementId, false);
                return;
            }

            config.claveSolicitud = clave;
            config.cargando = true;
            config.mensaje = "Cargando delimitaciones...";
            const secuencia = ++config.secuencia;
            actualizarControl(elementId);

            try {
                const items = await config.referenciaDotNet
                    .invokeMethodAsync(
                        "ObtenerPoligonosMapa",
                        bounds.getSouth(),
                        bounds.getNorth(),
                        bounds.getWest(),
                        bounds.getEast(),
                        zoom,
                        territorio.departamento,
                        territorio.municipio);

                if (secuencia !== config.secuencia) {
                    return;
                }

                config.cargando = false;
                const lista = Array.isArray(items) ? items : [];
                config.mensaje = lista.length > 0
                    ? `${lista.length} delimitaciones cargadas`
                    : "No hay delimitaciones en esta vista";

                establecerPoligonosParaMapa(
                    elementId,
                    lista);
            } catch (error) {
                if (secuencia !== config.secuencia) {
                    return;
                }

                console.warn(
                    "No fue posible cargar las delimitaciones visibles.",
                    error);

                config.cargando = false;
                config.mensaje =
                    "No fue posible cargar las delimitaciones";
                establecerPoligonosParaMapa(elementId, []);
            }

            actualizarControl(elementId);
        }

        function actualizarControl(elementId) {
            const config = estado(elementId);
            const control = document.querySelector(
                `.geo-polygons-dynamic-layer[data-map-id="${elementId}"]`);

            if (!control) return;

            const checkbox = control.querySelector(
                "input[type='checkbox']");
            const status = control.querySelector(
                ".geo-polygons-status");
            const loading = control.querySelector(
                ".geo-polygons-loading");

            if (checkbox) {
                checkbox.checked = config.visible;
            }

            if (status) {
                status.textContent = config.mensaje ||
                    (config.visible
                        ? "Preparando delimitaciones"
                        : "Capa desactivada");
            }

            control.classList.toggle(
                "is-loading",
                config.cargando);
            control.classList.toggle(
                "is-warning",
                config.visible &&
                !config.cargando &&
                !puedeDibujar(elementId));

            if (loading) {
                loading.hidden = !config.cargando;
            }
        }

        function establecerPoligonos(items) {
            poligonosGlobales = Array.isArray(items)
                ? items
                : [];

            for (const [elementId] of mapas) {
                const config = estado(elementId);

                if (config.dinamico) continue;

                config.visible = true;
                poligonosPorMapa.set(
                    elementId,
                    poligonosGlobales);
                redibujar(elementId, false);
            }
        }

        function establecerPoligonosParaMapa(
            elementId,
            items) {
            poligonosPorMapa.set(
                elementId,
                Array.isArray(items) ? items : []);
            redibujar(elementId, false);
        }

        function envolverModuloMapa() {
            const modulo = window.conatradecMapaInteligente;

            if (!modulo ||
                modulo.__poligonosTerrenoIntegradosV2) {
                return;
            }

            if (typeof modulo.mostrarTerrenos === "function") {
                const mostrarOriginal =
                    modulo.mostrarTerrenos.bind(modulo);

                modulo.mostrarTerrenos = function (
                    elementId,
                    terrenos,
                    ajustarVista) {
                    terrenosPorMapa.set(
                        elementId,
                        Array.isArray(terrenos)
                            ? terrenos
                            : []);

                    const resultado = mostrarOriginal(
                        elementId,
                        terrenos,
                        ajustarVista);

                    const config = estado(elementId);
                    if (!config.dinamico &&
                        !poligonosPorMapa.has(elementId)) {
                        poligonosPorMapa.set(
                            elementId,
                            poligonosGlobales);
                    }

                    setTimeout(() => {
                        redibujar(elementId, false);
                        programarCarga(elementId, true);
                    }, 0);

                    return resultado;
                };
            }

            modulo.__poligonosTerrenoIntegradosV2 = true;
        }

        registrarMapasLeaflet();
        envolverModuloMapa();

        return {
            establecerPoligonos,
            establecerPoligonosParaMapa,
            configurarCargaDinamica,
            desconfigurarCargaDinamica,
            establecerVisibilidad,
            redibujar,
            limpiar
        };
    })();
