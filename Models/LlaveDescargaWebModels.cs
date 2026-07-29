using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class NuevaLlaveDescargaWeb
{
    [Required]
    public string Plataforma { get; set; } = "ANDROID";

    [Required]
    public string Canal { get; set; } = "PRODUCCION";

    [Required(ErrorMessage = "Indique el destinatario o responsable.")]
    [MaxLength(200)]
    public string Destinatario { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Observacion { get; set; } = string.Empty;

    [Range(1, 100)]
    public int CantidadMaximaUsos { get; set; } = 1;

    public string TipoVigencia { get; set; } = "24";

    public DateTime? FechaExpiracionLocal { get; set; }
}

public sealed class CrearLlaveDescargaApi
{
    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;

    [JsonPropertyName("canal")]
    public string Canal { get; set; } = string.Empty;

    [JsonPropertyName("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [JsonPropertyName("observacion")]
    public string Observacion { get; set; } = string.Empty;

    [JsonPropertyName("cantidadMaximaUsos")]
    public int CantidadMaximaUsos { get; set; }

    [JsonPropertyName("vigenciaHoras")]
    public int? VigenciaHoras { get; set; }

    [JsonPropertyName("fechaExpiracionUtc")]
    public DateTime? FechaExpiracionUtc { get; set; }
}

public sealed class LlaveDescargaCreadaWeb
{
    [JsonPropertyName("actualizacionLlaveDescargaId")]
    public int ActualizacionLlaveDescargaId { get; set; }

    [JsonPropertyName("llave")]
    public string Llave { get; set; } = string.Empty;

    [JsonPropertyName("llaveEnmascarada")]
    public string LlaveEnmascarada { get; set; } = string.Empty;

    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;

    [JsonPropertyName("canal")]
    public string Canal { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [JsonPropertyName("observacion")]
    public string Observacion { get; set; } = string.Empty;

    [JsonPropertyName("cantidadMaximaUsos")]
    public int CantidadMaximaUsos { get; set; }

    [JsonPropertyName("cantidadUsos")]
    public int CantidadUsos { get; set; }

    [JsonPropertyName("usuarioCreacionId")]
    public int UsuarioCreacionId { get; set; }

    [JsonPropertyName("fechaCreacionUtc")]
    public DateTime FechaCreacionUtc { get; set; }

    [JsonPropertyName("fechaExpiracionUtc")]
    public DateTime FechaExpiracionUtc { get; set; }
}

public sealed class LlaveDescargaWebItem
{
    [JsonPropertyName("actualizacionLlaveDescargaId")]
    public int ActualizacionLlaveDescargaId { get; set; }

    [JsonPropertyName("llaveEnmascarada")]
    public string LlaveEnmascarada { get; set; } = string.Empty;

    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;

    [JsonPropertyName("canal")]
    public string Canal { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [JsonPropertyName("observacion")]
    public string Observacion { get; set; } = string.Empty;

    [JsonPropertyName("cantidadMaximaUsos")]
    public int CantidadMaximaUsos { get; set; }

    [JsonPropertyName("cantidadUsos")]
    public int CantidadUsos { get; set; }

    [JsonPropertyName("usuarioCreacionId")]
    public int UsuarioCreacionId { get; set; }

    [JsonPropertyName("usuarioRevocacionId")]
    public int? UsuarioRevocacionId { get; set; }

    [JsonPropertyName("fechaCreacionUtc")]
    public DateTime FechaCreacionUtc { get; set; }

    [JsonPropertyName("fechaExpiracionUtc")]
    public DateTime FechaExpiracionUtc { get; set; }

    [JsonPropertyName("fechaUltimoUsoUtc")]
    public DateTime? FechaUltimoUsoUtc { get; set; }

    [JsonPropertyName("fechaRevocacionUtc")]
    public DateTime? FechaRevocacionUtc { get; set; }
}

public sealed class AuditoriaDescargaWebItem
{
    [JsonPropertyName("actualizacionDescargaAuditoriaId")]
    public long ActualizacionDescargaAuditoriaId { get; set; }

    [JsonPropertyName("actualizacionLlaveDescargaId")]
    public int? ActualizacionLlaveDescargaId { get; set; }

    [JsonPropertyName("actualizacionAplicacionId")]
    public int? ActualizacionAplicacionId { get; set; }

    [JsonPropertyName("resultado")]
    public string Resultado { get; set; } = string.Empty;

    [JsonPropertyName("detalle")]
    public string Detalle { get; set; } = string.Empty;

    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;

    [JsonPropertyName("canal")]
    public string Canal { get; set; } = string.Empty;

    [JsonPropertyName("versionNombre")]
    public string VersionNombre { get; set; } = string.Empty;

    [JsonPropertyName("versionCodigo")]
    public long? VersionCodigo { get; set; }

    [JsonPropertyName("nombreArchivo")]
    public string NombreArchivo { get; set; } = string.Empty;

    [JsonPropertyName("ipCliente")]
    public string IpCliente { get; set; } = string.Empty;

    [JsonPropertyName("navegador")]
    public string Navegador { get; set; } = string.Empty;

    [JsonPropertyName("sistemaOperativo")]
    public string SistemaOperativo { get; set; } = string.Empty;

    [JsonPropertyName("tipoDispositivo")]
    public string TipoDispositivo { get; set; } = string.Empty;

    [JsonPropertyName("identificadorDispositivoWeb")]
    public string IdentificadorDispositivoWeb { get; set; } = string.Empty;

    [JsonPropertyName("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [JsonPropertyName("usuarioGeneradorId")]
    public int? UsuarioGeneradorId { get; set; }

    [JsonPropertyName("fechaUtc")]
    public DateTime FechaUtc { get; set; }
}
