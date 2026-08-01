using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class SeguimientoAlertasService
{
    private readonly ApiClientService api;

    public SeguimientoAlertasService(
        ApiClientService api)
    {
        this.api = api;
    }

    public async Task<List<SeguimientoAlertaItem>>
        ListarAsync(
            int? terrenoId = null,
            string? estado = null,
            CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();

        if (terrenoId.HasValue)
            parametros.Add($"terrenoId={terrenoId.Value}");

        if (!string.IsNullOrWhiteSpace(estado))
        {
            parametros.Add(
                $"estado={Uri.EscapeDataString(estado)}");
        }

        string ruta =
            "api/seguimiento-alertas-agricolas";

        if (parametros.Count > 0)
            ruta += "?" + string.Join("&", parametros);

        return await api.GetAsync<
            List<SeguimientoAlertaItem>>(
                ruta,
                cancellationToken) ?? [];
    }

    public async Task<SeguimientosPaginados>
        ListarPaginadoAsync(
            int pagina,
            int tamanoPagina,
            string? buscar = null,
            string? estado = null,
            int? responsableId = null,
            int? terrenoId = null,
            CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>
        {
            $"pagina={Math.Max(1, pagina)}",
            $"tamanoPagina={Math.Clamp(tamanoPagina, 6, 100)}"
        };

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            parametros.Add(
                $"buscar={Uri.EscapeDataString(buscar.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            parametros.Add(
                $"estado={Uri.EscapeDataString(estado.Trim())}");
        }

        if (responsableId.HasValue)
            parametros.Add($"responsableId={responsableId.Value}");

        if (terrenoId.HasValue)
            parametros.Add($"terrenoId={terrenoId.Value}");

        string ruta =
            "api/seguimiento-alertas-agricolas/paginado?" +
            string.Join("&", parametros);

        return await api.GetAsync<SeguimientosPaginados>(
                   ruta,
                   cancellationToken) ??
               new SeguimientosPaginados
               {
                   Pagina = Math.Max(1, pagina),
                   TamanoPagina = tamanoPagina
               };
    }

    public Task<SeguimientoAlertaItem?> ObtenerAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        api.GetAsync<SeguimientoAlertaItem>(
            $"api/seguimiento-alertas-agricolas/{id}",
            cancellationToken);

    public async Task<List<SeguimientoAlertaItem>>
        AbiertosAsync(
            CancellationToken cancellationToken = default) =>
        await api.GetAsync<
            List<SeguimientoAlertaItem>>(
                "api/seguimiento-alertas-agricolas/abiertos",
                cancellationToken) ?? [];

    public async Task<List<TecnicoAlertaItem>>
        TecnicosAsync(
            CancellationToken cancellationToken = default) =>
        await api.GetAsync<List<TecnicoAlertaItem>>(
            "api/seguimiento-alertas-agricolas/tecnicos",
            cancellationToken) ?? [];

    public async Task<List<HistorialAlertaItem>>
        HistorialAsync(
            int id,
            CancellationToken cancellationToken = default) =>
        await api.GetAsync<List<HistorialAlertaItem>>(
            $"api/seguimiento-alertas-agricolas/{id}/historial",
            cancellationToken) ?? [];

    public Task<SeguimientoAlertaItem?> CrearAsync(
        CrearSeguimientoAlerta request,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<
            CrearSeguimientoAlerta,
            SeguimientoAlertaItem>(
                "api/seguimiento-alertas-agricolas",
                request,
                cancellationToken);

    public Task<SeguimientoAlertaItem?> ActualizarAsync(
        int id,
        ActualizarSeguimientoAlerta request,
        CancellationToken cancellationToken = default) =>
        api.PutAsync<
            ActualizarSeguimientoAlerta,
            SeguimientoAlertaItem>(
                $"api/seguimiento-alertas-agricolas/{id}",
                request,
                cancellationToken);

    public async Task<List<ConfiguracionAlertaItem>>
        ConfiguracionesAsync(
            CancellationToken cancellationToken = default) =>
        await api.GetAsync<
            List<ConfiguracionAlertaItem>>(
                "api/configuracion-alertas-agricolas",
                cancellationToken) ?? [];

    public Task<object?> ActualizarUmbralAsync(
        int id,
        ActualizarUmbralAlerta request,
        CancellationToken cancellationToken = default) =>
        api.PutAsync<
            ActualizarUmbralAlerta,
            object>(
                $"api/configuracion-alertas-agricolas/{id}",
                request,
                cancellationToken);

    public Task<ResumenReporteAlertas?>
        ResumenReporteAsync(
            CancellationToken cancellationToken = default) =>
        api.GetAsync<ResumenReporteAlertas>(
            "api/reportes-alertas/resumen",
            cancellationToken);
}
