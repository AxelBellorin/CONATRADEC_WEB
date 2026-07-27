using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class DashboardService
{
    private readonly ApiClientService apiClient;

    public DashboardService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<ResultadoDatos<DashboardResumen>> ObtenerResumenAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            /*
             * Endpoint que agregaremos al backend en la siguiente etapa:
             * GET api/dashboard/resumen
             */
            var datos = await apiClient.GetAsync<DashboardResumen>(
                "api/dashboard/resumen",
                cancellationToken);

            if (datos is not null)
            {
                datos.ApiDisponible = true;
                return new ResultadoDatos<DashboardResumen>(datos);
            }
        }
        catch (Exception)
        {
            // Mientras el endpoint no exista, mostramos la estructura sin datos falsos.
        }

        return new ResultadoDatos<DashboardResumen>(
            DashboardResumen.CrearDemostracion(),
            "El portal ya está funcionando. Los indicadores aparecerán cuando agreguemos el endpoint api/dashboard/resumen al backend.");
    }
}
