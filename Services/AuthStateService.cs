using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class AuthStateService
{
    private readonly ApiClientService apiClient;

    public AuthStateService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public UsuarioSesion? Usuario { get; private set; }

    public bool IsAuthenticated => Usuario is not null;

    public bool EsAdministrador =>
        Usuario?.RolNombre.Contains(
            "admin",
            StringComparison.OrdinalIgnoreCase) == true;

    public async Task<ResultadoOperacion> IniciarSesionAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var usuario = await apiClient.PostAsync<LoginRequest, UsuarioSesion>(
                "api/auth/login",
                request,
                cancellationToken);

            if (usuario is null)
                return ResultadoOperacion.Fallido(
                    "La API no devolvió los datos del usuario.");

            if (!usuario.Activo)
                return ResultadoOperacion.Fallido("El usuario está inactivo.");

            Usuario = usuario;
            apiClient.ConfigurarToken(usuario.Token);

            return ResultadoOperacion.Correcto();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ResultadoOperacion.Fallido(
                LimpiarMensaje(ex.Message));
        }
        catch (HttpRequestException ex)
        {
            return ResultadoOperacion.Fallido(
                $"No fue posible iniciar sesión. {LimpiarMensaje(ex.Message)}");
        }
        catch (TaskCanceledException)
        {
            return ResultadoOperacion.Fallido(
                "La API tardó demasiado en responder.");
        }
        catch (Exception)
        {
            return ResultadoOperacion.Fallido(
                "Ocurrió un error inesperado al iniciar sesión.");
        }
    }

    public bool TienePermisoLectura(string nombreInterfaz)
    {
        if (EsAdministrador)
            return true;

        return Usuario?.Permisos.Any(item =>
            string.Equals(
                item.NombreInterfaz,
                nombreInterfaz,
                StringComparison.OrdinalIgnoreCase) &&
            item.Leer == true) == true;
    }

    public void CerrarSesion()
    {
        Usuario = null;
        apiClient.ConfigurarToken(null);
    }

    private static string LimpiarMensaje(string mensaje) =>
        mensaje.Trim().Trim('"');
}
