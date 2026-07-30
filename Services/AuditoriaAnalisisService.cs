using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace CONATRADEC.AdminWeb.Services;

public sealed class AuditoriaAnalisisService
{
    private readonly ApiClientService apiClient;
    private readonly AuthStateService authState;

    public AuditoriaAnalisisService(
        ApiClientService apiClient,
        AuthStateService authState)
    {
        this.apiClient = apiClient;
        this.authState = authState;
    }

    public async Task<AuditoriaAnalisisPaginada> ListarAsync(
        AuditoriaAnalisisFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var parametros = new Dictionary<string, string?>
        {
            ["buscar"] = ValorOpcional(filtro.Buscar),
            ["usuarioId"] = filtro.UsuarioId?.ToString(),
            ["laboratorio"] = ValorOpcional(filtro.Laboratorio),
            ["fechaLaboratorioDesde"] =
                filtro.FechaLaboratorioDesde?.ToString("yyyy-MM-dd"),
            ["fechaLaboratorioHasta"] =
                filtro.FechaLaboratorioHasta?.ToString("yyyy-MM-dd"),
            ["fechaRegistroDesde"] =
                filtro.FechaRegistroDesde?.ToString("yyyy-MM-dd"),
            ["fechaRegistroHasta"] =
                filtro.FechaRegistroHasta?.ToString("yyyy-MM-dd"),
            ["origen"] = ValorOpcional(filtro.Origen),
            ["estado"] = ValorOpcional(filtro.Estado),
            ["tieneFormula"] =
                filtro.TieneFormula?.ToString().ToLowerInvariant(),
            ["tieneEnmienda"] =
                filtro.TieneEnmienda?.ToString().ToLowerInvariant(),
            ["tieneMixta"] =
                filtro.TieneMixta?.ToString().ToLowerInvariant(),
            ["phMinimo"] = filtro.PhMinimo?.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["phMaximo"] = filtro.PhMaximo?.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["pagina"] = filtro.Pagina.ToString(),
            ["tamanoPagina"] = filtro.TamanoPagina.ToString()
        };

        string ruta = QueryHelpers.AddQueryString(
            "api/auditoria-analisis",
            parametros
                .Where(x => x.Value is not null)
                .ToDictionary(x => x.Key, x => x.Value!));

        AuditoriaApiRespuesta<AuditoriaAnalisisPaginada>? respuesta =
            await apiClient.GetAsync<
                AuditoriaApiRespuesta<AuditoriaAnalisisPaginada>>(
                    ruta,
                    authState.CrearEncabezadosSesion(),
                    cancellationToken);

        return respuesta?.Data ?? new AuditoriaAnalisisPaginada();
    }

    public async Task<AuditoriaAnalisisCatalogos> ObtenerCatalogosAsync(
        CancellationToken cancellationToken = default)
    {
        AuditoriaApiRespuesta<AuditoriaAnalisisCatalogos>? respuesta =
            await apiClient.GetAsync<
                AuditoriaApiRespuesta<AuditoriaAnalisisCatalogos>>(
                    "api/auditoria-analisis/catalogos",
                    authState.CrearEncabezadosSesion(),
                    cancellationToken);

        return respuesta?.Data ?? new AuditoriaAnalisisCatalogos();
    }

    public async Task<AuditoriaAnalisisDetalle?> ObtenerDetalleAsync(
        int analisisSueloId,
        CancellationToken cancellationToken = default)
    {
        AuditoriaApiRespuesta<AuditoriaAnalisisDetalle>? respuesta =
            await apiClient.GetAsync<
                AuditoriaApiRespuesta<AuditoriaAnalisisDetalle>>(
                    $"api/auditoria-analisis/{analisisSueloId}",
                    authState.CrearEncabezadosSesion(),
                    cancellationToken);

        return respuesta?.Data;
    }

    private static string? ValorOpcional(string valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
}
