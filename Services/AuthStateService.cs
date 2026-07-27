using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class AuthStateService
{
    private static readonly TimeSpan TiempoMaximoRestauracion =
        TimeSpan.FromSeconds(5);

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
        Usuario.Activo &&
        !string.IsNullOrWhiteSpace(
            Usuario.Token);

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
        if (Inicializado ||
            inicializando)
        {
            return;
        }

        inicializando = true;

        try
        {
            Task<UsuarioSesion?> lectura =
                browserSession.LeerAsync();

            Task esperaMaxima =
                Task.Delay(
                    TiempoMaximoRestauracion);

            Task completada =
                await Task.WhenAny(
                    lectura,
                    esperaMaxima);

            if (completada != lectura)
            {
                Usuario = null;
                apiClient.ConfigurarToken(null);
                return;
            }

            UsuarioSesion? restaurado =
                await lectura;

            if (restaurado is null ||
                !restaurado.Activo ||
                string.IsNullOrWhiteSpace(
                    restaurado.Token))
            {
                Usuario = null;
                apiClient.ConfigurarToken(null);
                return;
            }

            Usuario = restaurado;

            apiClient.ConfigurarToken(
                restaurado.Token);
        }
        catch
        {
            /*
             * Si localStorage o JavaScript falla durante desarrollo,
             * la aplicación continúa y permite volver al login.
             */
            Usuario = null;
            apiClient.ConfigurarToken(null);
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

            bool accesoPortal =
                usuario.Permisos.Any(
                    permiso =>
                        string.Equals(
                            permiso.NombreInterfaz,
                            PermisosWeb.Portal,
                            StringComparison.OrdinalIgnoreCase) &&
                        permiso.Leer == true);

            if (!accesoPortal)
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
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallido(
                $"Ocurrió un error inesperado al iniciar sesión. {ex.Message}");
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
        Usuario = null;
        apiClient.ConfigurarToken(null);

        await browserSession.EliminarAsync();

        Inicializado = true;
        NotificarCambio();
    }

    public Task CerrarSesion() =>
        CerrarSesionAsync();

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
