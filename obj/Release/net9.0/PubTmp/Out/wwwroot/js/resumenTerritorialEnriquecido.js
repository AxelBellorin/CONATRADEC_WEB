window.conatradecResumenTerritorialEnriquecido = (() => {
    const VERSION = "1.1.0";
    const mapas = new Map();
    const respuestas = new Map();
    const climas = new Map();

    const L = window.L;
    const resumenBase = window.conatradecResumenTerritorial;
    const mapaApi = window.conatradecMapaInteligente;

    if (!L || !resumenBase || !mapaApi) {
        return { inicializado: false, version: VERSION };
    }

    capturarMapas();
    envolverInicializacionMapa();
    envolverResumen();
    envolverClima();

    function capturarMapas() {
        if (L.__conatradecResumenEnriquecidoMapas) return;

        const crearMapaOriginal = L.map;

        L.map = function (elemento, opciones) {
            const mapa = crearMapaOriginal.call(this, elemento, opciones);
            const elementId = typeof elemento === "string"
                ? elemento
                : elemento?.id;

            if (elementId) registrarMapa(elementId, mapa);

            return mapa;
        };

        L.__conatradecResumenEnriquecidoMapas = true;
    }


    function envolverInicializacionMapa() {
        if (mapaApi.__conatradecResumenInicializacionEnriquecida) return;

        const inicializarOriginal = mapaApi.inicializar.bind(mapaApi);

        mapaApi.inicializar = (elementId, dotNetReference) => {
            let mapaCapturado = null;
            const crearMapaActual = L.map;

            L.map = function (...argumentos) {
                const mapa = crearMapaActual.apply(this, argumentos);
                mapaCapturado = mapa;
                return mapa;
            };

            let resultado;

            try {
                resultado = inicializarOriginal(elementId, dotNetReference);
            } finally {
                L.map = crearMapaActual;
            }

            if (resultado && mapaCapturado) {
                registrarMapa(elementId, mapaCapturado);
            }

            return resultado;
        };

        mapaApi.__conatradecResumenInicializacionEnriquecida = true;
    }

    function registrarMapa(elementId, mapa) {
        if (!elementId || !mapa) return;

        mapas.set(elementId, mapa);

        if (mapa.__conatradecResumenEnriquecidoRegistrado) return;
        mapa.__conatradecResumenEnriquecidoRegistrado = true;

        mapa.on("popupopen", evento =>
            enriquecerPopup(elementId, evento));

        mapa.on("unload", () => {
            mapas.delete(elementId);
            respuestas.delete(elementId);
            climas.delete(elementId);
        });
    }

    function envolverResumen() {
        if (resumenBase.__conatradecEnriquecido) return;

        const mostrarOriginal = resumenBase.mostrar.bind(resumenBase);
        const ocultarOriginal = resumenBase.ocultar.bind(resumenBase);

        resumenBase.mostrar = async (
            elementId,
            respuesta,
            terrenosActivos = null) => {
            respuestas.set(elementId, respuesta || null);
            return await mostrarOriginal(
                elementId,
                respuesta,
                terrenosActivos);
        };

        resumenBase.ocultar = elementId => {
            respuestas.delete(elementId);
            return ocultarOriginal(elementId);
        };

        resumenBase.__conatradecEnriquecido = true;
    }

    function envolverClima() {
        if (mapaApi.__conatradecResumenClimaEnriquecido) return;

        const mostrarClimaOriginal = mapaApi.mostrarClima.bind(mapaApi);
        const aplicarFiltroOriginal =
            mapaApi.aplicarFiltroTerritorial.bind(mapaApi);
        const destruirOriginal = mapaApi.destruir.bind(mapaApi);

        mapaApi.mostrarClima = (elementId, respuesta) => {
            const actual = climas.get(elementId) || {};
            climas.set(elementId, {
                ...actual,
                original: respuesta || null,
                actual: respuesta || null
            });
            return mostrarClimaOriginal(elementId, respuesta);
        };

        mapaApi.aplicarFiltroTerritorial = async (...argumentos) => {
            const resultado = await aplicarFiltroOriginal(...argumentos);
            const elementId = argumentos[0];
            const actual = climas.get(elementId) || {};
            climas.set(elementId, {
                ...actual,
                actual: resultado || null
            });
            return resultado;
        };

        mapaApi.destruir = elementId => {
            climas.delete(elementId);
            respuestas.delete(elementId);
            mapas.delete(elementId);
            return destruirOriginal(elementId);
        };

        mapaApi.__conatradecResumenClimaEnriquecido = true;
    }

    function enriquecerPopup(elementId, evento) {
        const popup = evento?.popup;
        const fuente = popup?._source;
        const feature = fuente?.feature;

        if (!feature?.geometry) return;

        requestAnimationFrame(() => {
            const elementoPopup = popup.getElement?.();
            const contenido = elementoPopup
                ?.querySelector(".geo-summary-map-popup");

            if (!contenido) return;

            const respuesta = respuestas.get(elementId);
            const region = buscarRegion(respuesta, feature);
            if (!region) return;

            contenido
                .querySelector(".geo-summary-enriched")
                ?.remove();

            compactarResumenBase(contenido, region);

            const bloque = document.createElement("section");
            bloque.className = "geo-summary-enriched";
            bloque.innerHTML = `
                ${construirClimaHtml(elementId, feature)}
                ${construirNutrientesHtml(region)}
                <details class="geo-summary-accordion geology"
                         data-geology-summary>
                    ${cabeceraAcordeon(
                        "fa-solid fa-mountain-sun",
                        "Contexto geológico",
                        "Fallas y volcanes del territorio")}
                    <div class="geo-summary-accordion-body">
                        <div class="geo-summary-extra-loading">
                            <i class="fa-solid fa-spinner fa-spin"></i>
                            Consultando fallas y volcanes del territorio...
                        </div>
                    </div>
                </details>`;

            const footer = contenido.querySelector("footer");
            if (footer) contenido.insertBefore(bloque, footer);
            else contenido.appendChild(bloque);

            configurarAcordeones(contenido);
            cargarGeologia(elementId, feature, bloque);
        });
    }

    function configurarAcordeones(contenido) {
        const acordeones = [
            ...contenido.querySelectorAll(".geo-summary-accordion")
        ];

        acordeones.forEach(acordeon => {
            if (acordeon.dataset.acordeonConfigurado === "true") return;
            acordeon.dataset.acordeonConfigurado = "true";

            acordeon.addEventListener("toggle", () => {
                if (!acordeon.open) return;

                acordeones.forEach(otro => {
                    if (otro !== acordeon) otro.open = false;
                });
            });
        });
    }

    function compactarResumenBase(contenido, region) {
        if (!contenido || contenido.dataset.acordeonesAplicados === "true") {
            return;
        }

        contenido.dataset.acordeonesAplicados = "true";

        const principal = contenido.querySelector(".geo-summary-main-grid");
        const cobertura = contenido.querySelector(".geo-summary-coverage");
        const suelo = contenido.querySelector(".geo-summary-soil-grid");
        const deficiencias = contenido.querySelector(".geo-summary-deficiencies");
        const advertencia = contenido.querySelector(".geo-summary-sample-warning");
        const footer = contenido.querySelector("footer");

        const referencia = principal || cobertura || suelo || deficiencias || footer;
        if (!referencia) return;

        const acordeonGeneral = crearAcordeonBase(
            "general",
            "fa-solid fa-chart-pie",
            "Resumen general",
            `${numeroEntero(region?.totalTerrenos || 0)} terreno${
                Number(region?.totalTerrenos || 0) === 1 ? "" : "s"}`,
            true);

        const acordeonSuelo = crearAcordeonBase(
            "soil",
            "fa-solid fa-flask-vial",
            "Suelo y deficiencias",
            "Promedios del último análisis",
            false);

        /*
         * Se insertan primero y luego se mueven los bloques originales.
         * Así no se pierde la referencia DOM usada por insertBefore.
         */
        contenido.insertBefore(acordeonGeneral.detalle, referencia);
        contenido.insertBefore(acordeonSuelo.detalle, footer || null);

        moverSiExiste(principal, acordeonGeneral.cuerpo);
        moverSiExiste(cobertura, acordeonGeneral.cuerpo);
        moverSiExiste(suelo, acordeonSuelo.cuerpo);
        moverSiExiste(deficiencias, acordeonSuelo.cuerpo);
        moverSiExiste(advertencia, acordeonSuelo.cuerpo);
    }

    function crearAcordeonBase(
        clase,
        icono,
        titulo,
        subtitulo,
        abierto) {
        const detalle = document.createElement("details");
        detalle.className = `geo-summary-accordion ${clase}`;
        detalle.open = Boolean(abierto);
        detalle.innerHTML = `
            ${cabeceraAcordeon(icono, titulo, subtitulo)}
            <div class="geo-summary-accordion-body"></div>`;

        return {
            detalle,
            cuerpo: detalle.querySelector(".geo-summary-accordion-body")
        };
    }

    function moverSiExiste(elemento, destino) {
        if (elemento && destino) destino.appendChild(elemento);
    }

    function cabeceraAcordeon(icono, titulo, subtitulo = "") {
        return `
            <summary>
                <span class="geo-summary-accordion-title">
                    <i class="${icono}"></i>
                    <strong>${escapar(titulo)}</strong>
                </span>
                ${subtitulo
                    ? `<small>${escapar(subtitulo)}</small>`
                    : ""}
                <i class="fa-solid fa-chevron-down geo-summary-accordion-chevron"
                   aria-hidden="true"></i>
            </summary>`;
    }

    async function cargarGeologia(elementId, feature, bloque) {
        const contenedor = bloque.querySelector("[data-geology-summary]");
        const geologia = window.conatradecGeologiaMapa;

        if (!contenedor ||
            !geologia?.prepararResumenTerritorial ||
            !geologia?.obtenerResumenTerritorial) {
            if (contenedor) {
                contenedor.innerHTML = construirGeologiaNoDisponible(
                    "El módulo geológico no está disponible.");
            }
            return;
        }

        try {
            await geologia.prepararResumenTerritorial(elementId);
            if (!contenedor.isConnected) return;

            const resumen = geologia.obtenerResumenTerritorial(
                elementId,
                feature);

            contenedor.innerHTML = construirGeologiaHtml(resumen);
        } catch {
            if (contenedor.isConnected) {
                contenedor.innerHTML = construirGeologiaNoDisponible(
                    "No fue posible consultar las fuentes geológicas.");
            }
        }
    }

    function construirClimaHtml(elementId, feature) {
        const clima = climas.get(elementId)?.original;
        const resumen = resumirClima(clima, feature);

        if (!resumen.disponible) {
            return `
                <details class="geo-summary-accordion climate unavailable">
                    ${cabeceraAcordeon(
                        "fa-solid fa-cloud-sun",
                        "Resumen climático",
                        "Sin datos meteorológicos")}
                    <div class="geo-summary-accordion-body">
                        <p>No hay puntos meteorológicos disponibles.</p>
                    </div>
                </details>`;
        }

        return `
            <details class="geo-summary-accordion climate">
                ${cabeceraAcordeon(
                    "fa-solid fa-cloud-sun",
                    "Resumen climático",
                    resumen.aproximado
                        ? "Estimación con el punto más cercano"
                        : `${resumen.totalPuntos} punto${
                            resumen.totalPuntos === 1 ? "" : "s"}`)}
                <div class="geo-summary-accordion-body">
                    <div class="geo-summary-extra-grid climate-grid">
                    ${datoResumen(
                        "Temperatura",
                        resumen.temperaturaPromedio,
                        "°C",
                        "fa-solid fa-temperature-half")}
                    ${datoResumen(
                        "Rango térmico",
                        resumen.temperaturaMinima !== null &&
                        resumen.temperaturaMaxima !== null
                            ? `${numero(resumen.temperaturaMinima, 1)}–${
                                numero(resumen.temperaturaMaxima, 1)}`
                            : null,
                        "°C",
                        "fa-solid fa-arrow-down-up-across-line")}
                    ${datoResumen(
                        "Humedad",
                        resumen.humedadPromedio,
                        "%",
                        "fa-solid fa-droplet")}
                    ${datoResumen(
                        "Precipitación máx.",
                        resumen.precipitacionMaxima,
                        " mm",
                        "fa-solid fa-cloud-rain")}
                    ${datoResumen(
                        "Viento máx.",
                        resumen.vientoMaximo,
                        " km/h",
                        "fa-solid fa-wind")}
                    </div>
                </div>
            </details>`;
    }

    function construirNutrientesHtml(region) {
        const nutrientes = Array.isArray(region?.nutrientes)
            ? [...region.nutrientes]
            : [];

        if (nutrientes.length === 0) {
            return `
                <details class="geo-summary-accordion nutrients unavailable">
                    ${cabeceraAcordeon(
                        "fa-solid fa-atom",
                        "Elementos químicos",
                        "Sin elementos comparables")}
                    <div class="geo-summary-accordion-body">
                        <p>No existen elementos químicos comparables.</p>
                    </div>
                </details>`;
        }

        nutrientes.sort((a, b) =>
            Number(b.porcentajeBajo || 0) -
            Number(a.porcentajeBajo || 0));

        const elementos = nutrientes.slice(0, 12).map(item => {
            const simbolo = item.simbolo || item.nombre || "Elemento";
            const unidad = item.unidad ? ` ${escapar(item.unidad)}` : "";
            const bajo = Number(item.porcentajeBajo || 0);

            return `
                <span class="geo-summary-nutrient ${bajo > 0 ? "low" : ""}">
                    <b>${escapar(simbolo)}</b>
                    <strong>${numero(item.promedio, 2)}${unidad}</strong>
                    <small>${numeroEntero(item.terrenosConDato)} terreno${
                        Number(item.terrenosConDato) === 1 ? "" : "s"}</small>
                    ${bajo > 0
                        ? `<em>${numero(bajo, 1)}% bajo</em>`
                        : ""}
                </span>`;
        }).join("");

        return `
            <details class="geo-summary-accordion nutrients">
                ${cabeceraAcordeon(
                    "fa-solid fa-atom",
                    "Elementos químicos",
                    `${nutrientes.length} elemento${
                        nutrientes.length === 1 ? "" : "s"}`)}
                <div class="geo-summary-accordion-body">
                    <small class="geo-summary-accordion-note">
                        Promedio del último análisis por terreno.
                    </small>
                    <div class="geo-summary-nutrients-grid">
                        ${elementos}
                    </div>
                </div>
            </details>`;
    }

    function construirGeologiaHtml(resumen) {
        if (!resumen?.disponible) {
            return construirGeologiaNoDisponible(
                resumen?.mensaje || "Información geológica no disponible.");
        }

        const fallas = Array.isArray(resumen.fallas)
            ? resumen.fallas
            : [];
        const volcanes = Array.isArray(resumen.volcanes)
            ? resumen.volcanes
            : [];

        return `
            ${cabeceraAcordeon(
                "fa-solid fa-mountain-sun",
                "Contexto geológico",
                "Información territorial")}
            <div class="geo-summary-accordion-body">
                <div class="geo-summary-extra-grid geology-grid">
                ${datoResumen(
                    "Fallas cartografiadas",
                    resumen.fallasDisponibles
                        ? resumen.totalFallas
                        : null,
                    "",
                    "fa-solid fa-wave-square")}
                ${datoResumen(
                    "Centros volcánicos",
                    resumen.volcanesDisponibles
                        ? resumen.totalVolcanes
                        : null,
                    "",
                    "fa-solid fa-volcano")}
                </div>
                ${listaGeologica("Fallas", fallas)}
                ${listaGeologica("Volcanes", volcanes)}
                <small class="geo-summary-source-note">
                    Fallas: ${escapar(resumen.fuenteFallas)} ·
                    Volcanes: ${escapar(resumen.fuenteVolcanes)}
                </small>
            </div>`;
    }

    function construirGeologiaNoDisponible(mensaje) {
        return `
            ${cabeceraAcordeon(
                "fa-solid fa-mountain-sun",
                "Contexto geológico",
                "Información no disponible")}
            <div class="geo-summary-accordion-body">
                <p>${escapar(mensaje)}</p>
            </div>`;
    }

    function listaGeologica(titulo, items) {
        if (!items.length) return "";

        return `
            <div class="geo-summary-geology-list">
                <strong>${escapar(titulo)}</strong>
                <span>${items.map(escapar).join(" · ")}</span>
            </div>`;
    }

    function datoResumen(etiqueta, valor, unidad, icono) {
        const disponible = valor !== null &&
            valor !== undefined &&
            valor !== "" &&
            (typeof valor === "string" || Number.isFinite(Number(valor)));

        return `
            <span>
                <i class="${icono}"></i>
                <small>${escapar(etiqueta)}</small>
                <strong>${disponible
                    ? `${typeof valor === "string"
                        ? escapar(valor)
                        : numero(valor, 1)}${escapar(unidad)}`
                    : "—"}</strong>
            </span>`;
    }

    function resumirClima(clima, feature) {
        const puntos = Array.isArray(clima?.puntos)
            ? clima.puntos.filter(punto =>
                coordenadaValida(
                    Number(punto.latitud),
                    Number(punto.longitud)))
            : [];

        if (!window.turf || puntos.length === 0) {
            return { disponible: false };
        }

        let seleccionados = puntos.filter(punto => {
            try {
                return window.turf.booleanPointInPolygon(
                    window.turf.point([
                        Number(punto.longitud),
                        Number(punto.latitud)
                    ]),
                    feature);
            } catch {
                return false;
            }
        });

        let aproximado = false;

        if (seleccionados.length === 0) {
            try {
                const centro = window.turf.centroid(feature).geometry.coordinates;
                seleccionados = [puntos
                    .map(punto => ({
                        punto,
                        distancia: window.turf.distance(
                            window.turf.point(centro),
                            window.turf.point([
                                Number(punto.longitud),
                                Number(punto.latitud)
                            ]),
                            { units: "kilometers" })
                    }))
                    .sort((a, b) => a.distancia - b.distancia)[0]
                    ?.punto]
                    .filter(Boolean);
                aproximado = seleccionados.length > 0;
            } catch {
                seleccionados = [];
            }
        }

        if (seleccionados.length === 0) {
            return { disponible: false };
        }

        return {
            disponible: true,
            aproximado,
            totalPuntos: seleccionados.length,
            temperaturaPromedio: promedio(
                seleccionados,
                "temperatura"),
            temperaturaMinima: minimo(
                seleccionados,
                "temperatura"),
            temperaturaMaxima: maximo(
                seleccionados,
                "temperatura"),
            humedadPromedio: promedio(
                seleccionados,
                "humedadRelativa"),
            precipitacionMaxima: maximo(
                seleccionados,
                "precipitacion"),
            vientoMaximo: maximo(
                seleccionados,
                "velocidadViento")
        };
    }

    function buscarRegion(respuesta, feature) {
        const regiones = Array.isArray(respuesta?.regiones)
            ? respuesta.regiones
            : [];
        const nivel = String(
            respuesta?.nivelAgrupacion || "DEPARTAMENTO")
            .toUpperCase();

        if (nivel === "MUNICIPIO") {
            const municipio = normalizar(nombreMunicipio(feature));
            const departamento = normalizar(departamentoMunicipio(feature));

            return regiones.find(region =>
                normalizar(region.municipio || region.nombreTerritorio) ===
                    municipio &&
                (!departamento ||
                 normalizar(region.departamento) === departamento)) || null;
        }

        const departamento = normalizar(nombreDepartamento(feature));
        return regiones.find(region =>
            normalizar(region.departamento || region.nombreTerritorio) ===
                departamento) || null;
    }

    function nombreDepartamento(feature) {
        const p = feature?.properties || {};
        return p.ADM1_ES || p.adm1_es || p.NAME_1 ||
            p.Departamento || p.DEPARTAMEN || p.nombre || "";
    }

    function nombreMunicipio(feature) {
        const p = feature?.properties || {};
        return p.Municipio || p.MUNICIPIO || p.NAME_2 ||
            p.NOMBRE || p.shapeName || "";
    }

    function departamentoMunicipio(feature) {
        const p = feature?.properties || {};
        return p.Departam_1 || p.DEPARTAMEN || p.Departamento ||
            p.NAME_1 || p.shapeGroup || "";
    }

    function promedio(items, propiedad) {
        const valores = valoresNumericos(items, propiedad);
        return valores.length
            ? valores.reduce((a, b) => a + b, 0) / valores.length
            : null;
    }

    function minimo(items, propiedad) {
        const valores = valoresNumericos(items, propiedad);
        return valores.length ? Math.min(...valores) : null;
    }

    function maximo(items, propiedad) {
        const valores = valoresNumericos(items, propiedad);
        return valores.length ? Math.max(...valores) : null;
    }

    function valoresNumericos(items, propiedad) {
        return items
            .map(item => Number(item?.[propiedad]))
            .filter(Number.isFinite);
    }

    function coordenadaValida(latitud, longitud) {
        return Number.isFinite(latitud) &&
            Number.isFinite(longitud) &&
            Math.abs(latitud) <= 90 &&
            Math.abs(longitud) <= 180;
    }

    function normalizar(valor) {
        return String(valor || "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .replace(/[.,;:()]/g, " ")
            .replace(/[-_/]/g, " ")
            .replace(/\s+/g, " ")
            .trim();
    }

    function numero(valor, decimales) {
        const convertido = Number(valor);
        return Number.isFinite(convertido)
            ? convertido.toLocaleString("es-NI", {
                minimumFractionDigits: decimales,
                maximumFractionDigits: decimales
            })
            : "—";
    }

    function numeroEntero(valor) {
        const convertido = Number(valor);
        return Number.isFinite(convertido)
            ? Math.round(convertido).toLocaleString("es-NI")
            : "0";
    }

    function escapar(valor) {
        return String(valor ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    return {
        inicializado: true,
        version: VERSION
    };
})();
