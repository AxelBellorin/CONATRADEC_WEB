using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class MapaService
{
    private readonly ApiClientService apiClient;

    public MapaService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<MapaInteligenteRespuesta>
        ObtenerMapaInteligenteAsync(
            MapaFiltro filtro,
            CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();

        Agregar(parametros, "buscar", filtro.Texto);

        if (filtro.DepartamentoId.HasValue)
            parametros.Add(
                $"departamentoId={filtro.DepartamentoId.Value}");

        if (filtro.MunicipioId.HasValue)
            parametros.Add(
                $"municipioId={filtro.MunicipioId.Value}");

        Agregar(parametros, "nivel", filtro.Nivel);
        Agregar(parametros, "indicador", filtro.Indicador);

        string ruta = "api/mapa/inteligente";

        if (parametros.Count > 0)
            ruta += "?" + string.Join("&", parametros);

        return await apiClient
            .GetAsync<MapaInteligenteRespuesta>(
                ruta,
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
