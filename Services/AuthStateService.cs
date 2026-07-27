using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class AuthStateService
{
    private readonly ApiClientService apiClient;
    private readonly BrowserSessionService browserSession;

    private bool inicializando;

    public AuthStateService(
        ApiClientService apiClient,
        BrowserSessionService browserSession)
    {
        this.apiClient = apiClient;
        this.browserSession = browserSession;
    }

    public event EventHandler? EstadoCambiado;

    public UsuarioSesion? Usuario { get; private set; }

    public bool Inicializado { get; private set; }

    public bool IsAuthenticated =>
        Usuario is not null &&
        !string.IsNullOrWhiteSpace(Usuario.Token);

    public bool EsAdministrador =>
        Usuario?.RolNombre.Contains(
            "ADMIN",
            StringComparison.OrdinalIgnoreCase) == true;

    public bool PuedeEntrarPortal =>
        TienePermisoDirecto(
            PermisosWeb.Portal,
            permiso => permiso.Leer == true);

    public async Task InicializarAsync()
    {
        if (Inicializado || inicializando)
            return;

        inicializando = true;

        try
        {
            UsuarioSesion? usuarioGuardado =
                await browserSession.LeerAsync();

            if (usuarioGuardado is null ||
                !usuarioGuardado.Activo ||
                string.IsNullOrWhiteSpace(
                    usuarioGuardado.Token))
            {
                Usuario = null;
                apiClient.ConfigurarToken(null);
                return;
            }

            Usuario = usuarioGuardado;

            apiClient.ConfigurarToken(
                usuarioGuardado.Token);

            if (!PuedeEntrarPortal)
            {
                await LimpiarSesionAsync();
            }
        }
        finally
        {
            inicializando = false;
            Inicializado = true;
            NotificarCambio();
        }
    }

    public async Task<ResultadoOperacion>
        IniciarSesionAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
    {
        try
        {
            UsuarioSesion? usuario =
                await apiClient.PostAsync<
                    LoginRequest,
                    UsuarioSesion>(
                    "api/auth/login",
                    request,
                    cancellationToken);

            if (usuario is null)
            {
                return ResultadoOperacion.Fallido(
                    "La API no devolvió los datos del usuario.");
            }

            if (!usuario.Activo)
            {
                return ResultadoOperacion.Fallido(
                    "El usuario está inactivo.");
            }

            bool puedeEntrar =
                usuario.Permisos.Any(
                    permiso =>
                        string.Equals(
                            permiso.NombreInterfaz,
                            PermisosWeb.Portal,
                            StringComparison.OrdinalIgnoreCase) &&
                        permiso.Leer == true);

            if (!puedeEntrar)
            {
                return ResultadoOperacion.Fallido(
                    "Tu usuario no tiene habilitado el acceso al portal administrativo.");
            }

            Usuario = usuario;

            apiClient.ConfigurarToken(
                usuario.Token);

            await browserSession.GuardarAsync(
                usuario);

            Inicializado = true;
            NotificarCambio();

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
        catch
        {
            return ResultadoOperacion.Fallido(
                "Ocurrió un error inesperado al iniciar sesión.");
        }
    }

    public bool TienePermisoLectura(
        string interfaz) =>
        TienePermiso(
            interfaz,
            permiso => permiso.Leer == true);

    public bool TienePermisoAgregar(
        string interfaz) =>
        TienePermiso(
            interfaz,
            permiso => permiso.Agregar == true);

    public bool TienePermisoActualizar(
        string interfaz) =>
        TienePermiso(
            interfaz,
            permiso => permiso.Actualizar == true);

    public bool TienePermisoEliminar(
        string interfaz) =>
        TienePermiso(
            interfaz,
            permiso => permiso.Eliminar == true);

    private bool TienePermiso(
        string interfaz,
        Func<PermisoInterfaz, bool> selector)
    {
        if (string.Equals(
                interfaz,
                PermisosWeb.Portal,
                StringComparison.OrdinalIgnoreCase))
        {
            return TienePermisoDirecto(
                interfaz,
                selector);
        }

        if (EsAdministrador)
            return true;

        return TienePermisoDirecto(
            interfaz,
            selector);
    }

    private bool TienePermisoDirecto(
        string interfaz,
        Func<PermisoInterfaz, bool> selector) =>
        Usuario?.Permisos.Any(
            permiso =>
                string.Equals(
                    permiso.NombreInterfaz,
                    interfaz,
                    StringComparison.OrdinalIgnoreCase) &&
                selector(permiso)) == true;

    public async Task CerrarSesionAsync()
    {
        await LimpiarSesionAsync();

        Inicializado = true;
        NotificarCambio();
    }

    public Task CerrarSesion() =>
        CerrarSesionAsync();

    private async Task LimpiarSesionAsync()
    {
        Usuario = null;

        apiClient.ConfigurarToken(null);

        await browserSession.EliminarAsync();
    }

    private void NotificarCambio()
    {
        EstadoCambiado?.Invoke(
            this,
            EventArgs.Empty);
    }

    private static string LimpiarMensaje(
        string mensaje) =>
        mensaje.Trim().Trim('"');
}
