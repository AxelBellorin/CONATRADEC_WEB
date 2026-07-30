(function () {
    "use strict";

    const eventos = [
        "pointerdown",
        "keydown",
        "touchstart",
        "wheel",
        "scroll"
    ];

    const intervaloNotificacionMs = 30000;

    let referenciaDotNet = null;
    let tiempoMaximoMs = 0;
    let ultimaActividad = 0;
    let ultimaNotificacion = 0;
    let intervalo = null;
    let expirando = false;

    function registrarActividad() {
        if (!referenciaDotNet || expirando) {
            return;
        }

        const ahora = Date.now();
        ultimaActividad = ahora;

        if (ahora - ultimaNotificacion < intervaloNotificacionMs) {
            return;
        }

        ultimaNotificacion = ahora;

        referenciaDotNet
            .invokeMethodAsync(
                "RegistrarActividadNavegadorAsync")
            .catch(function () {
                // El circuito puede haberse desconectado.
            });
    }

    function verificar() {
        if (!referenciaDotNet ||
            expirando ||
            tiempoMaximoMs <= 0) {
            return;
        }

        if (Date.now() - ultimaActividad < tiempoMaximoMs) {
            return;
        }

        expirando = true;

        const referencia = referenciaDotNet;
        detener(false);

        referencia
            .invokeMethodAsync(
                "CerrarSesionPorInactividadAsync")
            .catch(function () {
                // El circuito puede haberse desconectado.
            });
    }

    function alCambiarVisibilidad() {
        if (document.visibilityState === "visible") {
            verificar();
        }
    }

    function iniciar(dotNetRef, timeoutMs) {
        detener(true);

        referenciaDotNet = dotNetRef;
        tiempoMaximoMs = Math.max(
            60000,
            Number(timeoutMs) || 0);

        ultimaActividad = Date.now();
        ultimaNotificacion = 0;
        expirando = false;

        eventos.forEach(function (evento) {
            document.addEventListener(
                evento,
                registrarActividad,
                {
                    capture: true,
                    passive: true
                });
        });

        document.addEventListener(
            "visibilitychange",
            alCambiarVisibilidad,
            true);

        intervalo = window.setInterval(
            verificar,
            1000);
    }

    function detener(limpiarReferencia) {
        if (intervalo !== null) {
            window.clearInterval(intervalo);
            intervalo = null;
        }

        eventos.forEach(function (evento) {
            document.removeEventListener(
                evento,
                registrarActividad,
                true);
        });

        document.removeEventListener(
            "visibilitychange",
            alCambiarVisibilidad,
            true);

        tiempoMaximoMs = 0;
        ultimaActividad = 0;
        ultimaNotificacion = 0;

        if (limpiarReferencia !== false) {
            referenciaDotNet = null;
        }
    }

    window.conatradecSessionInactivity = {
        start: iniciar,
        stop: function () {
            detener(true);
        }
    };
})();
