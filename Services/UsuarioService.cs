using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace CONATRADEC.AdminWeb.Services;

public sealed class UsuarioService
{
    private readonly ApiClientService apiClient;

    public UsuarioService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    /// <summary>
    /// Endpoint histórico conservado para pantallas que todavía necesitan
    /// descargar la lista completa, como la administración de inactivos.
    /// </summary>
    public async Task<List<UsuarioListadoItem>> ListarAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<UsuarioListadoItem>>(
            "api/usuarios/listar",
            cancellationToken) ?? [];

    /// <summary>
    /// Listado paginado utilizado por la pantalla principal de usuarios.
    /// </summary>
    public async Task<ResultadoPaginado<UsuarioListadoItem>>
        ListarPaginadoAsync(
            int pagina,
            int tamanoPagina,
            string? buscar = null,
            CancellationToken cancellationToken = default)
    {
        string ruta =
            "api/usuarios/paginado" +
            $"?pagina={Math.Max(1, pagina)}" +
            $"&tamanoPagina={Math.Clamp(tamanoPagina, 6, 100)}";

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            ruta +=
                $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
        }

        return await apiClient.GetAsync<
                   ResultadoPaginado<UsuarioListadoItem>>(
                       ruta,
                       cancellationToken) ??
               new ResultadoPaginado<UsuarioListadoItem>
               {
                   Pagina = Math.Max(1, pagina),
                   TamanoPagina = tamanoPagina
               };
    }

    public async Task<UsuarioListadoItem?> ObtenerAsync(
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<UsuarioListadoItem>(
            $"api/usuarios/{usuarioId}",
            cancellationToken);

    public async Task<UsuarioListadoItem> CrearAsync(
        UsuarioCrearModel modelo,
        CancellationToken cancellationToken = default)
    {
        ValidarRol(modelo.EsInterno, modelo.RolId);

        UsuarioListadoItem? creado =
            await apiClient.PostAsync<
                UsuarioCrearModel,
                UsuarioListadoItem>(
                "api/usuarios/crear",
                modelo,
                cancellationToken);

        return creado ??
               throw new InvalidOperationException(
                   "La API no devolvió el usuario creado.");
    }

    public async Task<UsuarioListadoItem> ActualizarAsync(
        UsuarioActualizarModel modelo,
        CancellationToken cancellationToken = default)
    {
        ValidarRol(modelo.EsInterno, modelo.RolId);

        UsuarioListadoItem? actualizado =
            await apiClient.PutAsync<
                UsuarioActualizarModel,
                UsuarioListadoItem>(
                $"api/usuarios/actualizar/{modelo.UsuarioId}",
                modelo,
                cancellationToken);

        return actualizado ??
               throw new InvalidOperationException(
                   "La API no devolvió el usuario actualizado.");
    }

    public async Task DesactivarAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        UsuarioListadoItem? usuario = await ObtenerAsync(
            usuarioId,
            cancellationToken);

        if (usuario?.EsAdministradorProtegido == true)
        {
            throw new InvalidOperationException(
                "El usuario administrador es un registro protegido y no puede desactivarse.");
        }

        await apiClient.EliminarAsync(
            $"api/usuarios/eliminar/{usuarioId}",
            cancellationToken);
    }

    public Task SubirImagenAsync(
        int usuarioId,
        IBrowserFile archivo,
        CancellationToken cancellationToken = default) =>
        apiClient.SubirArchivoAsync(
            $"api/usuarios/{usuarioId}/SubirImagenUsuario",
            archivo,
            "archivo",
            8 * 1024 * 1024,
            cancellationToken);

    public async Task<List<RolSelectorItem>> ListarRolesAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<RolSelectorItem>>(
            "api/Rol/listarRoles",
            cancellationToken) ?? [];

    public async Task<List<MunicipioSelectorItem>> ListarMunicipiosAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<MunicipioSelectorItem>>(
            "api/municipio/listarTodos-por-departamento-por-pais",
            cancellationToken) ?? [];

    public string ConstruirUrlImagen(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return string.Empty;

        if (Uri.TryCreate(ruta, UriKind.Absolute, out Uri? absoluta))
            return absoluta.ToString();

        return new Uri(
            apiClient.BaseAddress!,
            ruta.TrimStart('/')).ToString();
    }

    private static void ValidarRol(bool esInterno, int? rolId)
    {
        if (esInterno && (!rolId.HasValue || rolId.Value <= 0))
        {
            throw new InvalidOperationException(
                "Seleccione un rol para el usuario interno.");
        }
    }
}
