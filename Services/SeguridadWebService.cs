using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class SeguridadWebService
{
    private readonly ApiClientService api;

    public SeguridadWebService(ApiClientService api)
    {
        this.api = api;
    }

    public async Task<List<RolWebItem>> ListarRolesAsync(
        CancellationToken cancellationToken = default) =>
        await api.GetAsync<List<RolWebItem>>(
            "api/Rol/listarRoles",
            cancellationToken) ?? [];

    public async Task<List<RolWebItem>>
        ListarRolesInactivosAsync(
            CancellationToken cancellationToken = default) =>
        await api.GetAsync<List<RolWebItem>>(
            "api/Rol/listarRolesInactivos",
            cancellationToken) ?? [];

    public Task<object?> CrearRolAsync(
        RolFormularioWeb formulario,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<RolFormularioWeb, object>(
            "api/Rol/crearRol",
            formulario,
            cancellationToken);

    public Task<object?> ActualizarRolAsync(
        int rolId,
        RolFormularioWeb formulario,
        CancellationToken cancellationToken = default) =>
        api.PutAsync<RolFormularioWeb, object>(
            $"api/Rol/editarRol/{rolId}",
            formulario,
            cancellationToken);

    public Task<object?> ReactivarRolAsync(
        int rolId,
        CancellationToken cancellationToken = default) =>
        api.PutAsync<object, object>(
            $"api/Rol/reactivarRol/{rolId}",
            new { },
            cancellationToken);

    public Task EliminarRolAsync(
        int rolId,
        CancellationToken cancellationToken = default) =>
        api.EliminarAsync(
            $"api/Rol/eliminarRol/{rolId}",
            cancellationToken);

    public async Task<List<RolConPermisosWeb>> MatrizAsync(
        CancellationToken cancellationToken = default) =>
        await api.GetAsync<List<RolConPermisosWeb>>(
            "api/rol-interfaz/matriz-por-rol",
            cancellationToken) ?? [];

    public Task<object?> GuardarMatrizAsync(
        RolConPermisosWeb matriz,
        CancellationToken cancellationToken = default) =>
        api.PutAsync<RolConPermisosWeb, object>(
            "api/rol-permisos/actualizar-interfaz",
            matriz,
            cancellationToken);
}
