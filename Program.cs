using CONATRADEC.AdminWeb.Components;
using CONATRADEC.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
