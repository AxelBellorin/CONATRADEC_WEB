window.conatradecPortalCentroGeoespacial =
    window.conatradecPortalCentroGeoespacial || (() => {
        const instancias = new Map();

        const contornoNicaragua = [
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

        const mapasBase = {
            CALLES: {
                url: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
                opciones: {
                    maxZoom: 19,
                    attribution: "&copy; OpenStreetMap contributors"
                }
            },
            TOPOGRAFICO: {
                url: "https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png",
                opciones: {
                    maxZoom: 17,
                    attribution: "Map data &copy; OpenStreetMap, SRTM | Map style &copy; OpenTopoMap"
                }
            },
            SATELITE: {
                url: "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
                opciones: {
                    maxZoom: 19,
                    attribution: "Tiles &copy; Esri"
                }
            }
        };

        function propiedad(objeto, camel, pascal) {
            if (!objeto) {
                return null;
            }

            if (Object.prototype.hasOwnProperty.call(objeto, camel)) {
                return objeto[camel];
            }

            return objeto[pascal];
        }

        function texto(valor) {
            return String(valor ?? "");
        }

        function escapar(valor) {
            return texto(valor)
                .replaceAll("&", "&amp;")
                .replaceAll("<", "&lt;")
                .replaceAll(">", "&gt;")
                .replaceAll('"', "&quot;")
                .replaceAll("'", "&#039;");
        }

        function numero(valor) {
            const resultado = Number(valor);
            return Number.isFinite(resultado) ? resultado : null;
        }

        function arreglo(valor) {
            return Array.isArray(valor) ? valor : [];
        }

        function crearInstancia(elementId, dotNetRef) {
            const elemento = document.getElementById(elementId);

            if (!elemento || typeof window.L === "undefined") {
                return null;
            }

            const mapa = window.L.map(elemento, {
                zoomControl: true,
                attributionControl: true,
                preferCanvas: true
            });

            mapa.setView([12.8654, -85.2072], 7);

            const instancia = {
                mapa,
                dotNetRef,
                baseLayer: null,
                baseKey: null,
                capas: [],
                leyenda: null,
                coordenadasTerrenos: [],
                municipiosVersion: 0,
                inicializado: false
            };

            instancias.set(elementId, instancia);
            return instancia;
        }

        function obtenerInstancia(elementId, dotNetRef) {
            let instancia = instancias.get(elementId);

            if (!instancia) {
                instancia = crearInstancia(elementId, dotNetRef);
            } else if (dotNetRef) {
                instancia.dotNetRef = dotNetRef;
            }

            return instancia;
        }

        function limpiarCapas(instancia) {
            for (const capa of instancia.capas) {
                try {
                    instancia.mapa.removeLayer(capa);
                } catch {
                }
            }

            instancia.capas = [];
            instancia.coordenadasTerrenos = [];

            if (instancia.leyenda) {
                instancia.mapa.removeControl(instancia.leyenda);
                instancia.leyenda = null;
            }
        }

        function aplicarMapaBase(instancia, clave) {
            const normalizada = mapasBase[clave] ? clave : "CALLES";

            if (instancia.baseKey === normalizada && instancia.baseLayer) {
                return;
            }

            if (instancia.baseLayer) {
                instancia.mapa.removeLayer(instancia.baseLayer);
            }

            const configuracion = mapasBase[normalizada];
            instancia.baseLayer = window.L.tileLayer(
                configuracion.url,
                configuracion.opciones);
            instancia.baseLayer.addTo(instancia.mapa);
            instancia.baseKey = normalizada;
        }

        function agregarCapa(instancia, capa) {
            capa.addTo(instancia.mapa);
            instancia.capas.push(capa);
            return capa;
        }

        function obtenerTerrenos(payload) {
            return arreglo(propiedad(payload, "terrenos", "Terrenos"));
        }

        function obtenerClima(payload) {
            return propiedad(payload, "clima", "Clima") || {};
        }

        function coordenadasTerreno(terreno) {
            const latitud = numero(propiedad(terreno, "latitud", "Latitud"));
            const longitud = numero(propiedad(terreno, "longitud", "Longitud"));

            if (!latitud || !longitud) {
                return null;
            }

            return [latitud, longitud];
        }

        function agregarLimiteNacional(instancia) {
            const poligono = window.L.polygon(
                contornoNicaragua,
                {
                    color: "#D97706",
                    weight: 2,
                    opacity: 0.9,
                    fillColor: "#F59E0B",
                    fillOpacity: 0.025,
                    interactive: false
                });

            agregarCapa(instancia, poligono);
        }

        async function agregarMunicipios(instancia, terrenos, version) {
            try {
                const respuesta = await fetch(
                    "/mapa-datos/municipios.geojson",
                    {
                        headers: {
                            Accept: "application/geo+json, application/json"
                        }
                    });

                if (!respuesta.ok || instancia.municipiosVersion !== version) {
                    return;
                }

                const geoJson = await respuesta.json();

                if (instancia.municipiosVersion !== version) {
                    return;
                }

                const departamentos = new Set(
                    terrenos
                        .map(terreno => texto(
                            propiedad(terreno, "departamento", "Departamento"))
                            .trim()
                            .toUpperCase())
                        .filter(Boolean));

                function departamentoFeature(feature) {
                    const propiedades = feature?.properties || {};
                    const candidatos = [
                        propiedades.Departam_1,
                        propiedades.DEPARTAMEN,
                        propiedades.departamento,
                        propiedades.Departamento,
                        propiedades.DEPTO,
                        propiedades.NOM_DEP
                    ];

                    return texto(candidatos.find(Boolean))
                        .trim()
                        .toUpperCase();
                }

                const capa = window.L.geoJSON(geoJson, {
                    filter: feature => {
                        if (departamentos.size === 0) {
                            return true;
                        }

                        const departamento = departamentoFeature(feature);
                        return !departamento || departamentos.has(departamento);
                    },
                    style: {
                        color: "#3B82F6",
                        weight: 1,
                        opacity: 0.65,
                        fillColor: "#60A5FA",
                        fillOpacity: 0.025
                    },
                    onEachFeature: (feature, layer) => {
                        const propiedades = feature?.properties || {};
                        const municipio =
                            propiedades.Municipio ||
                            propiedades.MUNICIPIO ||
                            propiedades.municipio ||
                            propiedades.NOM_MUN ||
                            "Municipio";

                        layer.bindTooltip(escapar(municipio), {
                            sticky: true,
                            direction: "top"
                        });
                    }
                });

                agregarCapa(instancia, capa);
            } catch {
                // El mapa continúa funcionando aunque la cartografía no responda.
            }
        }

        function marcadorTerreno(elementId, terreno) {
            const color =
                texto(propiedad(terreno, "colorEstado", "ColorEstado")) ||
                "#3B655B";

            const icono = window.L.divIcon({
                className: "",
                html:
                    `<div class="owner-geo-terrain-marker" style="--marker-color:${escapar(color)}">
                        <i class="fa-solid fa-seedling"></i>
                    </div>`,
                iconSize: [36, 36],
                iconAnchor: [18, 35],
                popupAnchor: [0, -34]
            });

            const coordenadas = coordenadasTerreno(terreno);
            const marcador = window.L.marker(coordenadas, { icon: icono });

            const id = numero(propiedad(terreno, "terrenoId", "TerrenoId"));
            const codigo = escapar(propiedad(terreno, "codigoTerreno", "CodigoTerreno"));
            const direccion = escapar(propiedad(terreno, "direccion", "Direccion"));
            const municipio = escapar(propiedad(terreno, "municipio", "Municipio"));
            const departamento = escapar(propiedad(terreno, "departamento", "Departamento"));
            const estado = escapar(propiedad(terreno, "estado", "Estado"));
            const extension = numero(propiedad(terreno, "extensionManzanas", "ExtensionManzanas")) ?? 0;
            const analisis = numero(propiedad(terreno, "totalAnalisis", "TotalAnalisis")) ?? 0;
            const ph = numero(propiedad(terreno, "ph", "Ph"));

            marcador.bindPopup(
                `<div class="owner-geo-popup">
                    <div class="popup-head">
                        <h4>${codigo}</h4>
                        <span class="popup-status" style="background:${escapar(color)}">${estado}</span>
                    </div>
                    <p>${direccion}</p>
                    <p>${municipio}${municipio && departamento ? ", " : ""}${departamento}</p>
                    <div class="popup-grid">
                        <span>${extension.toLocaleString("es-NI", { minimumFractionDigits: 2, maximumFractionDigits: 2 })} Mz</span>
                        <span>${analisis.toLocaleString("es-NI")} análisis</span>
                        <span>pH: ${ph === null ? "No disponible" : ph.toLocaleString("es-NI", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                        <span>${arreglo(propiedad(terreno, "alertas", "Alertas")).length} alerta(s)</span>
                    </div>
                    <button type="button" onclick="window.conatradecPortalCentroGeoespacial.seleccionar('${escapar(elementId)}', ${id})">
                        Ver detalle e historial
                    </button>
                </div>`);

            return marcador;
        }

        function marcadorAlerta(terreno) {
            const coordenadas = coordenadasTerreno(terreno);
            const nivel = texto(propiedad(terreno, "nivel", "Nivel"));
            const color = nivel === "CRITICO" ? "#EF4444" : "#F59E0B";

            const icono = window.L.divIcon({
                className: "",
                html:
                    `<div class="owner-geo-alert-marker" style="background:${color}">
                        <i class="fa-solid fa-exclamation"></i>
                    </div>`,
                iconSize: [25, 25],
                iconAnchor: [-2, 29]
            });

            const marcador = window.L.marker(coordenadas, {
                icon: icono,
                zIndexOffset: 1200
            });

            const alertas = arreglo(propiedad(terreno, "alertas", "Alertas"));
            const codigo = escapar(propiedad(terreno, "codigoTerreno", "CodigoTerreno"));

            marcador.bindTooltip(
                `${codigo}: ${alertas.length} alerta(s)`,
                {
                    direction: "top",
                    offset: [0, -18]
                });

            return marcador;
        }

        function valorSuelo(terreno, clave) {
            switch (clave) {
                case "ph":
                    return numero(propiedad(terreno, "ph", "Ph"));
                case "materia-organica":
                    return numero(propiedad(terreno, "materiaOrganica", "MateriaOrganica"));
                case "acidez-total":
                    return numero(propiedad(terreno, "acidezTotal", "AcidezTotal"));
                case "cice":
                    return numero(propiedad(terreno, "cice", "Cice"));
                case "saturacion-bases":
                    return numero(propiedad(terreno, "saturacionBases", "SaturacionBases"));
                default:
                    if (!clave.startsWith("nutriente-")) {
                        return null;
                    }

                    const elementoId = Number(clave.replace("nutriente-", ""));
                    const elementos = arreglo(propiedad(terreno, "elementos", "Elementos"));
                    const elemento = elementos.find(item =>
                        Number(propiedad(item, "elementoQuimicosId", "ElementoQuimicosId")) === elementoId);

                    return elemento
                        ? numero(propiedad(elemento, "valor", "Valor"))
                        : null;
            }
        }

        function clasificacionNutriente(terreno, clave) {
            if (!clave.startsWith("nutriente-")) {
                return "";
            }

            const elementoId = Number(clave.replace("nutriente-", ""));
            const elementos = arreglo(propiedad(terreno, "elementos", "Elementos"));
            const elemento = elementos.find(item =>
                Number(propiedad(item, "elementoQuimicosId", "ElementoQuimicosId")) === elementoId);

            return texto(
                elemento
                    ? propiedad(elemento, "clasificacion", "Clasificacion")
                    : "")
                .toUpperCase();
        }

        function colorSuelo(terreno, clave, valor, minimo, maximo) {
            if (clave.startsWith("nutriente-")) {
                const clasificacion = clasificacionNutriente(terreno, clave);

                if (clasificacion.includes("BAJO")) return "#EF4444";
                if (clasificacion.includes("MEDIO")) return "#F2C94C";
                if (clasificacion.includes("ALTO")) return "#3B82F6";
            }

            switch (clave) {
                case "ph":
                    if (valor < 5.5) return "#EF4444";
                    if (valor < 6.0) return "#F59E0B";
                    if (valor <= 6.5) return "#3B655B";
                    if (valor < 7.0) return "#3B82F6";
                    return "#7C3AED";
                case "materia-organica":
                    if (valor <= 3) return "#EF4444";
                    if (valor <= 5) return "#F2C94C";
                    return "#3B655B";
                case "acidez-total":
                    if (valor >= 1) return "#EF4444";
                    if (valor >= 0.5) return "#F2C94C";
                    return "#3B655B";
                case "cice":
                    if (valor < 5) return "#EF4444";
                    if (valor < 10) return "#F2C94C";
                    return "#3B655B";
                case "saturacion-bases":
                    if (valor < 40) return "#EF4444";
                    if (valor < 60) return "#F2C94C";
                    return "#3B655B";
                default:
                    return escalaContinua(valor, minimo, maximo);
            }
        }

        function escalaContinua(valor, minimo, maximo) {
            if (maximo <= minimo) {
                return "#3B82F6";
            }

            const proporcion = Math.max(0, Math.min(1, (valor - minimo) / (maximo - minimo)));
            const hue = 220 - (proporcion * 210);
            return `hsl(${hue}, 72%, 48%)`;
        }

        function agregarCapaSuelo(instancia, terrenos, clave) {
            const disponibles = terrenos
                .map(terreno => ({
                    terreno,
                    coordenadas: coordenadasTerreno(terreno),
                    valor: valorSuelo(terreno, clave)
                }))
                .filter(item => item.coordenadas && item.valor !== null);

            if (disponibles.length === 0) {
                agregarLeyenda(instancia, "Capa sin datos", [
                    { color: "#94A3B8", etiqueta: "No hay resultados disponibles" }
                ]);
                return;
            }

            const valores = disponibles.map(item => item.valor);
            const minimo = Math.min(...valores);
            const maximo = Math.max(...valores);

            for (const item of disponibles) {
                const color = colorSuelo(item.terreno, clave, item.valor, minimo, maximo);
                const circulo = window.L.circleMarker(item.coordenadas, {
                    radius: 17,
                    color: "#FFFFFF",
                    weight: 2,
                    fillColor: color,
                    fillOpacity: 0.82
                });

                const codigo = escapar(propiedad(item.terreno, "codigoTerreno", "CodigoTerreno"));
                circulo.bindTooltip(
                    `${codigo}: ${item.valor.toLocaleString("es-NI", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`,
                    { direction: "top" });

                agregarCapa(instancia, circulo);
            }

            const leyenda = leyendaSuelo(clave, minimo, maximo);
            agregarLeyenda(instancia, nombreCapaSuelo(clave), leyenda);
        }

        function leyendaSuelo(clave, minimo, maximo) {
            switch (clave) {
                case "ph":
                    return [
                        { color: "#EF4444", etiqueta: "Menor de 5.50" },
                        { color: "#F59E0B", etiqueta: "5.50 a 5.99" },
                        { color: "#3B655B", etiqueta: "6.00 a 6.50" },
                        { color: "#3B82F6", etiqueta: "6.51 a 6.99" },
                        { color: "#7C3AED", etiqueta: "7.00 o mayor" }
                    ];
                case "materia-organica":
                    return [
                        { color: "#EF4444", etiqueta: "Baja (≤ 3%)" },
                        { color: "#F2C94C", etiqueta: "Media (3 a 5%)" },
                        { color: "#3B655B", etiqueta: "Alta (> 5%)" }
                    ];
                case "acidez-total":
                    return [
                        { color: "#3B655B", etiqueta: "Baja (< 0.50)" },
                        { color: "#F2C94C", etiqueta: "Media (0.50 a 0.99)" },
                        { color: "#EF4444", etiqueta: "Alta (≥ 1.00)" }
                    ];
                default:
                    if (clave.startsWith("nutriente-")) {
                        return [
                            { color: "#EF4444", etiqueta: "Bajo" },
                            { color: "#F2C94C", etiqueta: "Medio" },
                            { color: "#3B82F6", etiqueta: "Alto" }
                        ];
                    }

                    return [
                        { color: escalaContinua(minimo, minimo, maximo), etiqueta: `Mínimo ${minimo.toFixed(2)}` },
                        { color: escalaContinua((minimo + maximo) / 2, minimo, maximo), etiqueta: "Valor intermedio" },
                        { color: escalaContinua(maximo, minimo, maximo), etiqueta: `Máximo ${maximo.toFixed(2)}` }
                    ];
            }
        }

        function nombreCapaSuelo(clave) {
            const nombres = {
                ph: "pH del suelo",
                "materia-organica": "Materia orgánica",
                "acidez-total": "Acidez total",
                cice: "CICE",
                "saturacion-bases": "Saturación de bases"
            };

            return nombres[clave] || "Nutriente del suelo";
        }

        function agregarCapaClima(instancia, terrenos, clima, clave) {
            const puntos = arreglo(propiedad(clima, "puntos", "Puntos"));

            if (puntos.length === 0) {
                agregarLeyenda(instancia, "Clima no disponible", [
                    { color: "#94A3B8", etiqueta: "Sin datos del proveedor" }
                ]);
                return;
            }

            const bounds = limitesTerrenos(terrenos);
            const filtrados = puntos.filter(punto => {
                const lat = numero(propiedad(punto, "latitud", "Latitud"));
                const lon = numero(propiedad(punto, "longitud", "Longitud"));

                if (lat === null || lon === null) return false;
                if (!bounds) return true;

                return lat >= bounds.sur - 0.8 &&
                    lat <= bounds.norte + 0.8 &&
                    lon >= bounds.oeste - 0.8 &&
                    lon <= bounds.este + 0.8;
            });

            const valores = filtrados
                .map(punto => valorClima(punto, clave))
                .filter(valor => valor !== null);

            if (valores.length === 0) {
                return;
            }

            const minimo = Math.min(...valores);
            const maximo = Math.max(...valores);

            for (const punto of filtrados) {
                const lat = numero(propiedad(punto, "latitud", "Latitud"));
                const lon = numero(propiedad(punto, "longitud", "Longitud"));
                const valor = valorClima(punto, clave);

                if (lat === null || lon === null || valor === null) continue;

                const color = colorClima(clave, valor, minimo, maximo);
                const circulo = window.L.circleMarker([lat, lon], {
                    radius: 23,
                    stroke: false,
                    fillColor: color,
                    fillOpacity: 0.42,
                    interactive: true
                });

                circulo.bindTooltip(
                    `${nombreCapaClima(clave)}: ${valor.toLocaleString("es-NI", { minimumFractionDigits: 1, maximumFractionDigits: 1 })}${unidadClima(clave)}`,
                    { direction: "top" });

                agregarCapa(instancia, circulo);
            }

            agregarLeyenda(instancia, nombreCapaClima(clave), [
                { color: colorClima(clave, minimo, minimo, maximo), etiqueta: `Mínimo ${minimo.toFixed(1)}${unidadClima(clave)}` },
                { color: colorClima(clave, (minimo + maximo) / 2, minimo, maximo), etiqueta: "Valor intermedio" },
                { color: colorClima(clave, maximo, minimo, maximo), etiqueta: `Máximo ${maximo.toFixed(1)}${unidadClima(clave)}` }
            ]);
        }

        function valorClima(punto, clave) {
            switch (clave) {
                case "temperatura": return numero(propiedad(punto, "temperatura", "Temperatura"));
                case "humedad": return numero(propiedad(punto, "humedadRelativa", "HumedadRelativa"));
                case "lluvia": return numero(propiedad(punto, "precipitacion", "Precipitacion"));
                case "viento": return numero(propiedad(punto, "velocidadViento", "VelocidadViento"));
                default: return null;
            }
        }

        function colorClima(clave, valor, minimo, maximo) {
            if (clave === "temperatura") {
                const proporcion = maximo <= minimo ? 0.5 : (valor - minimo) / (maximo - minimo);
                const hue = 220 - Math.max(0, Math.min(1, proporcion)) * 220;
                return `hsl(${hue}, 82%, 52%)`;
            }

            if (clave === "humedad") {
                const proporcion = maximo <= minimo ? 0.5 : (valor - minimo) / (maximo - minimo);
                return `hsl(${205 + proporcion * 25}, 78%, ${62 - proporcion * 20}%)`;
            }

            if (clave === "lluvia") {
                const proporcion = maximo <= minimo ? 0.5 : (valor - minimo) / (maximo - minimo);
                return `hsl(215, 85%, ${72 - proporcion * 34}%)`;
            }

            const proporcion = maximo <= minimo ? 0.5 : (valor - minimo) / (maximo - minimo);
            return `hsl(${155 - proporcion * 65}, 72%, ${62 - proporcion * 22}%)`;
        }

        function nombreCapaClima(clave) {
            return {
                temperatura: "Temperatura",
                humedad: "Humedad relativa",
                lluvia: "Precipitación",
                viento: "Velocidad del viento"
            }[clave] || "Clima";
        }

        function unidadClima(clave) {
            return {
                temperatura: " °C",
                humedad: " %",
                lluvia: " mm",
                viento: " km/h"
            }[clave] || "";
        }

        function agregarLeyenda(instancia, titulo, filas) {
            const control = window.L.control({ position: "bottomright" });

            control.onAdd = () => {
                const div = window.L.DomUtil.create("div", "owner-geo-map-legend");
                div.innerHTML =
                    `<strong>${escapar(titulo)}</strong>` +
                    filas.map(fila =>
                        `<div><i style="background:${escapar(fila.color)}"></i><span>${escapar(fila.etiqueta)}</span></div>`)
                        .join("");
                return div;
            };

            control.addTo(instancia.mapa);
            instancia.leyenda = control;
        }

        function limitesTerrenos(terrenos) {
            const coordenadas = terrenos
                .map(coordenadasTerreno)
                .filter(Boolean);

            if (coordenadas.length === 0) {
                return null;
            }

            const latitudes = coordenadas.map(item => item[0]);
            const longitudes = coordenadas.map(item => item[1]);

            return {
                sur: Math.min(...latitudes),
                norte: Math.max(...latitudes),
                oeste: Math.min(...longitudes),
                este: Math.max(...longitudes)
            };
        }

        function render(elementId, payload, dotNetRef) {
            const instancia = obtenerInstancia(elementId, dotNetRef);

            if (!instancia) {
                return;
            }

            aplicarMapaBase(
                instancia,
                texto(propiedad(payload, "mapaBase", "MapaBase")));

            limpiarCapas(instancia);

            const terrenos = obtenerTerrenos(payload);
            const clima = obtenerClima(payload);
            const mostrarLimiteNacional = Boolean(propiedad(payload, "mostrarLimiteNacional", "MostrarLimiteNacional"));
            const mostrarMunicipios = Boolean(propiedad(payload, "mostrarMunicipios", "MostrarMunicipios"));
            const mostrarTerrenos = Boolean(propiedad(payload, "mostrarTerrenos", "MostrarTerrenos"));
            const mostrarAlertas = Boolean(propiedad(payload, "mostrarAlertas", "MostrarAlertas"));
            const capaSuelo = texto(propiedad(payload, "capaSuelo", "CapaSuelo"));
            const capaClima = texto(propiedad(payload, "capaClima", "CapaClima"));

            instancia.coordenadasTerrenos = terrenos
                .map(coordenadasTerreno)
                .filter(Boolean);

            if (mostrarLimiteNacional) {
                agregarLimiteNacional(instancia);
            }

            instancia.municipiosVersion += 1;
            const version = instancia.municipiosVersion;

            if (mostrarMunicipios) {
                agregarMunicipios(instancia, terrenos, version);
            }

            if (capaClima) {
                agregarCapaClima(instancia, terrenos, clima, capaClima);
            }

            if (capaSuelo) {
                agregarCapaSuelo(instancia, terrenos, capaSuelo);
            }

            if (mostrarTerrenos) {
                for (const terreno of terrenos) {
                    if (!coordenadasTerreno(terreno)) continue;
                    agregarCapa(instancia, marcadorTerreno(elementId, terreno));
                }
            }

            if (mostrarAlertas) {
                for (const terreno of terrenos) {
                    const alertas = arreglo(propiedad(terreno, "alertas", "Alertas"));
                    if (alertas.length === 0 || !coordenadasTerreno(terreno)) continue;
                    agregarCapa(instancia, marcadorAlerta(terreno));
                }
            }

            if (!instancia.inicializado) {
                instancia.inicializado = true;
                centrar(elementId);
            }

            window.setTimeout(() => instancia.mapa.invalidateSize(), 80);
        }

        function centrar(elementId) {
            const instancia = instancias.get(elementId);

            if (!instancia) return;

            if (instancia.coordenadasTerrenos.length === 0) {
                instancia.mapa.setView([12.8654, -85.2072], 7);
                return;
            }

            if (instancia.coordenadasTerrenos.length === 1) {
                instancia.mapa.setView(instancia.coordenadasTerrenos[0], 15);
                return;
            }

            instancia.mapa.fitBounds(
                window.L.latLngBounds(instancia.coordenadasTerrenos),
                {
                    padding: [45, 45],
                    maxZoom: 15
                });
        }

        async function seleccionar(elementId, terrenoId) {
            const instancia = instancias.get(elementId);

            if (!instancia?.dotNetRef) {
                return;
            }

            try {
                await instancia.dotNetRef.invokeMethodAsync(
                    "SeleccionarTerrenoDesdeMapa",
                    Number(terrenoId));
            } catch {
            }
        }

        function dispose(elementId) {
            const instancia = instancias.get(elementId);

            if (!instancia) {
                return;
            }

            try {
                instancia.mapa.remove();
            } catch {
            }

            instancias.delete(elementId);
        }

        return {
            render,
            centrar,
            seleccionar,
            dispose
        };
    })();
