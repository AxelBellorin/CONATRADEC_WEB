using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class ApiClientService
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web);

    public ApiClientService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Uri? BaseAddress => httpClient.BaseAddress;

    public void ConfigurarToken(string? token)
    {
        httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(
        string ruta,
        CancellationToken cancellationToken = default) =>
        await GetAsync<T>(ruta, null, cancellationToken);

    public async Task<T?> GetAsync<T>(
        string ruta,
        IReadOnlyDictionary<string, string>? encabezados,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ruta);

        if (encabezados is not null)
        {
            foreach (var encabezado in encabezados)
            {
                request.Headers.TryAddWithoutValidation(
                    encabezado.Key,
                    encabezado.Value);
            }
        }

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        await ValidarRespuestaAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(
            jsonOptions,
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string ruta,
        TRequest datos,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            ruta,
            datos,
            jsonOptions,
            cancellationToken);

        await ValidarRespuestaAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<TResponse>(
            jsonOptions,
            cancellationToken);
    }

    private static async Task ValidarRespuestaAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var contenido = await response.Content.ReadAsStringAsync(cancellationToken);
        var mensaje = ExtraerMensaje(contenido);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException(mensaje);

        throw new HttpRequestException(
            mensaje,
            null,
            response.StatusCode);
    }

    private static string ExtraerMensaje(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return "La API no devolvió detalles del error.";

        try
        {
            using var documento = JsonDocument.Parse(contenido);
            var raiz = documento.RootElement;

            foreach (var propiedad in new[] { "message", "mensaje", "title", "error" })
            {
                if (raiz.TryGetProperty(propiedad, out var valor) &&
                    valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString() ?? contenido;
                }
            }
        }
        catch (JsonException)
        {
            // La API también puede devolver mensajes como texto plano.
        }

        return contenido.Trim('"');
    }
}
