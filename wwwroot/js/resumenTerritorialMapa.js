/*
 * CONATRADEC
 * Resumen territorial automático del Centro Geoespacial administrativo.
 *
 * Se activa cuando la capa "Terrenos registrados" está deshabilitada.
 * En vista nacional resume por departamento; con un departamento
 * seleccionado resume por municipio.
 */
window.conatradecResumenTerritorial =
    window.conatradecResumenTerritorial || (() => {
        const L = window.L;

        if (!L) {
            return {
                registrar: () => {},
                desregistrar: () => {},
                mostrar: () => {},
                ocultar: () => {},
                mostrarError: () => {}
            };
        }

        const mapas = new Map();
        const estados = new Map();
        const capas = new Map();
        const leyendas = new Map();
        const indicadores = new Map();
        const pendientes = new Map();

        let departamentosPromise = null;
        let municipiosPromise = null;

        const URL_DEPARTAMENTOS =
            "https://gis.unicef.org/server/rest/services/" +
            "Departamentos_Nicaragua_MIL1/MapServer/5/query" +
            "?where=1%3D1&outFields=ADM1_ES&returnGeometry=true" +
            "&outSR=4326&f=geojson";

        const URL_MUNICIPIOS = [
            "/mapa-datos/municipios.geojson?v=2.4",
            "https://cdn.jsdelivr.net/gh/armonge/" +
            "nicaragua.json@master/nicaragua.geojson",
            "https://raw.githubusercontent.com/armonge/" +
            "nicaragua.json/master/nicaragua.geojson"
        ];

        function estado(elementId) {
            if (!estados.has(elementId)) {
                estados.set(elementId, {
                    dotNetRef: null,
                    terrenosActivos: true,
                    departamentosActivos: true,
                    municipiosActivos: false,
                    departamento: "",
                    municipio: ""
                });
            }

            return estados.get(elementId);
        }

        function registrarMapas() {
            if (L.__conatradecResumenTerritorialMapas)
                return;

            const crearMapaOriginal = L.map;

            L.map = function (elemento, opciones) {
                const mapa =
                    crearMapaOriginal.call(
                        this,
                        elemento,
                        opciones);

                const elementId =
                    typeof elemento === "string"
                        ? elemento
                        : elemento?.id;

                if (elementId) {
                    mapas.set(elementId, mapa);
                    asegurarPane(mapa);

                    mapa.on("unload", () => {
                        limpiar(elementId);
                        mapas.delete(elementId);
                    });

                    setTimeout(
                        () => notificar(elementId),
                        0);
                }

                return mapa;
            };

            L.__conatradecResumenTerritorialMapas = true;
        }

        function envolverMapaInteligente() {
            const modulo =
                window.conatradecMapaInteligente;

            if (!modulo ||
                modulo.__resumenTerritorialIntegrado) {
                return;
            }

            if (typeof modulo.aplicarCapas === "function") {
                const original =
                    modulo.aplicarCapas.bind(modulo);

                modulo.aplicarCapas =
                    function (elementId, capasActivas) {
                        const resultado =
                            original(
                                elementId,
                                capasActivas);

                        const actual =
                            estado(elementId);

                        if (capasActivas) {
                            if (Object.prototype.hasOwnProperty.call(
                                    capasActivas,
                                    "terrenos")) {
                                actual.terrenosActivos =
                                    Boolean(
                                        capasActivas.terrenos);
                            }

                            if (Object.prototype.hasOwnProperty.call(
                                    capasActivas,
                                    "departamentos")) {
                                actual.departamentosActivos =
                                    Boolean(
                                        capasActivas.departamentos);
                            }

                            if (Object.prototype.hasOwnProperty.call(
                                    capasActivas,
                                    "municipios")) {
                                actual.municipiosActivos =
                                    Boolean(
                                        capasActivas.municipios);
                            }
                        }

                        if (!actual.departamentosActivos &&
                            !actual.municipiosActivos) {
                            ocultar(elementId);
                        }

                        notificar(elementId);
                        return resultado;
                    };
            }

            if (typeof modulo.cambiarCapa === "function") {
                const original =
                    modulo.cambiarCapa.bind(modulo);

                modulo.cambiarCapa =
                    function (
                        elementId,
                        clave,
                        activa) {
                        const resultado =
                            original(
                                elementId,
                                clave,
                                activa);

                        const claveNormalizada =
                            String(clave || "")
                                .toLowerCase();

                        if ([
                                "terrenos",
                                "departamentos",
                                "municipios"
                            ].includes(claveNormalizada)) {
                            const actual =
                                estado(elementId);

                            if (claveNormalizada === "terrenos") {
                                actual.terrenosActivos =
                                    Boolean(activa);
                            } else if (
                                claveNormalizada ===
                                "departamentos") {
                                actual.departamentosActivos =
                                    Boolean(activa);
                            } else {
                                actual.municipiosActivos =
                                    Boolean(activa);
                            }

                            if (!actual.departamentosActivos &&
                                !actual.municipiosActivos) {
                                ocultar(elementId);
                            }

                            notificar(elementId);
                        }

                        return resultado;
                    };
            }

            if (typeof modulo.aplicarFiltroTerritorial ===
                "function") {
                const original =
                    modulo.aplicarFiltroTerritorial
                        .bind(modulo);

                modulo.aplicarFiltroTerritorial =
                    async function (
                        elementId,
                        departamento,
                        municipio,
                        ajustarVista) {
                        const resultado =
                            await original(
                                elementId,
                                departamento,
                                municipio,
                                ajustarVista);

                        const actual =
                            estado(elementId);

                        actual.departamento =
                            String(
                                departamento || "")
                                .trim();

                        actual.municipio =
                            String(
                                municipio || "")
                                .trim();

                        notificar(elementId);
                        return resultado;
                    };
            }

            if (typeof modulo.destruir === "function") {
                const original =
                    modulo.destruir.bind(modulo);

                modulo.destruir =
                    function (elementId) {
                        limpiar(elementId);
                        mapas.delete(elementId);
                        estados.delete(elementId);
                        pendientes.delete(elementId);

                        return original(elementId);
                    };
            }

            modulo.__resumenTerritorialIntegrado = true;
        }

        function asegurarPane(mapa) {
            if (mapa.getPane(
                    "geoTerritorialSummaryPane")) {
                return;
            }

            const pane =
                mapa.createPane(
                    "geoTerritorialSummaryPane");

            /*
             * Sobre el mapa de calor, debajo de límites, capas de suelo,
             * alertas, marcadores y polígonos de terreno.
             */
            /*
             * Debe estar por encima de departamentos (360) y municipios
             * (372) para recibir hover y clic, pero debajo de suelo (390),
             * alertas y marcadores.
             */
            pane.style.zIndex = "425";
            pane.style.pointerEvents = "auto";
        }

        function registrar(elementId, dotNetRef) {
            const actual =
                estado(elementId);

            actual.dotNetRef = dotNetRef;
            notificar(elementId);

            if (pendientes.has(elementId)) {
                const respuesta =
                    pendientes.get(elementId);

                pendientes.delete(elementId);
                mostrar(elementId, respuesta);
            }
        }

        function desregistrar(elementId) {
            const actual =
                estado(elementId);

            actual.dotNetRef = null;
            ocultar(elementId);
        }

        function notificar(elementId) {
            const actual =
                estado(elementId);

            if (!actual.dotNetRef)
                return;

            actual.dotNetRef
                .invokeMethodAsync(
                    "EstadoResumenTerritorialCambiado",
                    Boolean(actual.terrenosActivos),
                    Boolean(actual.departamentosActivos),
                    Boolean(actual.municipiosActivos),
                    actual.departamento,
                    actual.municipio)
                .catch(error => {
                    console.warn(
                        "No fue posible actualizar el resumen territorial.",
                        error);
                });
        }

        async function mostrar(
            elementId,
            respuesta,
            terrenosActivos = null) {
            const mapa =
                mapas.get(elementId);

            const actual =
                estado(elementId);

            if (typeof terrenosActivos === "boolean") {
                actual.terrenosActivos =
                    terrenosActivos;
            }

            if (!mapa) {
                pendientes.set(
                    elementId,
                    respuesta);

                return;
            }

            limpiar(elementId);
            asegurarPane(mapa);

            const regiones =
                Array.isArray(respuesta?.regiones)
                    ? respuesta.regiones
                    : [];

            const resumenDisponible =
                Boolean(respuesta?.disponible) &&
                regiones.length > 0;

            const nivel =
                String(
                    respuesta?.nivelAgrupacion ||
                    (actual.municipiosActivos
                        ? "MUNICIPIO"
                        : "DEPARTAMENTO"))
                    .toUpperCase();

            const geojson =
                nivel === "MUNICIPIO"
                    ? await obtenerMunicipios()
                    : await obtenerDepartamentos();

            if (!geojson?.features?.length) {
                mostrarEstado(
                    elementId,
                    "No fue posible cargar la cartografía territorial.",
                    "error");

                return;
            }

            const indice =
                crearIndice(
                    regiones,
                    nivel);

            const capa =
                L.geoJSON(
                    geojson,
                    {
                        pane:
                            "geoTerritorialSummaryPane",

                        filter:
                            feature =>
                                nivel === "MUNICIPIO"
                                    ? Boolean(
                                        nombreMunicipio(
                                            feature))
                                    : Boolean(
                                        nombreDepartamento(
                                            feature)),

                        style:
                            feature =>
                                estiloRegion(
                                    feature,
                                    indice,
                                    nivel,
                                    actual.terrenosActivos),

                        onEachFeature:
                            (feature, layer) =>
                                configurarRegion(
                                    feature,
                                    layer,
                                    indice,
                                    nivel,
                                    actual.terrenosActivos,
                                    resumenDisponible)
                    });

            capa.addTo(mapa);
            capa.bringToFront?.();
            capas.set(elementId, capa);

            /*
             * Con terrenos visibles la capa territorial funciona como
             * identificador y ficha informativa. Sin terrenos también
             * colorea las regiones según el resumen.
             */
            if (!actual.terrenosActivos &&
                resumenDisponible) {
                crearLeyenda(
                    elementId,
                    mapa,
                    respuesta);

                mostrarEstado(
                    elementId,
                    nivel === "MUNICIPIO"
                        ? "Resumen municipal · terrenos individuales ocultos"
                        : "Resumen departamental · terrenos individuales ocultos",
                    "active",
                    respuesta.totalTerrenos,
                    respuesta.totalRegiones);
            } else if (!resumenDisponible) {
                mostrarEstado(
                    elementId,
                    respuesta?.mensaje ||
                    "Los límites están disponibles, pero el resumen no contiene datos.",
                    "empty");
            }
        }

        function crearIndice(regiones, nivel) {
            const indice = new Map();

            for (const region of regiones) {
                const clave =
                    nivel === "MUNICIPIO"
                        ? claveMunicipio(
                            region.departamento,
                            region.municipio ||
                            region.nombreTerritorio)
                        : normalizar(
                            region.departamento ||
                            region.nombreTerritorio);

                indice.set(clave, region);
            }

            return indice;
        }

        function estiloRegion(
            feature,
            indice,
            nivel,
            terrenosActivos) {
            const region =
                buscarRegion(
                    feature,
                    indice,
                    nivel);

            const color =
                region?.color ||
                "#CBD5E1";

            const modoIdentificacion =
                Boolean(terrenosActivos);

            return {
                color:
                    region
                        ? (modoIdentificacion
                            ? "#3B655B"
                            : "#FFFFFF")
                        : "#94A3B8",

                weight:
                    region
                        ? (modoIdentificacion
                            ? 1.7
                            : 1.6)
                        : 1,

                opacity:
                    modoIdentificacion
                        ? 0.78
                        : 0.95,

                fillColor: color,

                fillOpacity:
                    region
                        ? (modoIdentificacion
                            ? 0.035
                            : 0.63)
                        : (modoIdentificacion
                            ? 0.012
                            : 0.16),

                dashArray:
                    region
                        ? null
                        : "4 4"
            };
        }

        function configurarRegion(
            feature,
            layer,
            indice,
            nivel,
            terrenosActivos,
            resumenDisponible) {
            const region =
                buscarRegion(
                    feature,
                    indice,
                    nivel);

            const nombre =
                nivel === "MUNICIPIO"
                    ? nombreMunicipio(feature)
                    : nombreDepartamento(feature);

            if (!region) {
                layer.bindTooltip(
                    `<div class="geo-summary-map-tooltip empty">
                        <strong>${
                            nivel === "MUNICIPIO"
                                ? "Municipio"
                                : "Departamento"
                        }: ${escapar(nombre)}</strong>
                        <span>${
                            resumenDisponible
                                ? "Sin terrenos registrados"
                                : "Resumen no disponible"
                        }</span>
                    </div>`,
                    {
                        sticky: true,
                        direction: "top",
                        className:
                            "geo-summary-tooltip-container"
                    });

                layer.on({
                    mouseover: evento => {
                        evento.target.setStyle({
                            weight: 3,
                            fillOpacity: 0.18
                        });

                        evento.target.bringToFront?.();
                    },

                    mouseout: evento => {
                        evento.target.setStyle(
                            estiloRegion(
                                feature,
                                indice,
                                nivel,
                                terrenosActivos));
                    }
                });

                return;
            }

            layer.bindTooltip(
                construirTooltip(region),
                {
                    sticky: true,
                    direction: "top",
                    className:
                        "geo-summary-tooltip-container"
                });

            layer.bindPopup(
                construirPopup(region),
                {
                    maxWidth: 390,
                    className:
                        "geo-terrain-leaflet-popup"
                });

            layer.on({
                mouseover: evento => {
                    evento.target.setStyle({
                        weight: 3,
                        fillOpacity: 0.78
                    });

                    evento.target.bringToFront?.();
                },

                mouseout: evento => {
                    evento.target.setStyle(
                        estiloRegion(
                            feature,
                            indice,
                            nivel,
                            terrenosActivos));
                }
            });
        }

        function construirTooltip(region) {
            return `
                <div class="geo-summary-map-tooltip">
                    <strong>${
                        String(region.tipoTerritorio).toUpperCase() ===
                        "MUNICIPIO"
                            ? "Municipio"
                            : "Departamento"
                    }: ${escapar(
                        region.nombreTerritorio)}</strong>
                    <span>${escapar(
                        region.estadoTexto)}</span>
                    <b>${numeroEntero(
                        region.totalTerrenos)} terreno${
                            Number(region.totalTerrenos) === 1
                                ? ""
                                : "s"
                        }</b>
                    <small>
                        Cobertura:
                        ${numero(
                            region.coberturaAnalisisPorcentaje,
                            1)}%
                    </small>
                </div>`;
        }

        function construirPopup(region) {
            const fecha =
                region.fechaAnalisisMasReciente
                    ? new Date(
                        region.fechaAnalisisMasReciente)
                        .toLocaleDateString("es-NI")
                    : "Sin análisis";

            const nutrientes =
                Array.isArray(region.nutrientes)
                    ? region.nutrientes
                        .filter(item =>
                            Number(item.porcentajeBajo) > 0)
                        .slice(0, 5)
                    : [];

            const deficiencias =
                nutrientes.length === 0
                    ? `<div class="geo-summary-no-deficiency">
                           Sin deficiencias clasificadas como bajas.
                       </div>`
                    : nutrientes
                        .map(item => `
                            <span class="geo-summary-deficiency">
                                <b>${escapar(
                                    item.simbolo ||
                                    item.nombre)}</b>
                                <em>
                                    ${numero(
                                        item.porcentajeBajo,
                                        1)}% bajo
                                </em>
                                <small>
                                    Promedio:
                                    ${numero(
                                        item.promedio,
                                        2)}
                                    ${escapar(
                                        item.unidad || "")}
                                </small>
                            </span>`)
                        .join("");

            const advertencia =
                region.muestraLimitada
                    ? `<div class="geo-summary-sample-warning">
                           <i class="fa-solid fa-triangle-exclamation"></i>
                           Cobertura limitada; interprete los promedios
                           con cautela.
                       </div>`
                    : "";

            return `
                <div class="geo-summary-map-popup">
                    <header>
                        <span>${escapar(
                            region.tipoTerritorio)}</span>
                        <h3>${escapar(
                            region.nombreTerritorio)}</h3>
                        <b style="--summary-color:${escapar(
                            region.color)}">
                            ${escapar(region.estadoTexto)}
                        </b>
                    </header>

                    <div class="geo-summary-main-grid">
                        ${datoPopup(
                            "Terrenos",
                            numeroEntero(
                                region.totalTerrenos))}
                        ${datoPopup(
                            "Propietarios",
                            numeroEntero(
                                region.totalPropietarios))}
                        ${datoPopup(
                            "Extensión",
                            `${numero(
                                region.extensionTotalManzanas,
                                2)} Mz`)}
                        ${datoPopup(
                            "Con análisis",
                            `${numeroEntero(
                                region.conAnalisis)} de ${numeroEntero(
                                region.totalTerrenos)}`)}
                    </div>

                    <div class="geo-summary-coverage">
                        <span>
                            <i style="width:${limitarPorcentaje(
                                region.normalesPorcentaje)}%"></i>
                            Normal ${numero(
                                region.normalesPorcentaje,
                                1)}%
                        </span>
                        <span>
                            <i style="width:${limitarPorcentaje(
                                region.atencionPorcentaje)}%"></i>
                            Atención ${numero(
                                region.atencionPorcentaje,
                                1)}%
                        </span>
                        <span>
                            <i style="width:${limitarPorcentaje(
                                region.criticosPorcentaje)}%"></i>
                            Crítico ${numero(
                                region.criticosPorcentaje,
                                1)}%
                        </span>
                        <span>
                            <i style="width:${limitarPorcentaje(
                                region.sinAnalisisPorcentaje)}%"></i>
                            Sin análisis ${numero(
                                region.sinAnalisisPorcentaje,
                                1)}%
                        </span>
                    </div>

                    <div class="geo-summary-soil-grid">
                        ${valorSuelo(
                            "pH",
                            region.phPromedio)}
                        ${valorSuelo(
                            "Materia orgánica",
                            region.materiaOrganicaPromedio,
                            "%")}
                        ${valorSuelo(
                            "Acidez total",
                            region.acidezTotalPromedio)}
                        ${valorSuelo(
                            "CICE",
                            region.cicePromedio)}
                        ${valorSuelo(
                            "Saturación",
                            region.saturacionBasesPromedio,
                            "%")}
                    </div>

                    <section class="geo-summary-deficiencies">
                        <strong>
                            Principales deficiencias
                        </strong>
                        <div>${deficiencias}</div>
                    </section>

                    ${advertencia}

                    <footer>
                        <i class="fa-regular fa-calendar"></i>
                        Dato más reciente: ${escapar(fecha)}
                        <small>
                            Cada terreno aporta únicamente su último análisis.
                        </small>
                    </footer>
                </div>`;
        }

        function datoPopup(etiqueta, valor) {
            return `
                <span>
                    <small>${escapar(etiqueta)}</small>
                    <strong>${escapar(valor)}</strong>
                </span>`;
        }

        function valorSuelo(etiqueta, valor, unidad = "") {
            const disponible =
                valor !== null &&
                valor !== undefined &&
                Number.isFinite(Number(valor));

            return `
                <span>
                    <small>${escapar(etiqueta)}</small>
                    <strong>
                        ${disponible
                            ? `${numero(valor, 2)}${escapar(unidad)}`
                            : "—"}
                    </strong>
                </span>`;
        }

        function crearLeyenda(
            elementId,
            mapa,
            respuesta) {
            const control =
                L.control({
                    position: "bottomleft"
                });

            control.onAdd = () => {
                const div =
                    L.DomUtil.create(
                        "div",
                        "geo-summary-map-legend");

                L.DomEvent.disableClickPropagation(div);

                div.innerHTML = `
                    <strong>
                        Estado territorial
                    </strong>
                    <span>
                        <i style="--legend:#2F855A"></i>
                        Normal
                    </span>
                    <span>
                        <i style="--legend:#F59E0B"></i>
                        Atención
                    </span>
                    <span>
                        <i style="--legend:#DC2626"></i>
                        Crítico
                    </span>
                    <span>
                        <i style="--legend:#64748B"></i>
                        Sin información
                    </span>
                    <small>
                        ${numeroEntero(
                            respuesta.totalRegiones)}
                        ${
                            String(
                                respuesta.nivelAgrupacion)
                                .toUpperCase() ===
                                "MUNICIPIO"
                                ? "municipio(s)"
                                : "departamento(s)"
                        }
                    </small>`;

                return div;
            };

            control.addTo(mapa);
            leyendas.set(elementId, control);
        }

        function mostrarEstado(
            elementId,
            mensaje,
            clase,
            totalTerrenos = null,
            totalRegiones = null) {
            removerIndicador(elementId);

            const mapa =
                mapas.get(elementId);

            const stage =
                mapa
                    ?.getContainer()
                    ?.closest(
                        ".geo-map-stage");

            if (!stage)
                return;

            const indicador =
                document.createElement("div");

            indicador.className =
                `geo-summary-map-status ${clase}`;

            indicador.innerHTML = `
                <i class="fa-solid ${
                    clase === "error"
                        ? "fa-circle-exclamation"
                        : clase === "empty"
                            ? "fa-circle-info"
                            : "fa-chart-pie"
                }"></i>
                <div>
                    <strong>${escapar(mensaje)}</strong>
                    ${
                        totalTerrenos === null
                            ? ""
                            : `<span>
                                   ${numeroEntero(totalTerrenos)} terrenos ·
                                   ${numeroEntero(totalRegiones)} regiones
                               </span>`
                    }
                </div>`;

            stage.appendChild(indicador);
            indicadores.set(elementId, indicador);
        }

        async function mostrarError(
            elementId,
            mensaje) {
            const actual =
                estado(elementId);

            await mostrar(
                elementId,
                {
                    disponible: false,
                    mensaje:
                        mensaje ||
                        "No fue posible cargar el resumen territorial.",
                    nivelAgrupacion:
                        actual.municipiosActivos
                            ? "MUNICIPIO"
                            : "DEPARTAMENTO",
                    regiones: [],
                    totalTerrenos: 0,
                    totalRegiones: 0
                },
                actual.terrenosActivos);
        }

        function ocultar(elementId) {
            limpiar(elementId);
        }

        function limpiar(elementId) {
            const mapa =
                mapas.get(elementId);

            const capa =
                capas.get(elementId);

            if (mapa && capa) {
                try {
                    mapa.removeLayer(capa);
                } catch {
                }
            }

            capas.delete(elementId);

            const leyenda =
                leyendas.get(elementId);

            if (mapa && leyenda) {
                try {
                    mapa.removeControl(leyenda);
                } catch {
                }
            }

            leyendas.delete(elementId);
            removerIndicador(elementId);
        }

        function removerIndicador(elementId) {
            const indicador =
                indicadores.get(elementId);

            indicador?.remove();
            indicadores.delete(elementId);
        }

        function buscarRegion(feature, indice, nivel) {
            if (nivel === "MUNICIPIO") {
                return indice.get(
                    claveMunicipio(
                        departamentoMunicipio(feature),
                        nombreMunicipio(feature))) ||
                    null;
            }

            return indice.get(
                normalizar(
                    nombreDepartamento(feature))) ||
                null;
        }

        function claveMunicipio(
            departamento,
            municipio) {
            return `${normalizar(departamento)}|${normalizar(municipio)}`;
        }

        function nombreDepartamento(feature) {
            const p =
                feature?.properties || {};

            return primeraPropiedad(
                p,
                [
                    "ADM1_ES",
                    "NAME_1",
                    "departamento",
                    "Departamento",
                    "DEPARTAMEN",
                    "NOMBRE"
                ]);
        }

        function nombreMunicipio(feature) {
            const p =
                feature?.properties || {};

            return primeraPropiedad(
                p,
                [
                    "Municipio",
                    "MUNICIPIO",
                    "ADM2_ES",
                    "NAME_2",
                    "municipio",
                    "NOMBRE",
                    "shapeName"
                ]) ||
                campoDescripcion(
                    p.Description,
                    [
                        "MUNICIPIO",
                        "N_MUNIC",
                        "N_MUNICIPI"
                    ]);
        }

        function departamentoMunicipio(feature) {
            const p =
                feature?.properties || {};

            return primeraPropiedad(
                p,
                [
                    "Departam_1",
                    "DEPARTAMEN",
                    "Departamento",
                    "DEPARTAMENTO",
                    "ADM1_ES",
                    "NAME_1",
                    "departamento",
                    "shapeGroup"
                ]) ||
                campoDescripcion(
                    p.Description,
                    [
                        "N_DEPTO",
                        "DEPARTAMENTO"
                    ]);
        }

        function campoDescripcion(
            descripcion,
            campos) {
            if (!descripcion)
                return "";

            const texto =
                String(descripcion)
                    .replace(/<br\s*\/?>(?=.)/gi, "\n")
                    .replace(/&nbsp;/gi, " ");

            for (const campo of campos) {
                const expresion =
                    new RegExp(
                        `(?:^|\\n)\\s*${campo}` +
                        `\\s*=\\s*([^\\n<]+)`,
                        "i");

                const coincidencia =
                    texto.match(expresion);

                if (coincidencia?.[1]) {
                    return coincidencia[1]
                        .trim();
                }
            }

            return "";
        }

        function primeraPropiedad(objeto, nombres) {
            for (const nombre of nombres) {
                const valor =
                    objeto?.[nombre];

                if (valor !== null &&
                    valor !== undefined &&
                    String(valor).trim()) {
                    return String(valor).trim();
                }
            }

            return "";
        }

        async function obtenerDepartamentos() {
            if (!departamentosPromise) {
                departamentosPromise =
                    fetch(URL_DEPARTAMENTOS)
                        .then(validarRespuesta)
                        .catch(error => {
                            departamentosPromise = null;
                            throw error;
                        });
            }

            return await departamentosPromise;
        }

        async function obtenerMunicipios() {
            if (!municipiosPromise) {
                municipiosPromise =
                    cargarPrimeraFuente(
                        URL_MUNICIPIOS)
                        .catch(error => {
                            municipiosPromise = null;
                            throw error;
                        });
            }

            return await municipiosPromise;
        }

        async function cargarPrimeraFuente(urls) {
            let ultimoError = null;

            for (const url of urls) {
                try {
                    const respuesta =
                        await fetch(
                            url,
                            {
                                cache: "default"
                            });

                    if (!respuesta.ok)
                        continue;

                    const geojson =
                        await respuesta.json();

                    if (geojson?.features?.length)
                        return geojson;
                } catch (error) {
                    ultimoError = error;
                }
            }

            throw ultimoError ||
                new Error(
                    "No se encontró cartografía municipal.");
        }

        async function validarRespuesta(respuesta) {
            if (!respuesta.ok) {
                throw new Error(
                    `Cartografía no disponible (${respuesta.status}).`);
            }

            return await respuesta.json();
        }

        function normalizar(valor) {
            return String(valor || "")
                .normalize("NFD")
                .replace(/[\u0300-\u036f]/g, "")
                .toLowerCase()
                .replace(/\s+/g, " ")
                .trim();
        }

        function numero(valor, decimales = 2) {
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

        function numeroEntero(valor) {
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

        registrarMapas();
        envolverMapaInteligente();

        return {
            registrar,
            desregistrar,
            mostrar,
            ocultar,
            mostrarError
        };
    })();
