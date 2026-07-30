using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.Components;

namespace CONATRADEC.AdminWeb.Services;

public sealed class AuthStateService
{
    private static readonly TimeSpan TiempoMaximoRestauracion =
        TimeSpan.FromSeconds(5);

    private readonly ApiClientService apiClient;
    private readonly BrowserSessionService browserSession;
    private readonly WebActivityService actividad;
    private readonly NavigationManager navigation;

    private bool inicializando;
    private int invalidandoSesion;

    public AuthStateService(
        ApiClientService apiClient,
        BrowserSessionService browserSession,
        WebActivityService actividad,
        NavigationManager navigation)
    {
        this.apiClient = apiClient;
        this.browserSession = browserSession;
        this.actividad = actividad;
        this.navigation = navigation;

        this.apiClient.SesionInvalidada +=
            AlRecibirSesionInvalidada;
    }

    public event EventHandler? EstadoCambiado;

    public UsuarioSesion? Usuario { get; private set; }

    public bool Inicializado { get; private set; }

    public bool IsAuthenticated =>
        Usuario is not null &&
        Usuario.Activo &&
        !string.IsNullOrWhiteSpace(Usuario.Token) &&
        Usuario.ExpiraTokenUtc.HasValue &&
        Usuario.ExpiraTokenUtc.Value > DateTime.UtcNow &&
        Usuario.UsuarioId > 0 &&
        Usuario.VersionSesion > 0;

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
                LimpiarEstadoEnMemoria();
                return;
            }

            UsuarioSesion? restaurado =
                await lectura;

            if (!EsSesionValida(restaurado))
            {
                LimpiarEstadoEnMemoria();
                await browserSession.EliminarAsync();
                return;
            }

            Usuario = restaurado;
            actividad.Reiniciar();

            apiClient.ConfigurarToken(
                restaurado!.Token);
        }
        catch
        {
            LimpiarEstadoEnMemoria();
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

            if (usuario.UsuarioId <= 0 ||
                usuario.VersionSesion <= 0)
            {
                return ResultadoOperacion.Fallido(
                    "La API no devolvió una sesión válida. " +
                    "Verifique que el backend actualizado esté publicado.");
            }

            if (string.IsNullOrWhiteSpace(usuario.Token) ||
                !usuario.ExpiraTokenUtc.HasValue ||
                usuario.ExpiraTokenUtc.Value <= DateTime.UtcNow)
            {
                return ResultadoOperacion.Fallido(
                    "La API no devolvió un token de seguridad vigente. " +
                    "Publique primero el backend actualizado.");
            }

            usuario.MinutosInactividad = Math.Clamp(
                usuario.MinutosInactividad,
                1,
                1440);

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
            actividad.Reiniciar();

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

    /// <summary>
    /// Se conserva por compatibilidad con los servicios actuales. El backend
    /// reemplaza estos valores por la identidad firmada dentro del JWT.
    /// </summary>
    public IReadOnlyDictionary<string, string>
        CrearEncabezadosSesion()
    {
        UsuarioSesion? usuario = Usuario;

        if (!EsSesionValida(usuario))
        {
            throw new UnauthorizedAccessException(
                "La sesión local no es válida. Inicie sesión nuevamente.");
        }

        return new Dictionary<string, string>
        {
            ["X-Usuario-Id"] =
                usuario!.UsuarioId.ToString(),
            ["X-Version-Sesion"] =
                usuario.VersionSesion.ToString()
        };
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
        try
        {
            if (IsAuthenticated)
            {
                await apiClient.PostSinContenidoAsync(
                    "api/sesion/cerrar");
            }
        }
        catch
        {
            // El cierre local no depende de que la API responda.
        }

        LimpiarEstadoEnMemoria();

        await browserSession.EliminarAsync();

        Inicializado = true;
        NotificarCambio();
    }

    public Task CerrarSesion() =>
        CerrarSesionAsync();

    public Task InvalidarSesionDesdeApiAsync() =>
        InvalidarSesionDesdeServidorAsync();

    private void AlRecibirSesionInvalidada(
        object? sender,
        EventArgs e)
    {
        _ = InvalidarSesionDesdeServidorAsync();
    }

    private async Task InvalidarSesionDesdeServidorAsync()
    {
        if (Interlocked.Exchange(
                ref invalidandoSesion,
                1) == 1)
        {
            return;
        }

        try
        {
            LimpiarEstadoEnMemoria();
            Inicializado = true;
            NotificarCambio();

            try
            {
                await browserSession.EliminarAsync();
            }
            catch
            {
                // La navegación al login no depende de localStorage.
            }

            navigation.NavigateTo(
                "/login",
                forceLoad: false,
                replace: true);
        }
        finally
        {
            Interlocked.Exchange(
                ref invalidandoSesion,
                0);
        }
    }

    private void LimpiarEstadoEnMemoria()
    {
        Usuario = null;
        actividad.Reiniciar();
        apiClient.ConfigurarToken(null);
    }

    private void NotificarCambio()
    {
        EstadoCambiado?.Invoke(
            this,
            EventArgs.Empty);
    }

    private static bool EsSesionValida(
        UsuarioSesion? usuario) =>
        usuario is not null &&
        usuario.Activo &&
        usuario.UsuarioId > 0 &&
        usuario.VersionSesion > 0 &&
        !string.IsNullOrWhiteSpace(usuario.Token) &&
        usuario.ExpiraTokenUtc.HasValue &&
        usuario.ExpiraTokenUtc.Value > DateTime.UtcNow;

    private static string LimpiarMensaje(
        string mensaje) =>
        mensaje.Trim().Trim('"');
}
