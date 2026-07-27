using CONATRADEC.AdminWeb.Components;
using CONATRADEC.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ApiClientService>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException(
            "No se encontró ApiSettings:BaseUrl en appsettings.json.");

    client.BaseAddress = new Uri(
        baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<MapaService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<BitacoraService>();
builder.Services.AddScoped<UsuariosInactivosService>();
builder.Services.AddScoped<DispositivosConexionService>();

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
