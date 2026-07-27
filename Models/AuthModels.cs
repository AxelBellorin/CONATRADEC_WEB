using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "El usuario o correo es obligatorio.")]
    [JsonPropertyName("usuarioOEmail")]
    public string UsuarioOEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [JsonPropertyName("clave")]
    public string Clave { get; set; } = string.Empty;
}

public sealed class UsuarioSesion
{
    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("nombreUsuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [JsonPropertyName("nombreCompletoUsuario")]
    public string NombreCompletoUsuario { get; set; } = string.Empty;

    [JsonPropertyName("correoUsuario")]
    public string CorreoUsuario { get; set; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("rolNombre")]
    public string RolNombre { get; set; } = string.Empty;

    [JsonPropertyName("procedenciaId")]
    public int ProcedenciaId { get; set; }

    [JsonPropertyName("procedenciaNombre")]
    public string ProcedenciaNombre { get; set; } = string.Empty;

    [JsonPropertyName("esInterno")]
    public bool EsInterno { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("urlImagenUsuario")]
    public string UrlImagenUsuario { get; set; } = string.Empty;

    [JsonPropertyName("permisos")]
    public List<PermisoInterfaz> Permisos { get; set; } = [];
}

public sealed class PermisoInterfaz
{
    [JsonPropertyName("interfazId")]
    public int InterfazId { get; set; }

    [JsonPropertyName("nombreInterfaz")]
    public string NombreInterfaz { get; set; } = string.Empty;

    [JsonPropertyName("leer")]
    public bool? Leer { get; set; }

    [JsonPropertyName("agregar")]
    public bool? Agregar { get; set; }

    [JsonPropertyName("actualizar")]
    public bool? Actualizar { get; set; }

    [JsonPropertyName("eliminar")]
    public bool? Eliminar { get; set; }
}

public sealed record ResultadoOperacion(bool Exitoso, string Mensaje)
{
    public static ResultadoOperacion Correcto(string mensaje = "") =>
        new(true, mensaje);

    public static ResultadoOperacion Fallido(string mensaje) =>
        new(false, mensaje);
}
