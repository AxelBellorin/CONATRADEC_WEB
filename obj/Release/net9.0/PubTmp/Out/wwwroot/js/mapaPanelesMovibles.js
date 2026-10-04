/*
 * CONATRADEC
 * Pantalla completa del Centro Geoespacial con filtros disponibles y
 * paneles movibles.
 */
window.conatradecMapaPanelesMovibles =
    window.conatradecMapaPanelesMovibles || (() => {
        const SELECTOR_WORKSPACE =
            ".geo-workspace";

        const SELECTOR_STAGE =
            ".geo-map-stage";

        const SELECTOR_CONTROL =
            ".geo-control-panel";

        const SELECTOR_DETALLE =
            ".geo-detail-panel";

        const SELECTOR_POPUP =
            ".geo-terrain-leaflet-popup";

        const posiciones = {
            control: {
                x: 0,
                y: 0
            },
            detalle: {
                x: 0,
                y: 0
            }
        };

        const mapasLeaflet =
            new Map();

        const observadoresTamano =
            new Map();

        let arrastreActivo = null;
        let observador = null;
        let actualizacionPaginaPendiente = false;

        function instalar() {
            registrarMapasLeaflet();
            envolverPantallaCompleta();
            observarPaneles();
            prepararExistentes();
            programarActualizacionPagina();

            document.addEventListener(
                "fullscreenchange",
                pantallaCompletaCambiada);

            window.addEventListener(
                "resize",
                () => {
                    limitarPanelesVisibles();

                    requestAnimationFrame(
                        invalidarMapas);
                },
                {
                    passive: true
                });
        }

        function registrarMapasLeaflet() {
            if (typeof L === "undefined" ||
                L.__conatradecMapasResponsivos) {
                return;
            }

            const crearMapaOriginal =
                L.map;

            L.map = function (
                elemento,
                opciones) {
                const mapa =
                    crearMapaOriginal.call(
                        this,
                        elemento,
                        opciones);

                const contenedor =
                    mapa.getContainer?.();

                const elementId =
                    typeof elemento === "string"
                        ? elemento
                        : contenedor?.id;

                if (elementId) {
                    mapasLeaflet.set(
                        elementId,
                        mapa);

                    observarTamanoMapa(
                        elementId,
                        mapa);

                    mapa.once(
                        "unload",
                        () => {
                            observadoresTamano
                                .get(elementId)
                                ?.disconnect();

                            observadoresTamano
                                .delete(elementId);

                            mapasLeaflet
                                .delete(elementId);
                        });
                }

                return mapa;
            };

            L.__conatradecMapasResponsivos = true;
        }

        function observarTamanoMapa(
            elementId,
            mapa) {
            observadoresTamano
                .get(elementId)
                ?.disconnect();

            if (typeof ResizeObserver !==
                "function") {
                return;
            }

            const contenedor =
                mapa.getContainer?.();

            const objetivo =
                contenedor?.closest(
                    SELECTOR_WORKSPACE) ??
                contenedor;

            if (!objetivo)
                return;

            let frame = 0;

            const resizeObserver =
                new ResizeObserver(() => {
                    cancelAnimationFrame(frame);

                    frame =
                        requestAnimationFrame(() => {
                            try {
                                mapa.invalidateSize({
                                    animate: false,
                                    pan: false
                                });
                            } catch {
                            }
                        });
                });

            resizeObserver.observe(
                objetivo);

            if (contenedor &&
                contenedor !== objetivo) {
                resizeObserver.observe(
                    contenedor);
            }

            observadoresTamano.set(
                elementId,
                resizeObserver);
        }

        function programarActualizacionPagina() {
            if (actualizacionPaginaPendiente)
                return;

            actualizacionPaginaPendiente = true;

            requestAnimationFrame(() => {
                actualizacionPaginaPendiente = false;
                actualizarClasePagina();
            });
        }

        function actualizarClasePagina() {
            const activa =
                Boolean(
                    document.querySelector(
                        SELECTOR_WORKSPACE));

            document.body.classList.toggle(
                "geo-map-page-active",
                activa);

            if (!activa) {
                document.body.classList.remove(
                    "geo-workspace-fullscreen-active");
            }
        }

        function invalidarMapas() {
            mapasLeaflet.forEach(mapa => {
                try {
                    mapa.invalidateSize({
                        animate: false,
                        pan: false
                    });
                } catch {
                }
            });
        }

        function envolverPantallaCompleta() {
            const modulo =
                window.conatradecMapaInteligente;

            if (!modulo ||
                modulo.__pantallaCompletaConFiltros) {
                return;
            }

            modulo.alternarPantallaCompleta =
                async function (elementId) {
                    const mapa =
                        document.getElementById(elementId);

                    const workspace =
                        mapa?.closest(
                            SELECTOR_WORKSPACE);

                    if (!workspace)
                        return;

                    try {
                        if (!document.fullscreenElement) {
                            await workspace
                                .requestFullscreen();
                        } else {
                            await document
                                .exitFullscreen();
                        }
                    } catch (error) {
                        console.warn(
                            "No fue posible cambiar a pantalla completa.",
                            error);
                    }
                };

            modulo.__pantallaCompletaConFiltros = true;
        }

        function pantallaCompletaCambiada() {
            const workspace =
                document.fullscreenElement
                    ?.matches?.(SELECTOR_WORKSPACE)
                    ? document.fullscreenElement
                    : null;

            document.body.classList.toggle(
                "geo-workspace-fullscreen-active",
                Boolean(workspace));

            document
                .querySelectorAll(
                    SELECTOR_WORKSPACE)
                .forEach(item => {
                    item.classList.toggle(
                        "geo-fullscreen-workspace",
                        item === workspace);
                });

            if (workspace) {
                prepararPanelControl(
                    workspace.querySelector(
                        SELECTOR_CONTROL));
            }

            limitarPanelesVisibles();

            setTimeout(() => {
                window.dispatchEvent(
                    new Event("resize"));

                invalidarMapas();

                document
                    .querySelectorAll(
                        ".geo-national-map")
                    .forEach(elemento => {
                        elemento.dispatchEvent(
                            new Event(
                                "resize",
                                {
                                    bubbles: true
                                }));
                    });
            }, 180);
        }

        function observarPaneles() {
            if (observador)
                return;

            observador =
                new MutationObserver(
                    mutaciones => {
                        for (const mutacion of mutaciones) {
                            for (const nodo of mutacion.addedNodes) {
                                if (!(nodo instanceof Element))
                                    continue;

                                prepararNodo(nodo);
                            }
                        }

                        programarActualizacionPagina();
                    });

            observador.observe(
                document.body,
                {
                    childList: true,
                    subtree: true
                });
        }

        function prepararExistentes() {
            document
                .querySelectorAll(
                    SELECTOR_CONTROL)
                .forEach(
                    prepararPanelControl);

            document
                .querySelectorAll(
                    SELECTOR_DETALLE)
                .forEach(
                    prepararPanelDetalle);

            document
                .querySelectorAll(
                    SELECTOR_POPUP)
                .forEach(
                    prepararPopup);
        }

        function prepararNodo(nodo) {
            if (nodo.matches?.(
                    SELECTOR_CONTROL)) {
                prepararPanelControl(nodo);
            }

            if (nodo.matches?.(
                    SELECTOR_DETALLE)) {
                prepararPanelDetalle(nodo);
            }

            if (nodo.matches?.(
                    SELECTOR_POPUP)) {
                prepararPopup(nodo);
            }

            nodo.querySelectorAll?.(
                SELECTOR_CONTROL)
                .forEach(
                    prepararPanelControl);

            nodo.querySelectorAll?.(
                SELECTOR_DETALLE)
                .forEach(
                    prepararPanelDetalle);

            nodo.querySelectorAll?.(
                SELECTOR_POPUP)
                .forEach(
                    prepararPopup);
        }

        function prepararPanelControl(panel) {
            if (!panel ||
                panel.dataset.geoMovible === "true") {
                return;
            }

            const encabezado =
                panel.querySelector(
                    ".geo-panel-heading");

            if (!encabezado)
                return;

            panel.dataset.geoMovible = "true";
            panel.classList.add(
                "geo-movable-control-panel");

            agregarBotonMinimizar(
                panel,
                encabezado,
                "control");

            prepararEncabezado(
                encabezado,
                panel,
                "control",
                () =>
                    Boolean(
                        panel.closest(
                            ".geo-workspace:fullscreen, " +
                            ".geo-workspace.geo-fullscreen-workspace")));
        }

        function prepararPanelDetalle(panel) {
            if (!panel ||
                panel.dataset.geoMovible === "true") {
                return;
            }

            const encabezado =
                panel.querySelector(
                    ".geo-detail-heading");

            if (!encabezado)
                return;

            panel.dataset.geoMovible = "true";
            panel.classList.add(
                "geo-movable-detail-panel");

            agregarBotonMinimizar(
                panel,
                encabezado,
                "detalle");

            aplicarPosicion(
                panel,
                posiciones.detalle);

            prepararEncabezado(
                encabezado,
                panel,
                "detalle",
                () => true);
        }

        function agregarBotonMinimizar(
            panel,
            encabezado,
            tipo) {
            if (encabezado.querySelector(
                    ".geo-panel-minimize-button")) {
                return;
            }

            const boton =
                document.createElement("button");

            boton.type = "button";
            boton.className =
                "geo-icon-button " +
                "geo-panel-minimize-button";

            boton.dataset.panelTipo =
                tipo;

            boton.setAttribute(
                "aria-expanded",
                "true");

            actualizarBotonMinimizar(
                boton,
                false);

            /*
             * En los encabezados que ya tienen acciones, insertar antes
             * del botón de cerrar. En el panel de filtros se agrega al final.
             */
            const accionesExistentes =
                encabezado.querySelectorAll(
                    "button");

            const botonCerrar =
                [...accionesExistentes]
                    .find(item =>
                        item.querySelector(
                            ".fa-xmark"));

            if (botonCerrar) {
                botonCerrar.before(boton);
            } else {
                encabezado.appendChild(boton);
            }

            boton.addEventListener(
                "click",
                evento => {
                    evento.preventDefault();
                    evento.stopPropagation();

                    alternarMinimizado(
                        panel,
                        boton);
                });
        }

        function alternarMinimizado(
            panel,
            boton) {
            const minimizado =
                panel.classList.toggle(
                    "geo-panel-minimized");

            panel.dataset.geoMinimizado =
                minimizado
                    ? "true"
                    : "false";

            boton.setAttribute(
                "aria-expanded",
                minimizado
                    ? "false"
                    : "true");

            actualizarBotonMinimizar(
                boton,
                minimizado);

            setTimeout(() => {
                limitarPanelesVisibles();

                window.dispatchEvent(
                    new Event("resize"));
            }, 40);
        }

        function actualizarBotonMinimizar(
            boton,
            minimizado) {
            boton.title =
                minimizado
                    ? "Restaurar panel"
                    : "Minimizar panel";

            boton.setAttribute(
                "aria-label",
                boton.title);

            boton.innerHTML =
                minimizado
                    ? '<i class="fa-solid fa-window-maximize"></i>'
                    : '<i class="fa-solid fa-window-minimize"></i>';
        }

        function prepararEncabezado(
            encabezado,
            panel,
            tipo,
            puedeMover) {
            encabezado.classList.add(
                "geo-movable-heading");

            encabezado.title =
                tipo === "control"
                    ? "Arrastre para mover los filtros en pantalla completa. Doble clic para restaurar."
                    : "Arrastre para mover la ficha. Doble clic para restaurar.";

            encabezado.addEventListener(
                "pointerdown",
                evento => {
                    if (!puedeMover() ||
                        ignorarInicio(evento)) {
                        return;
                    }

                    iniciarArrastrePanel(
                        evento,
                        panel,
                        tipo);
                });

            encabezado.addEventListener(
                "dblclick",
                evento => {
                    if (ignorarInicio(evento))
                        return;

                    restaurarPanel(
                        panel,
                        tipo);
                });
        }

        function prepararPopup(popup) {
            if (!popup ||
                popup.dataset.geoMovible === "true") {
                return;
            }

            const wrapper =
                popup.querySelector(
                    ".leaflet-popup-content-wrapper");

            if (!wrapper)
                return;

            popup.dataset.geoMovible = "true";
            popup.classList.add(
                "geo-movable-leaflet-popup");

            const barra =
                document.createElement("div");

            barra.className =
                "geo-popup-toolbar";

            const handle =
                document.createElement("button");

            handle.type = "button";
            handle.className =
                "geo-popup-drag-handle";

            handle.title =
                "Arrastre para mover esta tarjeta. Doble clic para restaurar.";

            handle.innerHTML = `
                <i class="fa-solid fa-grip-lines"></i>
                <span>Mover ficha</span>`;

            const minimizar =
                document.createElement("button");

            minimizar.type = "button";
            minimizar.className =
                "geo-popup-minimize-button";

            actualizarBotonMinimizarPopup(
                minimizar,
                false);

            barra.append(
                handle,
                minimizar);

            wrapper.prepend(barra);

            handle.addEventListener(
                "pointerdown",
                evento =>
                    iniciarArrastrePopup(
                        evento,
                        popup,
                        wrapper));

            handle.addEventListener(
                "dblclick",
                evento => {
                    evento.preventDefault();
                    evento.stopPropagation();
                    restaurarPopup(popup);
                });

            minimizar.addEventListener(
                "click",
                evento => {
                    evento.preventDefault();
                    evento.stopPropagation();

                    const minimizado =
                        popup.classList.toggle(
                            "geo-popup-minimized");

                    actualizarBotonMinimizarPopup(
                        minimizar,
                        minimizado);
                });
        }

        function actualizarBotonMinimizarPopup(
            boton,
            minimizado) {
            boton.title =
                minimizado
                    ? "Restaurar ficha"
                    : "Minimizar ficha";

            boton.setAttribute(
                "aria-label",
                boton.title);

            boton.setAttribute(
                "aria-expanded",
                minimizado
                    ? "false"
                    : "true");

            boton.innerHTML =
                minimizado
                    ? '<i class="fa-solid fa-window-maximize"></i>'
                    : '<i class="fa-solid fa-window-minimize"></i>';
        }

        function ignorarInicio(evento) {
            return Boolean(
                evento.target.closest(
                    "button, a, input, select, textarea, " +
                    "label, summary, [role='button']"));
        }

        function iniciarArrastrePanel(
            evento,
            panel,
            tipo) {
            evento.preventDefault();

            const stage =
                panel.closest(
                    SELECTOR_WORKSPACE) ??
                panel.closest(
                    SELECTOR_STAGE);

            if (!stage)
                return;

            const posicion =
                tipo === "control"
                    ? posiciones.control
                    : posiciones.detalle;

            const rect =
                panel.getBoundingClientRect();

            const limite =
                stage.getBoundingClientRect();

            arrastreActivo = {
                tipo,
                elemento: panel,
                contenedor: stage,
                pointerId: evento.pointerId,
                inicioX: evento.clientX,
                inicioY: evento.clientY,
                origenX: posicion.x,
                origenY: posicion.y,
                rect,
                limite
            };

            panel.setPointerCapture?.(
                evento.pointerId);

            document.body.classList.add(
                "geo-panel-is-dragging");

            window.addEventListener(
                "pointermove",
                moverPanel);

            window.addEventListener(
                "pointerup",
                terminarArrastre,
                {
                    once: true
                });

            window.addEventListener(
                "pointercancel",
                terminarArrastre,
                {
                    once: true
                });
        }

        function moverPanel(evento) {
            const actual =
                arrastreActivo;

            if (!actual ||
                actual.tipo === "popup") {
                return;
            }

            const margen = 8;

            const dx =
                evento.clientX -
                actual.inicioX;

            const dy =
                evento.clientY -
                actual.inicioY;

            const minimoDx =
                actual.limite.left +
                margen -
                actual.rect.left;

            const maximoDx =
                actual.limite.right -
                margen -
                actual.rect.right;

            const minimoDy =
                actual.limite.top +
                margen -
                actual.rect.top;

            const maximoDy =
                actual.limite.bottom -
                margen -
                actual.rect.bottom;

            const nuevoX =
                actual.origenX +
                limitar(
                    dx,
                    minimoDx,
                    maximoDx);

            const nuevoY =
                actual.origenY +
                limitar(
                    dy,
                    minimoDy,
                    maximoDy);

            const posicion =
                actual.tipo === "control"
                    ? posiciones.control
                    : posiciones.detalle;

            posicion.x = nuevoX;
            posicion.y = nuevoY;

            aplicarPosicion(
                actual.elemento,
                posicion);
        }

        function aplicarPosicion(
            panel,
            posicion) {
            panel.style.setProperty(
                "--geo-panel-x",
                `${posicion.x}px`);

            panel.style.setProperty(
                "--geo-panel-y",
                `${posicion.y}px`);
        }

        function restaurarPanel(
            panel,
            tipo) {
            const posicion =
                tipo === "control"
                    ? posiciones.control
                    : posiciones.detalle;

            posicion.x = 0;
            posicion.y = 0;

            aplicarPosicion(
                panel,
                posicion);
        }

        function iniciarArrastrePopup(
            evento,
            popup,
            wrapper) {
            evento.preventDefault();
            evento.stopPropagation();

            const stage =
                popup.closest(
                    SELECTOR_STAGE) ??
                popup.closest(
                    ".leaflet-container");

            if (!stage)
                return;

            const x =
                numeroCss(
                    popup,
                    "--geo-popup-x");

            const y =
                numeroCss(
                    popup,
                    "--geo-popup-y");

            arrastreActivo = {
                tipo: "popup",
                elemento: popup,
                contenedor: stage,
                pointerId: evento.pointerId,
                inicioX: evento.clientX,
                inicioY: evento.clientY,
                origenX: x,
                origenY: y,
                rect:
                    wrapper
                        .getBoundingClientRect(),
                limite:
                    stage
                        .getBoundingClientRect()
            };

            evento.currentTarget
                .setPointerCapture?.(
                    evento.pointerId);

            document.body.classList.add(
                "geo-panel-is-dragging");

            window.addEventListener(
                "pointermove",
                moverPopup);

            window.addEventListener(
                "pointerup",
                terminarArrastre,
                {
                    once: true
                });

            window.addEventListener(
                "pointercancel",
                terminarArrastre,
                {
                    once: true
                });
        }

        function moverPopup(evento) {
            const actual =
                arrastreActivo;

            if (!actual ||
                actual.tipo !== "popup") {
                return;
            }

            const margen = 8;

            const dx =
                evento.clientX -
                actual.inicioX;

            const dy =
                evento.clientY -
                actual.inicioY;

            const minimoDx =
                actual.limite.left +
                margen -
                actual.rect.left;

            const maximoDx =
                actual.limite.right -
                margen -
                actual.rect.right;

            const minimoDy =
                actual.limite.top +
                margen -
                actual.rect.top;

            const maximoDy =
                actual.limite.bottom -
                margen -
                actual.rect.bottom;

            const nuevoX =
                actual.origenX +
                limitar(
                    dx,
                    minimoDx,
                    maximoDx);

            const nuevoY =
                actual.origenY +
                limitar(
                    dy,
                    minimoDy,
                    maximoDy);

            actual.elemento.style
                .setProperty(
                    "--geo-popup-x",
                    `${nuevoX}px`);

            actual.elemento.style
                .setProperty(
                    "--geo-popup-y",
                    `${nuevoY}px`);
        }

        function restaurarPopup(popup) {
            popup.style.setProperty(
                "--geo-popup-x",
                "0px");

            popup.style.setProperty(
                "--geo-popup-y",
                "0px");
        }

        function terminarArrastre() {
            arrastreActivo = null;

            document.body.classList.remove(
                "geo-panel-is-dragging");

            window.removeEventListener(
                "pointermove",
                moverPanel);

            window.removeEventListener(
                "pointermove",
                moverPopup);
        }

        function limitarPanelesVisibles() {
            document
                .querySelectorAll(
                    SELECTOR_DETALLE)
                .forEach(panel => {
                    const stage =
                        panel.closest(
                            SELECTOR_STAGE);

                    if (!stage)
                        return;

                    ajustarDentro(
                        panel,
                        stage,
                        posiciones.detalle);
                });

            const workspace =
                document.querySelector(
                    ".geo-workspace:fullscreen, " +
                    ".geo-workspace.geo-fullscreen-workspace");

            const control =
                workspace?.querySelector(
                    SELECTOR_CONTROL);

            if (workspace && control) {
                ajustarDentro(
                    control,
                    workspace,
                    posiciones.control);
            }
        }

        function ajustarDentro(
            elemento,
            contenedor,
            posicion) {
            const rect =
                elemento.getBoundingClientRect();

            const limite =
                contenedor.getBoundingClientRect();

            const margen = 8;
            let dx = 0;
            let dy = 0;

            if (rect.left <
                limite.left + margen) {
                dx =
                    limite.left +
                    margen -
                    rect.left;
            } else if (rect.right >
                       limite.right - margen) {
                dx =
                    limite.right -
                    margen -
                    rect.right;
            }

            if (rect.top <
                limite.top + margen) {
                dy =
                    limite.top +
                    margen -
                    rect.top;
            } else if (rect.bottom >
                       limite.bottom - margen) {
                dy =
                    limite.bottom -
                    margen -
                    rect.bottom;
            }

            if (dx === 0 && dy === 0)
                return;

            posicion.x += dx;
            posicion.y += dy;

            aplicarPosicion(
                elemento,
                posicion);
        }

        function numeroCss(
            elemento,
            propiedad) {
            const valor =
                getComputedStyle(elemento)
                    .getPropertyValue(propiedad);

            const numero =
                Number.parseFloat(valor);

            return Number.isFinite(numero)
                ? numero
                : 0;
        }

        function limitar(
            valor,
            minimo,
            maximo) {
            if (minimo > maximo)
                return 0;

            return Math.max(
                minimo,
                Math.min(
                    maximo,
                    valor));
        }

        instalar();

        return {
            prepararExistentes,
            limitarPanelesVisibles
        };
    })();
