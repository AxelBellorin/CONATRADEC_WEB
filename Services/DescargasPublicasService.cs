using System.Text.Json;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Services;

/// <summary>
/// Consulta únicamente metadatos públicos. La URL física del instalador nunca
/// se expone aquí; los enlaces permanentes redirigen al formulario protegido.
/// </summary>
public sealed class DescargasPublicasService
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web);

    public DescargasPublicasService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public string UrlValidacionFormulario =>
        new Uri(
            httpClient.BaseAddress
                ?? throw new InvalidOperationException(
                    "La URL base de la API no está configurada."),
            "api/actualizaciones/descargas/validar-formulario")
        .AbsoluteUri;

    public async Task<DescargaPublicaWeb?> ObtenerUltimaAsync(
        string plataforma,
        string canal = "PRODUCCION",
        CancellationToken cancellationToken = default)
    {
        string plataformaNormalizada =
            NormalizarPlataforma(plataforma);

        string canalNormalizado =
            NormalizarCanal(canal);

        if (string.IsNullOrWhiteSpace(plataformaNormalizada))
        {
            throw new ArgumentException(
                "La plataforma debe ser Android o Windows.",
                nameof(plataforma));
        }

        if (string.IsNullOrWhiteSpace(canalNormalizado))
        {
            throw new ArgumentException(
                "El canal debe ser Producción o Pruebas.",
                nameof(canal));
        }

        string ruta =
            "api/actualizaciones/descargas/portal" +
            $"?plataforma={Uri.EscapeDataString(plataformaNormalizada)}" +
            $"&canal={Uri.EscapeDataString(canalNormalizado)}";

        using HttpResponseMessage response = await httpClient.GetAsync(
            ruta,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        string contenido = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                ExtraerMensaje(contenido),
                null,
                response.StatusCode);
        }

        RespuestaPortal? respuesta =
            JsonSerializer.Deserialize<RespuestaPortal>(
                contenido,
                jsonOptions);

        if (respuesta?.Data == null)
            return null;

        DescargaPublicaWeb data = respuesta.Data;

        // El programa actual usa esta propiedad para construir sus botones.
        // Se devuelve la página protegida del propio portal, no el archivo.
        data.UrlDescarga =
            $"/descargas/{data.Plataforma.ToLowerInvariant()}";

        return data;
    }

    public static string NormalizarPlataforma(string? valor)
    {
        string normalizado =
            (valor ?? string.Empty).Trim().ToUpperInvariant();

        return normalizado switch
        {
            "ANDROID" => "ANDROID",
            "WINDOWS" => "WINDOWS",
            "WINUI" => "WINDOWS",
            _ => string.Empty
        };
    }

    private static string NormalizarCanal(string? valor)
    {
        string normalizado =
            (valor ?? string.Empty).Trim().ToUpperInvariant();

        return normalizado switch
        {
            "PRODUCCION" => "PRODUCCION",
            "PRODUCCIÓN" => "PRODUCCION",
            "PRUEBAS" => "PRUEBAS",
            _ => string.Empty
        };
    }

    private static string ExtraerMensaje(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return "La API no devolvió detalles del error.";

        try
        {
            using JsonDocument document = JsonDocument.Parse(contenido);
            JsonElement raiz = document.RootElement;

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
                    return valor.GetString() ?? contenido;
                }
            }
        }
        catch (JsonException)
        {
        }

        return contenido.Trim().Trim('"');
    }

    private sealed class RespuestaPortal
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public DescargaPublicaWeb? Data { get; set; }
    }
}

public sealed class DescargaPublicaWeb
{
    [JsonPropertyName("actualizacionAplicacionId")]
    public long ActualizacionAplicacionId { get; set; }

    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;

    [JsonPropertyName("canal")]
    public string Canal { get; set; } = string.Empty;

    [JsonPropertyName("versionNombre")]
    public string VersionNombre { get; set; } = string.Empty;

    [JsonPropertyName("versionCodigo")]
    public long VersionCodigo { get; set; }

    [JsonPropertyName("notasVersion")]
    public string NotasVersion { get; set; } = string.Empty;

    [JsonPropertyName("nombreArchivo")]
    public string NombreArchivo { get; set; } = string.Empty;

    [JsonPropertyName("tipoContenido")]
    public string TipoContenido { get; set; } = string.Empty;

    [JsonPropertyName("tamanoBytes")]
    public long TamanoBytes { get; set; }

    [JsonPropertyName("hashSha256")]
    public string HashSha256 { get; set; } = string.Empty;

    [JsonIgnore]
    public string UrlDescarga { get; set; } = string.Empty;

    [JsonPropertyName("fechaPublicacionUtc")]
    public DateTime? FechaPublicacionUtc { get; set; }

    [JsonIgnore]
    public string TamanoVisible => FormatearTamano(TamanoBytes);

    private static string FormatearTamano(long bytes)
    {
        string[] unidades = ["B", "KB", "MB", "GB"];
        double valor = Math.Max(0, bytes);
        int indice = 0;

        while (valor >= 1024 && indice < unidades.Length - 1)
        {
            valor /= 1024;
            indice++;
        }

        return $"{valor:0.##} {unidades[indice]}";
    }
}
