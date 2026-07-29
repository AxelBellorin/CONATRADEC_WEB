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

    public async Task<CentroGeoespacialCapasRespuesta>
        ObtenerCapasAsync(
            CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<CentroGeoespacialCapasRespuesta>(
            "api/centro-geoespacial/capas",
            cancellationToken) ?? new();

    public async Task<List<MunicipioSelectorItem>>
        ObtenerMunicipiosAsync(
            CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<MunicipioSelectorItem>>(
            "api/municipio/listarTodos-por-departamento-por-pais",
            cancellationToken) ?? [];

    public async Task<ClimaMapaRespuesta> ObtenerClimaAsync(
        bool forzarActualizacion = false,
        CancellationToken cancellationToken = default)
    {
        string ruta = "api/centro-geoespacial/clima";

        if (forzarActualizacion)
            ruta += "?forzarActualizacion=true";

        return await apiClient.GetAsync<ClimaMapaRespuesta>(
            ruta,
            cancellationToken) ?? new();
    }


    public async Task<CapaSueloMapaRespuesta> ObtenerCapaSueloAsync(
        string clave,
        int? departamentoId = null,
        int? municipioId = null,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();

        if (departamentoId.HasValue)
            parametros.Add($"departamentoId={departamentoId.Value}");

        if (municipioId.HasValue)
            parametros.Add($"municipioId={municipioId.Value}");

        string ruta =
            $"api/centro-geoespacial/suelos/{Uri.EscapeDataString(clave)}";

        if (parametros.Count > 0)
            ruta += "?" + string.Join("&", parametros);

        return await apiClient.GetAsync<CapaSueloMapaRespuesta>(
            ruta,
            cancellationToken) ?? new();
    }

    public async Task<HistorialTerrenoMapa?>
        ObtenerHistorialTerrenoAsync(
            int terrenoId,
            int limite = 20,
            CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<HistorialTerrenoMapa>(
            $"api/centro-geoespacial/terrenos/{terrenoId}/historial" +
            $"?limite={Math.Clamp(limite, 1, 100)}",
            cancellationToken);

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
