using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class PortalPropietarioService
{
    private readonly ApiClientService api;

    public PortalPropietarioService(
        ApiClientService api)
    {
        this.api = api;
    }

    public Task<PortalPropietarioResumen?>
        ObtenerMiResumenAsync(
            CancellationToken cancellationToken = default) =>
        api.GetAsync<PortalPropietarioResumen>(
            "api/portal-propietario/mi-resumen",
            cancellationToken);

    public Task<PortalCentroGeoespacialResponse?>
        ObtenerMiCentroGeoespacialAsync(
            bool forzarClima = false,
            CancellationToken cancellationToken = default) =>
        api.GetAsync<PortalCentroGeoespacialResponse>(
            "api/portal-propietario/mi-centro-geoespacial" +
            $"?forzarClima={forzarClima.ToString().ToLowerInvariant()}",
            cancellationToken);

    public Task<PortalHistorialTerreno?>
        ObtenerHistorialTerrenoAsync(
            int terrenoId,
            int limite = 20,
            CancellationToken cancellationToken = default) =>
        api.GetAsync<PortalHistorialTerreno>(
            $"api/portal-propietario/terrenos/{terrenoId}/historial" +
            $"?limite={Math.Clamp(limite, 1, 50)}",
            cancellationToken);
}
