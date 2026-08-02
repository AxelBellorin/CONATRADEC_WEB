using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CONATRADEC.AdminWeb.Services;

public sealed class AuthStateService : IAsyncDisposable
{
    private static readonly TimeSpan TiempoMaximoRestauracion =
        TimeSpan.FromSeconds(5);

    private const int IntervaloLatidoMilisegundos =
        45_000;

    private readonly ApiClientService apiClient;
    private readonly BrowserSessionService browserSession;
    private readonly WebActivityService actividad;
    private readonly NavigationManager navigation;
    private readonly IJSRuntime jsRuntime;

    private readonly SemaphoreSlim bloqueoLatido =
        new(initialCount: 1, maxCount: 1);

    private bool inicializando;
    private bool disposed;
    private int invalidandoSesion;

    private ContextoPresenciaNavegadorWeb?
        contextoPresencia;

    private DotNetObjectReference<AuthStateService>?
        referenciaLatidos;

    public AuthStateService(
        ApiClientService apiClient,
        BrowserSessionService browserSession,
        WebActivityService actividad,
        NavigationManager navigation,
        IJSRuntime jsRuntime)
    {
        this.apiClient = apiClient;
        this.browserSession = browserSession;
        this.actividad = actividad;
        this.navigation = navigation;
        this.jsRuntime = jsRuntime;

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

            /*
             * Al restaurar no se muestra un permiso nuevo. Si la ubicación ya
             * fue autorizada, el navegador la devolverá silenciosamente.
             */
            await IniciarLatidosNavegadorAsync(
                restaurado,
                solicitarUbicacion: false);
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

            /*
             * Un inicio de sesión correcto crea un único identificador
             * compartido por todas las pestañas del navegador.
             */
            await jsRuntime.InvokeAsync<string>(
                "conatradecBrowser.beginAuthenticatedSession");

            /*
             * La solicitud de ubicación se hace después de una acción directa
             * del usuario: presionar Iniciar sesión.
             */
            await IniciarLatidosNavegadorAsync(
                usuario,
                solicitarUbicacion: true);

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
                "Ocurrió un error inesperado al iniciar sesión. " +
                ex.Message);
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
        await DetenerLatidosNavegadorAsync(
            reportarDesconexion: true,
            motivo: "Cierre de sesión desde el portal web.");

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

    /// <summary>
    /// JavaScript invoca este método únicamente mientras la pestaña está
    /// realmente abierta.
    /// </summary>
    [JSInvokable]
    public async Task ReportarLatidoDesdeNavegadorAsync()
    {
        if (disposed ||
            !IsAuthenticated ||
            Usuario is null)
        {
            return;
        }

        if (!await bloqueoLatido.WaitAsync(0))
            return;

        try
        {
            /*
             * Se vuelve a consultar el contexto para obtener la página,
             * conexión y ubicación más recientes.
             */
            ContextoPresenciaNavegadorWeb? actualizado =
                await jsRuntime.InvokeAsync<
                    ContextoPresenciaNavegadorWeb>(
                    "conatradecBrowser.getPresenceContext",
                    false);

            if (ContextoValido(actualizado))
                contextoPresencia = actualizado;

            if (!ContextoValido(contextoPresencia))
                return;

            ReportarDispositivoConexionWebRequest request =
                CrearSolicitudLatido(
                    Usuario,
                    contextoPresencia!);

            await apiClient.PostAsync<
                ReportarDispositivoConexionWebRequest,
                ReportarDispositivoConexionWebResponse>(
                "conectividad/dispositivos/reportar",
                request);
        }
        catch (UnauthorizedAccessException)
        {
            // ApiClientService notificará la invalidación.
        }
        catch (HttpRequestException)
        {
            // El siguiente latido vuelve a intentarlo.
        }
        catch (TaskCanceledException)
        {
            // El siguiente latido vuelve a intentarlo.
        }
        catch (JSDisconnectedException)
        {
            // La pestaña o el circuito se está cerrando.
        }
        catch
        {
            // La presencia no debe interrumpir la navegación.
        }
        finally
        {
            bloqueoLatido.Release();
        }
    }

    private async Task IniciarLatidosNavegadorAsync(
        UsuarioSesion usuario,
        bool solicitarUbicacion)
    {
        if (disposed ||
            !EsSesionValida(usuario))
        {
            return;
        }

        await DetenerLatidosNavegadorAsync(
            reportarDesconexion: false,
            motivo: string.Empty);

        try
        {
            contextoPresencia =
                await jsRuntime.InvokeAsync<
                    ContextoPresenciaNavegadorWeb>(
                    "conatradecBrowser.getPresenceContext",
                    solicitarUbicacion);

            if (!ContextoValido(contextoPresencia))
            {
                contextoPresencia = null;
                return;
            }

            referenciaLatidos =
                DotNetObjectReference.Create(this);

            await jsRuntime.InvokeVoidAsync(
                "conatradecBrowser.startHeartbeat",
                referenciaLatidos,
                IntervaloLatidoMilisegundos);
        }
        catch (JSDisconnectedException)
        {
            contextoPresencia = null;
            LiberarReferenciaLatidos();
        }
        catch
        {
            contextoPresencia = null;
            LiberarReferenciaLatidos();
        }
    }

    private async Task DetenerLatidosNavegadorAsync(
        bool reportarDesconexion,
        string motivo)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync(
                "conatradecBrowser.stopHeartbeat");
        }
        catch
        {
            // La pestaña puede estar desconectándose.
        }

        if (reportarDesconexion &&
            IsAuthenticated &&
            ContextoValido(contextoPresencia))
        {
            await ReportarDesconexionSeguraAsync(
                contextoPresencia!,
                motivo);
        }

        contextoPresencia = null;
        LiberarReferenciaLatidos();
    }

    private async Task ReportarDesconexionSeguraAsync(
        ContextoPresenciaNavegadorWeb contexto,
        string motivo)
    {
        await bloqueoLatido.WaitAsync();

        try
        {
            var request =
                new DesconectarDispositivoConexionWebRequest
                {
                    InstalacionId =
                        contexto.InstalacionId,

                    SesionId =
                        contexto.SesionId,

                    Motivo =
                        Limitar(
                            motivo,
                            150)
                };

            await apiClient.PostAsync<
                DesconectarDispositivoConexionWebRequest,
                DesconectarDispositivoConexionWebResponse>(
                "conectividad/dispositivos/desconectar",
                request);
        }
        catch
        {
            /*
             * Si no se logra reportar el cierre, la tolerancia del backend
             * lo marcará desconectado automáticamente.
             */
        }
        finally
        {
            bloqueoLatido.Release();
        }
    }

    private ReportarDispositivoConexionWebRequest
        CrearSolicitudLatido(
            UsuarioSesion usuario,
            ContextoPresenciaNavegadorWeb contexto)
    {
        string pagina =
            navigation.ToBaseRelativePath(
                navigation.Uri);

        if (string.IsNullOrWhiteSpace(pagina))
            pagina = "/";

        if (!pagina.StartsWith('/'))
            pagina = "/" + pagina;

        return new ReportarDispositivoConexionWebRequest
        {
            InstalacionId =
                contexto.InstalacionId,

            SesionId =
                contexto.SesionId,

            UsuarioId =
                usuario.UsuarioId,

            Plataforma =
                Limitar(
                    contexto.Plataforma,
                    30,
                    "Web"),

            TipoDispositivo =
                Limitar(
                    contexto.TipoDispositivo,
                    30),

            Fabricante =
                Limitar(
                    contexto.Fabricante,
                    100),

            Modelo =
                Limitar(
                    contexto.Modelo,
                    150),

            NombreDispositivo =
                Limitar(
                    contexto.NombreDispositivo,
                    150),

            SistemaOperativo =
                Limitar(
                    contexto.SistemaOperativo,
                    100),

            VersionSistema =
                Limitar(
                    contexto.VersionSistema,
                    50),

            VersionApp =
                Limitar(
                    contexto.VersionApp,
                    50,
                    "Portal Web"),

            BuildApp =
                Limitar(
                    contexto.BuildApp,
                    50,
                    "1.0"),

            Idioma =
                Limitar(
                    contexto.Idioma,
                    20),

            TipoConexion =
                Limitar(
                    contexto.TipoConexion,
                    100),

            PaginaActual =
                Limitar(
                    pagina,
                    500),

            Latitud =
                contexto.Latitud,

            Longitud =
                contexto.Longitud,

            PrecisionMetros =
                contexto.PrecisionMetros,

            FechaUbicacionUtc =
                contexto.FechaUbicacionUtc,

            OrigenUbicacion =
                Limitar(
                    contexto.OrigenUbicacion,
                    30,
                    "NAVEGADOR"),

            EstadoPermisoUbicacion =
                Limitar(
                    contexto.EstadoPermisoUbicacion,
                    30,
                    "NO_SOLICITADA"),

            UbicacionSimulada =
                contexto.UbicacionSimulada
        };
    }

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
            await DetenerLatidosNavegadorAsync(
                reportarDesconexion: false,
                motivo: string.Empty);

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

    private void LiberarReferenciaLatidos()
    {
        referenciaLatidos?.Dispose();
        referenciaLatidos = null;
    }

    private void NotificarCambio()
    {
        EstadoCambiado?.Invoke(
            this,
            EventArgs.Empty);
    }

    private static bool ContextoValido(
        ContextoPresenciaNavegadorWeb? contexto) =>
        contexto is not null &&
        Guid.TryParse(
            contexto.InstalacionId,
            out _) &&
        Guid.TryParse(
            contexto.SesionId,
            out _);

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

    private static string Limitar(
        string? valor,
        int maximo,
        string predeterminado = "")
    {
        string texto =
            string.IsNullOrWhiteSpace(valor)
                ? predeterminado
                : valor.Trim();

        return texto.Length <= maximo
            ? texto
            : texto[..maximo];
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;

        apiClient.SesionInvalidada -=
            AlRecibirSesionInvalidada;

        await DetenerLatidosNavegadorAsync(
            reportarDesconexion: false,
            motivo: string.Empty);

        bloqueoLatido.Dispose();
    }
}
