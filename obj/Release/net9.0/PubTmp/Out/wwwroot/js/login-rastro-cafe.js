(() => {
    "use strict";

    let instanciaActual = null;

    const movimientoReducido = () =>
        window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    const configuraciones = {
        success: {
            texto: "Bienvenido",
            claseMensaje: "is-success",
            clasePagina: "is-login-success",
            duracion: 950
        },
        error: {
            texto: "Intente nuevamente",
            claseMensaje: "is-error",
            clasePagina: "is-login-error",
            duracion: 1450
        }
    };

    const limpiarEstadoMensaje = elemento => {
        elemento.classList.remove(
            "is-visible",
            "is-success",
            "is-error");
    };

    const limpiarEstadoPagina = pagina => {
        pagina?.classList.remove(
            "is-login-success",
            "is-login-error");
    };

    const reiniciarAnimacion = (
        elemento,
        configuracion) => {
        limpiarEstadoMensaje(elemento);

        const texto =
            elemento.querySelector(".login-bean-text");

        if (texto)
            texto.textContent = configuracion.texto;

        elemento.classList.add(
            configuracion.claseMensaje);

        void elemento.offsetWidth;
        elemento.classList.add("is-visible");
    };

    const clear = () => {
        if (!instanciaActual)
            return false;

        if (instanciaActual.ocultarTimeout) {
            window.clearTimeout(
                instanciaActual.ocultarTimeout);

            instanciaActual.ocultarTimeout = 0;
        }

        instanciaActual.saludos.forEach(
            limpiarEstadoMensaje);

        limpiarEstadoPagina(
            instanciaActual.pagina);

        return true;
    };

    const dispose = () => {
        if (!instanciaActual)
            return;

        clear();
        instanciaActual = null;
    };

    const init = (pageSelector = ".login-page") => {
        dispose();

        const pagina =
            document.querySelector(pageSelector);

        if (!pagina)
            return false;

        const saludos = Array.from(
            pagina.querySelectorAll(
                ".login-mascot-greeting"));

        if (saludos.length === 0)
            return false;

        instanciaActual = {
            pagina,
            saludos,
            ocultarTimeout: 0
        };

        return true;
    };

    const show = (tipo = "success") => {
        if (!instanciaActual ||
            movimientoReducido())
        {
            return false;
        }

        const configuracion =
            configuraciones[tipo] ??
            configuraciones.success;

        clear();

        instanciaActual.pagina.classList.add(
            configuracion.clasePagina);

        void instanciaActual.pagina.offsetWidth;

        instanciaActual.saludos.forEach(
            saludo => reiniciarAnimacion(
                saludo,
                configuracion));

        instanciaActual.ocultarTimeout =
            window.setTimeout(() => {
                if (!instanciaActual)
                    return;

                instanciaActual.saludos.forEach(
                    limpiarEstadoMensaje);

                limpiarEstadoPagina(
                    instanciaActual.pagina);

                instanciaActual.ocultarTimeout = 0;
            }, configuracion.duracion);

        return true;
    };

    const focusPassword = () => {
        window.setTimeout(() => {
            const campo =
                document.querySelector("#clave");

            if (!campo || campo.disabled)
                return;

            campo.focus({
                preventScroll: true
            });

            if (typeof campo.select === "function")
                campo.select();
        }, 120);
    };

    window.conatradecCoffeeTrail = {
        init,
        show,
        clear,
        focusPassword,
        dispose
    };
})();
