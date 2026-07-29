using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using CONATRADEC.AdminWeb.Components;
using CONATRADEC.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Caché y cliente HTTP utilizados por el proxy geográfico del mapa.
// El navegador consume el GeoJSON desde el mismo dominio de la web,
// evitando bloqueos CORS del proveedor cartográfico externo.
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient(
    "GeografiaMapa",
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(25);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "CONATRADEC-CentroGeoespacial/2.3");
        client.DefaultRequestHeaders.Accept.ParseAdd(
            "application/geo+json, application/json");
    });

builder.Services.AddHttpClient<ApiClientService>(
    (serviceProvider, client) =>
    {
        IConfiguration configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        string baseUrl =
            configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "No se encontró ApiSettings:BaseUrl en appsettings.json.");

        client.BaseAddress = new Uri(
            baseUrl.EndsWith('/')
                ? baseUrl
                : $"{baseUrl}/");

        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddHttpClient<ActualizacionesService>(
    (serviceProvider, client) =>
    {
        IConfiguration configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        string baseUrl =
            configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "No se encontró ApiSettings:BaseUrl en appsettings.json.");

        client.BaseAddress = new Uri(
            baseUrl.EndsWith('/')
                ? baseUrl
                : $"{baseUrl}/");

        // Un APK o MSIX puede tardar varios minutos en subir.
        client.Timeout = TimeSpan.FromMinutes(30);
    });

// Cliente público para resolver siempre la última versión publicada.
// No envía cabeceras de sesión porque reutiliza el endpoint público
// api/actualizaciones/comprobar de la API existente.
builder.Services.AddHttpClient<DescargasPublicasService>(
    (serviceProvider, client) =>
    {
        IConfiguration configuration =
            serviceProvider.GetRequiredService<IConfiguration>();

        string baseUrl =
            configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "No se encontró ApiSettings:BaseUrl en appsettings.json.");

        client.BaseAddress = new Uri(
            baseUrl.EndsWith('/')
                ? baseUrl
                : $"{baseUrl}/");

        client.Timeout = TimeSpan.FromMinutes(5);
    });

builder.Services.AddScoped<BrowserSessionService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<SeguridadWebService>();

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<MapaService>();
builder.Services.AddScoped<AlertasAgricolasService>();
builder.Services.AddScoped<SeguimientoAlertasService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<BitacoraService>();
builder.Services.AddScoped<UsuariosInactivosService>();
builder.Services.AddScoped<DispositivosConexionService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/error",
        createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// Enlaces públicos y permanentes del portal.
// Cada acceso vuelve a consultar la última versión PUBLICADA del canal
// PRODUCCION, por lo que nunca es necesario compartir un enlace nuevo.
app.MapGet(
    "/instalar/{plataforma}",
    DescargarUltimaVersionAsync);

// Alias adicional para que ambos nombres sean fáciles de recordar.
app.MapGet(
    "/descargar/{plataforma}",
    DescargarUltimaVersionAsync);

// Página pública con los botones de Android y Windows.
app.MapGet(
    "/descargas",
    MostrarPortalDescargasAsync);

// Proxy local para los límites municipales.
// Se mantiene en caché para no consultar el servicio externo en cada visita
// y se sirve desde el mismo origen del portal administrativo.
app.MapGet(
    "/mapa-datos/municipios.geojson",
    async Task<IResult> (
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        const string cacheKey =
            "centro-geoespacial:municipios:nicaragua:v2.3";

        try
        {
            if (!cache.TryGetValue(cacheKey, out string? geoJson) ||
                string.IsNullOrWhiteSpace(geoJson))
            {
                HttpClient client =
                    httpClientFactory.CreateClient("GeografiaMapa");

                string[] fuentes =
                [
                    "https://cdn.jsdelivr.net/gh/armonge/" +
                    "nicaragua.json@master/nicaragua.geojson",

                    "https://raw.githubusercontent.com/armonge/" +
                    "nicaragua.json/master/nicaragua.geojson",

                    "https://gis.unicef.org/server/rest/services/" +
                    "Limites_Municipales_MIL1/MapServer/0/query" +
                    "?where=1%3D1" +
                    "&outFields=Departam_1%2CMunicipio" +
                    "&returnGeometry=true" +
                    "&outSR=4326" +
                    "&geometryPrecision=5" +
                    "&maxAllowableOffset=0.0005" +
                    "&returnTrueCurves=false" +
                    "&f=geojson"
                ];

                Exception? ultimoError = null;

                foreach (string fuente in fuentes)
                {
                    try
                    {
                        using HttpResponseMessage response =
                            await client.GetAsync(
                                fuente,
                                HttpCompletionOption.ResponseHeadersRead,
                                cancellationToken);

                        if (!response.IsSuccessStatusCode)
                        {
                            ultimoError = new HttpRequestException(
                                $"La fuente geográfica respondió " +
                                $"{(int)response.StatusCode}.");
                            continue;
                        }

                        string contenido =
                            await response.Content.ReadAsStringAsync(
                                cancellationToken);

                        ValidarGeoJsonMunicipios(contenido);
                        geoJson = contenido;
                        break;
                    }
                    catch (JsonException exception)
                    {
                        ultimoError = exception;
                    }
                    catch (HttpRequestException exception)
                    {
                        ultimoError = exception;
                    }
                }

                if (string.IsNullOrWhiteSpace(geoJson))
                {
                    throw new HttpRequestException(
                        "No fue posible obtener la cartografía municipal.",
                        ultimoError);
                }

                cache.Set(
                    cacheKey,
                    geoJson,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromHours(24),
                        Size = 1
                    });
            }

            httpContext.Response.Headers.CacheControl =
                "public,max-age=21600";
            httpContext.Response.Headers["X-Content-Type-Options"] =
                "nosniff";

            return Results.Text(
                geoJson,
                "application/geo+json",
                Encoding.UTF8);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return Results.Problem(
                title: "Tiempo de espera agotado",
                detail:
                    "El proveedor de límites municipales no respondió " +
                    "dentro del tiempo esperado.",
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                title: "Cartografía municipal no disponible",
                detail:
                    "No fue posible consultar temporalmente los límites " +
                    "municipales de Nicaragua.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (JsonException)
        {
            return Results.Problem(
                title: "Respuesta geográfica inválida",
                detail:
                    "El proveedor devolvió una respuesta que no contiene " +
                    "un GeoJSON municipal válido.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task<IResult> DescargarUltimaVersionAsync(
    string plataforma,
    DescargasPublicasService descargas,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    httpContext.Response.Headers.CacheControl =
        "no-store, no-cache, must-revalidate";

    string plataformaNormalizada =
        DescargasPublicasService.NormalizarPlataforma(plataforma);

    if (string.IsNullOrWhiteSpace(plataformaNormalizada))
    {
        return PaginaEstadoDescarga(
            StatusCodes.Status400BadRequest,
            "Plataforma no válida",
            "Utilice Android o Windows en la dirección de descarga.");
    }

    try
    {
        DescargaPublicaWeb? ultima =
            await descargas.ObtenerUltimaAsync(
                plataformaNormalizada,
                "PRODUCCION",
                cancellationToken);

        if (ultima == null)
        {
            return PaginaEstadoDescarga(
                StatusCodes.Status404NotFound,
                "Actualización no disponible",
                $"Todavía no existe una versión de {TextoPlataforma(plataformaNormalizada)} publicada en Producción.");
        }

        return Results.Redirect(
            ultima.UrlDescarga,
            permanent: false,
            preserveMethod: false);
    }
    catch (OperationCanceledException)
        when (!cancellationToken.IsCancellationRequested)
    {
        return PaginaEstadoDescarga(
            StatusCodes.Status504GatewayTimeout,
            "Tiempo de espera agotado",
            "El servidor de actualizaciones tardó demasiado en responder. Intente nuevamente.");
    }
    catch (Exception)
    {
        return PaginaEstadoDescarga(
            StatusCodes.Status502BadGateway,
            "Descarga temporalmente no disponible",
            "No fue posible consultar la última versión publicada. Intente nuevamente en unos minutos.");
    }
}

static async Task<IResult> MostrarPortalDescargasAsync(
    DescargasPublicasService descargas,
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    httpContext.Response.Headers.CacheControl =
        "no-store, no-cache, must-revalidate";

    Task<DescargaPublicaWeb?> androidTask =
        ObtenerDescargaSeguraAsync(
            descargas,
            "ANDROID",
            cancellationToken);

    Task<DescargaPublicaWeb?> windowsTask =
        ObtenerDescargaSeguraAsync(
            descargas,
            "WINDOWS",
            cancellationToken);

    await Task.WhenAll(
        androidTask,
        windowsTask);

    DescargaPublicaWeb? android =
        await androidTask;

    DescargaPublicaWeb? windows =
        await windowsTask;

    string pathBase =
        httpContext.Request.PathBase.HasValue
            ? httpContext.Request.PathBase.Value!
            : string.Empty;

    string html =
        ConstruirPortalDescargas(
            android,
            windows,
            pathBase);

    return Results.Text(
        html,
        "text/html; charset=utf-8",
        Encoding.UTF8);
}

static async Task<DescargaPublicaWeb?> ObtenerDescargaSeguraAsync(
    DescargasPublicasService descargas,
    string plataforma,
    CancellationToken cancellationToken)
{
    try
    {
        return await descargas.ObtenerUltimaAsync(
            plataforma,
            "PRODUCCION",
            cancellationToken);
    }
    catch
    {
        return null;
    }
}

static IResult PaginaEstadoDescarga(
    int codigoEstado,
    string titulo,
    string mensaje)
{
    string tituloSeguro =
        WebUtility.HtmlEncode(titulo);

    string mensajeSeguro =
        WebUtility.HtmlEncode(mensaje);

    string html = $$"""
        <!doctype html>
        <html lang="es">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{tituloSeguro}} | CONATRADEC</title>
            <style>
                *{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;padding:24px;background:#f3f7f5;color:#17362f;font-family:Arial,sans-serif}.card{width:min(560px,100%);padding:32px;border:1px solid #d8e5df;border-radius:20px;background:white;box-shadow:0 20px 60px rgba(23,54,47,.12)}.brand{font-weight:900;letter-spacing:.06em;color:#315c52}.icon{display:grid;place-items:center;width:58px;height:58px;margin:24px 0 18px;border-radius:16px;background:#fff3bf;font-size:28px}h1{margin:0 0 12px;font-size:28px}p{margin:0;color:#526b64;line-height:1.6}.link{display:inline-flex;margin-top:24px;padding:12px 18px;border-radius:12px;background:#315c52;color:white;text-decoration:none;font-weight:800}
            </style>
        </head>
        <body>
            <main class="card">
                <div class="brand">CONATRADEC</div>
                <div class="icon">☕</div>
                <h1>{{tituloSeguro}}</h1>
                <p>{{mensajeSeguro}}</p>
                <a class="link" href="/descargas">Volver al portal de descargas</a>
            </main>
        </body>
        </html>
        """;

    return Results.Text(
        html,
        "text/html; charset=utf-8",
        Encoding.UTF8,
        codigoEstado);
}

static string ConstruirPortalDescargas(
    DescargaPublicaWeb? android,
    DescargaPublicaWeb? windows,
    string pathBase)
{
    string tarjetaAndroid =
        ConstruirTarjetaDescarga(
            "Android",
            "ANDROID",
            "🤖",
            android,
            $"{pathBase}/instalar/android");

    string tarjetaWindows =
        ConstruirTarjetaDescarga(
            "Windows",
            "WINDOWS",
            "▦",
            windows,
            $"{pathBase}/instalar/windows");

    return $$$"""
        <!doctype html>
        <html lang="es">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="description" content="Descargue la última versión publicada de ConatraCafé Soil para Android y Windows.">
            <title>Descargas | CONATRADEC</title>
            <style>
                :root{--green:#315c52;--green-dark:#17362f;--yellow:#f2c94c;--line:#d9e6e0;--muted:#61746e;--bg:#f3f7f5}*{box-sizing:border-box}body{margin:0;background:radial-gradient(circle at top right,#fff6cf 0,transparent 30%),var(--bg);color:var(--green-dark);font-family:Arial,sans-serif}.shell{width:min(1080px,calc(100% - 32px));margin:0 auto;padding:48px 0 64px}.brand{display:flex;align-items:center;gap:12px;font-weight:900;letter-spacing:.05em}.brand-mark{display:grid;place-items:center;width:48px;height:48px;border-radius:14px;background:var(--yellow);font-size:24px}.hero{padding:52px 0 34px;text-align:center}.eyebrow{margin:0 0 12px;color:var(--green);font-size:13px;font-weight:900;letter-spacing:.12em;text-transform:uppercase}.hero h1{max-width:780px;margin:0 auto;font-size:clamp(34px,6vw,60px);line-height:1.03}.hero p{max-width:700px;margin:20px auto 0;color:var(--muted);font-size:18px;line-height:1.65}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:22px;margin-top:28px}.download-card{display:flex;flex-direction:column;min-height:360px;padding:28px;border:1px solid var(--line);border-radius:24px;background:rgba(255,255,255,.94);box-shadow:0 20px 60px rgba(23,54,47,.10)}.platform{display:flex;align-items:center;gap:16px}.platform-icon{display:grid;place-items:center;width:58px;height:58px;border-radius:17px;background:#edf5f2;font-size:28px}.platform h2{margin:0;font-size:26px}.platform p{margin:5px 0 0;color:var(--muted)}.version{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;margin:26px 0}.version div{padding:14px;border-radius:14px;background:#f5f8f7}.version span{display:block;color:var(--muted);font-size:12px;font-weight:800;text-transform:uppercase}.version strong{display:block;margin-top:5px;font-size:18px}.notes{margin:0 0 24px;color:#435c55;line-height:1.55;white-space:pre-line}.button{display:flex;align-items:center;justify-content:center;gap:10px;margin-top:auto;padding:14px 18px;border-radius:14px;background:var(--green);color:white;text-decoration:none;font-weight:900}.button:hover{background:#244a41}.button.disabled{pointer-events:none;background:#9baba6}.notice{margin-top:28px;padding:18px 20px;border:1px solid #ecd37d;border-radius:16px;background:#fff8dd;color:#654d00;line-height:1.55}.footer{margin-top:34px;text-align:center;color:var(--muted);font-size:14px}@media(max-width:760px){.shell{padding-top:24px}.hero{padding-top:38px}.grid{grid-template-columns:1fr}.download-card{min-height:330px}}
            </style>
        </head>
        <body>
            <main class="shell">
                <div class="brand">
                    <div class="brand-mark">🌱</div>
                    <div>CONATRADEC</div>
                </div>
                <section class="hero">
                    <p class="eyebrow">Distribución oficial</p>
                    <h1>ConatraCafé Soil</h1>
                    <p>Descargue desde aquí la última versión publicada para su dispositivo. Los enlaces se actualizan automáticamente cuando se publica una compilación nueva.</p>
                </section>
                <section class="grid">
                    {{{tarjetaAndroid}}}
                    {{{tarjetaWindows}}}
                </section>
                <div class="notice"><strong>Importante:</strong> descargue únicamente desde este portal oficial. En Android puede ser necesario permitir temporalmente la instalación de aplicaciones provenientes del navegador.</div>
                <div class="footer">Centro Nacional de Trabajadores del Campo · CONATRADEC</div>
            </main>
        </body>
        </html>
        """;
}

static string ConstruirTarjetaDescarga(
    string nombrePlataforma,
    string codigoPlataforma,
    string icono,
    DescargaPublicaWeb? version,
    string urlEstable)
{
    string plataformaSegura =
        WebUtility.HtmlEncode(nombrePlataforma);

    string iconoSeguro =
        WebUtility.HtmlEncode(icono);

    if (version == null)
    {
        return $$"""
            <article class="download-card">
                <div class="platform">
                    <div class="platform-icon">{{iconoSeguro}}</div>
                    <div><h2>{{plataformaSegura}}</h2><p>Canal Producción</p></div>
                </div>
                <div class="version"><div><span>Versión</span><strong>No disponible</strong></div><div><span>Estado</span><strong>Pendiente</strong></div></div>
                <p class="notes">Todavía no existe una versión publicada para esta plataforma.</p>
                <a class="button disabled" aria-disabled="true">Descarga no disponible</a>
            </article>
            """;
    }

    string versionNombre =
        WebUtility.HtmlEncode(version.VersionNombre);

    string build =
        WebUtility.HtmlEncode(version.VersionCodigo.ToString());

    string tamano =
        WebUtility.HtmlEncode(version.TamanoVisible);

    string notas =
        WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(version.NotasVersion)
                ? "Última versión oficial disponible para descarga."
                : version.NotasVersion);

    string urlSegura =
        WebUtility.HtmlEncode(urlEstable);

    string textoBoton =
        codigoPlataforma == "ANDROID"
            ? "Descargar APK"
            : "Descargar para Windows";

    return $$"""
        <article class="download-card">
            <div class="platform">
                <div class="platform-icon">{{iconoSeguro}}</div>
                <div><h2>{{plataformaSegura}}</h2><p>Canal Producción</p></div>
            </div>
            <div class="version"><div><span>Versión</span><strong>{{versionNombre}}</strong></div><div><span>Compilación</span><strong>{{build}}</strong></div><div><span>Tamaño</span><strong>{{tamano}}</strong></div><div><span>Estado</span><strong>Publicada</strong></div></div>
            <p class="notes">{{notas}}</p>
            <a class="button" href="{{urlSegura}}">↓ {{textoBoton}}</a>
        </article>
        """;
}

static string TextoPlataforma(
    string plataforma) =>
    plataforma == "ANDROID"
        ? "Android"
        : "Windows";

static void ValidarGeoJsonMunicipios(string geoJson)
{
    if (string.IsNullOrWhiteSpace(geoJson))
        throw new JsonException("El GeoJSON municipal está vacío.");

    using JsonDocument document = JsonDocument.Parse(geoJson);
    JsonElement root = document.RootElement;

    if (!root.TryGetProperty("type", out JsonElement type) ||
        !string.Equals(
            type.GetString(),
            "FeatureCollection",
            StringComparison.OrdinalIgnoreCase) ||
        !root.TryGetProperty("features", out JsonElement features) ||
        features.ValueKind != JsonValueKind.Array ||
        features.GetArrayLength() == 0)
    {
        throw new JsonException(
            "La respuesta no contiene municipios en formato GeoJSON.");
    }
}
