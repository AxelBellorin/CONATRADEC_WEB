/*
 * CONATRADEC
 * Tarjeta territorial enriquecida — implementación segura.
 *
 * IMPORTANTE:
 * - No reemplaza L.map.
 * - No reemplaza L.geoJSON.
 * - No interviene en la creación de capas.
 * - Solo enriquece el popup territorial que el mapa ya creó.
 */
window.conatradecTarjetaTerritorial =
    window.conatradecTarjetaTerritorial || (() => {
        const estados =
            new Map();

        let observadorDocumento =
            null;

        function estado(elementId) {
            if (!estados.has(elementId)) {
                estados.set(elementId, {
                    elementId,
                    terrenos: []
                });
            }

            return estados.get(elementId);
        }

        function instalar() {
            envolverModuloMapa();
            observarPopups();
            procesarPopupsExistentes();
        }

        function envolverModuloMapa() {
            const modulo =
                window.conatradecMapaInteligente;

            if (!modulo ||
                modulo.__tarjetaTerritorialSegura) {
                return;
            }

            if (typeof modulo.mostrarTerrenos ===
                "function") {
                const original =
                    modulo.mostrarTerrenos
                        .bind(modulo);

                modulo.mostrarTerrenos =
                    function (
                        elementId,
                        terrenos,
                        ajustarVista = true) {
                        const actual =
                            estado(elementId);

                        actual.terrenos =
                            Array.isArray(terrenos)
                                ? terrenos
                                : [];

                        const resultado =
                            original(
                                elementId,
                                terrenos,
                                ajustarVista);

                        /*
                         * Si hay un popup abierto, se recalcula después
                         * de actualizar los filtros visibles.
                         */
                        setTimeout(
                            procesarPopupsExistentes,
                            60);

                        return resultado;
                    };
            }

            if (typeof modulo.destruir ===
                "function") {
                const original =
                    modulo.destruir
                        .bind(modulo);

                modulo.destruir =
                    function (elementId) {
                        estados.delete(elementId);
                        return original(elementId);
                    };
            }

            modulo.__tarjetaTerritorialSegura =
                true;
        }

        function observarPopups() {
            if (observadorDocumento)
                return;

            observadorDocumento =
                new MutationObserver(
                    mutaciones => {
                        for (const mutacion of mutaciones) {
                            for (const nodo of mutacion.addedNodes) {
                                if (!(nodo instanceof Element))
                                    continue;

                                procesarNodo(nodo);
                            }
                        }
                    });

            observadorDocumento.observe(
                document.body,
                {
                    childList: true,
                    subtree: true
                });
        }

        function procesarPopupsExistentes() {
            document
                .querySelectorAll(
                    ".geo-department-popup")
                .forEach(
                    procesarPopup);
        }

        function procesarNodo(nodo) {
            if (nodo.matches?.(
                    ".geo-department-popup")) {
                procesarPopup(nodo);
            }

            nodo.querySelectorAll?.(
                ".geo-department-popup")
                .forEach(
                    procesarPopup);
        }

        function procesarPopup(contenidoOriginal) {
            if (!contenidoOriginal ||
                contenidoOriginal.dataset
                    .territorialEnriquecida ===
                    "true") {
                return;
            }

            const contenedorMapa =
                contenidoOriginal.closest(
                    ".leaflet-container");

            const elementId =
                contenedorMapa?.id;

            if (!elementId)
                return;

            const actual =
                estado(elementId);

            const esMunicipio =
                contenidoOriginal.classList
                    .contains("municipality") ||
                String(
                    contenidoOriginal
                        .querySelector(":scope > span")
                        ?.textContent ||
                    "")
                    .toLowerCase()
                    .includes("municipio");

            const nombre =
                String(
                    contenidoOriginal
                        .querySelector(":scope > h3")
                        ?.textContent ||
                    "")
                    .trim();

            const departamentoMunicipio =
                esMunicipio
                    ? String(
                        contenidoOriginal
                            .querySelector(":scope > small")
                            ?.textContent ||
                        "")
                        .trim()
                    : nombre;

            if (!nombre)
                return;

            contenidoOriginal.dataset
                .territorialEnriquecida =
                "true";

            const resumen =
                calcularResumen(
                    actual.terrenos,
                    {
                        tipo:
                            esMunicipio
                                ? "MUNICIPIO"
                                : "DEPARTAMENTO",

                        nombre,

                        departamento:
                            departamentoMunicipio,

                        municipio:
                            esMunicipio
                                ? nombre
                                : ""
                    });

            contenidoOriginal.classList.add(
                "geo-territory-card-host");

            contenidoOriginal.innerHTML =
                construirTarjeta(
                    actual,
                    resumen);

            /*
             * Activa mover/minimizar sobre este popup sin modificar
             * el funcionamiento interno de Leaflet.
             */
            const popup =
                contenidoOriginal.closest(
                    ".leaflet-popup");

            if (popup) {
                popup.classList.add(
                    "geo-terrain-leaflet-popup",
                    "geo-territorial-rich-popup");

                window
                    .conatradecMapaPanelesMovibles
                    ?.prepararExistentes?.();
            }
        }

        function calcularResumen(
            terrenos,
            territorio) {
            const filtrados =
                (Array.isArray(terrenos)
                    ? terrenos
                    : [])
                    .filter(item =>
                        coincideTerritorio(
                            item,
                            territorio));

            const totalTerrenos =
                filtrados.length;

            const conAnalisis =
                filtrados.filter(
                    tieneAnalisis).length;

            const criticos =
                filtrados.filter(item =>
                    nivelTerreno(item) ===
                    "CRITICO").length;

            const atencion =
                filtrados.filter(item =>
                    nivelTerreno(item) ===
                    "ATENCION").length;

            const normales =
                filtrados.filter(item =>
                    nivelTerreno(item) ===
                    "NORMAL").length;

            const sinAnalisis =
                Math.max(
                    0,
                    totalTerrenos -
                    conAnalisis);

            const extension =
                sumar(
                    filtrados,
                    [
                        "extensionManzanas"
                    ]);

            const produccion =
                sumar(
                    filtrados,
                    [
                        "produccionQuintalesOro",
                        "cantidadQuintalesOro"
                    ]);

            const productores =
                new Set(
                    filtrados
                        .map(item =>
                            String(
                                propiedad(
                                    item,
                                    "productor") ||
                                "")
                                .trim())
                        .filter(Boolean))
                    .size;

            const cobertura =
                totalTerrenos > 0
                    ? conAnalisis /
                      totalTerrenos *
                      100
                    : 0;

            const totalAlertas =
                filtrados.reduce(
                    (total, item) =>
                        total +
                        alertasTerreno(item).length,
                    0);

            const hallazgos =
                resumirHallazgos(
                    filtrados);

            const porcentajeCritico =
                totalTerrenos > 0
                    ? criticos /
                      totalTerrenos *
                      100
                    : 0;

            const porcentajeAtencion =
                totalTerrenos > 0
                    ? atencion /
                      totalTerrenos *
                      100
                    : 0;

            let estadoClase =
                "normal";

            let estadoTexto =
                "Condición normal";

            let estadoIcono =
                "fa-solid fa-circle-check";

            if (conAnalisis === 0) {
                estadoClase =
                    "missing";

                estadoTexto =
                    "Sin información";

                estadoIcono =
                    "fa-solid fa-flask";
            } else if (
                porcentajeCritico >= 30) {
                estadoClase =
                    "critical";

                estadoTexto =
                    "Estado crítico";

                estadoIcono =
                    "fa-solid fa-circle-exclamation";
            } else if (
                porcentajeAtencion >= 30 ||
                criticos > 0) {
                estadoClase =
                    "attention";

                estadoTexto =
                    "Requiere atención";

                estadoIcono =
                    "fa-solid fa-triangle-exclamation";
            }

            const fechaReciente =
                filtrados
                    .map(item =>
                        fechaPropiedad(
                            item,
                            "fechaUltimoAnalisis"))
                    .filter(Boolean)
                    .sort((a, b) =>
                        b.getTime() -
                        a.getTime())[0] ??
                null;

            return {
                ...territorio,
                totalTerrenos,
                conAnalisis,
                sinAnalisis,
                criticos,
                atencion,
                normales,
                extension,
                produccion,
                productores,
                cobertura,
                totalAlertas,
                hallazgos,
                estadoClase,
                estadoTexto,
                estadoIcono,

                ph:
                    promedioPonderado(
                        filtrados,
                        [
                            "ultimoPh",
                            "ph"
                        ]),

                materiaOrganica:
                    promedioPonderado(
                        filtrados,
                        [
                            "materiaOrganica"
                        ]),

                acidezTotal:
                    promedioPonderado(
                        filtrados,
                        [
                            "acidezTotal"
                        ]),

                fechaRecienteTexto:
                    fechaReciente
                        ? fechaReciente
                            .toLocaleDateString(
                                "es-NI")
                        : "Sin análisis"
            };
        }

        function construirTarjeta(
            actual,
            resumen) {
            const propietario =
                esVistaPropietario(
                    actual.elementId);

            const etiqueta =
                resumen.tipo === "MUNICIPIO"
                    ? "Municipio"
                    : "Departamento";

            if (resumen.totalTerrenos <= 0) {
                return `
                    <article class="geo-territory-card no-data">
                        <header class="missing">
                            <div>
                                <span>${etiqueta}</span>
                                <h3>${escapar(resumen.nombre)}</h3>
                                <p>
                                    ${propietario
                                        ? "Resumen exclusivo de mis terrenos"
                                        : "Resumen administrativo"}
                                </p>
                            </div>

                            <b>
                                <i class="fa-solid fa-map-location-dot"></i>
                                Sin datos
                            </b>
                        </header>

                        <div class="geo-territory-empty">
                            <i class="fa-solid fa-map-location-dot"></i>
                            <strong>
                                Sin terrenos visibles
                            </strong>
                            <span>
                                No existen terrenos que coincidan con este
                                territorio y los filtros actuales.
                            </span>
                        </div>

                        <footer>
                            <small>
                                El resultado respeta las capas y filtros
                                aplicados en el mapa.
                            </small>
                        </footer>
                    </article>`;
            }

            const sueloDisponible =
                [
                    resumen.ph,
                    resumen.materiaOrganica,
                    resumen.acidezTotal
                ].some(valor =>
                    valor !== null);

            const cuartoDato =
                propietario
                    ? datoTarjeta(
                        "Con análisis",
                        entero(
                            resumen.conAnalisis),
                        "fa-solid fa-flask")
                    : datoTarjeta(
                        "Productores",
                        entero(
                            resumen.productores),
                        "fa-solid fa-users");

            const suelo =
                sueloDisponible
                    ? `
                        <section class="geo-territory-soil">
                            <div class="geo-territory-section-title">
                                <strong>
                                    <i class="fa-solid fa-vial-circle-check"></i>
                                    Promedios del último análisis
                                </strong>
                                <small>
                                    Ponderados por extensión
                                </small>
                            </div>

                            <div>
                                ${valorSuelo(
                                    "pH",
                                    resumen.ph)}

                                ${valorSuelo(
                                    "Materia orgánica",
                                    resumen.materiaOrganica,
                                    "%")}

                                ${valorSuelo(
                                    "Acidez total",
                                    resumen.acidezTotal)}
                            </div>
                        </section>`
                    : `
                        <section class="geo-territory-soil empty">
                            <i class="fa-solid fa-flask"></i>
                            No hay valores de suelo suficientes para
                            calcular promedios.
                        </section>`;

            const hallazgos =
                resumen.hallazgos.length > 0
                    ? `
                        <section class="geo-territory-findings">
                            <div class="geo-territory-section-title">
                                <strong>
                                    <i class="fa-solid fa-triangle-exclamation"></i>
                                    Principales hallazgos
                                </strong>

                                <small>
                                    ${entero(
                                        resumen.totalAlertas)}
                                    alertas
                                </small>
                            </div>

                            <div>
                                ${resumen.hallazgos
                                    .slice(0, 3)
                                    .map(item => `
                                        <span>
                                            <b>
                                                ${escapar(
                                                    item.nombre)}
                                            </b>
                                            <em>
                                                ${entero(
                                                    item.cantidad)}
                                            </em>
                                        </span>`)
                                    .join("")}
                            </div>
                        </section>`
                    : `
                        <section class="geo-territory-findings clear">
                            <i class="fa-solid fa-circle-check"></i>
                            Sin alertas visibles en este territorio.
                        </section>`;

            return `
                <article class="geo-territory-card">
                    <header class="${resumen.estadoClase}">
                        <div>
                            <span>${etiqueta}</span>
                            <h3>${escapar(resumen.nombre)}</h3>
                            <p>
                                ${propietario
                                    ? "Resumen exclusivo de mis terrenos"
                                    : "Resumen de los terrenos visibles"}
                            </p>
                        </div>

                        <b>
                            <i class="${resumen.estadoIcono}"></i>
                            ${escapar(resumen.estadoTexto)}
                        </b>
                    </header>

                    <section class="geo-territory-main-stats">
                        ${datoTarjeta(
                            "Terrenos",
                            entero(
                                resumen.totalTerrenos),
                            "fa-solid fa-seedling")}

                        ${datoTarjeta(
                            "Extensión",
                            `${numero(
                                resumen.extension,
                                2)} Mz`,
                            "fa-solid fa-ruler-combined")}

                        ${datoTarjeta(
                            "Producción",
                            `${numero(
                                resumen.produccion,
                                2)} QQ`,
                            "fa-solid fa-mug-hot")}

                        ${cuartoDato}
                    </section>

                    <section class="geo-territory-coverage">
                        <div>
                            <strong>
                                Cobertura de análisis
                            </strong>

                            <span>
                                ${entero(
                                    resumen.conAnalisis)}
                                de
                                ${entero(
                                    resumen.totalTerrenos)}
                                terrenos
                            </span>
                        </div>

                        <div class="geo-territory-progress">
                            <i style="width:${
                                limitarPorcentaje(
                                    resumen.cobertura)
                            }%"></i>
                        </div>

                        <small>
                            ${numero(
                                resumen.cobertura,
                                1)}%
                            con información de suelo
                        </small>
                    </section>

                    <section class="geo-territory-distribution">
                        ${estadoDistribucion(
                            "Crítico",
                            resumen.criticos,
                            resumen.totalTerrenos,
                            "critical")}

                        ${estadoDistribucion(
                            "Atención",
                            resumen.atencion,
                            resumen.totalTerrenos,
                            "attention")}

                        ${estadoDistribucion(
                            "Normal",
                            resumen.normales,
                            resumen.totalTerrenos,
                            "normal")}

                        ${estadoDistribucion(
                            "Sin análisis",
                            resumen.sinAnalisis,
                            resumen.totalTerrenos,
                            "missing")}
                    </section>

                    ${suelo}
                    ${hallazgos}

                    <footer>
                        <span>
                            <i class="fa-regular fa-calendar"></i>
                            Dato más reciente:
                            <b>
                                ${escapar(
                                    resumen.fechaRecienteTexto)}
                            </b>
                        </span>

                        <small>
                            Cada terreno aporta su información más reciente
                            y se respetan los filtros activos.
                        </small>
                    </footer>
                </article>`;
        }

        function datoTarjeta(
            etiqueta,
            valor,
            icono) {
            return `
                <span>
                    <i class="${icono}"></i>
                    <small>
                        ${escapar(etiqueta)}
                    </small>
                    <strong>
                        ${escapar(valor)}
                    </strong>
                </span>`;
        }

        function valorSuelo(
            etiqueta,
            valor,
            unidad = "") {
            return `
                <span>
                    <small>
                        ${escapar(etiqueta)}
                    </small>
                    <strong>
                        ${valor === null
                            ? "—"
                            : `${numero(
                                valor,
                                2)}${escapar(unidad)}`}
                    </strong>
                </span>`;
        }

        function estadoDistribucion(
            etiqueta,
            cantidad,
            total,
            clase) {
            const porcentaje =
                total > 0
                    ? cantidad /
                      total *
                      100
                    : 0;

            return `
                <span class="${clase}">
                    <small>
                        ${escapar(etiqueta)}
                    </small>

                    <strong>
                        ${entero(cantidad)}
                    </strong>

                    <em>
                        ${numero(
                            porcentaje,
                            0)}%
                    </em>
                </span>`;
        }

        function coincideTerritorio(
            item,
            territorio) {
            const departamento =
                String(
                    propiedad(
                        item,
                        "departamento") ||
                    "");

            if (normalizar(departamento) !==
                normalizar(
                    territorio.departamento ||
                    territorio.nombre)) {
                return false;
            }

            if (territorio.tipo !==
                "MUNICIPIO") {
                return true;
            }

            const municipio =
                String(
                    propiedad(
                        item,
                        "municipio") ||
                    "");

            return normalizar(municipio) ===
                normalizar(
                    territorio.municipio ||
                    territorio.nombre);
        }

        function nivelTerreno(item) {
            const valor =
                String(
                    propiedad(
                        item,
                        "nivelAlerta",
                        "nivel",
                        "estado") ||
                    "")
                    .trim()
                    .toUpperCase();

            if (valor.includes("CRIT"))
                return "CRITICO";

            if (valor.includes("ATENC"))
                return "ATENCION";

            if (valor.includes("NORMAL") ||
                valor.includes("ESTABLE")) {
                return "NORMAL";
            }

            return "SIN_ANALISIS";
        }

        function tieneAnalisis(item) {
            return numeroNullable(
                       item,
                       "ultimoPh",
                       "ph") !== null ||
                   Boolean(
                       propiedad(
                           item,
                           "fechaUltimoAnalisis"));
        }

        function sumar(
            items,
            propiedades) {
            return items.reduce(
                (total, item) =>
                    total +
                    numeroPropiedad(
                        item,
                        ...propiedades),
                0);
        }

        function promedioPonderado(
            items,
            propiedades) {
            let suma = 0;
            let pesos = 0;

            items.forEach(item => {
                const valor =
                    numeroNullable(
                        item,
                        ...propiedades);

                if (valor === null)
                    return;

                const peso =
                    Math.max(
                        numeroPropiedad(
                            item,
                            "extensionManzanas"),
                        1);

                suma +=
                    valor * peso;

                pesos +=
                    peso;
            });

            return pesos > 0
                ? suma / pesos
                : null;
        }

        function resumirHallazgos(items) {
            const conteos =
                new Map();

            items.forEach(item => {
                alertasTerreno(item)
                    .forEach(alerta => {
                        const nombre =
                            nombreAlerta(alerta);

                        if (!nombre)
                            return;

                        conteos.set(
                            nombre,
                            (conteos.get(nombre) || 0) +
                            1);
                    });
            });

            return [...conteos.entries()]
                .map(([nombre, cantidad]) => ({
                    nombre,
                    cantidad
                }))
                .sort((a, b) =>
                    b.cantidad -
                    a.cantidad ||
                    a.nombre.localeCompare(
                        b.nombre,
                        "es"));
        }

        function alertasTerreno(item) {
            const valor =
                propiedad(
                    item,
                    "alertas");

            return Array.isArray(valor)
                ? valor
                : [];
        }

        function nombreAlerta(alerta) {
            if (typeof alerta === "string")
                return alerta.trim();

            if (!alerta ||
                typeof alerta !== "object") {
                return "";
            }

            return String(
                propiedad(
                    alerta,
                    "titulo",
                    "tipo",
                    "clave",
                    "mensaje") ||
                "")
                .trim();
        }

        function propiedad(
            objeto,
            ...nombres) {
            if (!objeto ||
                typeof objeto !== "object") {
                return null;
            }

            for (const nombre of nombres) {
                if (Object.prototype
                    .hasOwnProperty.call(
                        objeto,
                        nombre)) {
                    return objeto[nombre];
                }
            }

            const entradas =
                Object.entries(objeto);

            for (const nombre of nombres) {
                const buscado =
                    nombre.toLowerCase();

                const entrada =
                    entradas.find(([clave]) =>
                        clave.toLowerCase() ===
                        buscado);

                if (entrada)
                    return entrada[1];
            }

            return null;
        }

        function numeroPropiedad(
            objeto,
            ...nombres) {
            const valor =
                propiedad(
                    objeto,
                    ...nombres);

            const dato =
                Number(valor);

            return Number.isFinite(dato)
                ? dato
                : 0;
        }

        function numeroNullable(
            objeto,
            ...nombres) {
            const valor =
                propiedad(
                    objeto,
                    ...nombres);

            if (valor === null ||
                valor === undefined ||
                valor === "") {
                return null;
            }

            const dato =
                Number(valor);

            return Number.isFinite(dato)
                ? dato
                : null;
        }

        function fechaPropiedad(
            objeto,
            ...nombres) {
            const valor =
                propiedad(
                    objeto,
                    ...nombres);

            if (!valor)
                return null;

            const fecha =
                new Date(valor);

            return Number.isNaN(
                fecha.getTime())
                ? null
                : fecha;
        }

        function esVistaPropietario(
            elementId) {
            return String(elementId)
                    .toLowerCase()
                    .includes("propietario") ||
                window.location.pathname
                    .toLowerCase()
                    .startsWith("/mi-portal/");
        }

        function normalizar(valor) {
            return String(valor || "")
                .normalize("NFD")
                .replace(
                    /[\u0300-\u036f]/g,
                    "")
                .toLowerCase()
                .replace(/\s+/g, " ")
                .trim();
        }

        function numero(
            valor,
            decimales = 2) {
            const dato =
                Number(valor);

            return Number.isFinite(dato)
                ? dato.toLocaleString(
                    "es-NI",
                    {
                        minimumFractionDigits:
                            decimales,
                        maximumFractionDigits:
                            decimales
                    })
                : "0";
        }

        function entero(valor) {
            const dato =
                Number(valor);

            return Number.isFinite(dato)
                ? Math.round(dato)
                    .toLocaleString("es-NI")
                : "0";
        }

        function limitarPorcentaje(valor) {
            return Math.max(
                0,
                Math.min(
                    100,
                    Number(valor) || 0));
        }

        function escapar(valor) {
            return String(valor ?? "")
                .replaceAll("&", "&amp;")
                .replaceAll("<", "&lt;")
                .replaceAll(">", "&gt;")
                .replaceAll('"', "&quot;")
                .replaceAll("'", "&#039;");
        }

        instalar();

        return {
            refrescar:
                procesarPopupsExistentes
        };
    })();
