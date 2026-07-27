using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class AlertasAgricolasService
{
    private readonly ApiClientService apiClient;

    public AlertasAgricolasService(
        ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<AlertasAgricolasPaginada>
        ListarAsync(
            int? departamentoId,
            int? municipioId,
            string? nivel,
            string? tipo,
            string? buscar,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>
        {
            $"pagina={Math.Max(1, pagina)}",
            $"tamanoPagina={Math.Clamp(tamanoPagina, 10, 100)}"
        };

        if (departamentoId.HasValue)
            parametros.Add(
                $"departamentoId={departamentoId.Value}");

        if (municipioId.HasValue)
            parametros.Add(
                $"municipioId={municipioId.Value}");

        Agregar(parametros, "nivel", nivel);
        Agregar(parametros, "tipo", tipo);
        Agregar(parametros, "buscar", buscar);

        return await apiClient
            .GetAsync<AlertasAgricolasPaginada>(
                "api/alertas-agricolas?" +
                string.Join("&", parametros),
                cancellationToken) ?? new();
    }

    private static void Agregar(
        ICollection<string> parametros,
        string nombre,
        string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            parametros.Add(
                $"{nombre}=" +
                Uri.EscapeDataString(valor.Trim()));
        }
    }
}
