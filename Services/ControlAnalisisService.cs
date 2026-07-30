using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class ControlAnalisisService
{
    private readonly ApiClientService apiClient;
    private readonly AuthStateService authState;

    public ControlAnalisisService(
        ApiClientService apiClient,
        AuthStateService authState)
    {
        this.apiClient = apiClient;
        this.authState = authState;
    }

    public async Task EliminarAsync(
        int analisisSueloId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        await EnviarMotivoAsync(
            $"api/control-analisis/{analisisSueloId}/eliminar",
            motivo,
            cancellationToken);
    }

    public async Task RecuperarAsync(
        int analisisSueloId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        await EnviarMotivoAsync(
            $"api/control-analisis/{analisisSueloId}/recuperar",
            motivo,
            cancellationToken);
    }

    public async Task<ControlAnalisisEstadoEliminacion?>
        ObtenerEstadoEliminacionAsync(
            int analisisSueloId,
            CancellationToken cancellationToken = default)
    {
        AuditoriaApiRespuesta<ControlAnalisisEstadoEliminacion>? respuesta =
            await apiClient.GetAsync<
                AuditoriaApiRespuesta<ControlAnalisisEstadoEliminacion>>(
                $"api/control-analisis/{analisisSueloId}/estado-eliminacion",
                authState.CrearEncabezadosSesion(),
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task<string> ObtenerPdfDataUriAsync(
        int analisisSueloCalculoId,
        CancellationToken cancellationToken = default)
    {
        UsuarioSesion usuario = ObtenerSesionActiva();

        using HttpClient client = CrearCliente(usuario);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/control-analisis/{analisisSueloCalculoId}/pdf");

        /* Abrir el PDF es una acción explícita del usuario. */
        request.Headers.TryAddWithoutValidation(
            "X-Actividad-Usuario",
            "true");

        using HttpResponseMessage response =
            await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);

        byte[] bytes =
            await response.Content.ReadAsByteArrayAsync(
                cancellationToken);

        return $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}";
    }

    private async Task EnviarMotivoAsync(
        string ruta,
        string motivo,
        CancellationToken cancellationToken)
    {
        UsuarioSesion usuario = ObtenerSesionActiva();

        using HttpClient client = CrearCliente(usuario);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            ruta)
        {
            Content = JsonContent.Create(
                new ControlAnalisisMotivoRequest
                {
                    Motivo = motivo
                })
        };

        /* Eliminar o recuperar es una acción explícita del usuario. */
        request.Headers.TryAddWithoutValidation(
            "X-Actividad-Usuario",
            "true");

        using HttpResponseMessage response =
            await client.SendAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);
    }

    private UsuarioSesion ObtenerSesionActiva()
    {
        UsuarioSesion usuario = authState.Usuario ??
            throw new UnauthorizedAccessException(
                "No existe una sesión activa.");

        if (string.IsNullOrWhiteSpace(usuario.Token))
        {
            throw new UnauthorizedAccessException(
                "La sesión no contiene un token de seguridad.");
        }

        return usuario;
    }

    private HttpClient CrearCliente(
        UsuarioSesion usuario)
    {
        var client = new HttpClient
        {
            BaseAddress = apiClient.BaseAddress,
            Timeout = TimeSpan.FromMinutes(2)
        };

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                usuario.Token);

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Usuario-Id",
            usuario.UsuarioId.ToString());

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Version-Sesion",
            usuario.VersionSesion.ToString());

        return client;
    }

    private async Task ValidarRespuestaAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string contenido =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        string mensaje = ExtraerMensaje(contenido);

        if (EsSesionInvalidada(response, contenido))
        {
            await authState.InvalidarSesionDesdeApiAsync();

            throw new UnauthorizedAccessException(
                mensaje);
        }

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(
                mensaje);
        }

        throw new HttpRequestException(
            mensaje,
            null,
            response.StatusCode);
    }

    private static bool EsSesionInvalidada(
        HttpResponseMessage response,
        string contenido)
    {
        bool porEncabezado =
            response.Headers.TryGetValues(
                "X-Sesion-Invalidada",
                out IEnumerable<string>? valores) &&
            valores.Any(valor =>
                string.Equals(
                    valor,
                    "true",
                    StringComparison.OrdinalIgnoreCase));

        if (porEncabezado)
            return true;

        string[] codigos =
        [
            "SESSION_INVALIDATED",
            "SESSION_INACTIVITY_TIMEOUT",
            "SESSION_TOKEN_EXPIRED",
            "SESSION_NOT_ACTIVE",
            "AUTH_TOKEN_REQUIRED",
            "AUTH_TOKEN_INVALID"
        ];

        return codigos.Any(codigo =>
            contenido.Contains(
                codigo,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string ExtraerMensaje(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return "La API no devolvió detalles del error.";

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(contenido);

            foreach (string propiedad in new[]
                     {
                         "message",
                         "mensaje",
                         "title",
                         "error"
                     })
            {
                if (document.RootElement.TryGetProperty(
                        propiedad,
                        out JsonElement value) &&
                    value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString() ?? contenido;
                }
            }
        }
        catch (JsonException)
        {
            // La API también puede devolver texto plano.
        }

        return contenido.Trim().Trim('"');
    }
}
