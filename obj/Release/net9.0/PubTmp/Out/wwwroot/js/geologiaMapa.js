window.conatradecGeologiaMapa = (() => {
    const VERSION = "1.6.0";
    const api = window.conatradecMapaInteligente;
    const contextos = new Map();

    const URL_FALLAS =
        "https://cdn.jsdelivr.net/gh/GEMScienceTools/" +
        "central_am_carib_faults@master/geojson/" +
        "central_am_caribbean_faults.geojson";

    const LIMITES_NICARAGUA = {
        oeste: -87.85,
        este: -82.45,
        sur: 10.65,
        norte: 15.20
    };

    const URL_GVP_BASE =
        "https://webservices.volcano.si.edu/geoserver/GVP-VOTW/ows";

    const URL_VOLCANES_HOLOCENO = construirUrlGvp(
        "Smithsonian_VOTW_Holocene_Volcanoes",
        1000);

    const URL_VOLCANES_PLEISTOCENO = construirUrlGvp(
        "Smithsonian_VOTW_Pleistocene_Volcanoes",
        1000);

    const URL_ERUPCIONES_HOLOCENO = construirUrlGvp(
        "Smithsonian_VOTW_Holocene_Eruptions",
        5000);

    // Respaldo secundario. La fuente principal es el WFS oficial de GVP.
    const URL_VOLCANES_ARCGIS_RESPALDO =
        "https://services.arcgis.com/BG6nSlhZSAWtExvp/ArcGIS/rest/services/" +
        "World_Volcanoes/FeatureServer/0/query" +
        "?where=1%3D1" +
        "&geometry=-87.85%2C10.65%2C-82.45%2C15.20" +
        "&geometryType=esriGeometryEnvelope" +
        "&inSR=4326" +
        "&spatialRel=esriSpatialRelIntersects" +
        "&outFields=*" +
        "&returnGeometry=true" +
        "&outSR=4326" +
        "&f=geojson";

    const URL_LIMITE_NACIONAL = "/data/nicaragua.geojson";

    const CONFIGURACION_RADIOS = {
        fallas: {
            minimo: 1,
            maximo: 50,
            paso: 1,
            inicial: 10,
            rapidos: [5, 10, 25, 50]
        },
        volcanes: {
            minimo: 5,
            maximo: 100,
            paso: 5,
            inicial: 25,
            rapidos: [10, 25, 50, 100]
        }
    };

    let fallasPromise = null;
    let volcanesPromise = null;
    let limitePromise = null;

    if (!api || api.__conatradecGeologiaAplicada) {
        return { inicializado: false, version: VERSION };
    }

    const inicializarOriginal = api.inicializar.bind(api);
    const mostrarTerrenosOriginal = api.mostrarTerrenos.bind(api);
    const destruirOriginal = api.destruir.bind(api);

    api.inicializar = (elementId, dotNetReference) => {
        let mapaCapturado = null;
        const crearMapaOriginal = window.L?.map;

        if (typeof crearMapaOriginal !== "function") {
            return inicializarOriginal(elementId, dotNetReference);
        }

        window.L.map = function (...argumentos) {
            const mapa = crearMapaOriginal.apply(this, argumentos);
            mapaCapturado = mapa;
            return mapa;
        };

        let resultado;

        try {
            resultado = inicializarOriginal(elementId, dotNetReference);
        } finally {
            window.L.map = crearMapaOriginal;
        }

        if (resultado && mapaCapturado) {
            prepararGeologia(elementId, mapaCapturado);
        }

        return resultado;
    };

    api.mostrarTerrenos = (
        elementId,
        terrenos,
        ajustarVista = true) => {
        const resultado = mostrarTerrenosOriginal(
            elementId,
            terrenos,
            ajustarVista);
        const contexto = contextos.get(elementId);

        if (contexto) {
            contexto.terrenos = Array.isArray(terrenos) ? terrenos : [];
            actualizarSelectorTerrenos(contexto);
            actualizarInformacionTerreno(contexto);
            actualizarResumenRadios(contexto);
            actualizarTerrenosEnRadio(contexto);
        }

        return resultado;
    };

    api.destruir = elementId => {
        const contexto = contextos.get(elementId);

        if (contexto) {
            contexto.mapa.off("popupopen", contexto.alAbrirPopup);
            contexto.mapa.off("zoomend", contexto.alCambiarZoom);
            Object.values(contexto.temporizadores || {}).forEach(id =>
                clearTimeout(id));
            desmontarIntegracionPanel(contexto);
            contexto.control?.remove();
            contexto.capas.fallas?.remove();
            contexto.capas.volcanes?.remove();
            contexto.capas.radioFallas?.remove();
            contexto.capas.radioVolcanes?.remove();
            contexto.capas.terrenosRadioFallas?.remove();
            contexto.capas.terrenosRadioVolcanes?.remove();
            contextos.delete(elementId);
        }

        return destruirOriginal(elementId);
    };

    function prepararGeologia(elementId, mapa) {
        const elemento = document.getElementById(elementId);
        if (!elemento || contextos.has(elementId)) return;

        crearPane(mapa, "geoGeologyRadiusPane", 344, "none");
        crearPane(mapa, "geoGeologyPane", 510, "auto");
        crearPane(mapa, "geoVolcanoPane", 520, "auto");
        crearPane(mapa, "geoGeologyHighlightPane", 500, "none");

        const contexto = {
            elementId,
            elemento,
            mapa,
            control: null,
            controlElemento: null,
            terrenos: [],
            terrenoSeleccionado: null,
            datos: {
                fallas: null,
                volcanes: null
            },
            capas: {
                fallas: null,
                volcanes: null,
                radioFallas: null,
                radioVolcanes: null,
                terrenosRadioFallas: L.layerGroup(),
                terrenosRadioVolcanes: L.layerGroup()
            },
            activas: {
                fallas: false,
                volcanes: false
            },
            radios: {
                fallas: CONFIGURACION_RADIOS.fallas.inicial,
                volcanes: CONFIGURACION_RADIOS.volcanes.inicial
            },
            distancias: {
                fallas: new Map(),
                volcanes: new Map()
            },
            temporizadores: {
                fallas: null,
                volcanes: null
            },
            alAbrirPopup: null,
            alCambiarZoom: null,
            integracion: {
                activa: false,
                panel: null,
                pestanas: null,
                contenidoOriginal: null,
                boton: null,
                observador: null,
                espacioObservado: null,
                temporizador: null,
                intentos: 0
            }
        };

        contexto.control = crearControlGeologia(contexto);
        contexto.control.addTo(mapa);
        programarIntegracionPanel(contexto);

        contexto.alAbrirPopup = evento => {
            const fuente = evento?.popup?._source;

            if (!fuente?.getLatLng || fuente.options?.geologiaTipo) return;

            const coordenada = fuente.getLatLng();
            const titulo = String(fuente.options?.title || "").trim();
            const terreno = buscarTerreno(
                contexto,
                titulo,
                coordenada.lat,
                coordenada.lng);

            if (terreno) {
                seleccionarTerreno(contexto, terreno);
            }
        };

        contexto.alCambiarZoom = () => {
            if (contexto.activas.fallas &&
                contexto.capas.radioFallas?.options?.radioVisualFallback) {
                actualizarCapaRadio(contexto, "fallas");
            }
        };

        mapa.on("popupopen", contexto.alAbrirPopup);
        mapa.on("zoomend", contexto.alCambiarZoom);
        contextos.set(elementId, contexto);
        sincronizarControlesRadio(contexto);
        actualizarResumenRadios(contexto);
    }

    function crearPane(mapa, nombre, zIndex, eventosPuntero) {
        const pane = mapa.getPane(nombre) || mapa.createPane(nombre);
        pane.style.zIndex = String(zIndex);
        pane.style.pointerEvents = eventosPuntero;
    }


    function programarIntegracionPanel(contexto) {
        if (!contexto?.integracion) return;

        if (contexto.integracion.temporizador) {
            clearTimeout(contexto.integracion.temporizador);
        }

        contexto.integracion.temporizador = setTimeout(() => {
            contexto.integracion.temporizador = null;
            integrarControlEnPanel(contexto);
        }, 40);
    }

    function integrarControlEnPanel(contexto) {
        const escenario = contexto.elemento.closest(".geo-map-stage");
        const espacio = escenario?.closest(".geo-workspace");
        const panel = espacio?.querySelector(
            ":scope > .geo-control-panel, " +
            ":scope > .owner-control-panel");
        const pestanas = panel?.querySelector(
            ".geo-global-filter-tabs, .owner-filter-tabs");
        const contenidoOriginal = panel?.querySelector(
            ".geo-global-panel-scroll, .owner-panel-scroll");
        const control = contexto.controlElemento;

        if (!espacio || !panel || !pestanas || !contenidoOriginal || !control) {
            contexto.integracion.intentos += 1;
            if (contexto.integracion.intentos < 30) {
                contexto.integracion.temporizador = setTimeout(() => {
                    contexto.integracion.temporizador = null;
                    integrarControlEnPanel(contexto);
                }, 160);
            }
            return;
        }

        contexto.integracion.intentos = 0;
        contexto.integracion.panel = panel;
        contexto.integracion.pestanas = pestanas;
        contexto.integracion.contenidoOriginal = contenidoOriginal;

        let boton = pestanas.querySelector("[data-geology-main-tab]");

        if (!boton) {
            boton = document.createElement("button");
            boton.type = "button";
            boton.className = "geo-geology-main-tab";
            boton.dataset.geologyMainTab = "true";
            boton.innerHTML = `
                <i class="fa-solid fa-mountain-sun"></i>
                <span>Geología</span>`;
            boton.addEventListener("click", evento => {
                evento.preventDefault();
                evento.stopPropagation();
                activarPanelGeologia(contexto);
            });
            pestanas.appendChild(boton);
        }

        contexto.integracion.boton = boton;

        Array.from(pestanas.querySelectorAll("button"))
            .filter(item => item !== boton)
            .forEach(item => {
                if (item.dataset.geologyDeactivateBound === "true") return;
                item.dataset.geologyDeactivateBound = "true";
                item.addEventListener(
                    "click",
                    () => desactivarPanelGeologia(contexto),
                    true);
            });

        if (control.parentElement !== panel) {
            panel.appendChild(control);
        }

        control.classList.add("geo-geology-integrated");
        control.classList.remove("leaflet-bar");
        control.querySelector(".geo-geology-toggle")
            ?.setAttribute("aria-hidden", "true");

        aplicarEstadoIntegracion(contexto);
        observarIntegracionPanel(contexto, espacio);
    }

    function observarIntegracionPanel(contexto, espacio) {
        if (contexto.integracion.espacioObservado === espacio &&
            contexto.integracion.observador) {
            return;
        }

        contexto.integracion.observador?.disconnect();
        contexto.integracion.espacioObservado = espacio;
        contexto.integracion.observador = new MutationObserver(() => {
            programarIntegracionPanel(contexto);
        });
        contexto.integracion.observador.observe(espacio, {
            childList: true,
            subtree: true
        });
    }

    function activarPanelGeologia(contexto) {
        contexto.integracion.activa = true;
        aplicarEstadoIntegracion(contexto);
    }

    function desactivarPanelGeologia(contexto) {
        if (!contexto?.integracion) return;
        contexto.integracion.activa = false;
        aplicarEstadoIntegracion(contexto);
    }

    function aplicarEstadoIntegracion(contexto) {
        const integracion = contexto.integracion;
        const panel = integracion.panel;
        const pestanas = integracion.pestanas;
        const contenidoOriginal = integracion.contenidoOriginal;
        const boton = integracion.boton;
        const control = contexto.controlElemento;

        if (!panel || !pestanas || !contenidoOriginal || !boton || !control) {
            return;
        }

        const activa = Boolean(integracion.activa);

        if (activa) {
            pestanas.querySelectorAll("button").forEach(item =>
                item.classList.toggle("active", item === boton));
        } else {
            boton.classList.remove("active");
        }

        contenidoOriginal.hidden = activa;
        control.hidden = !activa;
        control.classList.toggle("is-panel-active", activa);
        panel.classList.toggle("geo-geology-panel-active", activa);

        if (activa) {
            setTimeout(() => contexto.mapa.invalidateSize(), 80);
        }
    }

    function desmontarIntegracionPanel(contexto) {
        const integracion = contexto?.integracion;
        if (!integracion) return;

        if (integracion.temporizador) {
            clearTimeout(integracion.temporizador);
            integracion.temporizador = null;
        }

        integracion.observador?.disconnect();
        integracion.observador = null;
        integracion.boton?.remove();

        if (integracion.contenidoOriginal) {
            integracion.contenidoOriginal.hidden = false;
        }

        integracion.panel?.classList.remove("geo-geology-panel-active");
        integracion.activa = false;
    }

    function crearControlGeologia(contexto) {
        const ControlGeologia = L.Control.extend({
            options: {
                position: "topleft"
            },

            onAdd() {
                const contenedor = L.DomUtil.create(
                    "section",
                    "geo-geology-control leaflet-bar is-open");

                contenedor.innerHTML = `
                    <button class="geo-geology-toggle"
                            type="button"
                            aria-expanded="true">
                        <span>
                            <i class="fa-solid fa-layer-group"></i>
                            Geología
                        </span>
                        <i class="fa-solid fa-chevron-up"></i>
                    </button>

                    <div class="geo-geology-body">
                        <p class="geo-geology-intro">
                            Capas informativas de protección territorial y
                            contexto volcánico.
                        </p>

                        <label class="geo-geology-layer">
                            <span class="geo-geology-symbol fault"></span>
                            <span>
                                <strong>Fallas geológicas</strong>
                                <small>Protección y referencia territorial.</small>
                            </span>
                            <input type="checkbox" data-geology-layer="fallas" />
                            <span class="geo-geology-switch"></span>
                        </label>

                        <label class="geo-geology-layer">
                            <span class="geo-geology-symbol volcano">
                                <i class="fa-solid fa-volcano"></i>
                            </span>
                            <span>
                                <strong>Volcanes</strong>
                                <small>Ubicación y contexto geológico.</small>
                            </span>
                            <input type="checkbox" data-geology-layer="volcanes" />
                            <span class="geo-geology-switch"></span>
                        </label>

                        <div class="geo-geology-radius-card fault"
                             data-geology-radius-card="fallas">
                            <div class="geo-geology-radius-heading">
                                <span>
                                    <i class="fa-solid fa-wave-square"></i>
                                    Radio alrededor de fallas
                                </span>
                                <output data-geology-radius-output="fallas">10 km</output>
                            </div>
                            <div class="geo-geology-radius-incrementor">
                                <button type="button"
                                        aria-label="Reducir radio de fallas"
                                        data-geology-radius-step="fallas"
                                        data-direction="-1">
                                    <i class="fa-solid fa-minus"></i>
                                </button>
                                <input type="range"
                                       min="1"
                                       max="50"
                                       step="1"
                                       value="10"
                                       aria-label="Radio alrededor de fallas"
                                       data-geology-radius="fallas" />
                                <button type="button"
                                        aria-label="Aumentar radio de fallas"
                                        data-geology-radius-step="fallas"
                                        data-direction="1">
                                    <i class="fa-solid fa-plus"></i>
                                </button>
                            </div>
                            <div class="geo-geology-radius-quick"
                                 data-geology-radius-quick="fallas">
                                <button type="button" data-value="5">5 km</button>
                                <button type="button" data-value="10">10 km</button>
                                <button type="button" data-value="25">25 km</button>
                                <button type="button" data-value="50">50 km</button>
                            </div>
                        </div>

                        <div class="geo-geology-radius-card volcano"
                             data-geology-radius-card="volcanes">
                            <div class="geo-geology-radius-heading">
                                <span>
                                    <i class="fa-solid fa-volcano"></i>
                                    Radio alrededor de volcanes
                                </span>
                                <output data-geology-radius-output="volcanes">25 km</output>
                            </div>
                            <div class="geo-geology-radius-incrementor">
                                <button type="button"
                                        aria-label="Reducir radio de volcanes"
                                        data-geology-radius-step="volcanes"
                                        data-direction="-1">
                                    <i class="fa-solid fa-minus"></i>
                                </button>
                                <input type="range"
                                       min="5"
                                       max="100"
                                       step="5"
                                       value="25"
                                       aria-label="Radio alrededor de volcanes"
                                       data-geology-radius="volcanes" />
                                <button type="button"
                                        aria-label="Aumentar radio de volcanes"
                                        data-geology-radius-step="volcanes"
                                        data-direction="1">
                                    <i class="fa-solid fa-plus"></i>
                                </button>
                            </div>
                            <div class="geo-geology-radius-quick"
                                 data-geology-radius-quick="volcanes">
                                <button type="button" data-value="10">10 km</button>
                                <button type="button" data-value="25">25 km</button>
                                <button type="button" data-value="50">50 km</button>
                                <button type="button" data-value="100">100 km</button>
                            </div>
                        </div>

                        <div class="geo-geology-map-legend" aria-label="Leyenda geológica">
                            <strong>Leyenda del mapa</strong>
                            <span><i class="fault-line"></i> Trazo de falla cartografiada</span>
                            <span><i class="fault-radius"></i> Corredor del radio seleccionado</span>
                            <span><i class="volcano-point"><b class="fa-solid fa-volcano"></b></i> Centro volcánico</span>
                            <span><i class="volcano-radius"></i> Radio alrededor del volcán</span>
                        </div>

                        <div class="geo-geology-radius-summary"
                             data-geology-radius-summary>
                            Active una capa para visualizar su radio.
                        </div>

                        <div class="geo-geology-terrain">
                            <label>
                                Terreno para consultar
                                <select data-geology-terrain>
                                    <option value="">Seleccione un terreno</option>
                                </select>
                            </label>

                            <div class="geo-geology-result" data-geology-result>
                                <span class="empty">
                                    Seleccione un terreno o abra su marcador.
                                </span>
                            </div>
                        </div>

                        <div class="geo-geology-status" data-geology-status></div>

                        <small class="geo-geology-disclaimer">
                            Información cartográfica de referencia. No clasifica
                            seguridad, fertilidad ni aptitud agrícola.
                        </small>

                        <div class="geo-geology-sources">
                            <span>Fallas: CCAF-DB de GEM/CCARA, recortadas al límite nacional (CC BY 4.0; no es cartografía oficial de INETER).</span>
                            <span>Volcanes y erupciones: WFS oficial Smithsonian GVP (VOTW); incluye respaldo ArcGIS y catálogo local.</span>
                        </div>
                    </div>`;

                L.DomEvent.disableClickPropagation(contenedor);
                L.DomEvent.disableScrollPropagation(contenedor);

                configurarControl(contexto, contenedor);
                contexto.controlElemento = contenedor;
                return contenedor;
            }
        });

        return new ControlGeologia();
    }

    function configurarControl(contexto, contenedor) {
        const boton = contenedor.querySelector(".geo-geology-toggle");
        const icono = boton.querySelector(".fa-chevron-up");

        boton.addEventListener("click", () => {
            const abierto = contenedor.classList.toggle("is-open");
            boton.setAttribute("aria-expanded", String(abierto));
            icono.className = abierto
                ? "fa-solid fa-chevron-up"
                : "fa-solid fa-chevron-down";
        });

        contenedor
            .querySelectorAll("[data-geology-layer]")
            .forEach(entrada => {
                entrada.addEventListener("change", async evento => {
                    const clave = evento.target.dataset.geologyLayer;
                    const activa = Boolean(evento.target.checked);
                    await cambiarCapa(contexto, clave, activa);
                });
            });

        contenedor
            .querySelectorAll("[data-geology-radius]")
            .forEach(entrada => {
                entrada.addEventListener("input", evento => {
                    const clave = evento.target.dataset.geologyRadius;
                    establecerRadio(
                        contexto,
                        clave,
                        Number(evento.target.value),
                        false);
                    programarActualizacionRadio(contexto, clave);
                });

                entrada.addEventListener("change", evento => {
                    const clave = evento.target.dataset.geologyRadius;
                    establecerRadio(
                        contexto,
                        clave,
                        Number(evento.target.value),
                        true);
                });
            });

        contenedor
            .querySelectorAll("[data-geology-radius-step]")
            .forEach(botonPaso => {
                botonPaso.addEventListener("click", () => {
                    const clave = botonPaso.dataset.geologyRadiusStep;
                    const direccion = Number(botonPaso.dataset.direction) || 0;
                    const configuracion = CONFIGURACION_RADIOS[clave];
                    if (!configuracion) return;

                    establecerRadio(
                        contexto,
                        clave,
                        contexto.radios[clave] +
                            direccion * configuracion.paso,
                        true);
                });
            });

        contenedor
            .querySelectorAll("[data-geology-radius-quick] button")
            .forEach(botonRapido => {
                botonRapido.addEventListener("click", () => {
                    const grupo = botonRapido.closest(
                        "[data-geology-radius-quick]");
                    const clave = grupo?.dataset.geologyRadiusQuick;
                    establecerRadio(
                        contexto,
                        clave,
                        Number(botonRapido.dataset.value),
                        true);
                });
            });

        contenedor
            .querySelector("[data-geology-terrain]")
            .addEventListener("change", async evento => {
                const indice = Number(evento.target.value);
                const terreno = Number.isInteger(indice) && indice >= 0
                    ? contexto.terrenos[indice]
                    : null;

                contexto.terrenoSeleccionado = terreno || null;
                await actualizarInformacionTerreno(contexto, true);
            });
    }

    async function cambiarCapa(contexto, clave, activa) {
        if (!Object.prototype.hasOwnProperty.call(contexto.activas, clave)) {
            return;
        }

        contexto.activas[clave] = activa;
        actualizarEstadoTarjetaRadio(contexto, clave);

        if (!activa) {
            contexto.capas[clave]?.remove();
            removerCapaRadio(contexto, clave);
            removerTerrenosEnRadio(contexto, clave);
            actualizarEstado(contexto, "");
            actualizarResumenRadios(contexto);
            await actualizarInformacionTerreno(contexto);
            return;
        }

        actualizarEstado(contexto, `Cargando ${etiquetaCapa(clave)}...`);

        try {
            if (clave === "fallas") {
                await asegurarFallas(contexto);
            } else {
                await asegurarVolcanes(contexto);
            }

            if (contexto.activas[clave] &&
                contexto.capas[clave] &&
                !contexto.mapa.hasLayer(contexto.capas[clave])) {
                contexto.capas[clave].addTo(contexto.mapa);
            }

            await actualizarCapaRadio(contexto, clave);
            traerCapaGeologicaAlFrente(contexto, clave);
            actualizarTerrenosEnRadio(contexto, clave);
            actualizarResumenRadios(contexto);
            actualizarEstado(contexto, "");
            await actualizarInformacionTerreno(contexto);
        } catch (error) {
            contexto.activas[clave] = false;
            sincronizarCheckbox(contexto, clave, false);
            actualizarEstadoTarjetaRadio(contexto, clave);
            actualizarResumenRadios(contexto);
            actualizarEstado(
                contexto,
                `No fue posible cargar ${etiquetaCapa(clave)}.`,
                true);
            console.warn(`Error al cargar la capa ${clave}.`, error);
        }
    }

    async function asegurarFallas(contexto) {
        if (!contexto.datos.fallas) {
            contexto.datos.fallas = await cargarFallas();
            contexto.distancias.fallas.clear();
        }

        if (!contexto.capas.fallas) {
            contexto.capas.fallas = crearCapaFallas(contexto, contexto.datos.fallas);
        }
    }

    async function asegurarVolcanes(contexto) {
        if (!contexto.datos.volcanes) {
            contexto.datos.volcanes = await cargarVolcanes();
            contexto.distancias.volcanes.clear();
        }

        if (!contexto.capas.volcanes) {
            contexto.capas.volcanes = crearCapaVolcanes(contexto, contexto.datos.volcanes);
        }
    }


    function establecerRadio(contexto, clave, valor, aplicar) {
        const configuracion = CONFIGURACION_RADIOS[clave];
        if (!configuracion) return;

        const normalizado = Math.min(
            configuracion.maximo,
            Math.max(
                configuracion.minimo,
                Math.round(Number(valor) / configuracion.paso) *
                    configuracion.paso));

        contexto.radios[clave] = normalizado;
        sincronizarControlesRadio(contexto, clave);

        if (aplicar) {
            if (contexto.temporizadores[clave]) {
                clearTimeout(contexto.temporizadores[clave]);
                contexto.temporizadores[clave] = null;
            }

            aplicarCambioRadio(contexto, clave);
        }
    }

    function programarActualizacionRadio(contexto, clave) {
        if (contexto.temporizadores[clave]) {
            clearTimeout(contexto.temporizadores[clave]);
        }

        contexto.temporizadores[clave] = setTimeout(() => {
            contexto.temporizadores[clave] = null;
            aplicarCambioRadio(contexto, clave);
        }, 180);
    }

    async function aplicarCambioRadio(contexto, clave) {
        if (contexto.activas[clave]) {
            try {
                if (clave === "fallas") {
                    await asegurarFallas(contexto);
                } else {
                    await asegurarVolcanes(contexto);
                }

                await actualizarCapaRadio(contexto, clave);
            } catch (error) {
                console.warn(
                    `No fue posible actualizar el radio de ${clave}.`,
                    error);
            }
        }

        actualizarTerrenosEnRadio(contexto, clave);
        actualizarResumenRadios(contexto);
        await actualizarInformacionTerreno(contexto);
    }

    function sincronizarControlesRadio(contexto, claveUnica = null) {
        const claves = claveUnica
            ? [claveUnica]
            : Object.keys(CONFIGURACION_RADIOS);

        claves.forEach(clave => {
            const valor = contexto.radios[clave];
            const entrada = contexto.controlElemento
                ?.querySelector(`[data-geology-radius="${clave}"]`);
            const salida = contexto.controlElemento
                ?.querySelector(`[data-geology-radius-output="${clave}"]`);

            if (entrada) entrada.value = String(valor);
            if (salida) salida.textContent = `${valor} km`;

            contexto.controlElemento
                ?.querySelectorAll(
                    `[data-geology-radius-quick="${clave}"] button`)
                .forEach(boton => {
                    boton.classList.toggle(
                        "active",
                        Number(boton.dataset.value) === valor);
                });

            actualizarEstadoTarjetaRadio(contexto, clave);
        });
    }

    function actualizarEstadoTarjetaRadio(contexto, clave) {
        const tarjeta = contexto.controlElemento
            ?.querySelector(`[data-geology-radius-card="${clave}"]`);
        if (!tarjeta) return;

        tarjeta.classList.toggle("is-active", contexto.activas[clave]);
        tarjeta.classList.toggle("is-inactive", !contexto.activas[clave]);
    }

    function traerCapaGeologicaAlFrente(contexto, clave) {
        const capa = contexto.capas[clave];
        if (!capa) return;

        capa.eachLayer?.(item => {
            item.bringToFront?.();
            item.setZIndexOffset?.(1200);
        });
    }

    async function actualizarCapaRadio(contexto, clave) {
        removerCapaRadio(contexto, clave);
        if (!contexto.activas[clave]) return;

        const radioKm = contexto.radios[clave];

        if (clave === "fallas" && contexto.datos.fallas) {
            contexto.capas.radioFallas = crearRadioFallas(
                contexto,
                contexto.datos.fallas,
                radioKm);

            contexto.capas.radioFallas?.addTo(contexto.mapa);
            traerCapaGeologicaAlFrente(contexto, "fallas");
        }

        if (clave === "volcanes" && contexto.datos.volcanes) {
            contexto.capas.radioVolcanes = crearRadioVolcanes(
                contexto.datos.volcanes,
                radioKm);

            contexto.capas.radioVolcanes?.addTo(contexto.mapa);
            traerCapaGeologicaAlFrente(contexto, "volcanes");
        }
    }

    function removerCapaRadio(contexto, clave) {
        const propiedad = clave === "fallas"
            ? "radioFallas"
            : "radioVolcanes";

        contexto.capas[propiedad]?.remove();
        contexto.capas[propiedad] = null;
    }

    function crearRadioFallas(contexto, geoJson, radioKm) {
        if (window.turf?.buffer) {
            try {
                const area = window.turf.buffer(geoJson, radioKm, {
                    units: "kilometers",
                    steps: 6
                });

                if (area?.features?.length) {
                    return L.geoJSON(area, {
                        pane: "geoGeologyRadiusPane",
                        interactive: false,
                        style: {
                            color: "#dc2626",
                            weight: 1.6,
                            opacity: 0.82,
                            fillColor: "#f97316",
                            fillOpacity: 0.20,
                            lineJoin: "round"
                        }
                    });
                }
            } catch (error) {
                console.warn(
                    "No fue posible generar el corredor geográfico exacto; " +
                    "se utilizará una aproximación visual.",
                    error);
            }
        }

        const peso = pesoRadioFallasPixeles(contexto.mapa, radioKm);
        const capa = L.geoJSON(geoJson, {
            pane: "geoGeologyRadiusPane",
            interactive: false,
            style: {
                color: "#f97316",
                weight: peso,
                opacity: 0.24,
                lineCap: "round",
                lineJoin: "round"
            }
        });
        capa.options.radioVisualFallback = true;
        return capa;
    }

    function pesoRadioFallasPixeles(mapa, radioKm) {
        const latitud = mapa.getCenter().lat;
        const zoom = mapa.getZoom();
        const metrosPorPixel =
            156543.03392 * Math.cos(latitud * Math.PI / 180) /
            Math.pow(2, zoom);

        return Math.min(
            260,
            Math.max(8, radioKm * 2000 / Math.max(metrosPorPixel, 0.1)));
    }

    function crearRadioVolcanes(geoJson, radioKm) {
        const grupo = L.layerGroup();

        (geoJson?.features || []).forEach(feature => {
            const coordenadas = feature?.geometry?.coordinates;
            if (!Array.isArray(coordenadas) || coordenadas.length < 2) return;

            const longitud = Number(coordenadas[0]);
            const latitud = Number(coordenadas[1]);
            if (!coordenadaValida(latitud, longitud)) return;

            L.circle([latitud, longitud], {
                pane: "geoGeologyRadiusPane",
                radius: radioKm * 1000,
                color: "#b45309",
                weight: 2.2,
                opacity: 0.92,
                fillColor: "#fbbf24",
                fillOpacity: 0.17,
                interactive: false
            }).addTo(grupo);
        });

        return grupo;
    }

    function actualizarTerrenosEnRadio(contexto, claveUnica = null) {
        const claves = claveUnica ? [claveUnica] : ["fallas", "volcanes"];

        claves.forEach(clave => {
            removerTerrenosEnRadio(contexto, clave);
            if (!contexto.activas[clave]) return;

            const grupo = clave === "fallas"
                ? contexto.capas.terrenosRadioFallas
                : contexto.capas.terrenosRadioVolcanes;
            const radioKm = contexto.radios[clave];
            const color = clave === "fallas" ? "#dc2626" : "#b45309";
            const radioMarcador = clave === "fallas" ? 13 : 18;

            contexto.terrenos.forEach(terreno => {
                const resultado = distanciaTerrenoCapa(
                    contexto,
                    clave,
                    terreno);

                if (!resultado || resultado.distanciaKm > radioKm) return;

                const latitud = Number(terreno.latitud);
                const longitud = Number(terreno.longitud);
                if (!coordenadaValida(latitud, longitud)) return;

                const marcador = L.circleMarker([latitud, longitud], {
                    pane: "geoGeologyHighlightPane",
                    radius: radioMarcador,
                    color,
                    weight: 2,
                    opacity: 0.86,
                    fillColor: color,
                    fillOpacity: 0.08,
                    interactive: false
                });

                marcador.bindTooltip(
                    `${escapar(codigoTerreno(terreno) || "Terreno")} · ` +
                    `${formatearDistancia(resultado.distanciaKm)}`,
                    {
                        direction: "top",
                        className: "geo-geology-tooltip"
                    });
                grupo.addLayer(marcador);
            });

            if (!contexto.mapa.hasLayer(grupo)) grupo.addTo(contexto.mapa);
        });
    }

    function removerTerrenosEnRadio(contexto, clave) {
        const grupo = clave === "fallas"
            ? contexto.capas.terrenosRadioFallas
            : contexto.capas.terrenosRadioVolcanes;

        grupo?.clearLayers?.();
        grupo?.remove?.();
    }

    function distanciaTerrenoCapa(contexto, clave, terreno) {
        const latitud = Number(terreno?.latitud);
        const longitud = Number(terreno?.longitud);
        if (!coordenadaValida(latitud, longitud)) return null;

        const llave = `${latitud.toFixed(7)}|${longitud.toFixed(7)}`;
        const cache = contexto.distancias[clave];
        if (cache.has(llave)) return cache.get(llave);

        const datos = contexto.datos[clave];
        if (!datos) return null;

        const resultado = clave === "fallas"
            ? fallaMasCercana(latitud, longitud, datos)
            : volcanMasCercano(latitud, longitud, datos);

        cache.set(llave, resultado || null);
        return resultado || null;
    }

    function contarTerrenosEnRadio(contexto, clave) {
        if (!contexto.datos[clave]) return null;
        const radioKm = contexto.radios[clave];

        return contexto.terrenos.reduce((total, terreno) => {
            const resultado = distanciaTerrenoCapa(contexto, clave, terreno);
            return total + (resultado?.distanciaKm <= radioKm ? 1 : 0);
        }, 0);
    }

    function actualizarResumenRadios(contexto) {
        const resumen = contexto.controlElemento
            ?.querySelector("[data-geology-radius-summary]");
        if (!resumen) return;

        const bloques = ["fallas", "volcanes"].map(clave => {
            const activa = contexto.activas[clave];
            const cantidad = activa
                ? contarTerrenosEnRadio(contexto, clave)
                : null;
            const titulo = clave === "fallas" ? "Fallas" : "Volcanes";
            const icono = clave === "fallas"
                ? "fa-solid fa-wave-square"
                : "fa-solid fa-volcano";

            return `
                <div class="geo-geology-radius-summary-item ${clave} ${
                    activa ? "active" : "inactive"}">
                    <i class="${icono}"></i>
                    <span>
                        <small>${titulo} · ${contexto.radios[clave]} km</small>
                        <strong>${activa
                            ? (cantidad === null
                                ? "Cargando datos..."
                                : `${cantidad} terreno${cantidad === 1 ? "" : "s"} dentro`)
                            : "Capa desactivada"}</strong>
                    </span>
                </div>`;
        });

        resumen.innerHTML = bloques.join("");
    }

    function cargarFallas() {
        if (fallasPromise) return fallasPromise;

        fallasPromise = Promise.all([
            fetch(URL_FALLAS, {
                cache: "force-cache",
                mode: "cors",
                credentials: "omit",
                headers: {
                    Accept: "application/geo+json, application/json"
                }
            })
                .then(validarRespuesta)
                .then(respuesta => respuesta.json()),
            cargarLimiteNacional().catch(() => null)
        ])
            .then(([geoJson, limite]) =>
                recortarFallasNicaragua(geoJson, limite))
            .catch(error => {
                fallasPromise = null;
                throw error;
            });

        return fallasPromise;
    }

    function construirUrlGvp(nombreCapa, maxFeatures) {
        const parametros = new URLSearchParams({
            service: "WFS",
            version: "1.0.0",
            request: "GetFeature",
            typeName: `GVP-VOTW:${nombreCapa}`,
            outputFormat: "application/json",
            srsName: "EPSG:4326",
            maxFeatures: String(maxFeatures),
            bbox:
                `${LIMITES_NICARAGUA.oeste},${LIMITES_NICARAGUA.sur},` +
                `${LIMITES_NICARAGUA.este},${LIMITES_NICARAGUA.norte},` +
                "EPSG:4326"
        });

        return `${URL_GVP_BASE}?${parametros.toString()}`;
    }

    function cargarVolcanes() {
        if (volcanesPromise) return volcanesPromise;

        volcanesPromise = cargarCatalogoVolcanicoGvp()
            .catch(async errorGvp => {
                console.warn(
                    "No fue posible cargar el WFS oficial de Smithsonian GVP. " +
                    "Se intentará el respaldo ArcGIS.",
                    errorGvp);

                try {
                    const [geoJson, limite] = await Promise.all([
                        descargarGeoJson(URL_VOLCANES_ARCGIS_RESPALDO),
                        cargarLimiteNacional().catch(() => null)
                    ]);

                    return filtrarVolcanesNicaragua(
                        normalizarCatalogoArcGis(geoJson),
                        limite);
                } catch (errorRespaldo) {
                    console.warn(
                        "Se utilizará el catálogo volcánico local de respaldo.",
                        errorRespaldo);
                    return crearVolcanesRespaldo();
                }
            });

        return volcanesPromise;
    }

    async function cargarCatalogoVolcanicoGvp() {
        const [resultadoHoloceno, resultadoPleistoceno, resultadoErupciones,
            limite] = await Promise.all([
            descargarGeoJson(URL_VOLCANES_HOLOCENO)
                .then(geoJson => marcarEraVolcanes(geoJson, "HOLOCENO")),
            descargarGeoJson(URL_VOLCANES_PLEISTOCENO)
                .then(geoJson => marcarEraVolcanes(geoJson, "PLEISTOCENO")),
            descargarGeoJson(URL_ERUPCIONES_HOLOCENO)
                .catch(error => {
                    console.warn(
                        "El catálogo de erupciones no estuvo disponible; " +
                        "se mostrarán los datos generales de volcanes.",
                        error);
                    return { type: "FeatureCollection", features: [] };
                }),
            cargarLimiteNacional().catch(() => null)
        ]);

        const combinado = combinarCatalogosVolcanicos(
            resultadoHoloceno,
            resultadoPleistoceno,
            resultadoErupciones);
        const filtrado = filtrarVolcanesNicaragua(combinado, limite);

        if (!Array.isArray(filtrado?.features) || filtrado.features.length === 0) {
            throw new Error(
                "Smithsonian GVP no devolvió centros volcánicos para Nicaragua.");
        }

        return filtrado;
    }

    async function descargarGeoJson(url, esperaMs = 20000) {
        const controlador = new AbortController();
        const temporizador = setTimeout(() => controlador.abort(), esperaMs);

        try {
            const respuesta = await fetch(url, {
                cache: "force-cache",
                mode: "cors",
                credentials: "omit",
                signal: controlador.signal,
                headers: {
                    Accept: "application/geo+json, application/json"
                }
            });

            validarRespuesta(respuesta);
            const geoJson = await respuesta.json();

            if (!geoJson ||
                geoJson.type !== "FeatureCollection" ||
                !Array.isArray(geoJson.features)) {
                throw new Error("El proveedor no devolvió un GeoJSON válido.");
            }

            return geoJson;
        } finally {
            clearTimeout(temporizador);
        }
    }

    function marcarEraVolcanes(geoJson, era) {
        return {
            ...geoJson,
            features: (geoJson?.features || []).map(feature => ({
                ...feature,
                properties: {
                    ...(feature?.properties || {}),
                    __conatradecEra: era,
                    __conatradecFuente: "Smithsonian GVP WFS"
                }
            }))
        };
    }

    function combinarCatalogosVolcanicos(
        holoceno,
        pleistoceno,
        erupciones) {
        const indiceErupciones = new Map();

        (erupciones?.features || []).forEach(feature => {
            const propiedades = feature?.properties || {};
            const claves = clavesRelacionVolcan(propiedades);

            claves.forEach(clave => {
                if (!indiceErupciones.has(clave)) {
                    indiceErupciones.set(clave, []);
                }
                indiceErupciones.get(clave).push(propiedades);
            });
        });

        const porVolcan = new Map();
        const candidatas = [
            ...(holoceno?.features || []),
            ...(pleistoceno?.features || [])
        ];

        candidatas.forEach(feature => {
            const propiedades = feature?.properties || {};
            const claves = clavesRelacionVolcan(propiedades);
            const clavePrincipal = claves[0] ||
                `coordenada:${feature?.geometry?.coordinates?.join(",")}`;
            const existente = porVolcan.get(clavePrincipal);

            // Si el mismo centro aparece en ambos catálogos, se conserva la
            // clasificación holocena por ser el registro temporal más reciente.
            if (existente &&
                existente.properties?.__conatradecEra === "HOLOCENO") {
                return;
            }

            const registros = [];
            claves.forEach(clave => {
                (indiceErupciones.get(clave) || []).forEach(item => {
                    if (!registros.includes(item)) registros.push(item);
                });
            });

            const resumen = construirResumenVolcan(propiedades, registros);

            porVolcan.set(clavePrincipal, {
                ...feature,
                properties: {
                    ...propiedades,
                    __conatradecResumen: resumen,
                    __conatradecErupciones: registros
                        .map(normalizarErupcion)
                        .sort((a, b) => b.anioOrden - a.anioOrden)
                }
            });
        });

        return {
            type: "FeatureCollection",
            name: "volcanes_nicaragua_smithsonian_gvp",
            features: [...porVolcan.values()]
        };
    }

    function normalizarCatalogoArcGis(geoJson) {
        return {
            ...geoJson,
            features: (geoJson?.features || []).map(feature => {
                const propiedades = feature?.properties || {};

                /*
                 * ArcGIS se utiliza únicamente para ubicación y datos básicos.
                 * No se interpreta STATUS como época geológica ni se asume que
                 * la ausencia de registros equivale a cero erupciones.
                 */
                const propiedadesRespaldo = {
                    ...propiedades,
                    __conatradecEra: "SIN_CLASIFICAR",
                    __conatradecFuente:
                        "ArcGIS World_Volcanoes (ubicación de respaldo)",
                    __conatradecDatosEruptivosDisponibles: false
                };

                return {
                    ...feature,
                    properties: {
                        ...propiedadesRespaldo,
                        __conatradecResumen: construirResumenVolcan(
                            propiedadesRespaldo,
                            [])
                    }
                };
            })
        };
    }

    function cargarLimiteNacional() {
        if (limitePromise) return limitePromise;

        limitePromise = fetch(URL_LIMITE_NACIONAL, {
            cache: "force-cache"
        })
            .then(validarRespuesta)
            .then(respuesta => respuesta.json())
            .catch(error => {
                limitePromise = null;
                throw error;
            });

        return limitePromise;
    }

    function validarRespuesta(respuesta) {
        if (!respuesta.ok) {
            throw new Error(`El proveedor respondió ${respuesta.status}.`);
        }

        return respuesta;
    }

    function recortarFallasNicaragua(geoJson, limiteNacional) {
        const candidatas = Array.isArray(geoJson?.features)
            ? geoJson.features.filter(feature => {
                const tipo = feature?.geometry?.type;
                return (tipo === "LineString" || tipo === "MultiLineString") &&
                    geometriaIntersectaLimites(feature.geometry);
            })
            : [];

        if (!limiteNacional ||
            !window.turf?.lineSplit ||
            !window.turf?.polygonToLine ||
            !window.turf?.booleanPointInPolygon) {
            return crearColeccionFallas(candidatas);
        }

        try {
            const poligonos = extraerPoligonosNacionales(limiteNacional);
            const segmentos = [];

            candidatas.forEach(feature => {
                const lineas = convertirFallaALineas(feature);

                lineas.forEach(linea => {
                    poligonos.forEach(poligono => {
                        recortarLineaConPoligono(linea, poligono)
                            .forEach(segmento => segmentos.push(segmento));
                    });
                });
            });

            return crearColeccionFallas(segmentos);
        } catch (error) {
            console.warn(
                "No fue posible recortar las fallas al contorno nacional; " +
                "se aplicará únicamente el filtro geográfico de respaldo.",
                error);
            return crearColeccionFallas(candidatas);
        }
    }

    function crearColeccionFallas(features) {
        return {
            type: "FeatureCollection",
            name: "fallas_geologicas_nicaragua",
            features: Array.isArray(features) ? features : []
        };
    }

    function extraerPoligonosNacionales(geoJson) {
        const poligonos = [];

        (geoJson?.features || []).forEach(feature => {
            const geometria = feature?.geometry;
            if (!geometria) return;

            if (geometria.type === "Polygon") {
                poligonos.push(window.turf.polygon(
                    geometria.coordinates,
                    feature.properties || {}));
            } else if (geometria.type === "MultiPolygon") {
                (geometria.coordinates || []).forEach(coordenadas => {
                    poligonos.push(window.turf.polygon(
                        coordenadas,
                        feature.properties || {}));
                });
            }
        });

        return poligonos;
    }

    function convertirFallaALineas(feature) {
        const geometria = feature?.geometry;
        const propiedades = feature?.properties || {};
        if (!geometria) return [];

        if (geometria.type === "LineString") {
            return [window.turf.lineString(
                geometria.coordinates || [],
                propiedades)];
        }

        if (geometria.type === "MultiLineString") {
            return (geometria.coordinates || [])
                .filter(coordenadas => Array.isArray(coordenadas) &&
                    coordenadas.length >= 2)
                .map(coordenadas => window.turf.lineString(
                    coordenadas,
                    propiedades));
        }

        return [];
    }

    function recortarLineaConPoligono(linea, poligono) {
        const borde = window.turf.polygonToLine(poligono);
        const division = window.turf.lineSplit(linea, borde);
        const partes = division?.features?.length
            ? division.features
            : [linea];

        return partes
            .filter(segmento => segmentoDentroPoligono(segmento, poligono))
            .map(segmento => ({
                ...segmento,
                properties: {
                    ...(linea.properties || {}),
                    ...(segmento.properties || {})
                }
            }));
    }

    function segmentoDentroPoligono(segmento, poligono) {
        const coordenadas = segmento?.geometry?.coordinates || [];
        if (coordenadas.length === 0) return false;

        let puntoPrueba = window.turf.point(coordenadas[0]);

        if (window.turf.length && window.turf.along) {
            const longitud = window.turf.length(segmento, {
                units: "kilometers"
            });

            if (Number.isFinite(longitud) && longitud > 0) {
                puntoPrueba = window.turf.along(
                    segmento,
                    longitud / 2,
                    { units: "kilometers" });
            }
        }

        return window.turf.booleanPointInPolygon(
            puntoPrueba,
            poligono,
            { ignoreBoundary: false });
    }

    function filtrarVolcanesNicaragua(geoJson, limiteNacional) {
        const features = Array.isArray(geoJson?.features)
            ? geoJson.features.filter(feature => {
                const coordenadas = feature?.geometry?.coordinates;
                if (!Array.isArray(coordenadas) || coordenadas.length < 2) {
                    return false;
                }

                const longitud = Number(coordenadas[0]);
                const latitud = Number(coordenadas[1]);

                if (!coordenadaEnLimites(longitud, latitud)) return false;
                if (!limiteNacional) return true;

                return puntoDentroGeoJson(longitud, latitud, limiteNacional);
            })
            : [];

        if (features.length === 0) {
            return crearVolcanesRespaldo();
        }

        return {
            type: "FeatureCollection",
            name: "volcanes_nicaragua",
            features
        };
    }

    function crearCapaFallas(contexto, geoJson) {
        const visibles = L.geoJSON(geoJson, {
            pane: "geoGeologyPane",
            interactive: false,
            style: feature => estiloFallaVisible(feature)
        });

        const interaccion = L.geoJSON(geoJson, {
            pane: "geoGeologyPane",
            interactive: true,
            bubblingMouseEvents: false,
            style: {
                color: "#111827",
                weight: 30,
                opacity: 0.01,
                lineCap: "round",
                lineJoin: "round",
                className: "geo-fault-hit"
            },
            onEachFeature: (feature, layer) => {
                configurarInteraccionFalla(contexto, feature, layer);
            }
        });

        return L.layerGroup([visibles, interaccion]);
    }

    function estiloFallaVisible(feature) {
        const calidad = Number(
            feature?.properties?.epistemic_quality || 1);

        return {
            color: "#dc2626",
            weight: calidad >= 3 ? 3.4 : 4.6,
            opacity: calidad >= 3 ? 0.88 : 1,
            dashArray: calidad >= 3 ? "9 7" : null,
            lineCap: "round",
            lineJoin: "round",
            className: "geo-fault-path"
        };
    }

    function configurarInteraccionFalla(contexto, feature, layer) {
        const propiedades = feature?.properties || {};
        const nombre = valorPropiedad(
            propiedades,
            ["name", "fs_name"],
            "Falla cartografiada");
        const zona = valorPropiedad(
            propiedades,
            ["fs_name"],
            "Sin zona registrada");
        const tipo = valorPropiedad(
            propiedades,
            ["slip_type"],
            "Sin clasificación disponible");
        const referencia = valorPropiedad(
            propiedades,
            ["reference"],
            "GEM / CCARA");
        const actividad = Number(propiedades.is_active) === 1
            ? "Registrada como activa"
            : "Actividad no especificada";
        const precision = valorPropiedad(
            propiedades,
            ["accuracy"],
            "Sin dato");
        const movimiento = valorPropiedad(
            propiedades,
            ["last_movement"],
            "Sin dato disponible");

        const contenidoPopup = `
            <div class="geo-geology-popup">
                <span>Falla geológica cartografiada</span>
                <h3>${escapar(nombre)}</h3>
                <dl>
                    <div><dt>Zona</dt><dd>${escapar(zona)}</dd></div>
                    <div><dt>Tipo de movimiento</dt><dd>${escapar(tipo)}</dd></div>
                    <div><dt>Actividad</dt><dd>${escapar(actividad)}</dd></div>
                    <div><dt>Precisión del trazado</dt><dd>${escapar(precision)}</dd></div>
                    <div><dt>Último movimiento</dt><dd>${escapar(movimiento)}</dd></div>
                    <div><dt>Referencia</dt><dd>${escapar(referencia)}</dd></div>
                </dl>
                <small>
                    El trazo fue recortado al límite territorial de Nicaragua.
                    La ausencia de una línea cercana no demuestra ausencia de
                    fallas no cartografiadas.
                </small>
            </div>`;

        layer.options.geologiaTipo = "falla";
        layer.bindTooltip(nombre, {
            sticky: true,
            direction: "top",
            className: "geo-geology-tooltip"
        });

        layer.on("click", evento => {
            detenerEventoGeologia(evento);
            abrirPopupGeologia(
                contexto,
                contenidoPopup,
                evento?.latlng,
                350);
        });
    }

    function crearCapaVolcanes(contexto, geoJson) {
        return L.geoJSON(geoJson, {
            pane: "geoVolcanoPane",
            pointToLayer: (feature, latlng) => {
                const propiedades = feature?.properties || {};
                const nombre = nombreVolcan(propiedades);
                const resumen = obtenerResumenVolcan(propiedades);
                const claseEstado = resumen.enCurso
                    ? "ongoing"
                    : resumen.era === "HOLOCENO"
                        ? "holocene"
                        : resumen.era === "PLEISTOCENO"
                            ? "pleistocene"
                            : "unknown";

                return L.marker(latlng, {
                    pane: "geoVolcanoPane",
                    title: nombre,
                    alt: nombre,
                    keyboard: true,
                    riseOnHover: true,
                    bubblingMouseEvents: false,
                    geologiaTipo: "volcan",
                    icon: L.divIcon({
                        className:
                            `geo-volcano-marker-wrapper ${claseEstado}`,
                        html: `
                            <span class="geo-volcano-marker">
                                <i class="fa-solid fa-volcano"></i>
                            </span>`,
                        iconSize: [44, 48],
                        iconAnchor: [22, 44],
                        popupAnchor: [0, -40]
                    })
                });
            },
            onEachFeature: (feature, layer) => {
                const propiedades = feature?.properties || {};
                const nombre = nombreVolcan(propiedades);
                const resumen = obtenerResumenVolcan(propiedades);
                const tipo = valorPropiedad(
                    propiedades,
                    [
                        "Primary Volcano Type",
                        "Primary_Volcano_Type",
                        "primary_vo",
                        "TYPE",
                        "SimpleType"
                    ],
                    "");
                const elevacion = numeroPropiedad(
                    propiedades,
                    ["Elevation", "Elevation m", "Elevation_m", "elevation", "ELEV"]);
                const numeroVolcan = valorPropiedad(
                    propiedades,
                    ["Volcano Number", "Volcano_Number", "volcano_nu"],
                    "");
                const configuracionTectonica = valorPropiedad(
                    propiedades,
                    ["Tectonic Setting", "Tectonic_Setting", "tectonic_s"],
                    "");
                const rocaPrincipal = valorPropiedad(
                    propiedades,
                    ["Major Rock 1", "Major_Rock_1", "major_rock"],
                    "");
                const coordenada = layer.getLatLng?.();
                const coordenadasTexto = coordenada
                    ? `${coordenada.lat.toFixed(5)}, ${coordenada.lng.toFixed(5)}`
                    : "Sin dato";
                const historial = construirHistorialErupcionesHtml(
                    propiedades.__conatradecErupciones || [],
                    resumen.datosEruptivosDisponibles);
                const enlaceGvp = resumen.datosEruptivosDisponibles && numeroVolcan
                    ? `<a class="geo-volcano-source-link"
                           href="https://volcano.si.edu/volcano.cfm?vn=${encodeURIComponent(numeroVolcan)}"
                           target="_blank"
                           rel="noopener noreferrer">
                           Abrir perfil oficial GVP
                       </a>`
                    : "";

                layer.bindTooltip(nombre, {
                    permanent: true,
                    interactive: false,
                    direction: "right",
                    offset: [18, -4],
                    className: "geo-volcano-label"
                });

                const contenidoPopup = `
                    <div class="geo-geology-popup volcano">
                        <span>Centro volcánico</span>
                        <h3>${escapar(nombre)}</h3>
                        ${construirBanderasVolcanHtml(resumen)}
                        <dl>
                            ${filaDatoVolcan("N.º Smithsonian", numeroVolcan,
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("Época geológica",
                                resumen.era !== "SIN_CLASIFICAR"
                                    ? etiquetaEraVolcan(resumen.era)
                                    : "",
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("Tipo principal", tipo)}
                            ${filaDatoVolcan("Última erupción",
                                resumen.ultimaErupcionTexto,
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("Erupciones holocenas",
                                resumen.cantidadErupciones,
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("Registro en curso",
                                resumen.enCurso
                                    ? "Sí, marcado como continuo por GVP"
                                    : "No identificado como continuo",
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("VEI máximo registrado",
                                resumen.veiMaximo,
                                resumen.datosEruptivosDisponibles &&
                                    resumen.veiMaximo !== null)}
                            ${filaDatoVolcan("Registro en últimos 20 años",
                                resumen.reciente ? "Sí" : "No",
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("Configuración tectónica",
                                configuracionTectonica,
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("Roca principal",
                                rocaPrincipal,
                                resumen.datosEruptivosDisponibles)}
                            ${filaDatoVolcan("Elevación",
                                elevacion === null
                                    ? ""
                                    : `${elevacion.toLocaleString("es-NI")} m`)}
                            ${filaDatoVolcan("Coordenadas", coordenadasTexto)}
                            ${filaDatoVolcan("Fuente", resumen.fuente)}
                        </dl>
                        ${historial}
                        ${enlaceGvp}
                        <small>
                            ${resumen.datosEruptivosDisponibles
                                ? `“Holoceno” indica registro geológico o eruptivo en
                                   aproximadamente los últimos 12 000 años. No equivale
                                   por sí solo a una alerta operativa. Para el estado de
                                   vigilancia actual debe consultarse INETER.`
                                : `La fuente de respaldo confirma únicamente la ubicación
                                   y los datos básicos mostrados. La información eruptiva
                                   se oculta mientras no esté disponible el registro oficial.`}
                        </small>
                    </div>`;

                layer.on("click", evento => {
                    detenerEventoGeologia(evento);
                    abrirPopupGeologia(
                        contexto,
                        contenidoPopup,
                        evento?.latlng || layer.getLatLng?.(),
                        400);
                });
            }
        });
    }

    function filaDatoVolcan(etiqueta, valor, mostrar = true) {
        if (!mostrar || valor === null || valor === undefined) return "";

        const texto = String(valor).trim();
        if (!texto) return "";

        return `<div><dt>${escapar(etiqueta)}</dt><dd>${escapar(texto)}</dd></div>`;
    }

    function detenerEventoGeologia(evento) {
        const original = evento?.originalEvent;
        if (!original) return;

        L.DomEvent.stop(original);
        original.preventDefault?.();
    }

    function abrirPopupGeologia(
        contexto,
        contenido,
        latlng,
        maxWidth) {
        if (!contexto?.mapa || !latlng) return;

        L.popup({
            maxWidth,
            minWidth: 255,
            autoPan: true,
            closeButton: true,
            className: "geo-terrain-leaflet-popup geo-geology-leaflet-popup"
        })
            .setLatLng(latlng)
            .setContent(contenido)
            .openOn(contexto.mapa);
    }

    function actualizarSelectorTerrenos(contexto) {
        const selector = contexto.controlElemento
            ?.querySelector("[data-geology-terrain]");
        if (!selector) return;

        const codigoSeleccionado = codigoTerreno(
            contexto.terrenoSeleccionado);

        selector.innerHTML =
            '<option value="">Seleccione un terreno</option>';

        contexto.terrenos.forEach((terreno, indice) => {
            const latitud = Number(terreno?.latitud);
            const longitud = Number(terreno?.longitud);
            if (!coordenadaValida(latitud, longitud)) return;

            const opcion = document.createElement("option");
            opcion.value = String(indice);
            opcion.textContent = codigoTerreno(terreno) || `Terreno ${indice + 1}`;
            selector.appendChild(opcion);

            if (codigoSeleccionado &&
                codigoTerreno(terreno) === codigoSeleccionado) {
                selector.value = String(indice);
                contexto.terrenoSeleccionado = terreno;
            }
        });

        if (selector.value === "" && codigoSeleccionado) {
            contexto.terrenoSeleccionado = null;
        }
    }

    function seleccionarTerreno(contexto, terreno) {
        contexto.terrenoSeleccionado = terreno;
        const selector = contexto.controlElemento
            ?.querySelector("[data-geology-terrain]");

        if (selector) {
            const indice = contexto.terrenos.indexOf(terreno);
            selector.value = indice >= 0 ? String(indice) : "";
        }

        actualizarInformacionTerreno(contexto, true);
    }

    async function actualizarInformacionTerreno(
        contexto,
        cargarDatos = false) {
        const resultado = contexto.controlElemento
            ?.querySelector("[data-geology-result]");
        const terreno = contexto.terrenoSeleccionado;

        if (!resultado) return;

        if (!terreno) {
            resultado.innerHTML = `
                <span class="empty">
                    Seleccione un terreno o abra su marcador.
                </span>`;
            return;
        }

        const latitud = Number(terreno.latitud);
        const longitud = Number(terreno.longitud);

        if (!coordenadaValida(latitud, longitud)) {
            resultado.innerHTML = `
                <span class="empty error">
                    El terreno no posee coordenadas válidas.
                </span>`;
            return;
        }

        if (cargarDatos &&
            (!contexto.datos.fallas || !contexto.datos.volcanes)) {
            resultado.innerHTML = `
                <span class="empty loading">
                    <i class="fa-solid fa-spinner fa-spin"></i>
                    Consultando contexto geológico...
                </span>`;

            const respuestas = await Promise.allSettled([
                asegurarFallas(contexto),
                asegurarVolcanes(contexto)
            ]);

            if (respuestas.every(item => item.status === "rejected")) {
                resultado.innerHTML = `
                    <span class="empty error">
                        No fue posible consultar las fuentes geológicas.
                    </span>`;
                return;
            }

            contexto.distancias.fallas.clear();
            contexto.distancias.volcanes.clear();
            actualizarResumenRadios(contexto);
            actualizarTerrenosEnRadio(contexto);
        }

        const falla = contexto.datos.fallas
            ? distanciaTerrenoCapa(contexto, "fallas", terreno)
            : null;
        const volcan = contexto.datos.volcanes
            ? distanciaTerrenoCapa(contexto, "volcanes", terreno)
            : null;

        resultado.innerHTML = `
            <div class="geo-geology-result-heading">
                <i class="fa-solid fa-location-crosshairs"></i>
                <strong>${escapar(codigoTerreno(terreno) || "Terreno")}</strong>
            </div>
            ${tarjetaDistancia(
                "Falla cartografiada más cercana",
                falla?.nombre,
                falla?.distanciaKm,
                "fa-solid fa-wave-square",
                contexto.radios.fallas,
                "fallas")}
            ${tarjetaDistancia(
                "Volcán más cercano",
                volcan?.nombre,
                volcan?.distanciaKm,
                "fa-solid fa-volcano",
                contexto.radios.volcanes,
                "volcanes")}
            <small class="geo-geology-distance-note">
                Distancias aproximadas en línea recta desde la coordenada
                registrada del terreno. La distancia a falla se calcula hasta
                el tramo cartografiado más próximo. Los radios son únicamente
                herramientas de proximidad visual.
            </small>`;
    }

    function tarjetaDistancia(
        etiqueta,
        nombre,
        distancia,
        icono,
        radioKm,
        clave) {
        if (!nombre || !Number.isFinite(distancia)) {
            return `
                <div class="geo-geology-distance unavailable">
                    <i class="${icono}"></i>
                    <span>
                        <small>${escapar(etiqueta)}</small>
                        <strong>Dato no disponible</strong>
                    </span>
                </div>`;
        }

        const dentro = distancia <= radioKm;
        const textoRadio = dentro
            ? `Dentro del radio de ${radioKm} km`
            : `Fuera del radio de ${radioKm} km`;

        return `
            <div class="geo-geology-distance ${clave}">
                <i class="${icono}"></i>
                <span>
                    <small>${escapar(etiqueta)}</small>
                    <strong>${escapar(nombre)}</strong>
                    <em class="geo-geology-radius-badge ${
                        dentro ? "inside" : "outside"}">
                        ${textoRadio}
                    </em>
                </span>
                <b>${formatearDistancia(distancia)}</b>
            </div>`;
    }

    function fallaMasCercana(latitud, longitud, geoJson) {
        let mejor = null;

        (geoJson?.features || []).forEach(feature => {
            const lineas = extraerLineas(feature?.geometry);

            lineas.forEach(linea => {
                for (let indice = 1; indice < linea.length; indice += 1) {
                    const inicio = linea[indice - 1];
                    const fin = linea[indice];
                    const distancia = distanciaPuntoSegmentoKm(
                        latitud,
                        longitud,
                        Number(inicio[1]),
                        Number(inicio[0]),
                        Number(fin[1]),
                        Number(fin[0]));

                    if (!Number.isFinite(distancia)) continue;

                    if (!mejor || distancia < mejor.distanciaKm) {
                        mejor = {
                            distanciaKm: distancia,
                            nombre: valorPropiedad(
                                feature?.properties || {},
                                ["name", "fs_name"],
                                "Falla cartografiada")
                        };
                    }
                }
            });
        });

        return mejor;
    }

    function volcanMasCercano(latitud, longitud, geoJson) {
        let mejor = null;

        (geoJson?.features || []).forEach(feature => {
            const coordenadas = feature?.geometry?.coordinates;
            if (!Array.isArray(coordenadas) || coordenadas.length < 2) return;

            const distancia = haversineKm(
                latitud,
                longitud,
                Number(coordenadas[1]),
                Number(coordenadas[0]));

            if (!Number.isFinite(distancia)) return;

            if (!mejor || distancia < mejor.distanciaKm) {
                mejor = {
                    distanciaKm: distancia,
                    nombre: nombreVolcan(feature?.properties || {})
                };
            }
        });

        return mejor;
    }

    function distanciaPuntoSegmentoKm(
        latitud,
        longitud,
        latitudA,
        longitudA,
        latitudB,
        longitudB) {
        if (![latitud, longitud, latitudA, longitudA, latitudB, longitudB]
            .every(Number.isFinite)) {
            return Number.NaN;
        }

        const radianes = Math.PI / 180;
        const coseno = Math.cos(latitud * radianes);
        const escalaX = 111.320 * coseno;
        const escalaY = 110.574;

        const ax = (longitudA - longitud) * escalaX;
        const ay = (latitudA - latitud) * escalaY;
        const bx = (longitudB - longitud) * escalaX;
        const by = (latitudB - latitud) * escalaY;
        const dx = bx - ax;
        const dy = by - ay;
        const longitudCuadrada = dx * dx + dy * dy;

        if (longitudCuadrada === 0) {
            return Math.sqrt(ax * ax + ay * ay);
        }

        const t = Math.max(
            0,
            Math.min(1, -(ax * dx + ay * dy) / longitudCuadrada));
        const px = ax + t * dx;
        const py = ay + t * dy;

        return Math.sqrt(px * px + py * py);
    }

    function haversineKm(latitudA, longitudA, latitudB, longitudB) {
        if (![latitudA, longitudA, latitudB, longitudB]
            .every(Number.isFinite)) {
            return Number.NaN;
        }

        const radio = 6371.0088;
        const radianes = Math.PI / 180;
        const dLatitud = (latitudB - latitudA) * radianes;
        const dLongitud = (longitudB - longitudA) * radianes;
        const origen = latitudA * radianes;
        const destino = latitudB * radianes;
        const senoLatitud = Math.sin(dLatitud / 2);
        const senoLongitud = Math.sin(dLongitud / 2);
        const a = senoLatitud * senoLatitud +
            Math.cos(origen) * Math.cos(destino) *
            senoLongitud * senoLongitud;

        return 2 * radio * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function extraerLineas(geometria) {
        if (!geometria) return [];
        if (geometria.type === "LineString") {
            return [geometria.coordinates || []];
        }
        if (geometria.type === "MultiLineString") {
            return geometria.coordinates || [];
        }
        return [];
    }

    function geometriaIntersectaLimites(geometria) {
        return extraerLineas(geometria).some(linea => {
            const coordenadas = (linea || [])
                .map(item => [Number(item?.[0]), Number(item?.[1])])
                .filter(item => Number.isFinite(item[0]) &&
                    Number.isFinite(item[1]));

            if (coordenadas.length === 0) return false;

            const longitudes = coordenadas.map(item => item[0]);
            const latitudes = coordenadas.map(item => item[1]);
            const oeste = Math.min(...longitudes);
            const este = Math.max(...longitudes);
            const sur = Math.min(...latitudes);
            const norte = Math.max(...latitudes);

            return este >= LIMITES_NICARAGUA.oeste &&
                oeste <= LIMITES_NICARAGUA.este &&
                norte >= LIMITES_NICARAGUA.sur &&
                sur <= LIMITES_NICARAGUA.norte;
        });
    }

    function coordenadaEnLimites(longitud, latitud) {
        return Number.isFinite(longitud) &&
            Number.isFinite(latitud) &&
            longitud >= LIMITES_NICARAGUA.oeste &&
            longitud <= LIMITES_NICARAGUA.este &&
            latitud >= LIMITES_NICARAGUA.sur &&
            latitud <= LIMITES_NICARAGUA.norte;
    }

    function puntoDentroGeoJson(longitud, latitud, geoJson) {
        return (geoJson?.features || []).some(feature =>
            puntoDentroGeometria(longitud, latitud, feature?.geometry));
    }

    function puntoDentroGeometria(longitud, latitud, geometria) {
        if (!geometria) return false;

        if (geometria.type === "Polygon") {
            return puntoDentroPoligono(longitud, latitud, geometria.coordinates);
        }

        if (geometria.type === "MultiPolygon") {
            return (geometria.coordinates || []).some(poligono =>
                puntoDentroPoligono(longitud, latitud, poligono));
        }

        return false;
    }

    function puntoDentroPoligono(longitud, latitud, anillos) {
        if (!Array.isArray(anillos) || anillos.length === 0) return false;
        if (!puntoDentroAnillo(longitud, latitud, anillos[0])) return false;

        for (let indice = 1; indice < anillos.length; indice += 1) {
            if (puntoDentroAnillo(longitud, latitud, anillos[indice])) {
                return false;
            }
        }

        return true;
    }

    function puntoDentroAnillo(longitud, latitud, anillo) {
        if (!Array.isArray(anillo) || anillo.length < 3) return false;

        let dentro = false;

        for (let actual = 0, anterior = anillo.length - 1;
             actual < anillo.length;
             anterior = actual++) {
            const xActual = Number(anillo[actual]?.[0]);
            const yActual = Number(anillo[actual]?.[1]);
            const xAnterior = Number(anillo[anterior]?.[0]);
            const yAnterior = Number(anillo[anterior]?.[1]);

            const intersecta =
                (yActual > latitud) !== (yAnterior > latitud) &&
                longitud <
                    ((xAnterior - xActual) * (latitud - yActual)) /
                    ((yAnterior - yActual) || Number.EPSILON) + xActual;

            if (intersecta) dentro = !dentro;
        }

        return dentro;
    }

    function crearVolcanesRespaldo() {
        const datos = [
            ["Cosigüina", -87.57, 12.98, 872, "Composite", "HOLOCENO", "1859", false],
            ["San Cristóbal", -87.004, 12.702, 1745, "Composite", "HOLOCENO", "2024", false],
            ["Telica", -86.845, 12.602, 1061, "Composite", "HOLOCENO", "2025", false],
            ["Cerro Negro", -86.702, 12.506, 728, "Minor", "HOLOCENO", "1999", false],
            ["Momotombo", -86.540, 12.423, 1297, "Composite", "HOLOCENO", "2016", false],
            ["Apoyeque", -86.342, 12.242, 518, "Caldera", "HOLOCENO", "50 BCE", false],
            ["Nejapa-Miraflores", -86.320, 12.120, 360, "Cluster", "HOLOCENO", "1060", false],
            ["Masaya", -86.1688, 11.9844, 594, "Caldera", "HOLOCENO", "2026", true],
            ["Mombacho", -85.968, 11.826, 1344, "Composite", "HOLOCENO", "Unknown - Unrest / Holocene", false],
            ["Zapatera", -85.820, 11.730, 629, "Shield", "HOLOCENO", "Unknown - Evidence Credible", false],
            ["Concepción", -85.622, 11.538, 1610, "Composite", "HOLOCENO", "2024", false],
            ["Maderas", -85.515, 11.446, 1394, "Composite", "PLEISTOCENO", "Eruptions not Recorded", false]
        ];

        return {
            type: "FeatureCollection",
            name: "volcanes_nicaragua_respaldo",
            features: datos.map(item => {
                const propiedades = {
                    NAME: item[0],
                    ELEV: item[3],
                    TYPE: item[4],
                    STATUS: item[6],
                    __conatradecEra: item[5],
                    __conatradecFuente:
                        "Catálogo local de respaldo basado en Smithsonian GVP"
                };

                propiedades.__conatradecResumen = {
                    era: item[5],
                    cantidadErupciones: 0,
                    ultimaErupcionTexto: item[6],
                    ultimaErupcionAnio: extraerAnioComparable(item[6]),
                    veiMaximo: null,
                    enCurso: item[7],
                    reciente:
                        extraerAnioComparable(item[6]) >=
                        new Date().getFullYear() - 20,
                    fuente: propiedades.__conatradecFuente
                };

                return {
                    type: "Feature",
                    properties: propiedades,
                    geometry: {
                        type: "Point",
                        coordinates: [item[1], item[2]]
                    }
                };
            })
        };
    }

    function clavesRelacionVolcan(propiedades) {
        const numero = valorPropiedad(
            propiedades,
            ["Volcano Number", "Volcano_Number", "volcano_nu"],
            "");
        const nombre = nombreVolcan(propiedades);
        const claves = [];

        if (numero) claves.push(`numero:${normalizarTextoClave(numero)}`);
        if (nombre && nombre !== "Volcán") {
            claves.push(`nombre:${normalizarTextoClave(nombre)}`);
        }

        return claves;
    }

    function construirResumenVolcan(propiedades, erupciones) {
        const fuente = valorPropiedad(
            propiedades,
            ["__conatradecFuente"],
            "Smithsonian GVP WFS");
        const datosEruptivosDisponibles =
            propiedades?.__conatradecDatosEruptivosDisponibles !== false &&
            /Smithsonian GVP/i.test(fuente);
        const normalizadas = datosEruptivosDisponibles
            ? (erupciones || []).map(normalizarErupcion)
            : [];
        const era = datosEruptivosDisponibles
            ? valorPropiedad(
                propiedades,
                ["__conatradecEra", "Geological Epoch", "geological"],
                "SIN_CLASIFICAR").toUpperCase()
            : "SIN_CLASIFICAR";
        const ultimaRegistrada = normalizadas
            .filter(item => Number.isFinite(item.anioOrden))
            .sort((a, b) => b.anioOrden - a.anioOrden)[0] || null;
        const ultimaPropiedad = datosEruptivosDisponibles
            ? valorPropiedad(
                propiedades,
                [
                    "Last Eruption Year",
                    "Last_Eruption_Year",
                    "last_erupt",
                    "Last Eruption"
                ],
                "")
            : "";
        const ultimaTexto = ultimaRegistrada?.inicioTexto || ultimaPropiedad;
        const ultimaAnio = ultimaRegistrada?.anioOrden ??
            extraerAnioComparable(ultimaPropiedad);
        const veiDisponibles = normalizadas
            .map(item => item.vei)
            .filter(Number.isFinite);
        const enCurso = datosEruptivosDisponibles &&
            (normalizadas.some(item => item.enCurso) ||
             Boolean(propiedades?.__conatradecEnCurso));
        const anioActual = new Date().getFullYear();

        return {
            datosEruptivosDisponibles,
            era,
            cantidadErupciones: datosEruptivosDisponibles
                ? normalizadas.length
                : null,
            ultimaErupcionTexto: datosEruptivosDisponibles
                ? (ultimaTexto || "Sin dato disponible")
                : null,
            ultimaErupcionAnio: datosEruptivosDisponibles &&
                Number.isFinite(ultimaAnio)
                    ? ultimaAnio
                    : null,
            veiMaximo: datosEruptivosDisponibles &&
                veiDisponibles.length > 0
                    ? Math.max(...veiDisponibles)
                    : null,
            enCurso,
            reciente: datosEruptivosDisponibles &&
                Number.isFinite(ultimaAnio) &&
                ultimaAnio >= anioActual - 20,
            fuente
        };
    }

    function obtenerResumenVolcan(propiedades) {
        return propiedades?.__conatradecResumen ||
            construirResumenVolcan(
                propiedades || {},
                propiedades?.__conatradecErupciones || []);
    }

    function normalizarErupcion(propiedades) {
        const inicioTexto = construirFechaErupcion(propiedades, "inicio");
        const finTexto = construirFechaErupcion(propiedades, "fin");
        const certeza = valorPropiedad(
            propiedades,
            [
                "Eruption Category",
                "Eruption_Category",
                "Start Evidence",
                "startevide",
                "activity_t"
            ],
            "Sin dato");
        const vei = numeroPropiedad(
            propiedades,
            ["VEI", "Explosivity Index Max", "explosivit"]);
        const textoCompleto = Object.values(propiedades || {})
            .filter(valor => valor !== null && valor !== undefined)
            .join(" ");
        const enCurso = /continuing|ongoing|in progress|current eruption|en curso|continua/i
            .test(textoCompleto);

        return {
            inicioTexto,
            finTexto,
            certeza,
            vei,
            enCurso,
            anioOrden: extraerAnioComparable(inicioTexto)
        };
    }

    function construirFechaErupcion(propiedades, tipo) {
        const inicio = tipo === "inicio";
        const anio = valorPropiedad(
            propiedades,
            inicio
                ? ["Start Date Year", "Start_Date_Year", "startdatey"]
                : ["End Date Year", "End_Date_Year", "enddateyea"],
            "");
        const mes = numeroPropiedad(
            propiedades,
            inicio
                ? ["Start Date Month", "Start_Date_Month", "startdatem"]
                : ["End Date Month", "End_Date_Month", "enddatemon"]);
        const dia = valorPropiedad(
            propiedades,
            inicio
                ? ["Start Date Day", "Start_Date_Day", "startdated"]
                : ["End Date Day", "End_Date_Day", "enddateday"],
            "");

        if (!anio) return tipo === "fin" ? "Desconocido" : "Sin fecha";

        const partes = [formatearAnioVolcanico(anio)];
        if (mes !== null && mes >= 1 && mes <= 12) {
            partes.push(nombreMes(mes));
        }
        if (dia && Number(dia) > 0) partes.push(String(Number(dia)));

        return partes.join(" ");
    }

    function construirBanderasVolcanHtml(resumen) {
        if (!resumen.datosEruptivosDisponibles) {
            return `
                <div class="geo-volcano-flags">
                    <span class="geo-volcano-flag neutral">
                        Información eruptiva no disponible
                    </span>
                </div>`;
        }

        const banderas = [];
        const claseEra = resumen.era === "HOLOCENO"
            ? "holocene"
            : resumen.era === "PLEISTOCENO"
                ? "pleistocene"
                : "neutral";

        if (resumen.era !== "SIN_CLASIFICAR") {
            banderas.push(
                `<span class="geo-volcano-flag ${claseEra}">${
                    escapar(etiquetaEraVolcan(resumen.era))}</span>`);
        }

        banderas.push(resumen.cantidadErupciones > 0
            ? `<span class="geo-volcano-flag recorded">
                   ${resumen.cantidadErupciones} erupción${
                       resumen.cantidadErupciones === 1 ? "" : "es"} registrada${
                       resumen.cantidadErupciones === 1 ? "" : "s"}
               </span>`
            : `<span class="geo-volcano-flag no-record">
                   Sin erupciones holocenas registradas
               </span>`);

        if (resumen.enCurso) {
            banderas.push(
                `<span class="geo-volcano-flag ongoing">
                    <i class="fa-solid fa-circle-radiation"></i>
                    Registro continuo en GVP
                 </span>`);
        } else if (resumen.reciente) {
            banderas.push(
                `<span class="geo-volcano-flag recent">
                    Registro en los últimos 20 años
                 </span>`);
        }

        return `<div class="geo-volcano-flags">${banderas.join("")}</div>`;
    }

    function construirHistorialErupcionesHtml(erupciones, disponible = true) {
        if (!disponible) return "";
        const registros = Array.isArray(erupciones)
            ? erupciones.slice(0, 4)
            : [];

        if (registros.length === 0) {
            return `
                <div class="geo-volcano-history empty">
                    <strong>Historial eruptivo</strong>
                    <span>No hay erupciones holocenas asociadas en el conjunto consultado.</span>
                </div>`;
        }

        const filas = registros.map(item => `
            <li>
                <span>
                    <b>${escapar(item.inicioTexto)}</b>
                    <small>hasta ${escapar(item.finTexto)}</small>
                </span>
                <span>
                    ${item.vei === null ? "VEI —" : `VEI ${escapar(String(item.vei))}`}
                    <small>${escapar(item.certeza)}</small>
                </span>
            </li>`).join("");

        return `
            <div class="geo-volcano-history">
                <strong>Erupciones más recientes</strong>
                <ul>${filas}</ul>
            </div>`;
    }

    function etiquetaEraVolcan(era) {
        if (era === "HOLOCENO") return "Holoceno";
        if (era === "PLEISTOCENO") return "Pleistoceno";
        return "Sin clasificación temporal";
    }

    function formatearAnioVolcanico(valor) {
        const texto = String(valor ?? "").trim();
        const numero = Number(texto);

        if (Number.isFinite(numero)) {
            return numero < 0
                ? `${Math.abs(numero)} a. C.`
                : String(numero);
        }

        return texto;
    }

    function extraerAnioComparable(valor) {
        const texto = String(valor ?? "").trim();
        if (!texto) return Number.NEGATIVE_INFINITY;

        const coincidencia = texto.match(/-?\d{1,4}/);
        if (!coincidencia) return Number.NEGATIVE_INFINITY;

        let anio = Number(coincidencia[0]);
        if (/BCE|BC|a\.\s*C\./i.test(texto) && anio > 0) anio *= -1;
        return Number.isFinite(anio) ? anio : Number.NEGATIVE_INFINITY;
    }

    function nombreMes(mes) {
        return [
            "", "ene", "feb", "mar", "abr", "may", "jun",
            "jul", "ago", "sep", "oct", "nov", "dic"
        ][mes] || "";
    }

    function buscarTerreno(contexto, codigo, latitud, longitud) {
        const porCodigo = contexto.terrenos.find(terreno =>
            codigo && codigoTerreno(terreno) === codigo);
        if (porCodigo) return porCodigo;

        return contexto.terrenos.find(terreno =>
            Math.abs(Number(terreno.latitud) - latitud) < 0.000001 &&
            Math.abs(Number(terreno.longitud) - longitud) < 0.000001) || null;
    }

    function codigoTerreno(terreno) {
        return String(
            terreno?.codigo ||
            terreno?.nombre ||
            terreno?.Codigo ||
            terreno?.Nombre ||
            "").trim();
    }

    function nombreVolcan(propiedades) {
        return valorPropiedad(
            propiedades,
            [
                "Volcano Name",
                "Volcano_Name",
                "Volcano_Name_",
                "volcano_na",
                "NAME",
                "Name"
            ],
            "Volcán");
    }

    function valorPropiedad(propiedades, claves, respaldo) {
        const entradas = Object.entries(propiedades || {});
        const indice = new Map(
            entradas.map(([clave, valor]) => [normalizarTextoClave(clave), valor]));

        for (const clave of claves) {
            let valor = propiedades?.[clave];

            if (valor === null || valor === undefined || !String(valor).trim()) {
                valor = indice.get(normalizarTextoClave(clave));
            }

            if (valor !== null && valor !== undefined && String(valor).trim()) {
                return String(valor).trim();
            }
        }

        return respaldo;
    }

    function numeroPropiedad(propiedades, claves) {
        const valorTexto = valorPropiedad(propiedades, claves, "");
        if (!valorTexto) return null;

        const valor = Number(String(valorTexto).replace(",", "."));
        return Number.isFinite(valor) ? valor : null;
    }

    function normalizarTextoClave(valor) {
        return String(valor ?? "")
            .normalize("NFD")
            .replace(/[̀-ͯ]/g, "")
            .toLowerCase()
            .replace(/[^a-z0-9]/g, "");
    }

    function etiquetaCapa(clave) {
        return clave === "fallas"
            ? "las fallas geológicas"
            : "los volcanes";
    }

    function sincronizarCheckbox(contexto, clave, activa) {
        const entrada = contexto.controlElemento
            ?.querySelector(`[data-geology-layer="${clave}"]`);
        if (entrada) entrada.checked = activa;
    }

    function actualizarEstado(contexto, mensaje, error = false) {
        const estado = contexto.controlElemento
            ?.querySelector("[data-geology-status]");
        if (!estado) return;

        estado.textContent = mensaje;
        estado.classList.toggle("error", error);
        estado.classList.toggle("visible", Boolean(mensaje));
    }

    function formatearDistancia(distancia) {
        if (distancia < 1) {
            return `${Math.round(distancia * 1000)} m`;
        }

        return `${distancia.toLocaleString("es-NI", {
            minimumFractionDigits: distancia < 10 ? 1 : 0,
            maximumFractionDigits: distancia < 10 ? 1 : 0
        })} km`;
    }

    function coordenadaValida(latitud, longitud) {
        return Number.isFinite(latitud) &&
            Number.isFinite(longitud) &&
            Math.abs(latitud) <= 90 &&
            Math.abs(longitud) <= 180 &&
            !(latitud === 0 && longitud === 0);
    }

    function escapar(valor) {
        return String(valor ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }


    async function prepararResumenTerritorial(elementId) {
        const contexto = contextos.get(elementId);
        if (!contexto) return false;

        const resultados = await Promise.allSettled([
            asegurarFallas(contexto),
            asegurarVolcanes(contexto)
        ]);

        return resultados.some(item => item.status === "fulfilled");
    }

    function obtenerResumenTerritorial(elementId, territorio) {
        const contexto = contextos.get(elementId);

        if (!contexto || !territorio?.geometry) {
            return {
                disponible: false,
                mensaje: "El territorio no posee una geometría válida."
            };
        }

        const fallasDisponibles = Array.isArray(
            contexto.datos.fallas?.features);
        const volcanesDisponibles = Array.isArray(
            contexto.datos.volcanes?.features);

        if (!fallasDisponibles && !volcanesDisponibles) {
            return {
                disponible: false,
                mensaje: "Las fuentes geológicas todavía no están cargadas."
            };
        }

        const fallas = fallasDisponibles
            ? contexto.datos.fallas.features.filter(item =>
                intersectaTerritorio(item, territorio))
            : [];

        const volcanes = volcanesDisponibles
            ? contexto.datos.volcanes.features.filter(item =>
                puntoDentroTerritorio(item, territorio))
            : [];

        return {
            disponible: true,
            fallasDisponibles,
            volcanesDisponibles,
            totalFallas: fallas.length,
            totalVolcanes: volcanes.length,
            fallas: fallas
                .map(item => valorPropiedad(
                    item?.properties,
                    ["name", "Name", "fs_name"],
                    "Falla sin nombre"))
                .filter((item, indice, lista) =>
                    lista.indexOf(item) === indice)
                .slice(0, 6),
            volcanes: volcanes
                .map(item => nombreVolcan(item?.properties || {}))
                .filter((item, indice, lista) =>
                    lista.indexOf(item) === indice)
                .slice(0, 6),
            fuenteFallas: fallasDisponibles
                ? "GEM CCAF-DB"
                : "No disponible",
            fuenteVolcanes: volcanesDisponibles
                ? fuenteVolcanesResumen(volcanes)
                : "No disponible"
        };
    }

    function intersectaTerritorio(elemento, territorio) {
        if (!window.turf || !elemento?.geometry) return false;

        try {
            if (typeof window.turf.booleanIntersects === "function") {
                return window.turf.booleanIntersects(elemento, territorio);
            }

            const intersecciones = window.turf.lineIntersect(
                elemento,
                territorio);

            if (intersecciones?.features?.length > 0) return true;

            const coordenadas = coordenadasLineales(elemento.geometry);
            return coordenadas.some(coordenada =>
                window.turf.booleanPointInPolygon(
                    window.turf.point(coordenada),
                    territorio));
        } catch {
            return false;
        }
    }

    function puntoDentroTerritorio(elemento, territorio) {
        if (!window.turf || elemento?.geometry?.type !== "Point") {
            return false;
        }

        try {
            return window.turf.booleanPointInPolygon(elemento, territorio);
        } catch {
            return false;
        }
    }

    function coordenadasLineales(geometria) {
        if (geometria?.type === "LineString") {
            return Array.isArray(geometria.coordinates)
                ? geometria.coordinates
                : [];
        }

        if (geometria?.type === "MultiLineString") {
            return (geometria.coordinates || []).flat();
        }

        return [];
    }

    function fuenteVolcanesResumen(volcanes) {
        const fuentes = volcanes
            .map(item => valorPropiedad(
                item?.properties,
                ["__conatradecFuente"],
                ""))
            .filter(Boolean);

        if (fuentes.some(item =>
            item.toLowerCase().includes("smithsonian"))) {
            return "Smithsonian GVP";
        }

        return fuentes[0] || "Catálogo volcánico disponible";
    }

    api.__conatradecGeologiaAplicada = true;

    return {
        inicializado: true,
        version: VERSION,
        prepararResumenTerritorial,
        obtenerResumenTerritorial,
        recargar(elementId) {
            const contexto = contextos.get(elementId);
            if (!contexto) return;

            fallasPromise = null;
            volcanesPromise = null;
            contexto.datos.fallas = null;
            contexto.datos.volcanes = null;
            contexto.distancias.fallas.clear();
            contexto.distancias.volcanes.clear();
            contexto.capas.fallas?.remove();
            contexto.capas.volcanes?.remove();
            contexto.capas.radioFallas?.remove();
            contexto.capas.radioVolcanes?.remove();
            contexto.capas.terrenosRadioFallas?.clearLayers();
            contexto.capas.terrenosRadioVolcanes?.clearLayers();
            contexto.capas.fallas = null;
            contexto.capas.volcanes = null;
            contexto.capas.radioFallas = null;
            contexto.capas.radioVolcanes = null;
            actualizarResumenRadios(contexto);
        }
    };
})();
