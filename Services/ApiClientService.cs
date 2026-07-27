using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

    public Task<T?> GetAsync<T>(
        string ruta,
        CancellationToken cancellationToken = default) =>
        GetAsync<T>(ruta, null, cancellationToken);

    public async Task<T?> GetAsync<T>(
        string ruta,
        IReadOnlyDictionary<string, string>? encabezados,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ruta);
        AgregarEncabezados(request, encabezados);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ValidarRespuestaAsync(response, cancellationToken);

        return await LeerJsonAsync<T>(response, cancellationToken);
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
        return await LeerJsonAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string ruta,
        TRequest datos,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            ruta,
            datos,
            jsonOptions,
            cancellationToken);

        await ValidarRespuestaAsync(response, cancellationToken);
        return await LeerJsonAsync<TResponse>(response, cancellationToken);
    }


public async Task PutSinContenidoAsync(
    string ruta,
    IReadOnlyDictionary<string, string>? encabezados = null,
    CancellationToken cancellationToken = default)
{
    using var request = new HttpRequestMessage(HttpMethod.Put, ruta);
    AgregarEncabezados(request, encabezados);

    using var response = await httpClient.SendAsync(
        request,
        cancellationToken);

    await ValidarRespuestaAsync(response, cancellationToken);
}

    public async Task EliminarAsync(
        string ruta,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(ruta, cancellationToken);
        await ValidarRespuestaAsync(response, cancellationToken);
    }

    public async Task SubirArchivoAsync(
        string ruta,
        IBrowserFile archivo,
        string nombreCampo = "archivo",
        long tamanoMaximo = 8 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        using var contenido = new MultipartFormDataContent();
        await using var stream = archivo.OpenReadStream(tamanoMaximo, cancellationToken);
        using var archivoContenido = new StreamContent(stream);

        archivoContenido.Headers.ContentType =
            new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(archivo.ContentType)
                    ? "application/octet-stream"
                    : archivo.ContentType);

        contenido.Add(archivoContenido, nombreCampo, archivo.Name);

        using var response = await httpClient.PostAsync(
            ruta,
            contenido,
            cancellationToken);

        await ValidarRespuestaAsync(response, cancellationToken);
    }

    private static void AgregarEncabezados(
        HttpRequestMessage request,
        IReadOnlyDictionary<string, string>? encabezados)
    {
        if (encabezados is null)
            return;

        foreach (var encabezado in encabezados)
        {
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

        string contenido = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(contenido))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(contenido, jsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static async Task ValidarRespuestaAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string contenido = await response.Content.ReadAsStringAsync(cancellationToken);
        string mensaje = ExtraerMensaje(contenido);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException(mensaje);

        throw new HttpRequestException(mensaje, null, response.StatusCode);
    }

    private static string ExtraerMensaje(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return "La API no devolvió detalles del error.";

        try
        {
            using var documento = JsonDocument.Parse(contenido);
            JsonElement raiz = documento.RootElement;

            foreach (string propiedad in new[]
                     {
                         "message", "mensaje", "title", "error"
                     })
            {
                if (raiz.TryGetProperty(propiedad, out JsonElement valor) &&
                    valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString() ?? contenido;
                }
            }

            if (raiz.TryGetProperty("errors", out JsonElement errores) &&
                errores.ValueKind == JsonValueKind.Object)
            {
                var mensajes = new List<string>();

                foreach (JsonProperty error in errores.EnumerateObject())
                {
                    if (error.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    mensajes.AddRange(
                        error.Value.EnumerateArray()
                            .Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString())
                            .Where(item => !string.IsNullOrWhiteSpace(item))!);
                }

                if (mensajes.Count > 0)
                    return string.Join(" ", mensajes);
            }
        }
        catch (JsonException)
        {
            // La API también puede devolver texto plano.
        }

        return contenido.Trim('"');
    }
}
