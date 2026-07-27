using CONATRADEC.AdminWeb.Components;
using CONATRADEC.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]?.Trim();

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "No se encontró la configuración ApiSettings:BaseUrl.");
}

var apiBaseUrlNormalizada = apiBaseUrl.EndsWith('/')
    ? apiBaseUrl
    : $"{apiBaseUrl}/";

if (!Uri.TryCreate(
        apiBaseUrlNormalizada,
        UriKind.Absolute,
        out var apiBaseUri) ||
    (apiBaseUri.Scheme != Uri.UriSchemeHttp &&
     apiBaseUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "ApiSettings:BaseUrl debe contener una dirección HTTP o HTTPS válida.");
}

builder.Services
    .AddHttpClient<ApiClientService>(client =>
    {
        client.BaseAddress = apiBaseUri;
        client.Timeout = TimeSpan.FromSeconds(60);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<MapaService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<BitacoraService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
