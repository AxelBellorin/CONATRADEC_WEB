using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class ApiClientService : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly WebActivityService actividad;

    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web);

    public ApiClientService(
        HttpClient httpClient,
        WebActivityService actividad)
    {
        this.httpClient = httpClient;
        this.actividad = actividad;
    }

    /// <summary>
    /// Se dispara cuando el backend confirma que el token o la sesión ya no
    /// son válidos.
    /// </summary>
    public event EventHandler? SesionInvalidada;

    public Uri? BaseAddress => httpClient.BaseAddress;

    public void ConfigurarToken(string? token)
    {
        httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue(
                    "Bearer",
                    token);
    }

    public Task<T?> GetAsync<T>(
        string ruta,
        CancellationToken cancellationToken = default) =>
        GetAsync<T>(
            ruta,
            null,
            cancellationToken);

    public async Task<T?> GetAsync<T>(
        string ruta,
        IReadOnlyDictionary<string, string>? encabezados,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                ruta);

        AgregarEncabezados(
            request,
            encabezados);

        using HttpResponseMessage response =
            await EnviarAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);

        return await LeerJsonAsync<T>(
            response,
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string ruta,
        TRequest datos,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                ruta)
            {
                Content = JsonContent.Create(
                    datos,
                    options: jsonOptions)
            };

        using HttpResponseMessage response =
            await EnviarAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);

        return await LeerJsonAsync<TResponse>(
            response,
            cancellationToken);
    }

    public async Task PostSinContenidoAsync(
        string ruta,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                ruta);

        using HttpResponseMessage response =
            await EnviarAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string ruta,
        TRequest datos,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Put,
                ruta)
            {
                Content = JsonContent.Create(
                    datos,
                    options: jsonOptions)
            };

        using HttpResponseMessage response =
            await EnviarAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);

        return await LeerJsonAsync<TResponse>(
            response,
            cancellationToken);
    }

    public async Task PutSinContenidoAsync(
        string ruta,
        IReadOnlyDictionary<string, string>? encabezados = null,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Put,
                ruta);

        AgregarEncabezados(
            request,
            encabezados);

        using HttpResponseMessage response =
            await EnviarAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);
    }

    public async Task EliminarAsync(
        string ruta,
        CancellationToken cancellationToken = default)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                ruta);

        using HttpResponseMessage response =
            await EnviarAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);
    }

    public async Task SubirArchivoAsync(
        string ruta,
        IBrowserFile archivo,
        string nombreCampo = "archivo",
        long tamanoMaximo = 8 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        using var contenido =
            new MultipartFormDataContent();

        await using Stream stream =
            archivo.OpenReadStream(
                tamanoMaximo,
                cancellationToken);

        using var archivoContenido =
            new StreamContent(stream);

        archivoContenido.Headers.ContentType =
            new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(
                    archivo.ContentType)
                    ? "application/octet-stream"
                    : archivo.ContentType);

        contenido.Add(
            archivoContenido,
            nombreCampo,
            archivo.Name);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                ruta)
            {
                Content = contenido
            };

        using HttpResponseMessage response =
            await EnviarAsync(
                request,
                cancellationToken);

        await ValidarRespuestaAsync(
            response,
            cancellationToken);
    }

    private async Task<HttpResponseMessage> EnviarAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        long versionActividad = 0;

        if (httpClient.DefaultRequestHeaders.Authorization != null)
        {
            versionActividad =
                actividad.ObtenerVersionPendiente();

            if (versionActividad > 0)
            {
                request.Headers.TryAddWithoutValidation(
                    "X-Actividad-Usuario",
                    "true");
            }
        }

        HttpResponseMessage response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        if (versionActividad > 0)
        {
            actividad.Confirmar(
                versionActividad);
        }

        return response;
    }

    private static void AgregarEncabezados(
        HttpRequestMessage request,
        IReadOnlyDictionary<string, string>? encabezados)
    {
        if (encabezados is null)
            return;

        foreach (KeyValuePair<string, string> encabezado
                 in encabezados)
        {
            if (string.IsNullOrWhiteSpace(
                    encabezado.Value))
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(
                encabezado.Key,
                encabezado.Value);
        }
    }

    private async Task<T?> LeerJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
            return default;

        string contenido =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (string.IsNullOrWhiteSpace(contenido))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(
                contenido,
                jsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
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

        string mensaje =
            ExtraerMensaje(contenido);

        if (EsSesionInvalidada(
                response,
                contenido))
        {
            SesionInvalidada?.Invoke(
                this,
                EventArgs.Empty);

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

        if (string.IsNullOrWhiteSpace(contenido))
            return false;

        string[] codigos =
        [
            "SESSION_INVALIDATED",
            "SESSION_INACTIVITY_TIMEOUT",
            "SESSION_TOKEN_EXPIRED",
            "SESSION_NOT_ACTIVE",
            "AUTH_TOKEN_REQUIRED",
            "AUTH_TOKEN_INVALID"
        ];

        return codigos.Any(
            codigo =>
                contenido.Contains(
                    codigo,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static string ExtraerMensaje(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return "La API no devolvió detalles del error.";
        }

        try
        {
            using JsonDocument documento =
                JsonDocument.Parse(contenido);

            JsonElement raiz =
                documento.RootElement;

            foreach (string propiedad in new[]
                     {
                         "message",
                         "mensaje",
                         "title",
                         "error"
                     })
            {
                if (raiz.TryGetProperty(
                        propiedad,
                        out JsonElement valor) &&
                    valor.ValueKind ==
                        JsonValueKind.String)
                {
                    return valor.GetString() ??
                           contenido;
                }
            }

            if (raiz.TryGetProperty(
                    "errors",
                    out JsonElement errores) &&
                errores.ValueKind ==
                    JsonValueKind.Object)
            {
                var mensajes =
                    new List<string>();

                foreach (JsonProperty error in
                         errores.EnumerateObject())
                {
                    if (error.Value.ValueKind !=
                        JsonValueKind.Array)
                    {
                        continue;
                    }

                    mensajes.AddRange(
                        error.Value
                            .EnumerateArray()
                            .Where(item =>
                                item.ValueKind ==
                                    JsonValueKind.String)
                            .Select(item =>
                                item.GetString())
                            .Where(item =>
                                !string.IsNullOrWhiteSpace(
                                    item))!);
                }

                if (mensajes.Count > 0)
                {
                    return string.Join(
                        " ",
                        mensajes);
                }
            }
        }
        catch (JsonException)
        {
            // La API también puede devolver texto plano.
        }

        return contenido.Trim('"');
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

}
