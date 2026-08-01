using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class ParametrizacionAccesoService
{
    private readonly ApiClientService api;

    public ParametrizacionAccesoService(ApiClientService api)
    {
        this.api = api;
    }

    public Task<List<PropietarioAccesoItem>?> ListarPropietariosAsync(
        string? buscar = null,
        bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        string ruta =
            "api/parametrizacion-acceso/propietarios" +
            $"?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";

        if (!string.IsNullOrWhiteSpace(buscar))
            ruta += $"&buscar={Uri.EscapeDataString(buscar.Trim())}";

        return api.GetAsync<List<PropietarioAccesoItem>>(
            ruta,
            cancellationToken);
    }

    public async Task<ResultadoPaginado<PropietarioAccesoItem>>
        ListarPropietariosPaginadoAsync(
            int pagina,
            int tamanoPagina,
            string? buscar = null,
            bool incluirInactivos = false,
            CancellationToken cancellationToken = default)
    {
        string ruta =
            "api/parametrizacion-acceso/propietarios/paginado" +
            $"?pagina={Math.Max(1, pagina)}" +
            $"&tamanoPagina={Math.Clamp(tamanoPagina, 6, 100)}" +
            $"&incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";

        if (!string.IsNullOrWhiteSpace(buscar))
            ruta += $"&buscar={Uri.EscapeDataString(buscar.Trim())}";

        return await api.GetAsync<
                   ResultadoPaginado<PropietarioAccesoItem>>(
                       ruta,
                       cancellationToken) ??
               new ResultadoPaginado<PropietarioAccesoItem>
               {
                   Pagina = Math.Max(1, pagina),
                   TamanoPagina = tamanoPagina
               };
    }

    public Task<PropietarioDetalleRespuesta?> ObtenerPropietarioAsync(
        int propietarioId,
        CancellationToken cancellationToken = default) =>
        api.GetAsync<PropietarioDetalleRespuesta>(
            $"api/parametrizacion-acceso/propietarios/{propietarioId}",
            cancellationToken);

    public Task<ApiOperacionAccesoRespuesta?> CrearPropietarioAsync(
        PropietarioAccesoFormulario formulario,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<
            PropietarioAccesoFormulario,
            ApiOperacionAccesoRespuesta>(
                "api/parametrizacion-acceso/propietarios",
                formulario,
                cancellationToken);

    public Task<ApiOperacionAccesoRespuesta?> ActualizarPropietarioAsync(
        PropietarioAccesoFormulario formulario,
        CancellationToken cancellationToken = default) =>
        api.PutAsync<
            PropietarioAccesoFormulario,
            ApiOperacionAccesoRespuesta>(
                $"api/parametrizacion-acceso/propietarios/{formulario.PropietarioId}",
                formulario,
                cancellationToken);

    public Task EliminarPropietarioAsync(
        int propietarioId,
        CancellationToken cancellationToken = default) =>
        api.EliminarAsync(
            $"api/parametrizacion-acceso/propietarios/{propietarioId}",
            cancellationToken);

    public Task<ApiOperacionAccesoRespuesta?> VincularTerrenoAsync(
        int propietarioId,
        int terrenoId,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<
            VincularTerrenoPropietarioRequest,
            ApiOperacionAccesoRespuesta>(
                $"api/parametrizacion-acceso/propietarios/{propietarioId}/terrenos",
                new VincularTerrenoPropietarioRequest
                {
                    TerrenoId = terrenoId
                },
                cancellationToken);

    public Task DesvincularTerrenoAsync(
        int propietarioId,
        int terrenoId,
        CancellationToken cancellationToken = default) =>
        api.EliminarAsync(
            $"api/parametrizacion-acceso/propietarios/{propietarioId}/terrenos/{terrenoId}",
            cancellationToken);

    public Task<ApiOperacionAccesoRespuesta?> VincularUsuarioPropietarioAsync(
        int usuarioId,
        int propietarioId,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<
            VincularUsuarioPropietarioRequest,
            ApiOperacionAccesoRespuesta>(
                "api/parametrizacion-acceso/usuario-propietario",
                new VincularUsuarioPropietarioRequest
                {
                    UsuarioId = usuarioId,
                    PropietarioId = propietarioId
                },
                cancellationToken);

    public Task DesvincularUsuarioPropietarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        api.EliminarAsync(
            $"api/parametrizacion-acceso/usuario-propietario/{usuarioId}",
            cancellationToken);

    public Task<List<AsignacionTerrenoItem>?> ListarAsignacionesAsync(
        CancellationToken cancellationToken = default) =>
        api.GetAsync<List<AsignacionTerrenoItem>>(
            "api/parametrizacion-acceso/asignaciones",
            cancellationToken);

    public Task<ApiOperacionAccesoRespuesta?> CrearAsignacionAsync(
        AsignarUsuarioTerrenoRequest request,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<
            AsignarUsuarioTerrenoRequest,
            ApiOperacionAccesoRespuesta>(
                "api/parametrizacion-acceso/asignaciones",
                request,
                cancellationToken);

    public Task EliminarAsignacionAsync(
        int asignacionId,
        CancellationToken cancellationToken = default) =>
        api.EliminarAsync(
            $"api/parametrizacion-acceso/asignaciones/{asignacionId}",
            cancellationToken);

    public Task<List<CoberturaTerritorialItem>?> ListarCoberturasAsync(
        CancellationToken cancellationToken = default) =>
        api.GetAsync<List<CoberturaTerritorialItem>>(
            "api/parametrizacion-acceso/coberturas",
            cancellationToken);

    public Task<ApiOperacionAccesoRespuesta?> CrearCoberturaAsync(
        GuardarCoberturaTerritorialRequest request,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<
            GuardarCoberturaTerritorialRequest,
            ApiOperacionAccesoRespuesta>(
                "api/parametrizacion-acceso/coberturas",
                request,
                cancellationToken);

    public Task EliminarCoberturaAsync(
        int coberturaId,
        CancellationToken cancellationToken = default) =>
        api.EliminarAsync(
            $"api/parametrizacion-acceso/coberturas/{coberturaId}",
            cancellationToken);

    public Task<List<UsuarioAccesoCatalogo>?> ListarUsuariosAsync(
        CancellationToken cancellationToken = default) =>
        api.GetAsync<List<UsuarioAccesoCatalogo>>(
            "api/parametrizacion-acceso/catalogos/usuarios",
            cancellationToken);

    public Task<List<TerrenoAccesoCatalogo>?> ListarTerrenosAsync(
        CancellationToken cancellationToken = default) =>
        api.GetAsync<List<TerrenoAccesoCatalogo>>(
            "api/parametrizacion-acceso/catalogos/terrenos",
            cancellationToken);

    public Task<List<DepartamentoAccesoCatalogo>?> ListarDepartamentosAsync(
        CancellationToken cancellationToken = default) =>
        api.GetAsync<List<DepartamentoAccesoCatalogo>>(
            "api/parametrizacion-acceso/catalogos/departamentos",
            cancellationToken);

    public Task<List<MunicipioAccesoCatalogo>?> ListarMunicipiosAsync(
        int? departamentoId = null,
        CancellationToken cancellationToken = default)
    {
        string ruta =
            "api/parametrizacion-acceso/catalogos/municipios";

        if (departamentoId.HasValue)
            ruta += $"?departamentoId={departamentoId.Value}";

        return api.GetAsync<List<MunicipioAccesoCatalogo>>(
            ruta,
            cancellationToken);
    }
}
