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

// Solo consulta los metadatos públicos del instalador. Nunca descarga el
// archivo físico ni obtiene una URL que permita omitir la llave.
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

        client.Timeout = TimeSpan.FromMinutes(2);
    });

builder.Services.AddScoped<WebActivityService>();
builder.Services.AddScoped<BrowserSessionService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<SeguridadWebService>();

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<MapaService>();
builder.Services.AddScoped<AlertasAgricolasService>();
builder.Services.AddScoped<SeguimientoAlertasService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<BitacoraService>();
builder.Services.AddScoped<AuditoriaAnalisisService>();
builder.Services.AddScoped<ControlAnalisisService>();
builder.Services.AddScoped<UsuariosInactivosService>();
builder.Services.AddScoped<DispositivosConexionService>();
builder.Services.AddScoped<LlavesDescargaService>();

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

/*
 * Estas direcciones se conservan por compatibilidad con enlaces compartidos,
 * pero ya no descargan ningún archivo. Únicamente redirigen al formulario
 * público donde la persona debe ingresar una llave válida.
 */
app.MapGet(
    "/instalar/{plataforma}",
    RedirigirAlFormularioProtegido);

app.MapGet(
    "/descargar/{plataforma}",
    RedirigirAlFormularioProtegido);

// Proxy local para los límites municipales.
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

static IResult RedirigirAlFormularioProtegido(
    string plataforma,
    HttpContext httpContext)
{
    string normalizada =
        DescargasPublicasService.NormalizarPlataforma(plataforma);

    string pathBase = httpContext.Request.PathBase.HasValue
        ? httpContext.Request.PathBase.Value!
        : string.Empty;

    if (string.IsNullOrWhiteSpace(normalizada))
    {
        return Results.Redirect(
            $"{pathBase}/descargas",
            permanent: false,
            preserveMethod: false);
    }

    return Results.Redirect(
        $"{pathBase}/descargas/{normalizada.ToLowerInvariant()}",
        permanent: false,
        preserveMethod: false);
}

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
