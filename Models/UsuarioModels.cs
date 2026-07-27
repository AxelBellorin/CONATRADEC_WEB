using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class UsuarioListadoItem
{
    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("nombreUsuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [JsonPropertyName("nombreCompletoUsuario")]
    public string NombreCompletoUsuario { get; set; } = string.Empty;

    [JsonPropertyName("correoUsuario")]
    public string CorreoUsuario { get; set; } = string.Empty;

    [JsonPropertyName("telefonoUsuario")]
    public string? TelefonoUsuario { get; set; }

    [JsonPropertyName("fechaNacimientoUsuario")]
    public DateOnly? FechaNacimientoUsuario { get; set; }

    [JsonPropertyName("identificacionUsuario")]
    public string IdentificacionUsuario { get; set; } = string.Empty;

    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("procedenciaId")]
    public int ProcedenciaId { get; set; }

    [JsonPropertyName("municipioId")]
    public int? MunicipioId { get; set; }

    [JsonPropertyName("rolNombre")]
    public string RolNombre { get; set; } = string.Empty;

    [JsonPropertyName("procedenciaNombre")]
    public string ProcedenciaNombre { get; set; } = string.Empty;

    [JsonPropertyName("esInterno")]
    public bool EsInterno { get; set; }

    [JsonPropertyName("urlImagenUsuario")]
    public string UrlImagenUsuario { get; set; } = string.Empty;
}
