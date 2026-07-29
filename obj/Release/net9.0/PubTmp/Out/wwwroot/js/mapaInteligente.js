window.conatradecMapaInteligente = (() => {
    const mapas = new Map();
    let departamentosPromise = null;

    const CENTRO_NICARAGUA = [12.8654, -85.2072];
    const LIMITES_NACIONALES = L.latLngBounds(
        [10.55, -88.05],
        [15.25, -82.35]);
    const VISTA_NACIONAL = L.latLngBounds(
        [10.72, -87.82],
        [15.08, -82.82]);

    const CONTORNO_RESPALDO = [
        [15.02, -87.68],
        [15.08, -86.45],
        [15.00, -85.20],
        [14.88, -84.10],
        [14.60, -83.15],
        [13.75, -83.22],
        [12.80, -83.50],
        [11.78, -83.72],
        [10.72, -83.90],
        [10.70, -85.65],
        [11.02, -86.77],
        [11.68, -87.18],
        [12.72, -87.70],
        [13.75, -87.75]
    ];

    const URL_LIMITE_NACIONAL = "data/nicaragua.geojson";

    const URL_DEPARTAMENTOS =
        "https://gis.unicef.org/server/rest/services/" +
        "Departamentos_Nicaragua_MIL1/MapServer/5/query" +
        "?where=1%3D1&outFields=ADM1_ES&returnGeometry=true" +
        "&outSR=4326&f=geojson";

    // Fuentes municipales ordenadas por rapidez y confiabilidad.
    // El CDN y GitHub se consultan directamente desde el navegador; el
    // proxy del portal y ArcGIS quedan como respaldos. Esto evita que la
    // interfaz quede esperando al proveedor cartográfico del servidor.
    const URL_MUNICIPIOS_CDN =
        "https://cdn.jsdelivr.net/gh/armonge/" +
        "nicaragua.json@master/nicaragua.geojson";

    const URL_MUNICIPIOS_GITHUB =
        "https://raw.githubusercontent.com/armonge/" +
        "nicaragua.json/master/nicaragua.geojson";

    const URL_MUNICIPIOS_LOCAL =
        "/mapa-datos/municipios.geojson?v=2.4";

    const URL_MUNICIPIOS_EXTERNO =
        "https://gis.unicef.org/server/rest/services/" +
        "Limites_Municipales_MIL1/MapServer/0/query" +
        "?where=1%3D1&outFields=Departam_1,Municipio" +
        "&returnGeometry=true&outSR=4326" +
        "&geometryPrecision=5&maxAllowableOffset=0.0005" +
        "&returnTrueCurves=false&f=geojson";

    const configuracionCapasInicial = {
        departamentos: true,
        municipios: false,
        terrenos: true,
        alertas: true,
        ph: false,
        temperatura: false,
        humedad: false,
        lluvia: false,
        viento: false
    };

    function inicializar(elementId, dotNetReference) {
        destruir(elementId);

        const elemento = document.getElementById(elementId);
        if (!elemento || typeof L === "undefined") {
            return false;
        }

        const mapa = L.map(elementId, {
            center: CENTRO_NICARAGUA,
            zoom: 7,
            minZoom: 6,
            maxZoom: 19,
            maxBounds: LIMITES_NACIONALES,
            maxBoundsViscosity: 1,
            zoomControl: false,
            preferCanvas: true,
            worldCopyJump: false
        });

        crearPaneles(mapa);

        const capasBase = crearCapasBase();
        capasBase.calles.addTo(mapa);

        L.control.zoom({
            position: "bottomright",
            zoomInTitle: "Acercar",
            zoomOutTitle: "Alejar"
        }).addTo(mapa);

        const contexto = {
            elementId,
            elemento,
            mapa,
            dotNetReference,
            capasBase,
            mapaBaseActivo: "calles",
            capasActivas: { ...configuracionCapasInicial },
            terrenos: [],
            clima: null,
            climaOriginal: null,
            filtroTerritorial: {
                departamento: "",
                municipio: "",
                nivel: "NACIONAL",
                feature: null
            },
            capas: {
                departamentos: null,
                municipios: null,
                mascaraNacional: null,
                bordeNacional: null,
                terrenos: crearGrupoTerrenos(),
                alertas: L.layerGroup([], { pane: "geoAlertPane" }),
                ph: L.layerGroup([], { pane: "geoSoilPane" }),
                temperatura: null,
                humedad: null,
                lluvia: null,
                viento: null
            },
            respuestasSuelo: {},
            geojsonDepartamentos: null,
            geojsonMunicipios: null,
            municipiosCargando: null,
            controlLeyenda: crearControlLeyenda(mapa)
        };

        mapas.set(elementId, contexto);

        mapa.fitBounds(VISTA_NACIONAL, {
            padding: [12, 12],
            animate: false
        });

        contexto.capas.terrenos.addTo(mapa);
        contexto.capas.alertas.addTo(mapa);

        // Oculta el exterior desde el primer cuadro mientras se obtiene
        // la cartografía detallada del país.
        construirMascaraNacional(contexto, [CONTORNO_RESPALDO]);

        configurarEventos(contexto);
        cargarGeografiaNacional(contexto);

        setTimeout(() => mapa.invalidateSize(), 160);
        return true;
    }

    function crearPaneles(mapa) {
        const paneles = [
            ["geoHeatPane", 330],
            ["geoMaskPane", 350],
            ["geoDepartmentPane", 360],
            ["geoMunicipalityPane", 372],
            ["geoSoilPane", 390],
            ["geoAlertPane", 410],
            ["geoMarkerPane", 430]
        ];

        paneles.forEach(([nombre, zIndex]) => {
            const panel = mapa.createPane(nombre);
            panel.style.zIndex = String(zIndex);
        });

        mapa.getPane("geoMaskPane").style.pointerEvents = "none";
        mapa.getPane("geoHeatPane").style.pointerEvents = "none";
        mapa.getPane("geoSoilPane").style.pointerEvents = "auto";
        mapa.getPane("geoAlertPane").style.pointerEvents = "none";
    }

    function crearCapasBase() {
        const opcionesComunes = {
            minZoom: 6,
            maxZoom: 19,
            noWrap: true,
            bounds: LIMITES_NACIONALES,
            updateWhenIdle: true,
            keepBuffer: 2
        };

        return {
            calles: L.tileLayer(
                "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
                {
                    ...opcionesComunes,
                    attribution: "&copy; OpenStreetMap contributors"
                }),

            satelite: L.tileLayer(
                "https://server.arcgisonline.com/ArcGIS/rest/services/" +
                "World_Imagery/MapServer/tile/{z}/{y}/{x}",
                {
                    ...opcionesComunes,
                    attribution:
                        "Tiles &copy; Esri, Maxar, Earthstar Geographics"
                })
        };
    }

    function crearGrupoTerrenos() {
        if (typeof L.markerClusterGroup === "function") {
            return L.markerClusterGroup({
                showCoverageOnHover: false,
                maxClusterRadius: 52,
                spiderfyOnMaxZoom: true,
                removeOutsideVisibleBounds: true,
                chunkedLoading: true,
                chunkInterval: 80,
                chunkDelay: 25
            });
        }

        return L.layerGroup();
    }

    function configurarEventos(contexto) {
        const { mapa, elemento } = contexto;

        mapa.on("mousemove", evento => {
            const etiqueta = document.getElementById("mapaCoordenadas");
            if (!etiqueta) return;

            etiqueta.textContent =
                `${evento.latlng.lat.toFixed(4)}, ` +
                `${evento.latlng.lng.toFixed(4)}`;
        });

        mapa.on("mouseout", () => {
            const etiqueta = document.getElementById("mapaCoordenadas");
            if (etiqueta) etiqueta.textContent = "Nicaragua";
        });

        mapa.on("zoomend", () => {
            const etiqueta = document.getElementById("mapaZoom");
            if (etiqueta) etiqueta.textContent = String(mapa.getZoom());
        });

        const eventoPantallaCompleta = () => {
            elemento.closest(".geo-map-stage")
                ?.classList.toggle(
                    "is-fullscreen",
                    Boolean(document.fullscreenElement));

            setTimeout(() => mapa.invalidateSize(), 160);
        };

        document.addEventListener(
            "fullscreenchange",
            eventoPantallaCompleta);

        contexto.eventoPantallaCompleta = eventoPantallaCompleta;
    }

    async function cargarGeografiaNacional(contexto) {
        let limiteNacional = null;

        try {
            limiteNacional = await obtenerLimiteNacional();

            if (!mapas.has(contexto.elementId)) return;

            construirMascaraNacional(
                contexto,
                extraerAnillosNacionales(limiteNacional));
        } catch (error) {
            console.warn(
                "No fue posible obtener el límite nacional local.",
                error);
        }

        let departamentos = null;

        try {
            departamentos = await obtenerDepartamentos();
        } catch (error) {
            console.warn(
                "No fue posible obtener la cartografía departamental.",
                error);
        }

        if (!mapas.has(contexto.elementId)) return;

        if (departamentos) {
            construirMascaraNacional(
                contexto,
                extraerAnillosNacionales(departamentos));
            construirDepartamentos(contexto, departamentos);
        } else {
            construirBordeRespaldo(contexto);
        }

        aplicarVisibilidad(contexto, "departamentos");
        actualizarLeyenda(contexto);
    }

    async function obtenerLimiteNacional() {
        const respuesta = await fetch(URL_LIMITE_NACIONAL, {
            cache: "force-cache"
        });

        if (!respuesta.ok) {
            throw new Error(
                "No fue posible cargar el límite nacional de Nicaragua.");
        }

        return await respuesta.json();
    }

    function obtenerDepartamentos() {
        if (departamentosPromise) return departamentosPromise;

        departamentosPromise = fetch(URL_DEPARTAMENTOS, {
            cache: "force-cache"
        })
            .then(respuesta => {
                if (!respuesta.ok) {
                    throw new Error(
                        "No fue posible descargar los límites de Nicaragua.");
                }

                return respuesta.json();
            })
            .catch(error => {
                departamentosPromise = null;
                throw error;
            });

        return departamentosPromise;
    }

    function construirDepartamentos(contexto, geojson) {
        contexto.geojsonDepartamentos = geojson;

        if (contexto.capas.departamentos) {
            contexto.capas.departamentos.remove();
        }

        contexto.capas.departamentos = L.geoJSON(geojson, {
            pane: "geoDepartmentPane",
            style: feature => estiloDepartamento(contexto, feature),
            onEachFeature: (feature, layer) => {
                const nombre = nombreDepartamento(feature);

                layer.bindTooltip(nombre, {
                    sticky: true,
                    direction: "top",
                    className: "department-tooltip"
                });

                layer.on({
                    mouseover: evento => {
                        evento.target.setStyle({
                            weight: 2.8,
                            fillOpacity: 0.16
                        });
                    },
                    mouseout: evento => {
                        contexto.capas.departamentos
                            ?.resetStyle(evento.target);
                    },
                    click: evento => {
                        contexto.mapa.fitBounds(
                            evento.target.getBounds(),
                            {
                                padding: [35, 35],
                                maxZoom: 10
                            });

                        const estadistica = estadisticaDepartamento(
                            contexto,
                            nombre);

                        evento.target.bindPopup(`
                            <div class="geo-department-popup">
                                <span>Departamento</span>
                                <h3>${escapar(nombre)}</h3>
                                <div>
                                    <strong>${estadistica.terrenos}</strong>
                                    <small>Terrenos registrados</small>
                                </div>
                                <div>
                                    <strong>${estadistica.extension.toFixed(2)} Mz</strong>
                                    <small>Extensión visible</small>
                                </div>
                                <small class="geo-territorial-hint">
                                    Se mostrarán sus municipios y terrenos.
                                </small>
                            </div>
                        `).openPopup();

                        seleccionarDepartamento(contexto, nombre);
                    }
                });
            }
        });
    }

    function estiloDepartamento(contexto, feature) {
        const nombre = nombreDepartamento(feature);
        const estadistica = estadisticaDepartamento(contexto, nombre);
        const seleccionado = contexto.filtroTerritorial?.departamento;
        const coincide = !seleccionado ||
            normalizar(nombre) === normalizar(seleccionado);

        return {
            color: coincide
                ? (estadistica.criticos > 0 ? "#9f2f2f" : "#3B655B")
                : "#94a3b8",
            weight: seleccionado && coincide ? 2.8 : 1.35,
            opacity: coincide ? 0.96 : 0.28,
            fillColor: estadistica.criticos > 0 ? "#EF4444" : "#5c8a7e",
            fillOpacity: coincide
                ? (estadistica.terrenos > 0 ? 0.09 : 0.035)
                : 0.008
        };
    }

    function estadisticaDepartamento(contexto, nombre) {
        const normalizado = normalizar(nombre);
        const terrenos = contexto.terrenos.filter(
            item => normalizar(item.departamento) === normalizado);

        return {
            terrenos: terrenos.length,
            criticos: terrenos.filter(
                item => String(item.nivelAlerta).toUpperCase() === "CRITICA")
                .length,
            extension: terrenos.reduce(
                (total, item) =>
                    total + (Number(item.extensionManzanas) || 0),
                0)
        };
    }

    function nombreDepartamento(feature) {
        const propiedades = feature?.properties || {};

        return propiedades.ADM1_ES ||
            propiedades.adm1_es ||
            propiedades.NAME_1 ||
            propiedades.nombre ||
            "Departamento";
    }

    function extraerAnillosNacionales(geojson) {
        const anillos = [];

        (geojson?.features || []).forEach(feature => {
            const geometria = feature?.geometry;
            if (!geometria) return;

            if (geometria.type === "Polygon") {
                agregarAnillo(anillos, geometria.coordinates?.[0]);
            } else if (geometria.type === "MultiPolygon") {
                (geometria.coordinates || []).forEach(poligono => {
                    agregarAnillo(anillos, poligono?.[0]);
                });
            }
        });

        return anillos.length > 0 ? anillos : [CONTORNO_RESPALDO];
    }

    function agregarAnillo(destino, coordenadas) {
        if (!Array.isArray(coordenadas) || coordenadas.length < 3) return;

        destino.push(
            coordenadas
                .filter(item => Array.isArray(item) && item.length >= 2)
                .map(item => [Number(item[1]), Number(item[0])])
                .filter(item =>
                    Number.isFinite(item[0]) && Number.isFinite(item[1])));
    }

    function construirMascaraNacional(contexto, anillosNicaragua) {
        contexto.capas.mascaraNacional?.remove();
        contexto.capas.bordeNacional?.remove();

        const mundo = [
            [-90, -180],
            [-90, 180],
            [90, 180],
            [90, -180]
        ];

        contexto.capas.mascaraNacional = L.polygon(
            [mundo, ...anillosNicaragua],
            {
                pane: "geoMaskPane",
                stroke: false,
                fill: true,
                fillColor: "#12211c",
                fillOpacity: 1,
                fillRule: "evenodd",
                interactive: false
            })
            .addTo(contexto.mapa);

        contexto.capas.bordeNacional = L.polyline(
            anillosNicaragua,
            {
                pane: "geoDepartmentPane",
                color: "#f0c94d",
                weight: 2.25,
                opacity: 0.95,
                interactive: false
            })
            .addTo(contexto.mapa);
    }

    function construirBordeRespaldo(contexto) {
        contexto.capas.departamentos = L.polygon(
            CONTORNO_RESPALDO,
            {
                pane: "geoDepartmentPane",
                color: "#3B655B",
                weight: 1.5,
                fillColor: "#3B655B",
                fillOpacity: 0.04,
                interactive: false
            });
    }

    async function cargarMunicipios(contexto) {
        if (contexto.capas.municipios) {
            aplicarVisibilidad(contexto, "municipios");
            return contexto.capas.municipios;
        }

        if (contexto.municipiosCargando) {
            return contexto.municipiosCargando;
        }

        contexto.municipiosCargando = descargarMunicipiosGeoJson()
            .then(geojson => {
                if (!mapas.has(contexto.elementId)) return null;

                construirMunicipios(contexto, geojson);
                aplicarVisibilidad(contexto, "municipios");
                mostrarMensajeMapa(
                    contexto,
                    `${geojson.features.length} límites municipales cargados.`,
                    "info");
                return contexto.capas.municipios;
            })
            .catch(error => {
                contexto.capasActivas.municipios = false;
                mostrarMensajeMapa(
                    contexto,
                    "No fue posible cargar los límites municipales. " +
                    "Actualiza la página e inténtalo nuevamente.",
                    "error");
                throw error;
            })
            .finally(() => {
                contexto.municipiosCargando = null;
            });

        return contexto.municipiosCargando;
    }

    async function descargarMunicipiosGeoJson() {
        const fuentes = [
            { url: URL_MUNICIPIOS_CDN, espera: 18000 },
            { url: URL_MUNICIPIOS_GITHUB, espera: 18000 },
            { url: URL_MUNICIPIOS_LOCAL, espera: 12000 },
            { url: URL_MUNICIPIOS_EXTERNO, espera: 18000 }
        ];

        let ultimoError = null;

        for (const fuente of fuentes) {
            const controlador = new AbortController();
            const temporizador = setTimeout(
                () => controlador.abort(),
                fuente.espera);

            try {
                const respuesta = await fetch(fuente.url, {
                    cache: "force-cache",
                    mode: "cors",
                    credentials: "omit",
                    signal: controlador.signal,
                    headers: {
                        Accept: "application/geo+json, application/json"
                    }
                });

                if (!respuesta.ok) {
                    throw new Error(
                        `La cartografía municipal respondió ${respuesta.status}.`);
                }

                const geojson = await respuesta.json();
                validarMunicipiosGeoJson(geojson);
                return geojson;
            } catch (error) {
                ultimoError = error;
                console.warn(
                    `No fue posible cargar municipios desde ${fuente.url}.`,
                    error);
            } finally {
                clearTimeout(temporizador);
            }
        }

        throw ultimoError ||
            new Error("No fue posible descargar los municipios.");
    }

    function validarMunicipiosGeoJson(geojson) {
        if (!geojson ||
            geojson.type !== "FeatureCollection" ||
            !Array.isArray(geojson.features) ||
            geojson.features.length === 0) {
            throw new Error(
                "La respuesta no contiene límites municipales válidos.");
        }
    }

    function mostrarMensajeMapa(contexto, mensaje, tipo = "info") {
        if (!contexto?.elemento) return;

        contexto.elemento
            .querySelectorAll(".geo-map-message")
            .forEach(elemento => elemento.remove());

        const aviso = document.createElement("div");
        aviso.className = `geo-map-message geo-map-message-${tipo}`;
        aviso.setAttribute("role", "alert");
        aviso.textContent = mensaje;
        contexto.elemento.appendChild(aviso);

        setTimeout(() => {
            aviso.classList.add("is-visible");
        }, 10);

        setTimeout(() => {
            aviso.classList.remove("is-visible");
            setTimeout(() => aviso.remove(), 250);
        }, 6500);
    }

    function construirMunicipios(contexto, geojson) {
        contexto.geojsonMunicipios = geojson;

        if (contexto.capas.municipios) {
            contexto.capas.municipios.remove();
        }

        contexto.capas.municipios = L.geoJSON(geojson, {
            pane: "geoMunicipalityPane",
            style: feature => estiloMunicipio(contexto, feature),
            onEachFeature: (feature, layer) => {
                const municipio = nombreMunicipio(feature);
                const departamento = departamentoMunicipio(feature);

                layer.bindTooltip(
                    departamento
                        ? `${municipio}, ${departamento}`
                        : municipio,
                    {
                        sticky: true,
                        direction: "top",
                        className: "municipality-tooltip"
                    });

                layer.on({
                    mouseover: evento => {
                        evento.target.setStyle({
                            weight: 2.3,
                            fillOpacity: 0.18
                        });
                    },
                    mouseout: evento => {
                        contexto.capas.municipios?.resetStyle(evento.target);
                    },
                    click: evento => {
                        contexto.mapa.fitBounds(evento.target.getBounds(), {
                            padding: [42, 42],
                            maxZoom: 13
                        });

                        const estadistica = estadisticaMunicipio(
                            contexto,
                            municipio,
                            departamento);

                        evento.target.bindPopup(`
                            <div class="geo-department-popup municipality">
                                <span>Municipio</span>
                                <h3>${escapar(municipio)}</h3>
                                <small>${escapar(departamento)}</small>
                                <div>
                                    <strong>${estadistica.terrenos}</strong>
                                    <small>Terrenos registrados</small>
                                </div>
                                <div>
                                    <strong>${estadistica.extension.toFixed(2)} Mz</strong>
                                    <small>Extensión visible</small>
                                </div>
                            </div>
                        `).openPopup();

                        seleccionarMunicipio(
                            contexto,
                            municipio,
                            departamento);
                    }
                });
            }
        });
    }

    function estiloMunicipio(contexto, feature) {
        const municipio = nombreMunicipio(feature);
        const departamento = departamentoMunicipio(feature);
        const estadistica = estadisticaMunicipio(
            contexto,
            municipio,
            departamento);
        const filtro = contexto.filtroTerritorial || {};
        const coincideDepartamento = !filtro.departamento ||
            normalizar(departamento) === normalizar(filtro.departamento);
        const coincideMunicipio = !filtro.municipio ||
            (normalizar(municipio) === normalizar(filtro.municipio) &&
             coincideDepartamento);
        const coincide = coincideDepartamento && coincideMunicipio;

        return {
            color: coincide
                ? (estadistica.terrenos > 0 ? "#9B552C" : "#2f6f61")
                : "#94a3b8",
            weight: filtro.municipio && coincide
                ? 2.8
                : (estadistica.terrenos > 0 ? 1.35 : 1.05),
            opacity: coincide ? 0.96 : 0.18,
            dashArray: filtro.municipio && coincide ? null : "4 3",
            lineCap: "round",
            fillColor: estadistica.terrenos > 0 ? "#F2C94C" : "#3B655B",
            fillOpacity: coincide
                ? (filtro.municipio ? 0.16 :
                    (estadistica.terrenos > 0 ? 0.09 : 0.035))
                : 0.005
        };
    }

    function nombreMunicipio(feature) {
        const propiedades = feature?.properties || {};
        return propiedades.Municipio ||
            propiedades.MUNICIPIO ||
            propiedades.NAME_2 ||
            propiedades.NOMBRE ||
            propiedades.shapeName ||
            campoDescripcionMunicipal(
                propiedades.Description,
                ["MUNICIPIO", "N_MUNIC", "N_MUNICIPI"]) ||
            "Municipio";
    }

    function departamentoMunicipio(feature) {
        const propiedades = feature?.properties || {};
        return propiedades.Departam_1 ||
            propiedades.DEPARTAMEN ||
            propiedades.Departamento ||
            propiedades.NAME_1 ||
            propiedades.shapeGroup ||
            campoDescripcionMunicipal(
                propiedades.Description,
                ["N_DEPTO", "DEPARTAMENTO"]) ||
            "";
    }

    function campoDescripcionMunicipal(descripcion, campos) {
        if (!descripcion) return "";

        const texto = String(descripcion)
            .replace(/<br\s*\/?>(?=.)/gi, "\n")
            .replace(/&nbsp;/gi, " ");

        for (const campo of campos) {
            const expresion = new RegExp(
                `(?:^|\\n)\\s*${campo}\\s*=\\s*([^\\n<]+)`,
                "i");
            const coincidencia = texto.match(expresion);

            if (coincidencia?.[1]) {
                return coincidencia[1].trim();
            }
        }

        return "";
    }

    function estadisticaMunicipio(contexto, municipio, departamento) {
        const municipioNormalizado = normalizar(municipio);
        const departamentoNormalizado = normalizar(departamento);
        const terrenos = contexto.terrenos.filter(item =>
            normalizar(item.municipio) === municipioNormalizado &&
            (!departamentoNormalizado ||
             normalizar(item.departamento) === departamentoNormalizado));

        return {
            terrenos: terrenos.length,
            extension: terrenos.reduce(
                (total, item) => total +
                    (Number(item.extensionManzanas) || 0),
                0)
        };
    }

    function seleccionarDepartamento(contexto, nombre) {
        if (!contexto.dotNetReference) return;

        contexto.dotNetReference
            .invokeMethodAsync("SeleccionarDepartamentoMapa", nombre)
            .catch(error => {
                console.warn("No fue posible filtrar el departamento.", error);
            });
    }

    function seleccionarMunicipio(contexto, municipio, departamento) {
        if (!contexto.dotNetReference) return;

        contexto.dotNetReference
            .invokeMethodAsync(
                "SeleccionarMunicipioMapa",
                municipio,
                departamento)
            .catch(error => {
                console.warn("No fue posible filtrar el municipio.", error);
            });
    }

    function mostrarTerrenos(
        elementId,
        terrenos,
        ajustarVista = true) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        contexto.terrenos = Array.isArray(terrenos) ? terrenos : [];
        contexto.capas.terrenos.clearLayers();
        contexto.capas.alertas.clearLayers();
        if (contexto.capas.ph?.clearLayers) {
            contexto.capas.ph.clearLayers();
        }

        const limites = [];

        contexto.terrenos.forEach(terreno => {
            const latitud = Number(terreno.latitud);
            const longitud = Number(terreno.longitud);

            if (!coordenadaValida(latitud, longitud)) return;

            const clase = clasePorNivel(terreno.nivelAlerta);
            const icono = crearIconoTerreno(clase);

            const marcador = L.marker(
                [latitud, longitud],
                {
                    icon: icono,
                    pane: "geoMarkerPane",
                    riseOnHover: true,
                    title: terreno.codigo || "Terreno"
                });

            marcador.bindPopup(
                construirPopup(contexto, terreno),
                {
                    maxWidth: 360,
                    minWidth: 290,
                    className: "geo-terrain-leaflet-popup"
                });

            marcador.on("click", () => {
                seleccionarTerreno(contexto, terreno.terrenoId);
            });

            contexto.capas.terrenos.addLayer(marcador);
            limites.push([latitud, longitud]);

            agregarIndicadorAlerta(
                contexto,
                terreno,
                latitud,
                longitud,
                clase);

        });

        if (contexto.capas.departamentos) {
            contexto.capas.departamentos.setStyle(
                feature => estiloDepartamento(contexto, feature));
        }

        if (ajustarVista) {
            ajustarVistaTerrenos(contexto, limites);
        }

        Object.keys(contexto.capasActivas).forEach(
            clave => aplicarVisibilidad(contexto, clave));

        actualizarLeyenda(contexto);
        setTimeout(() => contexto.mapa.invalidateSize(), 120);
    }

    function crearIconoTerreno(clase) {
        return L.divIcon({
            className: "smart-marker-wrapper",
            html: `
                <span class="smart-marker ${clase}">
                    <i class="fa-solid fa-seedling"></i>
                </span>`,
            iconSize: [38, 45],
            iconAnchor: [19, 42],
            popupAnchor: [0, -38]
        });
    }

    function agregarIndicadorAlerta(
        contexto,
        terreno,
        latitud,
        longitud,
        clase) {
        if (clase !== "critical" && clase !== "attention") return;

        const indicador = L.circleMarker(
            [latitud, longitud],
            {
                pane: "geoAlertPane",
                radius: clase === "critical" ? 18 : 14,
                color: clase === "critical" ? "#EF4444" : "#FF9800",
                weight: 2,
                opacity: 0.68,
                fillColor: clase === "critical" ? "#EF4444" : "#FF9800",
                fillOpacity: 0.14,
                className: `geo-alert-ring ${clase}`,
                interactive: false
            });

        indicador.options.terrenoId = terreno.terrenoId;
        contexto.capas.alertas.addLayer(indicador);
    }

    function agregarIndicadorPh(
        contexto,
        terreno,
        latitud,
        longitud) {
        const ph = Number(terreno.ultimoPh);
        if (!Number.isFinite(ph)) return;

        const color = colorPh(ph);

        const punto = L.circleMarker(
            [latitud, longitud],
            {
                pane: "geoSoilPane",
                radius: 10,
                color: "#ffffff",
                weight: 1.5,
                opacity: 0.95,
                fillColor: color,
                fillOpacity: 0.88,
                interactive: false
            });

        contexto.capas.ph.addLayer(punto);
    }

    function colorPh(ph) {
        if (ph < 4.8) return "#b91c1c";
        if (ph < 5.3) return "#f97316";
        if (ph < 6.5) return "#2f855a";
        if (ph < 7.2) return "#0ea5e9";
        return "#6d28d9";
    }

    function ajustarVistaTerrenos(contexto, limites) {
        if (limites.length === 1) {
            contexto.mapa.setView(limites[0], 15, { animate: true });
        } else if (limites.length > 1) {
            const bounds = L.latLngBounds(limites);

            contexto.mapa.fitBounds(bounds, {
                padding: [48, 48],
                maxZoom: 13,
                animate: true
            });
        } else {
            centrarNicaragua(contexto.elementId);
        }
    }

    async function mostrarCapaSuelo(elementId, respuesta) {
        const contexto = mapas.get(elementId);
        if (!contexto || !respuesta?.clave) return;

        const clave = String(respuesta.clave).toLowerCase();
        const capaAnterior = contexto.capas[clave];

        if (capaAnterior && contexto.mapa.hasLayer(capaAnterior)) {
            contexto.mapa.removeLayer(capaAnterior);
        }

        const nivel = String(
            respuesta.nivelAgrupacion || "DEPARTAMENTO")
            .toUpperCase();

        if (nivel === "MUNICIPIO" && !contexto.geojsonMunicipios) {
            try {
                await cargarMunicipios(contexto);
            } catch (error) {
                console.warn(
                    "No fue posible preparar la cartografía municipal " +
                    "para la capa de suelo.",
                    error);
            }
        } else if (nivel === "DEPARTAMENTO" &&
                   !contexto.geojsonDepartamentos) {
            try {
                const departamentos = await obtenerDepartamentos();

                if (!contexto.geojsonDepartamentos) {
                    construirDepartamentos(contexto, departamentos);
                    aplicarVisibilidad(contexto, "departamentos");
                }
            } catch (error) {
                console.warn(
                    "No fue posible preparar la cartografía departamental " +
                    "para la capa de suelo.",
                    error);
            }
        }

        const geojson = nivel === "MUNICIPIO"
            ? contexto.geojsonMunicipios
            : contexto.geojsonDepartamentos;

        const regiones = Array.isArray(respuesta.regiones)
            ? respuesta.regiones
            : [];
        const indice = crearIndiceRegionesSuelo(regiones, nivel);

        if (!geojson || regiones.length === 0) {
            contexto.capas[clave] = L.layerGroup([], {
                pane: "geoSoilPane"
            });
            contexto.respuestasSuelo[clave] = respuesta;
            aplicarVisibilidad(contexto, clave);
            actualizarLeyenda(contexto);
            return;
        }

        const capa = L.geoJSON(geojson, {
            pane: "geoSoilPane",
            filter: feature => {
                const region = buscarRegionSuelo(
                    indice,
                    feature,
                    nivel);

                return Boolean(region);
            },
            style: feature => {
                const region = buscarRegionSuelo(
                    indice,
                    feature,
                    nivel);

                return estiloRegionSuelo(region);
            },
            onEachFeature: (feature, layer) => {
                const region = buscarRegionSuelo(
                    indice,
                    feature,
                    nivel);

                if (!region) return;

                configurarRegionSuelo(
                    contexto,
                    layer,
                    respuesta,
                    region,
                    nivel);
            }
        });

        contexto.capas[clave] = capa;
        contexto.respuestasSuelo[clave] = respuesta;
        aplicarVisibilidad(contexto, clave);
        actualizarLeyenda(contexto);
    }

    function crearIndiceRegionesSuelo(regiones, nivel) {
        const indice = new Map();

        regiones.forEach(region => {
            const clave = nivel === "MUNICIPIO"
                ? claveMunicipioSuelo(
                    region.municipio || region.nombreTerritorio,
                    region.departamento)
                : normalizarDepartamentoSuelo(
                    region.departamento || region.nombreTerritorio);

            if (clave) indice.set(clave, region);
        });

        return indice;
    }

    function buscarRegionSuelo(indice, feature, nivel) {
        if (!indice || !feature) return null;

        const clave = nivel === "MUNICIPIO"
            ? claveMunicipioSuelo(
                nombreMunicipio(feature),
                departamentoMunicipio(feature))
            : normalizarDepartamentoSuelo(
                nombreDepartamento(feature));

        return indice.get(clave) || null;
    }

    function claveMunicipioSuelo(municipio, departamento) {
        return `${normalizarDepartamentoSuelo(departamento)}|` +
            `${normalizarGeografico(municipio)}`;
    }

    function normalizarGeografico(valor) {
        return normalizar(valor)
            .replace(/[.,;:()]/g, " ")
            .replace(/[-_/]/g, " ")
            .replace(/\bmunicipio de\b/g, " ")
            .replace(/\bdepartamento de\b/g, " ")
            .replace(/\s+/g, " ")
            .trim();
    }

    function normalizarDepartamentoSuelo(valor) {
        const nombre = normalizarGeografico(valor);
        const compacto = nombre.replace(/\s+/g, "");

        if (nombre.includes("costa caribe norte") ||
            nombre.includes("atlantico norte") ||
            ["raan", "raccn"].includes(compacto)) {
            return "raccn";
        }

        if (nombre.includes("costa caribe sur") ||
            nombre.includes("atlantico sur") ||
            ["raas", "raccs"].includes(compacto)) {
            return "raccs";
        }

        return nombre;
    }

    function estiloRegionSuelo(region) {
        return {
            color: region?.color || "#64748b",
            weight: 1.55,
            opacity: 0.96,
            fillColor: region?.color || "#64748b",
            fillOpacity: 0.62,
            lineJoin: "round",
            bubblingMouseEvents: false
        };
    }

    function configurarRegionSuelo(
        contexto,
        layer,
        respuesta,
        region,
        nivel) {
        const unidad = respuesta.unidad
            ? ` ${escapar(respuesta.unidad)}`
            : "";
        const promedio = Number(region.promedio);
        const minimo = Number(region.minimo);
        const maximo = Number(region.maximo);
        const terrenos = Number(region.terrenosAnalizados || 0);
        const fecha = region.fechaMasReciente
            ? new Date(region.fechaMasReciente)
                .toLocaleDateString("es-NI")
            : "Sin fecha";
        const tipo = nivel === "MUNICIPIO"
            ? "Municipio"
            : "Departamento";
        const nombre = region.nombreTerritorio ||
            (nivel === "MUNICIPIO"
                ? region.municipio
                : region.departamento);
        const advertencia = region.muestraLimitada
            ? `<small class="geo-soil-sample-warning">
                   Muestra limitada: interpretar con cautela.
               </small>`
            : "";
        const rango = Number.isFinite(minimo) &&
                      Number.isFinite(maximo)
            ? `${numero(minimo)}–${numero(maximo)}${unidad}`
            : "Sin rango";

        layer.bindTooltip(`
            <div class="geo-soil-tooltip territorial">
                <strong>${escapar(nombre || tipo)}</strong>
                <span>${escapar(respuesta.nombre)} promedio</span>
                <b>${numero(promedio)}${unidad}</b>
                <small>${escapar(
                    region.clasificacion || "Sin clasificar")}</small>
                <small>${terrenos} terreno${terrenos === 1 ? "" : "s"}</small>
            </div>
        `, {
            sticky: true,
            direction: "top",
            className: "soil-tooltip"
        });

        layer.bindPopup(`
            <div class="geo-soil-popup territorial">
                <span>${escapar(tipo)}</span>
                <h3>${escapar(nombre || tipo)}</h3>
                ${nivel === "MUNICIPIO" && region.departamento
                    ? `<p>${escapar(region.departamento)}</p>`
                    : ""}
                <div class="geo-soil-main-value">
                    <small>${escapar(respuesta.nombre)} promedio</small>
                    <strong>${numero(promedio)}${unidad}</strong>
                    <b>${escapar(
                        region.clasificacion || "Sin clasificar")}</b>
                </div>
                <div class="geo-soil-territorial-grid">
                    <span>
                        <small>Terrenos analizados</small>
                        <strong>${terrenos}</strong>
                    </span>
                    <span>
                        <small>Rango observado</small>
                        <strong>${escapar(rango)}</strong>
                    </span>
                    <span>
                        <small>Dato más reciente</small>
                        <strong>${escapar(fecha)}</strong>
                    </span>
                </div>
                ${advertencia}
                <small class="geo-territorial-hint">
                    Cada terreno aporta únicamente su análisis más reciente.
                </small>
                <button type="button"
                        class="geo-soil-territory-button"
                        data-level="${escapar(nivel)}"
                        data-department="${escapar(
                            region.departamento || nombre)}"
                        data-municipality="${escapar(
                            region.municipio || "")}">
                    ${nivel === "MUNICIPIO"
                        ? "Ver terrenos del municipio"
                        : "Ver promedios municipales"}
                </button>
            </div>
        `, {
            maxWidth: 330,
            className: "geo-terrain-leaflet-popup"
        });

        layer.on({
            mouseover: evento => {
                evento.target.setStyle({
                    weight: 2.7,
                    fillOpacity: 0.76
                });
                evento.target.bringToFront?.();
            },
            mouseout: evento => {
                const clave = String(respuesta.clave).toLowerCase();
                contexto.capas[clave]?.resetStyle?.(evento.target);
            }
        });
    }

    function mostrarClima(elementId, respuesta) {
        const contexto = mapas.get(elementId);
        if (!contexto) return null;

        contexto.climaOriginal = respuesta || null;
        const filtrada = construirClimaTerritorial(contexto);
        renderizarClima(contexto, filtrada);
        return filtrada;
    }

    function renderizarClima(contexto, respuesta) {
        contexto.clima = respuesta || null;
        removerCapasClimaticas(contexto);

        const puntos = Array.isArray(respuesta?.puntos)
            ? respuesta.puntos.filter(item =>
                coordenadaValida(
                    Number(item.latitud),
                    Number(item.longitud)))
            : [];

        if (!respuesta?.disponible || puntos.length === 0 || !L.heatLayer) {
            actualizarPopups(contexto);
            actualizarLeyenda(contexto);
            return;
        }

        contexto.capas.temperatura = crearCapaCalor(
            puntos,
            "temperatura",
            {
                min: numeroSeguro(respuesta.temperaturaMinima, 18),
                max: numeroSeguro(respuesta.temperaturaMaxima, 36),
                radius: 38,
                blur: 32,
                gradient: {
                    0.10: "#2457c5",
                    0.32: "#24a7d8",
                    0.52: "#2dbb75",
                    0.70: "#f2c94c",
                    0.86: "#f17b31",
                    1.00: "#bd1e2d"
                }
            });

        contexto.capas.humedad = crearCapaCalor(
            puntos,
            "humedadRelativa",
            {
                min: numeroSeguro(respuesta.humedadMinima, 35),
                max: numeroSeguro(respuesta.humedadMaxima, 100),
                radius: 38,
                blur: 32,
                gradient: {
                    0.10: "#d9b382",
                    0.35: "#7fc8a9",
                    0.58: "#42a5c6",
                    0.78: "#2878b5",
                    1.00: "#153e75"
                }
            });

        contexto.capas.lluvia = crearCapaCalor(
            puntos,
            "precipitacion",
            {
                min: 0,
                max: Math.max(
                    numeroSeguro(respuesta.precipitacionMaxima, 1),
                    0.5),
                radius: 40,
                blur: 34,
                gradient: {
                    0.10: "#dff3ff",
                    0.35: "#86d6f2",
                    0.58: "#3182ce",
                    0.80: "#2448a8",
                    1.00: "#4a148c"
                }
            });

        contexto.capas.viento = crearCapaCalor(
            puntos,
            "velocidadViento",
            {
                min: 0,
                max: Math.max(
                    numeroSeguro(respuesta.vientoMaximo, 25),
                    5),
                radius: 38,
                blur: 30,
                gradient: {
                    0.10: "#d7f5ef",
                    0.35: "#54c6b2",
                    0.58: "#f2c94c",
                    0.80: "#f18f3b",
                    1.00: "#c0392b"
                }
            });

        ["temperatura", "humedad", "lluvia", "viento"]
            .forEach(clave => aplicarVisibilidad(contexto, clave));

        actualizarPopups(contexto);
        actualizarLeyenda(contexto);
    }

    async function aplicarFiltroTerritorial(
        elementId,
        departamento,
        municipio,
        ajustarVista = true) {
        const contexto = mapas.get(elementId);
        if (!contexto) return null;

        const nombreDepartamento = String(departamento || "").trim();
        const nombreMunicipioSeleccionado = String(municipio || "").trim();

        if (nombreDepartamento && !contexto.geojsonDepartamentos) {
            try {
                const geojson = await obtenerDepartamentos();
                if (!contexto.geojsonDepartamentos) {
                    construirDepartamentos(contexto, geojson);
                    aplicarVisibilidad(contexto, "departamentos");
                }
            } catch (error) {
                console.warn(
                    "No fue posible preparar el departamento seleccionado.",
                    error);
            }
        }

        if (nombreMunicipioSeleccionado && !contexto.geojsonMunicipios) {
            try {
                await cargarMunicipios(contexto);
            } catch (error) {
                console.warn(
                    "No fue posible preparar el municipio seleccionado.",
                    error);
            }
        }

        let feature = null;
        let nivel = "NACIONAL";

        if (nombreMunicipioSeleccionado) {
            feature = buscarFeatureMunicipio(
                contexto,
                nombreMunicipioSeleccionado,
                nombreDepartamento);
            nivel = "MUNICIPIO";
        } else if (nombreDepartamento) {
            feature = buscarFeatureDepartamento(
                contexto,
                nombreDepartamento);
            nivel = "DEPARTAMENTO";
        }

        contexto.filtroTerritorial = {
            departamento: nombreDepartamento,
            municipio: nombreMunicipioSeleccionado,
            nivel,
            feature
        };

        contexto.capas.departamentos?.setStyle?.(
            item => estiloDepartamento(contexto, item));
        contexto.capas.municipios?.setStyle?.(
            item => estiloMunicipio(contexto, item));

        const filtrada = construirClimaTerritorial(contexto);
        renderizarClima(contexto, filtrada);

        if (ajustarVista && feature) {
            const limites = L.geoJSON(feature).getBounds();
            if (limites.isValid()) {
                contexto.mapa.fitBounds(limites, {
                    padding: [42, 42],
                    maxZoom: nivel === "MUNICIPIO" ? 13 : 10,
                    animate: true
                });
            }
        }

        return filtrada;
    }

    function buscarFeatureDepartamento(contexto, departamento) {
        const buscado = normalizar(departamento);
        return (contexto.geojsonDepartamentos?.features || [])
            .find(feature =>
                normalizar(nombreDepartamento(feature)) === buscado) || null;
    }

    function buscarFeatureMunicipio(contexto, municipio, departamento) {
        const municipioBuscado = normalizar(municipio);
        const departamentoBuscado = normalizar(departamento);

        return (contexto.geojsonMunicipios?.features || [])
            .find(feature => {
                const coincideMunicipio =
                    normalizar(nombreMunicipio(feature)) === municipioBuscado;
                const coincideDepartamento =
                    !departamentoBuscado ||
                    normalizar(departamentoMunicipio(feature)) ===
                        departamentoBuscado;

                return coincideMunicipio && coincideDepartamento;
            }) || null;
    }

    function construirClimaTerritorial(contexto) {
        const original = contexto.climaOriginal;
        if (!original) return null;

        const todos = Array.isArray(original.puntos)
            ? original.puntos.filter(item =>
                coordenadaValida(
                    Number(item.latitud),
                    Number(item.longitud)))
            : [];
        const filtro = contexto.filtroTerritorial || {};
        const feature = filtro.feature;

        if (!feature || filtro.nivel === "NACIONAL") {
            return recalcularResumenClima(
                original,
                todos,
                original.mensaje);
        }

        let puntos = todos.filter(item =>
            puntoDentroFeature(
                Number(item.longitud),
                Number(item.latitud),
                feature));
        let aproximado = false;

        if (puntos.length === 0 && todos.length > 0) {
            const centro = L.geoJSON(feature).getBounds().getCenter();
            const cercano = [...todos]
                .sort((a, b) =>
                    distanciaCuadrada(a, centro) -
                    distanciaCuadrada(b, centro))[0];

            if (cercano) {
                puntos = [{
                    ...cercano,
                    latitud: centro.lat,
                    longitud: centro.lng
                }];
                aproximado = true;
            }
        }

        const territorio = filtro.municipio || filtro.departamento;
        const mensaje = aproximado
            ? `Estimación climática para ${territorio} basada en el punto ` +
              "meteorológico más cercano."
            : `Condiciones meteorológicas estimadas para ${territorio}.`;

        return recalcularResumenClima(original, puntos, mensaje);
    }

    function recalcularResumenClima(original, puntos, mensaje) {
        const disponibles = Array.isArray(puntos) ? puntos : [];
        const resultado = {
            ...original,
            disponible: Boolean(original?.disponible) && disponibles.length > 0,
            mensaje: disponibles.length > 0
                ? mensaje
                : "No hay datos meteorológicos para el filtro territorial actual.",
            puntos: disponibles,
            temperaturaMinima: minimoClima(disponibles, "temperatura"),
            temperaturaMaxima: maximoClima(disponibles, "temperatura"),
            humedadMinima: minimoClima(disponibles, "humedadRelativa"),
            humedadMaxima: maximoClima(disponibles, "humedadRelativa"),
            precipitacionMaxima: maximoClima(disponibles, "precipitacion"),
            vientoMaximo: maximoClima(disponibles, "velocidadViento")
        };

        return resultado;
    }

    function minimoClima(puntos, propiedad) {
        const valores = puntos
            .map(item => Number(item[propiedad]))
            .filter(Number.isFinite);
        return valores.length > 0 ? Math.min(...valores) : null;
    }

    function maximoClima(puntos, propiedad) {
        const valores = puntos
            .map(item => Number(item[propiedad]))
            .filter(Number.isFinite);
        return valores.length > 0 ? Math.max(...valores) : null;
    }

    function distanciaCuadrada(punto, centro) {
        const latitud = Number(punto.latitud);
        const longitud = Number(punto.longitud);
        return Math.pow(latitud - centro.lat, 2) +
            Math.pow(longitud - centro.lng, 2);
    }

    function puntoDentroFeature(longitud, latitud, feature) {
        const geometria = feature?.geometry;
        if (!geometria) return false;

        if (geometria.type === "Polygon") {
            return puntoDentroPoligono(
                longitud,
                latitud,
                geometria.coordinates);
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

        for (let indice = 1; indice < anillos.length; indice++) {
            if (puntoDentroAnillo(longitud, latitud, anillos[indice])) {
                return false;
            }
        }

        return true;
    }

    function puntoDentroAnillo(longitud, latitud, anillo) {
        if (!Array.isArray(anillo) || anillo.length < 3) return false;

        let dentro = false;
        let anterior = anillo.length - 1;

        for (let actual = 0; actual < anillo.length; actual++) {
            const actualPunto = anillo[actual];
            const anteriorPunto = anillo[anterior];
            const xActual = Number(actualPunto?.[0]);
            const yActual = Number(actualPunto?.[1]);
            const xAnterior = Number(anteriorPunto?.[0]);
            const yAnterior = Number(anteriorPunto?.[1]);

            if (![xActual, yActual, xAnterior, yAnterior]
                .every(Number.isFinite)) {
                anterior = actual;
                continue;
            }

            const cruza =
                ((yActual > latitud) !== (yAnterior > latitud)) &&
                (longitud <
                    (xAnterior - xActual) *
                    (latitud - yActual) /
                    ((yAnterior - yActual) || Number.EPSILON) +
                    xActual);

            if (cruza) dentro = !dentro;
            anterior = actual;
        }

        return dentro;
    }

    function crearCapaCalor(puntos, propiedad, opciones) {
        const min = Number(opciones.min);
        const max = Number(opciones.max);
        const rango = Math.max(max - min, 0.0001);

        const datos = puntos
            .map(item => {
                const valor = Number(item[propiedad]);
                if (!Number.isFinite(valor)) return null;

                const intensidad = Math.min(
                    1,
                    Math.max(0.08, (valor - min) / rango));

                return [
                    Number(item.latitud),
                    Number(item.longitud),
                    intensidad
                ];
            })
            .filter(Boolean);

        return L.heatLayer(datos, {
            pane: "geoHeatPane",
            radius: opciones.radius,
            blur: opciones.blur,
            maxZoom: 11,
            minOpacity: 0.32,
            max: 1,
            gradient: opciones.gradient
        });
    }

    function removerCapasClimaticas(contexto) {
        ["temperatura", "humedad", "lluvia", "viento"]
            .forEach(clave => {
                const capa = contexto.capas[clave];
                if (capa && contexto.mapa.hasLayer(capa)) {
                    contexto.mapa.removeLayer(capa);
                }
                contexto.capas[clave] = null;
            });
    }

    function cambiarCapa(elementId, clave, activa) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        contexto.capasActivas[clave] = Boolean(activa);
        aplicarVisibilidad(contexto, clave);
        actualizarLeyenda(contexto);
    }

    function aplicarCapas(elementId, estadoCapas) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        Object.entries(estadoCapas || {}).forEach(([clave, activa]) => {
            contexto.capasActivas[clave] = Boolean(activa);
            aplicarVisibilidad(contexto, clave);
        });

        actualizarLeyenda(contexto);
    }

    function aplicarVisibilidad(contexto, clave) {
        if (clave === "municipios" &&
            contexto.capasActivas.municipios &&
            !contexto.capas.municipios) {
            cargarMunicipios(contexto).catch(error => {
                console.warn("No fue posible cargar municipios.", error);
            });
            return;
        }

        const capa = contexto.capas[clave];
        if (!capa) return;

        const activa = Boolean(contexto.capasActivas[clave]);
        const estaEnMapa = contexto.mapa.hasLayer(capa);

        if (activa && !estaEnMapa) {
            capa.addTo(contexto.mapa);
        } else if (!activa && estaEnMapa) {
            contexto.mapa.removeLayer(capa);
        }
    }

    function cambiarMapaBase(elementId, clave) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        const nueva = contexto.capasBase[clave];
        if (!nueva || contexto.mapaBaseActivo === clave) return;

        const actual = contexto.capasBase[contexto.mapaBaseActivo];
        if (actual && contexto.mapa.hasLayer(actual)) {
            contexto.mapa.removeLayer(actual);
        }

        nueva.addTo(contexto.mapa);
        nueva.bringToBack();
        contexto.mapaBaseActivo = clave;
    }

    function centrarNicaragua(elementId) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        contexto.mapa.fitBounds(VISTA_NACIONAL, {
            padding: [12, 12],
            maxZoom: 8,
            animate: true,
            duration: 0.55
        });
    }

    async function alternarPantallaCompleta(elementId) {
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        const contenedor = contexto.elemento.closest(".geo-map-stage");
        if (!contenedor) return;

        try {
            if (!document.fullscreenElement) {
                await contenedor.requestFullscreen();
            } else {
                await document.exitFullscreen();
            }
        } catch (error) {
            console.warn("No fue posible cambiar a pantalla completa.", error);
        }
    }

    function construirPopup(contexto, terreno) {
        const alertas = Array.isArray(terreno.alertas)
            ? terreno.alertas
            : [];

        const listaAlertas = alertas.length === 0
            ? `<span class="smart-popup-ok">
                   <i class="fa-solid fa-circle-check"></i>
                   Sin alertas agrícolas
               </span>`
            : `<ul>${alertas
                .map(item => `<li>${escapar(item)}</li>`)
                .join("")}</ul>`;

        const fecha = terreno.fechaUltimoAnalisis
            ? new Date(terreno.fechaUltimoAnalisis)
                .toLocaleDateString("es-NI")
            : "Sin análisis";

        const clima = obtenerClimaCercano(
            contexto,
            Number(terreno.latitud),
            Number(terreno.longitud));

        return `
            <div class="smart-map-popup geo-smart-popup">
                <div class="smart-popup-header">
                    <div>
                        <span class="geo-popup-country">Nicaragua</span>
                        <strong>${escapar(terreno.codigo)}</strong>
                        <span>${escapar(terreno.estado)}</span>
                    </div>
                    <span class="geo-popup-status ${clasePorNivel(terreno.nivelAlerta)}">
                        <i class="fa-solid fa-seedling"></i>
                    </span>
                </div>

                <div class="geo-popup-grid">
                    <p>
                        <i class="fa-solid fa-user"></i>
                        <span><strong>Productor</strong>${escapar(terreno.productor)}</span>
                    </p>
                    <p>
                        <i class="fa-solid fa-location-dot"></i>
                        <span><strong>Ubicación</strong>${escapar(terreno.municipio)}, ${escapar(terreno.departamento)}</span>
                    </p>
                    <p>
                        <i class="fa-solid fa-ruler-combined"></i>
                        <span><strong>Extensión</strong>${numero(terreno.extensionManzanas)} Mz</span>
                    </p>
                    <p>
                        <i class="fa-solid fa-mug-hot"></i>
                        <span><strong>Producción</strong>${numero(terreno.produccionQuintalesOro)} QQ oro</span>
                    </p>
                </div>

                <div class="smart-popup-values">
                    <span>
                        <b>pH</b>
                        ${valor(terreno.ultimoPh)}
                    </span>
                    <span>
                        <b>Materia orgánica</b>
                        ${valor(terreno.materiaOrganica, "%")}
                    </span>
                    <span>
                        <b>Acidez</b>
                        ${valor(terreno.acidezTotal)}
                    </span>
                </div>

                ${construirClimaPopup(clima)}

                <p class="geo-popup-analysis-date">
                    <i class="fa-regular fa-calendar"></i>
                    Último análisis: ${escapar(fecha)}
                </p>

                <div class="smart-popup-alerts">
                    ${listaAlertas}
                </div>

                <div class="geo-popup-actions">
                    <button type="button"
                            class="geo-popup-detail-button"
                            data-terreno-id="${Number(terreno.terrenoId)}">
                        <i class="fa-solid fa-clock-rotate-left"></i>
                        Ver línea de tiempo
                    </button>
                    <a href="${escapar(terreno.googleMapsUrl)}"
                       target="_blank"
                       rel="noopener noreferrer"
                       class="device-google-maps-button">
                        <i class="fa-solid fa-route"></i>
                        Cómo llegar
                    </a>
                </div>
            </div>`;
    }

    function construirClimaPopup(clima) {
        if (!clima) {
            return `
                <div class="geo-popup-climate unavailable">
                    <i class="fa-solid fa-cloud"></i>
                    Activa una capa climática para consultar condiciones cercanas.
                </div>`;
        }

        return `
            <div class="geo-popup-climate">
                <span>
                    <i class="fa-solid fa-temperature-half"></i>
                    <b>${valor(clima.temperatura, " °C")}</b>
                    Temperatura
                </span>
                <span>
                    <i class="fa-solid fa-droplet"></i>
                    <b>${valor(clima.humedadRelativa, "%")}</b>
                    Humedad
                </span>
                <span>
                    <i class="fa-solid fa-cloud-rain"></i>
                    <b>${valor(clima.precipitacion, " mm")}</b>
                    Lluvia
                </span>
                <span>
                    <i class="fa-solid fa-wind"></i>
                    <b>${valor(clima.velocidadViento, " km/h")}</b>
                    Viento
                </span>
            </div>`;
    }

    function actualizarPopups(contexto) {
        contexto.capas.terrenos.eachLayer(capa => {
            if (!capa?.getLatLng || !capa?.getPopup) return;

            const latlng = capa.getLatLng();
            const terreno = contexto.terrenos.find(item =>
                Math.abs(Number(item.latitud) - latlng.lat) < 0.0000001 &&
                Math.abs(Number(item.longitud) - latlng.lng) < 0.0000001);

            if (terreno) {
                capa.setPopupContent(construirPopup(contexto, terreno));
            }
        });
    }

    function obtenerClimaCercano(contexto, latitud, longitud) {
        const puntos = contexto.clima?.puntos;
        if (!Array.isArray(puntos) || puntos.length === 0) return null;

        let mejor = null;
        let distanciaMejor = Number.POSITIVE_INFINITY;

        puntos.forEach(punto => {
            const lat = Number(punto.latitud);
            const lon = Number(punto.longitud);
            if (!coordenadaValida(lat, lon)) return;

            const distancia =
                Math.pow(lat - latitud, 2) +
                Math.pow(lon - longitud, 2);

            if (distancia < distanciaMejor) {
                distanciaMejor = distancia;
                mejor = punto;
            }
        });

        return mejor;
    }

    function seleccionarTerreno(contexto, terrenoId) {
        const id = Number(terrenoId);
        if (!Number.isFinite(id) || !contexto.dotNetReference) return;

        contexto.dotNetReference
            .invokeMethodAsync("SeleccionarTerreno", id)
            .catch(error => {
                console.warn(
                    "No fue posible abrir la ficha territorial.",
                    error);
            });
    }

    function crearControlLeyenda(mapa) {
        const control = L.control({ position: "bottomleft" });

        control.onAdd = () => {
            const contenedor = L.DomUtil.create(
                "div",
                "geo-map-legend-control");

            L.DomEvent.disableClickPropagation(contenedor);
            control.contenedor = contenedor;
            return contenedor;
        };

        control.addTo(mapa);
        return control;
    }

    function actualizarLeyenda(contexto) {
        const contenedor = contexto.controlLeyenda?.contenedor;
        if (!contenedor) return;

        let html = "";
        const claveSuelo = Object.keys(contexto.respuestasSuelo || {})
            .find(clave => contexto.capasActivas[clave]);

        if (claveSuelo) {
            html = leyendaSuelo(
                contexto.respuestasSuelo[claveSuelo]);
        } else if (contexto.capasActivas.temperatura) {
            html = leyendaGradiente(
                "Temperatura",
                `${formatoLeyenda(contexto.clima?.temperaturaMinima)} °C`,
                `${formatoLeyenda(contexto.clima?.temperaturaMaxima)} °C`,
                "temperature");
        } else if (contexto.capasActivas.humedad) {
            html = leyendaGradiente(
                "Humedad relativa",
                `${formatoLeyenda(contexto.clima?.humedadMinima)}%`,
                `${formatoLeyenda(contexto.clima?.humedadMaxima)}%`,
                "humidity");
        } else if (contexto.capasActivas.lluvia) {
            html = leyendaGradiente(
                "Precipitación",
                "0 mm",
                `${formatoLeyenda(contexto.clima?.precipitacionMaxima)} mm`,
                "rain");
        } else if (contexto.capasActivas.viento) {
            html = leyendaGradiente(
                "Velocidad del viento",
                "0 km/h",
                `${formatoLeyenda(contexto.clima?.vientoMaximo)} km/h`,
                "wind");
        } else if (contexto.capasActivas.terrenos) {
            html = `
                <strong>Estado agrícola</strong>
                <span><i class="geo-legend-dot normal"></i>Normal</span>
                <span><i class="geo-legend-dot attention"></i>Atención</span>
                <span><i class="geo-legend-dot critical"></i>Crítico</span>
                <span><i class="geo-legend-dot without-analysis"></i>Sin análisis</span>`;
        }

        contenedor.innerHTML = html;
        contenedor.style.display = html ? "grid" : "none";
    }

    function leyendaSuelo(respuesta) {
        const rangos = Array.isArray(respuesta?.leyenda)
            ? respuesta.leyenda
            : [];
        const unidad = respuesta?.unidad
            ? ` ${escapar(respuesta.unidad)}`
            : "";

        const filas = rangos.length > 0
            ? rangos.map(item => {
                const desde = item.desde === null || item.desde === undefined
                    ? ""
                    : Number(item.desde).toFixed(2);
                const hasta = item.hasta === null || item.hasta === undefined
                    ? ""
                    : Number(item.hasta).toFixed(2);
                const rango = desde && hasta
                    ? `<small>${desde}–${hasta}${unidad}</small>`
                    : "";

                return `
                    <span class="geo-soil-legend-row">
                        <i style="--legend-color:${escapar(item.color || "#64748b")}"></i>
                        <b>${escapar(item.etiqueta || "Sin clasificar")}</b>
                        ${rango}
                    </span>`;
            }).join("")
            : `<small>Sin valores disponibles para los filtros actuales.</small>`;

        const nivel = String(
            respuesta?.nivelAgrupacion || "DEPARTAMENTO")
            .toUpperCase();
        const totalRegiones = Number(respuesta?.totalRegiones || 0);
        const totalTerrenos = Number(
            respuesta?.totalTerrenosAnalizados || 0);
        const etiquetaRegion = nivel === "MUNICIPIO"
            ? (totalRegiones === 1 ? "municipio" : "municipios")
            : (totalRegiones === 1 ? "departamento" : "departamentos");

        return `
            <strong>${escapar(respuesta?.nombre || "Capa de suelo")}</strong>
            <div class="geo-soil-legend">${filas}</div>
            <small>
                ${totalRegiones} ${etiquetaRegion} ·
                ${totalTerrenos} terreno${totalTerrenos === 1 ? "" : "s"}
            </small>`;
    }

    function leyendaGradiente(titulo, minimo, maximo, clase) {
        return `
            <strong>${escapar(titulo)}</strong>
            <div class="geo-gradient-bar ${clase}"></div>
            <div class="geo-gradient-labels">
                <span>${escapar(minimo)}</span>
                <span>${escapar(maximo)}</span>
            </div>`;
    }

    function formatoLeyenda(valor) {
        const numero = Number(valor);
        return Number.isFinite(numero) ? numero.toFixed(1) : "—";
    }

    async function obtenerMunicipios(rutaRelativa) {
        try {
            const respuesta = await fetch(rutaRelativa, {
                credentials: "same-origin"
            });

            if (!respuesta.ok) return [];
            return await respuesta.json();
        } catch {
            return [];
        }
    }

    function clasePorNivel(nivel) {
        switch (String(nivel || "").toUpperCase()) {
            case "CRITICA":
                return "critical";
            case "ATENCION":
                return "attention";
            case "NORMAL":
                return "normal";
            default:
                return "without-analysis";
        }
    }

    function valor(dato, sufijo = "") {
        if (dato === null ||
            dato === undefined ||
            Number.isNaN(Number(dato))) {
            return "—";
        }

        return `${Number(dato).toFixed(1)}${sufijo}`;
    }

    function numero(dato) {
        if (dato === null ||
            dato === undefined ||
            Number.isNaN(Number(dato))) {
            return "0.00";
        }

        return Number(dato).toFixed(2);
    }

    function numeroSeguro(valor, respaldo) {
        const numero = Number(valor);
        return Number.isFinite(numero) ? numero : respaldo;
    }

    function coordenadaValida(latitud, longitud) {
        return Number.isFinite(latitud) &&
            Number.isFinite(longitud) &&
            latitud >= 10.45 &&
            latitud <= 15.35 &&
            longitud >= -88.15 &&
            longitud <= -82.25;
    }

    function normalizar(valor) {
        return String(valor || "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .replace(/\s+/g, " ")
            .trim();
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
        const contexto = mapas.get(elementId);
        if (!contexto) return;

        if (contexto.eventoPantallaCompleta) {
            document.removeEventListener(
                "fullscreenchange",
                contexto.eventoPantallaCompleta);
        }

        contexto.mapa.remove();
        mapas.delete(elementId);
    }

    document.addEventListener("click", evento => {
        const botonTerreno = evento.target.closest(
            ".geo-popup-detail-button");

        if (botonTerreno) {
            const id = Number(botonTerreno.dataset.terrenoId);
            const contexto = [...mapas.values()].find(item =>
                item.elemento.contains(botonTerreno));

            if (contexto) seleccionarTerreno(contexto, id);
            return;
        }

        const botonTerritorio = evento.target.closest(
            ".geo-soil-territory-button");

        if (!botonTerritorio) return;

        const contexto = [...mapas.values()].find(item =>
            item.elemento.contains(botonTerritorio));

        if (!contexto) return;

        const nivel = String(
            botonTerritorio.dataset.level || "")
            .toUpperCase();
        const departamento =
            botonTerritorio.dataset.department || "";
        const municipio =
            botonTerritorio.dataset.municipality || "";

        if (nivel === "MUNICIPIO") {
            seleccionarMunicipio(
                contexto,
                municipio,
                departamento);
        } else {
            seleccionarDepartamento(
                contexto,
                departamento);
        }
    });

    return {
        inicializar,
        mostrarTerrenos,
        mostrar: mostrarTerrenos,
        mostrarClima,
        aplicarFiltroTerritorial,
        mostrarCapaSuelo,
        cambiarCapa,
        aplicarCapas,
        cambiarMapaBase,
        centrarNicaragua,
        alternarPantallaCompleta,
        destruir,
        obtenerMunicipios
    };
})();
