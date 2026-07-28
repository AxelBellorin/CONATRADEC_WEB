using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class DashboardService
{
    private readonly ApiClientService apiClient;

    public DashboardService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<DashboardEjecutivoResumen> ObtenerResumenAsync(
        CancellationToken cancellationToken = default)
    {
        return await apiClient.GetAsync<DashboardEjecutivoResumen>(
            "api/dashboard-ejecutivo/resumen",
            cancellationToken) ?? new DashboardEjecutivoResumen();
    }
}
