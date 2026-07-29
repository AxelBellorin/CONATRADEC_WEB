using CONATRADEC.AdminWeb.Models;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class LlavesDescargaService : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly AuthStateService authState;
    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web);

    public LlavesDescargaService(
        IConfiguration configuration,
        AuthStateService authState)
    {
        this.authState = authState;

        string baseUrl = configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException(
                "No se encontró ApiSettings:BaseUrl en appsettings.json.");

        httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/"),
            Timeout = TimeSpan.FromMinutes(2)
        };
    }

    public async Task<List<LlaveDescargaWebItem>> ListarAsync(
        int usuarioId,
        string? plataforma,
        string? canal,
        string? estado,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();
        AgregarParametro(parametros, "plataforma", plataforma);
        AgregarParametro(parametros, "canal", canal);
        AgregarParametro(parametros, "estado", estado);

        string ruta = "api/actualizaciones/descargas/llaves";

        if (parametros.Count > 0)
            ruta += "?" + string.Join("&", parametros);

        RespuestaApi<List<LlaveDescargaWebItem>>? respuesta =
            await EnviarAsync<RespuestaApi<List<LlaveDescargaWebItem>>>(
                HttpMethod.Get,
                ruta,
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data ?? [];
    }

    public async Task<LlaveDescargaCreadaWeb?> CrearAsync(
        int usuarioId,
        CrearLlaveDescargaApi modelo,
        CancellationToken cancellationToken = default)
    {
        using JsonContent contenido = JsonContent.Create(modelo);

        RespuestaApi<LlaveDescargaCreadaWeb>? respuesta =
            await EnviarAsync<RespuestaApi<LlaveDescargaCreadaWeb>>(
                HttpMethod.Post,
                "api/actualizaciones/descargas/llaves",
                usuarioId,
                contenido,
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task<LlaveDescargaWebItem?> RevocarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default)
    {
        RespuestaApi<LlaveDescargaWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<LlaveDescargaWebItem>>(
                HttpMethod.Put,
                $"api/actualizaciones/descargas/llaves/{id}/revocar",
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data;
    }

    public Task<LlaveDescargaWebItem?> BloquearAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default) =>
        EjecutarEstadoAsync(
            usuarioId,
            id,
            "bloquear",
            cancellationToken);

    public Task<LlaveDescargaWebItem?> ReactivarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default) =>
        EjecutarEstadoAsync(
            usuarioId,
            id,
            "reactivar",
            cancellationToken);

    private async Task<LlaveDescargaWebItem?> EjecutarEstadoAsync(
        int usuarioId,
        int id,
        string accion,
        CancellationToken cancellationToken)
    {
        RespuestaApi<LlaveDescargaWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<LlaveDescargaWebItem>>(
                HttpMethod.Put,
                $"api/actualizaciones/descargas/llaves/{id}/{accion}",
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task<List<AuditoriaDescargaWebItem>> AuditoriaAsync(
        int usuarioId,
        int limite = 200,
        CancellationToken cancellationToken = default)
    {
        limite = Math.Clamp(limite, 1, 1000);

        RespuestaApi<List<AuditoriaDescargaWebItem>>? respuesta =
            await EnviarAsync<RespuestaApi<List<AuditoriaDescargaWebItem>>>(
                HttpMethod.Get,
                $"api/actualizaciones/descargas/auditoria?limite={limite}",
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data ?? [];
    }

    private async Task<T?> EnviarAsync<T>(
        HttpMethod metodo,
        string ruta,
        int usuarioId,
        HttpContent? contenido,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(metodo, ruta);

        IReadOnlyDictionary<string, string> encabezados =
            authState.CrearEncabezadosSesion();

        string usuarioSesion = encabezados["X-Usuario-Id"];

        if (!string.Equals(
                usuarioSesion,
                usuarioId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                "La sesión activa no corresponde al usuario solicitado.");
        }

        foreach (KeyValuePair<string, string> encabezado in encabezados)
        {
            request.Headers.TryAddWithoutValidation(
                encabezado.Key,
                encabezado.Value);
        }

        request.Content = contenido;

        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        string texto = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (EsSesionInvalidada(response, texto))
        {
            await authState.InvalidarSesionDesdeApiAsync();
            throw new UnauthorizedAccessException(ExtraerMensaje(texto));
        }

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

    private static bool EsSesionInvalidada(
        HttpResponseMessage response,
        string contenido)
    {
        bool porEncabezado = response.Headers.TryGetValues(
                "X-Sesion-Invalidada",
                out IEnumerable<string>? valores) &&
            valores.Any(valor => string.Equals(
                valor,
                "true",
                StringComparison.OrdinalIgnoreCase));

        return porEncabezado ||
               contenido.Contains(
                   "SESSION_INVALIDATED",
                   StringComparison.OrdinalIgnoreCase);
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
                         "message",
                         "mensaje",
                         "title",
                         "error"
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

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
