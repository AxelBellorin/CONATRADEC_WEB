using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class RespuestaApi<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class ActualizacionWebItem
{
    private string urlDescargaApi = string.Empty;

    [JsonPropertyName("actualizacionAplicacionId")]
    public int ActualizacionAplicacionId { get; set; }

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

    [JsonPropertyName("obligatoria")]
    public bool Obligatoria { get; set; }

    [JsonPropertyName("versionMinimaCodigo")]
    public long? VersionMinimaCodigo { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("nombreArchivo")]
    public string NombreArchivo { get; set; } = string.Empty;

    [JsonPropertyName("tipoContenido")]
    public string TipoContenido { get; set; } = string.Empty;

    [JsonPropertyName("tamanoBytes")]
    public long TamanoBytes { get; set; }

    [JsonPropertyName("hashSha256")]
    public string HashSha256 { get; set; } = string.Empty;

    [JsonPropertyName("usuarioCreacionId")]
    public int UsuarioCreacionId { get; set; }

    [JsonPropertyName("usuarioUltimaModificacionId")]
    public int UsuarioUltimaModificacionId { get; set; }

    [JsonPropertyName("fechaCreacionUtc")]
    public DateTime FechaCreacionUtc { get; set; }

    [JsonPropertyName("fechaUltimaModificacionUtc")]
    public DateTime FechaUltimaModificacionUtc { get; set; }

    [JsonPropertyName("fechaPublicacionUtc")]
    public DateTime? FechaPublicacionUtc { get; set; }

    /// <summary>
    /// La API todavía entrega el campo por compatibilidad, pero el portal nunca
    /// lo usa directamente. Toda descarga administrativa abre la página con llave.
    /// </summary>
    [JsonPropertyName("urlDescarga")]
    public string UrlDescarga
    {
        get => !string.Equals(Estado, "PUBLICADA", StringComparison.Ordinal) ||
               string.IsNullOrWhiteSpace(Plataforma)
            ? string.Empty
            : $"/descargas/{Plataforma.ToLowerInvariant()}";
        set => urlDescargaApi = value ?? string.Empty;
    }

    [JsonIgnore]
    public string UrlDescargaApi => urlDescargaApi;

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

public sealed class SiguienteVersionWeb
{
    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;

    [JsonPropertyName("canal")]
    public string Canal { get; set; } = string.Empty;

    [JsonPropertyName("ultimaVersionNombre")]
    public string UltimaVersionNombre { get; set; } = string.Empty;

    [JsonPropertyName("ultimaVersionCodigo")]
    public long UltimaVersionCodigo { get; set; }

    [JsonPropertyName("siguienteVersionNombre")]
    public string SiguienteVersionNombre { get; set; } = string.Empty;

    [JsonPropertyName("siguienteVersionCodigo")]
    public long SiguienteVersionCodigo { get; set; }
}

public sealed class ActualizacionNuevaWeb
{
    [Required(ErrorMessage = "Seleccione la plataforma.")]
    public string Plataforma { get; set; } = "ANDROID";

    [Required(ErrorMessage = "Seleccione el canal.")]
    public string Canal { get; set; } = "PRODUCCION";

    [Required(ErrorMessage = "La versión es obligatoria.")]
    [RegularExpression(
        @"^\d+\.\d+\.\d+(?:\.\d+)?$",
        ErrorMessage = "Use un formato como 1.0.2.")]
    public string VersionNombre { get; set; } = "1.0.2";

    [Range(
        1,
        long.MaxValue,
        ErrorMessage = "La compilación debe ser mayor que cero.")]
    public long VersionCodigo { get; set; } = 3;

    [MaxLength(4000)]
    public string NotasVersion { get; set; } = string.Empty;

    public bool Obligatoria { get; set; }

    public bool DefinirVersionMinima { get; set; }

    [Range(1, long.MaxValue)]
    public long? VersionMinimaCodigo { get; set; }

    public IBrowserFile? Archivo { get; set; }
}

public sealed class ActualizacionConfiguracionWeb
{
    [MaxLength(4000)]
    [JsonPropertyName("notasVersion")]
    public string NotasVersion { get; set; } = string.Empty;

    [JsonPropertyName("obligatoria")]
    public bool Obligatoria { get; set; }

    [JsonPropertyName("versionMinimaCodigo")]
    public long? VersionMinimaCodigo { get; set; }
}
