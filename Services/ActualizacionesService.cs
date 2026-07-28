using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class ActualizacionesService
{
    public const long TamanoMaximoArchivo =
        1024L * 1024L * 1024L;

    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web);

    public ActualizacionesService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<List<ActualizacionWebItem>> ListarAsync(
        int usuarioId,
        string? plataforma = null,
        string? canal = null,
        string? estado = null,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();

        AgregarParametro(parametros, "plataforma", plataforma);
        AgregarParametro(parametros, "canal", canal);
        AgregarParametro(parametros, "estado", estado);

        string ruta = "api/actualizaciones/administrar";
        if (parametros.Count > 0)
            ruta += "?" + string.Join("&", parametros);

        RespuestaApi<List<ActualizacionWebItem>>? respuesta =
            await EnviarAsync<RespuestaApi<List<ActualizacionWebItem>>>(
                HttpMethod.Get,
                ruta,
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data ?? [];
    }

    public async Task<SiguienteVersionWeb?> ObtenerSiguienteAsync(
        int usuarioId,
        string plataforma,
        string canal,
        CancellationToken cancellationToken = default)
    {
        string ruta =
            "api/actualizaciones/siguiente-version" +
            $"?plataforma={Uri.EscapeDataString(plataforma)}" +
            $"&canal={Uri.EscapeDataString(canal)}";

        RespuestaApi<SiguienteVersionWeb>? respuesta =
            await EnviarAsync<RespuestaApi<SiguienteVersionWeb>>(
                HttpMethod.Get,
                ruta,
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task<ActualizacionWebItem?> SubirAsync(
        int usuarioId,
        ActualizacionNuevaWeb modelo,
        CancellationToken cancellationToken = default)
    {
        if (modelo.Archivo == null)
            throw new InvalidOperationException("Debe seleccionar un archivo.");

        using var contenido = new MultipartFormDataContent();

        await using Stream stream = modelo.Archivo.OpenReadStream(
            TamanoMaximoArchivo,
            cancellationToken);

        using var archivoContenido = new StreamContent(stream);

        if (!string.IsNullOrWhiteSpace(modelo.Archivo.ContentType))
        {
            archivoContenido.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    modelo.Archivo.ContentType);
        }

        contenido.Add(
            archivoContenido,
            "Archivo",
            modelo.Archivo.Name);

        contenido.Add(new StringContent(modelo.Plataforma), "Plataforma");
        contenido.Add(new StringContent(modelo.Canal), "Canal");
        contenido.Add(new StringContent(modelo.VersionNombre), "VersionNombre");
        contenido.Add(
            new StringContent(
                modelo.VersionCodigo.ToString(CultureInfo.InvariantCulture)),
            "VersionCodigo");
        contenido.Add(
            new StringContent(modelo.NotasVersion ?? string.Empty),
            "NotasVersion");
        contenido.Add(
            new StringContent(
                modelo.Obligatoria.ToString(CultureInfo.InvariantCulture)),
            "Obligatoria");

        if (modelo.DefinirVersionMinima &&
            modelo.VersionMinimaCodigo.HasValue)
        {
            contenido.Add(
                new StringContent(
                    modelo.VersionMinimaCodigo.Value.ToString(
                        CultureInfo.InvariantCulture)),
                "VersionMinimaCodigo");
        }

        RespuestaApi<ActualizacionWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<ActualizacionWebItem>>(
                HttpMethod.Post,
                "api/actualizaciones/subir",
                usuarioId,
                contenido,
                cancellationToken);

        return respuesta?.Data;
    }

    public Task<ActualizacionWebItem?> PublicarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default) =>
        EjecutarAccionAsync(
            usuarioId,
            id,
            "publicar",
            cancellationToken);

    public Task<ActualizacionWebItem?> RevocarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default) =>
        EjecutarAccionAsync(
            usuarioId,
            id,
            "revocar",
            cancellationToken);

    public async Task<ActualizacionWebItem?> ConfigurarAsync(
        int usuarioId,
        int id,
        ActualizacionConfiguracionWeb modelo,
        CancellationToken cancellationToken = default)
    {
        using JsonContent contenido = JsonContent.Create(modelo);

        RespuestaApi<ActualizacionWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<ActualizacionWebItem>>(
                HttpMethod.Put,
                $"api/actualizaciones/{id}/configuracion",
                usuarioId,
                contenido,
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task EliminarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default)
    {
        await EnviarAsync<object>(
            HttpMethod.Delete,
            $"api/actualizaciones/{id}",
            usuarioId,
            null,
            cancellationToken);
    }

    private async Task<ActualizacionWebItem?> EjecutarAccionAsync(
        int usuarioId,
        int id,
        string accion,
        CancellationToken cancellationToken)
    {
        RespuestaApi<ActualizacionWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<ActualizacionWebItem>>(
                HttpMethod.Put,
                $"api/actualizaciones/{id}/{accion}",
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data;
    }

    private async Task<T?> EnviarAsync<T>(
        HttpMethod metodo,
        string ruta,
        int usuarioId,
        HttpContent? contenido,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(metodo, ruta);

        request.Headers.TryAddWithoutValidation(
            "X-Usuario-Id",
            usuarioId.ToString(CultureInfo.InvariantCulture));

        request.Content = contenido;

        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        string texto = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string mensaje = ExtraerMensaje(texto);

            if (response.StatusCode is
                HttpStatusCode.Unauthorized or
                HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(mensaje);
            }

            throw new HttpRequestException(
                mensaje,
                null,
                response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(texto))
            return default;

        return JsonSerializer.Deserialize<T>(texto, jsonOptions);
    }

    private static void AgregarParametro(
        ICollection<string> parametros,
        string nombre,
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return;

        parametros.Add(
            $"{nombre}={Uri.EscapeDataString(valor.Trim())}");
    }

    private static string ExtraerMensaje(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "La API no devolvió detalles del error.";

        try
        {
            using JsonDocument documento = JsonDocument.Parse(texto);
            JsonElement raiz = documento.RootElement;

            foreach (string propiedad in new[]
                     {
                         "message", "mensaje", "title", "error"
                     })
            {
                if (raiz.TryGetProperty(propiedad, out JsonElement valor) &&
                    valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString() ?? texto;
                }
            }
        }
        catch (JsonException)
        {
        }

        return texto.Trim().Trim('"');
    }
}
