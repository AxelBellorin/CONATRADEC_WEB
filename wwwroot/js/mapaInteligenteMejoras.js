window.conatradecMapaInteligenteMejoras = (() => {
    const api = window.conatradecMapaInteligente;
    const respuestasOriginales = new Map();

    if (!api || api.__conatradecMejorasAplicadas) {
        return { cerrarPopups: () => { } };
    }

    const mostrarClimaOriginal = api.mostrarClima.bind(api);
    const aplicarFiltroOriginal = api.aplicarFiltroTerritorial.bind(api);
    const destruirOriginal = api.destruir.bind(api);

    function cerrarPopups(elementId) {
        const contenedor = document.getElementById(elementId);
        if (!contenedor) return;

        contenedor
            .querySelectorAll(".leaflet-popup-close-button")
            .forEach(boton => boton.click());
    }

    function numero(valor) {
        const convertido = Number(valor);
        return Number.isFinite(convertido) ? convertido : null;
    }

    function rangoVisual(puntos, propiedad, respaldoMinimo, respaldoMaximo) {
        const valores = puntos
            .map(item => numero(item[propiedad]))
            .filter(valor => valor !== null);

        if (valores.length === 0) return [respaldoMinimo, respaldoMaximo];

        const minimo = Math.min(...valores);
        const maximo = Math.max(...valores);

        if (propiedad === "temperatura") {
            return maximo - minimo < 1
                ? [minimo - 2, maximo + 2]
                : [minimo, maximo];
        }

        if (propiedad === "humedadRelativa") {
            return maximo - minimo < 4
                ? [Math.max(0, minimo - 10), Math.min(100, maximo + 10)]
                : [minimo, maximo];
        }

        if (propiedad === "precipitacion") {
            return [0, Math.max(maximo * 1.25, 0.5)];
        }

        if (propiedad === "velocidadViento") {
            return [0, Math.max(maximo * 1.35, 5)];
        }

        return [minimo, maximo];
    }

    function crearRespuestaVisual(respuesta) {
        const puntos = Array.isArray(respuesta?.puntos)
            ? respuesta.puntos
            : [];

        if (!respuesta?.disponible || puntos.length === 0) return respuesta;

        const desplazamientos = puntos.length <= 6
            ? [
                [0, 0], [0.012, 0], [-0.012, 0],
                [0, 0.012], [0, -0.012],
                [0.009, 0.009], [0.009, -0.009],
                [-0.009, 0.009], [-0.009, -0.009]
            ]
            : [[0, 0]];

        const visuales = [];

        puntos.forEach(punto => {
            const latitud = numero(punto.latitud);
            const longitud = numero(punto.longitud);
            if (latitud === null || longitud === null) return;

            desplazamientos.forEach(([dLatitud, dLongitud]) => {
                visuales.push({
                    ...punto,
                    latitud: latitud + dLatitud,
                    longitud: longitud + dLongitud
                });
            });
        });

        const [temperaturaMinima, temperaturaMaxima] =
            rangoVisual(puntos, "temperatura", 18, 36);
        const [humedadMinima, humedadMaxima] =
            rangoVisual(puntos, "humedadRelativa", 35, 100);
        const [, precipitacionMaxima] =
            rangoVisual(puntos, "precipitacion", 0, 1);
        const [, vientoMaximo] =
            rangoVisual(puntos, "velocidadViento", 0, 25);

        return {
            ...respuesta,
            puntos: visuales.length > 0 ? visuales : puntos,
            temperaturaMinima,
            temperaturaMaxima,
            humedadMinima,
            humedadMaxima,
            precipitacionMaxima,
            vientoMaximo
        };
    }

    api.mostrarClima = (elementId, respuesta) => {
        respuestasOriginales.set(elementId, respuesta);
        return mostrarClimaOriginal(elementId, crearRespuestaVisual(respuesta));
    };

    api.aplicarFiltroTerritorial = async (
        elementId,
        departamento,
        municipio,
        ajustarVista = true) => {
        const original = respuestasOriginales.get(elementId);
        if (original) mostrarClimaOriginal(elementId, original);

        const resultadoReal = await aplicarFiltroOriginal(
            elementId,
            departamento,
            municipio,
            ajustarVista);

        if (resultadoReal) {
            mostrarClimaOriginal(
                elementId,
                crearRespuestaVisual(resultadoReal));
        }

        return resultadoReal;
    };

    api.destruir = elementId => {
        respuestasOriginales.delete(elementId);
        return destruirOriginal(elementId);
    };

    api.__conatradecMejorasAplicadas = true;

    let observacionPendiente = false;

    function actualizarSeparacionTarjetas() {
        document
            .querySelectorAll(".geo-map-stage")
            .forEach(escenario => {
                const detalleAbierto = Boolean(
                    escenario.querySelector(".geo-detail-panel"));

                escenario.classList.toggle(
                    "has-detail-panel",
                    detalleAbierto);
            });
    }

    const observador = new MutationObserver(() => {
        if (observacionPendiente) return;
        observacionPendiente = true;

        requestAnimationFrame(() => {
            observacionPendiente = false;
            actualizarSeparacionTarjetas();
        });
    });

    observador.observe(document.body, {
        childList: true,
        subtree: true
    });

    actualizarSeparacionTarjetas();

    return { cerrarPopups };
})();
