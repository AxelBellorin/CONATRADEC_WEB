(() => {
    "use strict";

    let actualizacionPendiente = false;

    const etiquetasPorIcono = [
        ["fa-pen", "Editar"],
        ["fa-pen-to-square", "Editar"],
        ["fa-eye", "Ver detalles"],
        ["fa-arrow-right", "Abrir"],
        ["fa-trash", "Eliminar"],
        ["fa-trash-can", "Eliminar"],
        ["fa-ban", "Desactivar"],
        ["fa-rotate-left", "Reactivar"],
        ["fa-rotate", "Actualizar"],
        ["fa-download", "Descargar"],
        ["fa-file-pdf", "Descargar PDF"],
        ["fa-file-excel", "Descargar Excel"],
        ["fa-images", "Ver fotografías"],
        ["fa-location-dot", "Ver ubicación"],
        ["fa-map-location-dot", "Ver en el mapa"],
        ["fa-key", "Administrar acceso"],
        ["fa-circle-xmark", "Cerrar"],
        ["fa-xmark", "Cerrar"]
    ];

    const textoVisible = elemento =>
        (elemento.textContent || "")
            .replace(/\s+/g, " ")
            .trim();

    const obtenerEtiquetaPorIcono = elemento => {
        const icono = elemento.querySelector("i");

        if (!icono)
            return null;

        for (const [clase, etiqueta] of etiquetasPorIcono) {
            if (icono.classList.contains(clase))
                return etiqueta;
        }

        return null;
    };

    const mejorarAcciones = raiz => {
        const acciones = raiz.querySelectorAll(
            "button, a.table-action, a.icon-button, button.icon-button, button.modal-close");

        acciones.forEach(elemento => {
            if (elemento.hasAttribute("aria-label"))
                return;

            const titulo = elemento.getAttribute("title")?.trim();
            const contenido = textoVisible(elemento);

            if (titulo) {
                elemento.setAttribute("aria-label", titulo);
                return;
            }

            if (contenido && contenido !== "×")
                return;

            const etiqueta =
                elemento.classList.contains("modal-close")
                    ? "Cerrar ventana"
                    : obtenerEtiquetaPorIcono(elemento);

            if (etiqueta)
                elemento.setAttribute("aria-label", etiqueta);
        });
    };

    const mejorarAlertas = raiz => {
        raiz.querySelectorAll(".alert").forEach(alerta => {
            const esError = alerta.classList.contains("alert-error");

            if (!alerta.hasAttribute("role"))
                alerta.setAttribute("role", esError ? "alert" : "status");

            if (!alerta.hasAttribute("aria-live"))
                alerta.setAttribute("aria-live", esError ? "assertive" : "polite");
        });
    };

    const mejorarEstadosCarga = raiz => {
        raiz.querySelectorAll(".loading-state, .route-guard-loading").forEach(estado => {
            if (!estado.hasAttribute("role"))
                estado.setAttribute("role", "status");

            if (!estado.hasAttribute("aria-live"))
                estado.setAttribute("aria-live", "polite");

            estado.setAttribute("aria-busy", "true");
        });
    };

    const mejorarModales = raiz => {
        raiz.querySelectorAll(".modal-card").forEach((modal, indice) => {
            modal.setAttribute("role", "dialog");
            modal.setAttribute("aria-modal", "true");

            const titulo = modal.querySelector(".modal-header h1, .modal-header h2, .modal-header h3");

            if (!titulo)
                return;

            if (!titulo.id)
                titulo.id = `conatradec-modal-title-${indice}-${Date.now()}`;

            modal.setAttribute("aria-labelledby", titulo.id);
        });
    };

    const mejorarTablas = raiz => {
        raiz.querySelectorAll("table.data-table").forEach(tabla => {
            if (!tabla.hasAttribute("aria-label")) {
                const panel = tabla.closest("section, article, .panel");
                const titulo = panel?.querySelector("h1, h2, h3")?.textContent?.trim();

                if (titulo)
                    tabla.setAttribute("aria-label", titulo);
            }
        });
    };

    const aplicarMejoras = (raiz = document) => {
        mejorarAcciones(raiz);
        mejorarAlertas(raiz);
        mejorarEstadosCarga(raiz);
        mejorarModales(raiz);
        mejorarTablas(raiz);
    };

    const programarActualizacion = () => {
        if (actualizacionPendiente)
            return;

        actualizacionPendiente = true;

        window.requestAnimationFrame(() => {
            actualizacionPendiente = false;
            aplicarMejoras(document);
        });
    };

    const iniciar = () => {
        aplicarMejoras(document);

        const observador = new MutationObserver(programarActualizacion);

        observador.observe(document.body, {
            childList: true,
            subtree: true
        });
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciar, { once: true });
    } else {
        iniciar();
    }
})();
