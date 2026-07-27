using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace CONATRADEC.AdminWeb.Services;

public sealed class BitacoraService
{
    private readonly ApiClientService apiClient;
    private readonly AuthStateService authState;

    public BitacoraService(
        ApiClientService apiClient,
        AuthStateService authState)
    {
        this.apiClient = apiClient;
        this.authState = authState;
    }

    public async Task<BitacoraPaginada> ListarAsync(
        BitacoraFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var parametros = new Dictionary<string, string?>
        {
            ["pagina"] = filtro.Pagina.ToString(),
            ["tamanoPagina"] = filtro.TamanoPagina.ToString(),
            ["buscar"] = ValorOpcional(filtro.Buscar),
            ["accion"] = ValorOpcional(filtro.Accion),
            ["modulo"] = ValorOpcional(filtro.Modulo),
            ["usuarioId"] = filtro.UsuarioId?.ToString(),
            ["fechaDesdeUtc"] = filtro.FechaDesde?.ToUniversalTime().ToString("O"),
            ["fechaHastaUtc"] = filtro.FechaHasta?.ToUniversalTime().ToString("O"),
            ["exitoso"] = filtro.Estado switch
            {
                "exitoso" => "true",
                "fallido" => "false",
                _ => null
            }
        };

        string ruta = QueryHelpers.AddQueryString(
            "api/bitacora",
            parametros
                .Where(item => item.Value is not null)
                .ToDictionary(item => item.Key, item => item.Value!));

        return await apiClient.GetAsync<BitacoraPaginada>(
            ruta,
            CrearEncabezados(),
            cancellationToken) ?? new BitacoraPaginada();
    }

    public async Task<BitacoraCatalogos> ObtenerCatalogosAsync(
        CancellationToken cancellationToken = default)
    {
        return await apiClient.GetAsync<BitacoraCatalogos>(
            "api/bitacora/catalogos",
            CrearEncabezados(),
            cancellationToken) ?? new BitacoraCatalogos();
    }

    public async Task<BitacoraDetalle?> ObtenerDetalleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await apiClient.GetAsync<BitacoraDetalle>(
            $"api/bitacora/{id}",
            CrearEncabezados(),
            cancellationToken);
    }

    private Dictionary<string, string> CrearEncabezados()
    {
        int usuarioId = authState.Usuario?.UsuarioId ?? 0;

        return new Dictionary<string, string>
        {
            ["X-Usuario-Id"] = usuarioId.ToString()
        };
    }

    private static string? ValorOpcional(string valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
