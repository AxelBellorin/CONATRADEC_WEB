/*
 * CONATRADEC
 * Mapa de relaciones detallado de un análisis de suelo.
 *
 * Incluye expansión y contracción de relaciones internas:
 * - elementos del requerimiento;
 * - fuentes y aportes del balance;
 * - fuentes, aportes y resultados de fertilización mixta.
 */
window.conatradecMapaRelacionesAnalisis =
    window.conatradecMapaRelacionesAnalisis || (() => {
        const version = "1.1.1";
        const instancias = new Map();

        function crear(
            elementId,
            detailElementId,
            payload,
            horizontal = true) {
            destruir(elementId);

            const contenedor =
                document.getElementById(elementId);

            const detalle =
                document.getElementById(detailElementId);

            if (!contenedor) {
                throw new Error(
                    "No se encontró el contenedor del mapa.");
            }

            if (!window.cytoscape) {
                throw new Error(
                    "Cytoscape.js no está disponible.");
            }

            const nodos =
                Array.isArray(payload?.nodos)
                    ? payload.nodos
                    : [];

            const relaciones =
                Array.isArray(payload?.relaciones)
                    ? payload.relaciones
                    : [];

            const elementos = [
                ...nodos.map(nodo => {
                    const expandible =
                        Boolean(nodo.expandible);

                    const etiquetaBase =
                        construirEtiqueta(
                            nodo.titulo,
                            nodo.subtitulo);

                    return {
                        data: {
                            id: String(nodo.id),
                            tipo: String(nodo.tipo || ""),
                            grupo: String(nodo.grupo || ""),
                            titulo: String(nodo.titulo || ""),
                            subtitulo: String(nodo.subtitulo || ""),
                            estado: String(
                                nodo.estado || "NEUTRAL"),
                            padreId: String(
                                nodo.padreId || ""),
                            expandible,
                            expandido: false,
                            ocultoInicial:
                                Boolean(nodo.ocultoInicial),
                            etiquetaBase,
                            etiqueta:
                                etiquetaConEstadoExpansion(
                                    etiquetaBase,
                                    expandible,
                                    false),
                            detalles:
                                nodo.detalles || {}
                        }
                    };
                }),
                ...relaciones.map(relacion => ({
                    data: {
                        id: String(relacion.id),
                        source: String(relacion.origen),
                        target: String(relacion.destino),
                        etiqueta:
                            String(relacion.etiqueta || ""),
                        estado:
                            String(relacion.estado || "NORMAL"),
                        ocultaInicial:
                            Boolean(relacion.ocultaInicial)
                    }
                }))
            ];

            const cy = window.cytoscape({
                container: contenedor,
                elements: elementos,
                wheelSensitivity: 0.18,
                minZoom: 0.25,
                maxZoom: 2.6,
                boxSelectionEnabled: false,
                autoungrabify: false,
                style: estilos()
            });

            const instancia = {
                cy,
                detailElementId,
                horizontal: Boolean(horizontal)
            };

            instancias.set(elementId, instancia);

            cy.nodes().forEach(nodo => {
                if (Boolean(nodo.data("ocultoInicial"))) {
                    nodo.hide();
                }
            });

            actualizarVisibilidadAristas(cy);

            cy.on("tap", "node", evento => {
                const nodo = evento.target;

                cy.elements().removeClass(
                    "relationship-selected");

                nodo.addClass(
                    "relationship-selected");

                mostrarDetalle(
                    elementId,
                    detalle,
                    nodo.data());
            });

            cy.on("tap", evento => {
                if (evento.target !== cy)
                    return;

                cy.elements().removeClass(
                    "relationship-selected");

                mostrarEstadoInicial(detalle);
            });

            cy.on("mouseover", "node", evento => {
                evento.target.style(
                    "cursor",
                    "pointer");
            });

            try {
                aplicarLayout(
                    elementId,
                    Boolean(horizontal),
                    false);
            } catch (error) {
                console.warn(
                    "No fue posible aplicar la distribución jerárquica.",
                    error);

                cy.layout({
                    name: "grid",
                    fit: true,
                    padding: 42,
                    animate: false
                }).run();
            }

            setTimeout(() => {
                cy.resize();
                cy.fit(
                    elementosVisibles(cy),
                    42);
            }, 80);
        }

        function estilos() {
            return [
                {
                    selector: "node",
                    style: {
                        "background-color": "#E2E8F0",
                        "border-color": "#94A3B8",
                        "border-width": 2,
                        "color": "#1E293B",
                        "font-family":
                            "Montserrat, Arial, sans-serif",
                        "font-size": 11,
                        "font-weight": 700,
                        "height": 76,
                        "label": "data(etiqueta)",
                        "padding": 8,
                        "shape": "round-rectangle",
                        "text-halign": "center",
                        "text-max-width": 150,
                        "text-outline-color": "#FFFFFF",
                        "text-outline-opacity": 0.94,
                        "text-outline-width": 2,
                        "text-overflow-wrap": "anywhere",
                        "text-valign": "center",
                        "text-wrap": "wrap",
                        "width": 176
                    }
                },
                {
                    selector: "node[estado = 'CORRECTO']",
                    style: {
                        "background-color": "#DCFCE7",
                        "border-color": "#16A34A"
                    }
                },
                {
                    selector: "node[estado = 'INFORMACION']",
                    style: {
                        "background-color": "#E0F2FE",
                        "border-color": "#0284C7"
                    }
                },
                {
                    selector: "node[estado = 'OFFLINE']",
                    style: {
                        "background-color": "#F3E8FF",
                        "border-color": "#9333EA"
                    }
                },
                {
                    selector: "node[estado = 'NEUTRAL']",
                    style: {
                        "background-color": "#F1F5F9",
                        "border-color": "#94A3B8",
                        "border-style": "dashed",
                        "color": "#64748B"
                    }
                },
                {
                    selector: "node[estado = 'ADVERTENCIA']",
                    style: {
                        "background-color": "#FEF3C7",
                        "border-color": "#D97706"
                    }
                },
                {
                    selector: "node[estado = 'INACTIVO']",
                    style: {
                        "background-color": "#FEE2E2",
                        "border-color": "#DC2626",
                        "opacity": 0.84
                    }
                },
                {
                    selector: "node[tipo = 'ANALISIS']",
                    style: {
                        "height": 92,
                        "width": 202,
                        "font-size": 12,
                        "border-width": 4
                    }
                },
                {
                    selector:
                        "node[tipo = 'REQUERIMIENTO'], " +
                        "node[tipo = 'FORMULA'], " +
                        "node[tipo = 'MIXTA']",
                    style: {
                        "height": 88,
                        "width": 194,
                        "border-width": 3
                    }
                },
                {
                    selector:
                        "node[tipo = 'ELEMENTO_REQUERIMIENTO'], " +
                        "node[tipo = 'APORTE_FORMULA'], " +
                        "node[tipo = 'APORTE_MIXTA'], " +
                        "node[tipo = 'RESULTADO_MIXTA']",
                    style: {
                        "height": 68,
                        "width": 164,
                        "font-size": 10
                    }
                },
                {
                    selector:
                        "node[tipo = 'FUENTE_FORMULA'], " +
                        "node[tipo = 'FUENTE_MIXTA']",
                    style: {
                        "shape": "round-tag",
                        "height": 76,
                        "width": 182
                    }
                },
                {
                    selector: "node[tipo = 'GRUPO']",
                    style: {
                        "background-color": "#ECFDF5",
                        "border-color": "#3B655B",
                        "border-style": "dashed",
                        "shape": "barrel"
                    }
                },
                {
                    selector: "node[tipo = 'ALERTA']",
                    style: {
                        "shape": "diamond",
                        "height": 92,
                        "width": 160
                    }
                },
                {
                    selector: "node[tipo = 'SINCRONIZACION']",
                    style: {
                        "shape": "hexagon"
                    }
                },
                {
                    selector: "node.relationship-selected",
                    style: {
                        "border-color": "#F2C94C",
                        "border-width": 6,
                        "overlay-color": "#F2C94C",
                        "overlay-opacity": 0.12,
                        "overlay-padding": 8
                    }
                },
                {
                    selector: "edge",
                    style: {
                        "curve-style": "bezier",
                        "font-family":
                            "Montserrat, Arial, sans-serif",
                        "font-size": 8,
                        "font-weight": 700,
                        "label": "data(etiqueta)",
                        "line-color": "#94A3B8",
                        "target-arrow-color": "#64748B",
                        "target-arrow-shape": "triangle",
                        "text-background-color": "#FFFFFF",
                        "text-background-opacity": 0.92,
                        "text-background-padding": 3,
                        "text-rotation": "autorotate",
                        "width": 2
                    }
                },
                {
                    selector: "edge[estado = 'ADVERTENCIA']",
                    style: {
                        "line-color": "#DC2626",
                        "target-arrow-color": "#DC2626",
                        "line-style": "dashed",
                        "width": 3
                    }
                },
                {
                    selector: "edge[estado = 'DESTACADA']",
                    style: {
                        "line-color": "#D97706",
                        "target-arrow-color": "#D97706",
                        "line-style": "dashed",
                        "width": 2.5
                    }
                }
            ];
        }

        function alternarNodo(
            elementId,
            nodeId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            const cy = instancia.cy;
            const nodo = cy.getElementById(nodeId);

            if (!nodo ||
                nodo.empty() ||
                !Boolean(nodo.data("expandible"))) {
                return;
            }

            const expandido =
                Boolean(nodo.data("expandido"));

            if (expandido) {
                contraerNodo(
                    cy,
                    nodo);
            } else {
                expandirNodo(
                    cy,
                    nodo);
            }

            actualizarVisibilidadAristas(cy);
            actualizarEtiquetaExpansion(nodo);

            aplicarLayout(
                elementId,
                instancia.horizontal,
                true);

            const detalle =
                document.getElementById(
                    instancia.detailElementId);

            mostrarDetalle(
                elementId,
                detalle,
                nodo.data());
        }

        function expandirNodo(
            cy,
            nodo) {
            const id =
                String(nodo.id());

            const hijos =
                hijosDirectos(
                    cy,
                    id);

            hijos.show();
            nodo.data(
                "expandido",
                true);
        }

        function contraerNodo(
            cy,
            nodo) {
            const descendientes =
                obtenerDescendientes(
                    cy,
                    String(nodo.id()));

            descendientes.forEach(hijo => {
                hijo.hide();

                if (Boolean(hijo.data("expandible"))) {
                    hijo.data(
                        "expandido",
                        false);

                    actualizarEtiquetaExpansion(
                        hijo);
                }
            });

            nodo.data(
                "expandido",
                false);
        }

        function expandirTodo(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            const cy = instancia.cy;

            cy.nodes().show();

            cy.nodes().forEach(nodo => {
                if (Boolean(nodo.data("expandible"))) {
                    nodo.data(
                        "expandido",
                        true);

                    actualizarEtiquetaExpansion(
                        nodo);
                }
            });

            actualizarVisibilidadAristas(cy);

            aplicarLayout(
                elementId,
                instancia.horizontal,
                true);
        }

        function contraerTodo(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            const cy = instancia.cy;

            cy.nodes().forEach(nodo => {
                const padreId =
                    String(nodo.data("padreId") || "");

                if (padreId) {
                    nodo.hide();
                }

                if (Boolean(nodo.data("expandible"))) {
                    nodo.data(
                        "expandido",
                        false);

                    actualizarEtiquetaExpansion(
                        nodo);
                }
            });

            actualizarVisibilidadAristas(cy);

            aplicarLayout(
                elementId,
                instancia.horizontal,
                true);
        }

        function hijosDirectos(
            cy,
            padreId) {
            return cy.nodes().filter(nodo =>
                String(nodo.data("padreId") || "") ===
                String(padreId));
        }

        function obtenerDescendientes(
            cy,
            padreId) {
            let resultado =
                cy.collection();

            const pendientes = [
                String(padreId)
            ];

            while (pendientes.length > 0) {
                const actual =
                    pendientes.shift();

                const hijos =
                    hijosDirectos(
                        cy,
                        actual);

                hijos.forEach(hijo => {
                    if (!resultado.contains(hijo)) {
                        resultado =
                            resultado.union(hijo);

                        pendientes.push(
                            String(hijo.id()));
                    }
                });
            }

            return resultado;
        }

        function actualizarVisibilidadAristas(cy) {
            cy.edges().forEach(arista => {
                const visible =
                    arista.source().visible() &&
                    arista.target().visible();

                if (visible) {
                    arista.show();
                } else {
                    arista.hide();
                }
            });
        }

        function actualizarEtiquetaExpansion(nodo) {
            const base =
                String(
                    nodo.data("etiquetaBase") || "");

            nodo.data(
                "etiqueta",
                etiquetaConEstadoExpansion(
                    base,
                    Boolean(nodo.data("expandible")),
                    Boolean(nodo.data("expandido"))));
        }

        function etiquetaConEstadoExpansion(
            base,
            expandible,
            expandido) {
            if (!expandible)
                return base;

            return `${expandido ? "▾" : "▸"} ${base}`;
        }

        function aplicarLayout(
            elementId,
            horizontal,
            animar = true) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            instancia.horizontal =
                Boolean(horizontal);

            const cy = instancia.cy;
            const visibles =
                elementosVisibles(cy);

            if (visibles.empty())
                return;

            const nodosVisibles =
                visibles.nodes();

            const raices =
                nodosVisibles.filter(nodo =>
                    nodo.indegree(
                        true) === 0);

            const opciones = {
                name: "breadthfirst",
                directed: true,
                circle: false,
                grid: false,
                spacingFactor: 1.32,
                padding: 42,
                fit: false,
                animate: false,
                maximal: true
            };

            if (!raices.empty()) {
                opciones.roots =
                    raices;
            }

            const layout =
                visibles.layout(opciones);

            layout.one(
                "layoutstop",
                () => {
                    if (horizontal) {
                        const posiciones =
                            new Map();

                        nodosVisibles.forEach(nodo => {
                            posiciones.set(
                                nodo.id(),
                                {
                                    x: nodo.position("y"),
                                    y: nodo.position("x")
                                });
                        });

                        nodosVisibles.positions(nodo =>
                            posiciones.get(
                                nodo.id()));
                    }

                    cy.resize();

                    if (animar) {
                        cy.animate(
                            {
                                fit: {
                                    eles:
                                        elementosVisibles(cy),
                                    padding: 42
                                }
                            },
                            {
                                duration: 320
                            });
                    } else {
                        cy.fit(
                            elementosVisibles(cy),
                            42);
                    }
                });

            layout.run();
        }

        function elementosVisibles(cy) {
            return cy.elements().filter(
                elemento => elemento.visible());
        }

        function centrar(elementId) {
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            instancia.cy.resize();
            instancia.cy.fit(
                elementosVisibles(instancia.cy),
                42);
            instancia.cy.center(
                elementosVisibles(instancia.cy));
        }

        function cambiarOrientacion(
            elementId,
            horizontal) {
            aplicarLayout(
                elementId,
                Boolean(horizontal),
                true);
        }

        async function pantallaCompleta(
            workspaceId,
            elementId) {
            const workspace =
                document.getElementById(workspaceId);

            const instancia =
                instancias.get(elementId);

            if (!workspace || !instancia)
                return;

            try {
                if (!document.fullscreenElement) {
                    await workspace.requestFullscreen();
                } else {
                    await document.exitFullscreen();
                }
            } catch (error) {
                console.warn(
                    "No fue posible cambiar a pantalla completa.",
                    error);
            }

            setTimeout(() => {
                instancia.cy.resize();
                instancia.cy.fit(
                    elementosVisibles(instancia.cy),
                    44);
            }, 160);
        }

        function mostrarDetalle(
            elementId,
            contenedor,
            data) {
            if (!contenedor)
                return;

            const detalles =
                data?.detalles &&
                typeof data.detalles === "object"
                    ? Object.entries(data.detalles)
                    : [];

            const filas =
                detalles.length === 0
                    ? `<div class="relationship-detail-no-data">
                           No hay información adicional disponible.
                       </div>`
                    : detalles
                        .map(([etiqueta, valor]) => `
                            <div class="relationship-detail-row">
                                <span>${escapar(etiqueta)}</span>
                                <strong>${escapar(valor)}</strong>
                            </div>`)
                        .join("");

            const expandible =
                Boolean(data?.expandible);

            const expandido =
                Boolean(data?.expandido);

            const botonExpansion =
                expandible
                    ? `
                        <button type="button"
                                class="relationship-detail-toggle"
                                data-relationship-toggle="${escapar(
                                    data.id)}">
                            <i class="fa-solid ${
                                expandido
                                    ? "fa-compress"
                                    : "fa-expand"
                            }"></i>
                            ${
                                expandido
                                    ? "Ocultar relaciones internas"
                                    : "Mostrar relaciones internas"
                            }
                        </button>`
                    : "";

            contenedor.innerHTML = `
                <div class="relationship-node-detail-header">
                    <span>${escapar(data.grupo || "RELACIÓN")}</span>
                    <h2>${escapar(data.titulo || "Nodo")}</h2>
                    <p>${escapar(data.subtitulo || "")}</p>
                    <b class="relationship-node-state ${claseEstado(
                        data.estado)}">
                        ${nombreEstado(data.estado)}
                    </b>
                </div>

                ${botonExpansion}

                <div class="relationship-node-detail-body">
                    ${filas}
                </div>

                <footer>
                    <i class="fa-solid fa-circle-info"></i>
                    Mapa informativo de solo lectura.
                </footer>`;

            const boton =
                contenedor.querySelector(
                    "[data-relationship-toggle]");

            if (boton) {
                boton.addEventListener(
                    "click",
                    () => alternarNodo(
                        elementId,
                        String(data.id)));
            }
        }

        function mostrarEstadoInicial(contenedor) {
            if (!contenedor)
                return;

            contenedor.innerHTML = `
                <div class="relationship-detail-empty">
                    <i class="fa-solid fa-diagram-project"></i>
                    <h2>Seleccione un nodo</h2>
                    <p>
                        Presione cualquier elemento para consultar sus datos.
                        Los nodos con ▸ contienen relaciones internas.
                    </p>
                </div>`;
        }

        function construirEtiqueta(
            titulo,
            subtitulo) {
            const principal =
                String(titulo || "").trim();

            const secundario =
                String(subtitulo || "").trim();

            return secundario
                ? `${principal}\n${secundario}`
                : principal;
        }

        function claseEstado(estado) {
            switch (String(estado || "").toUpperCase()) {
                case "CORRECTO":
                    return "correct";
                case "INFORMACION":
                    return "information";
                case "OFFLINE":
                    return "offline";
                case "ADVERTENCIA":
                    return "warning";
                case "INACTIVO":
                    return "inactive";
                default:
                    return "neutral";
            }
        }

        function nombreEstado(estado) {
            switch (String(estado || "").toUpperCase()) {
                case "CORRECTO":
                    return "Correcto";
                case "INFORMACION":
                    return "Informativo";
                case "OFFLINE":
                    return "Sincronización offline";
                case "ADVERTENCIA":
                    return "Requiere revisión";
                case "INACTIVO":
                    return "Inactivo";
                default:
                    return "Sin registro opcional";
            }
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
            const instancia =
                instancias.get(elementId);

            if (!instancia)
                return;

            try {
                instancia.cy.destroy();
            } catch {
            }

            instancias.delete(elementId);
        }

        return {
            version,
            crear,
            centrar,
            expandirTodo,
            contraerTodo,
            cambiarOrientacion,
            pantallaCompleta,
            destruir
        };
    })();
