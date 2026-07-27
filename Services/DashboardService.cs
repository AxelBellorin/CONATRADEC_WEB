using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class DashboardService
{
    private readonly ApiClientService apiClient;

    public DashboardService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<ResultadoDatos<DashboardResumen>>
        ObtenerResumenAsync(
            CancellationToken cancellationToken = default)
    {
        try
        {
            DashboardResumen? datos =
                await apiClient.GetAsync<DashboardResumen>(
                    "api/dashboard/resumen",
                    cancellationToken);

            if (datos is null)
            {
                return new ResultadoDatos<DashboardResumen>(
                    DashboardResumen.Vacio(),
                    "La API no devolvió información para el dashboard.");
            }

            datos.ApiDisponible = true;

            return new ResultadoDatos<DashboardResumen>(datos);
        }
        catch (Exception ex)
        {
            return new ResultadoDatos<DashboardResumen>(
                DashboardResumen.Vacio(),
                $"No fue posible cargar los indicadores. {ex.Message}");
        }
    }
}
