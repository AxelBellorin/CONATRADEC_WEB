using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class SeguimientoAlertasService
{
    private readonly ApiClientService api;

    public SeguimientoAlertasService(ApiClientService api)
    {
        this.api = api;
    }

    public async Task<List<SeguimientoAlertaItem>> ListarAsync(
        int? terrenoId = null,
        string? estado = null,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();

        if (terrenoId.HasValue)
            parametros.Add($"terrenoId={terrenoId.Value}");

        if (!string.IsNullOrWhiteSpace(estado))
            parametros.Add($"estado={Uri.EscapeDataString(estado)}");

        string ruta = "api/seguimiento-alertas-agricolas";
        if (parametros.Count > 0)
            ruta += "?" + string.Join("&", parametros);

        return await api.GetAsync<List<SeguimientoAlertaItem>>(
            ruta, cancellationToken) ?? [];
    }

    public async Task<SeguimientoAlertaItem?> CrearAsync(
        CrearSeguimientoAlerta request,
        CancellationToken cancellationToken = default) =>
        await api.PostAsync<CrearSeguimientoAlerta, SeguimientoAlertaItem>(
            "api/seguimiento-alertas-agricolas",
            request,
            cancellationToken);

    public async Task<SeguimientoAlertaItem?> ActualizarAsync(
        int id,
        ActualizarSeguimientoAlerta request,
        CancellationToken cancellationToken = default) =>
        await api.PutAsync<ActualizarSeguimientoAlerta, SeguimientoAlertaItem>(
            $"api/seguimiento-alertas-agricolas/{id}",
            request,
            cancellationToken);

    public async Task<List<ConfiguracionAlertaItem>> ConfiguracionesAsync(
        CancellationToken cancellationToken = default) =>
        await api.GetAsync<List<ConfiguracionAlertaItem>>(
            "api/configuracion-alertas-agricolas",
            cancellationToken) ?? [];
}
