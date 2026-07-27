using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
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

public sealed class UsuarioCrearModel
{
    [Required(ErrorMessage = "Ingrese el nombre de usuario.")]
    [MaxLength(100, ErrorMessage = "El nombre de usuario no puede superar 100 caracteres.")]
    [JsonPropertyName("nombreUsuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el nombre completo.")]
    [MaxLength(150, ErrorMessage = "El nombre completo no puede superar 150 caracteres.")]
    [JsonPropertyName("nombreCompletoUsuario")]
    public string NombreCompletoUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [MaxLength(150)]
    [JsonPropertyName("correoUsuario")]
    public string CorreoUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el teléfono.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe contener exactamente 8 dígitos.")]
    [JsonPropertyName("telefonoUsuario")]
    public string TelefonoUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione la fecha de nacimiento.")]
    [JsonPropertyName("fechaNacimientoUsuario")]
    public DateOnly? FechaNacimientoUsuario { get; set; }

    [JsonPropertyName("esInterno")]
    public bool EsInterno { get; set; } = true;

    [JsonPropertyName("rolId")]
    public int? RolId { get; set; }

    [JsonPropertyName("municipioId")]
    public int? MunicipioId { get; set; }

    [Required(ErrorMessage = "Ingrese la identificación.")]
    [RegularExpression(
        @"^\d{3}-\d{6}-\d{4}[A-Za-z]$",
        ErrorMessage = "La identificación debe tener el formato 001-080701-1050R.")]
    [MaxLength(18)]
    [JsonPropertyName("identificacionUsuario")]
    public string IdentificacionUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese una contraseña.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#_\-]).{8,}$",
        ErrorMessage = "Debe tener al menos 8 caracteres, mayúscula, minúscula, número y símbolo.")]
    [JsonPropertyName("clave")]
    public string Clave { get; set; } = string.Empty;

    [JsonIgnore]
    public IBrowserFile? Imagen { get; set; }
}

public sealed class UsuarioActualizarModel
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el nombre completo.")]
    [MaxLength(150)]
    [JsonPropertyName("nombreCompletoUsuario")]
    public string NombreCompletoUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [MaxLength(150)]
    [JsonPropertyName("correoUsuario")]
    public string CorreoUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el teléfono.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe contener exactamente 8 dígitos.")]
    [JsonPropertyName("telefonoUsuario")]
    public string TelefonoUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione la fecha de nacimiento.")]
    [JsonPropertyName("fechaNacimientoUsuario")]
    public DateOnly? FechaNacimientoUsuario { get; set; }

    [JsonPropertyName("esInterno")]
    public bool EsInterno { get; set; }

    [JsonPropertyName("rolId")]
    public int? RolId { get; set; }

    [JsonPropertyName("municipioId")]
    public int? MunicipioId { get; set; }

    [Required(ErrorMessage = "Ingrese la identificación.")]
    [RegularExpression(
        @"^\d{3}-\d{6}-\d{4}[A-Za-z]$",
        ErrorMessage = "La identificación debe tener el formato 001-080701-1050R.")]
    [MaxLength(18)]
    [JsonPropertyName("identificacionUsuario")]
    public string IdentificacionUsuario { get; set; } = string.Empty;

    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&.#_\-]).{8,}$",
        ErrorMessage = "Debe tener al menos 8 caracteres, mayúscula, minúscula, número y símbolo.")]
    [JsonPropertyName("nuevaClave")]
    public string? NuevaClave { get; set; }

    [JsonPropertyName("activo")]
    public bool? Activo { get; set; } = true;

    [JsonPropertyName("urlImagenUsuario")]
    public string? UrlImagenUsuario { get; set; }

    [JsonIgnore]
    public IBrowserFile? Imagen { get; set; }

    public static UsuarioActualizarModel Desde(UsuarioListadoItem item) =>
        new()
        {
            UsuarioId = item.UsuarioId,
            NombreUsuario = item.NombreUsuario,
            NombreCompletoUsuario = item.NombreCompletoUsuario,
            CorreoUsuario = item.CorreoUsuario,
            TelefonoUsuario = item.TelefonoUsuario ?? string.Empty,
            FechaNacimientoUsuario = item.FechaNacimientoUsuario,
            EsInterno = item.EsInterno,
            RolId = item.EsInterno ? item.RolId : null,
            MunicipioId = item.MunicipioId,
            IdentificacionUsuario = item.IdentificacionUsuario,
            Activo = true,
            UrlImagenUsuario = item.UrlImagenUsuario
        };
}

public sealed class RolSelectorItem
{
    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } = string.Empty;

    [JsonPropertyName("descripcionRol")]
    public string DescripcionRol { get; set; } = string.Empty;
}

public sealed class MunicipioSelectorItem
{
    [JsonPropertyName("municipioId")]
    public int MunicipioId { get; set; }

    [JsonPropertyName("nombreMunicipio")]
    public string NombreMunicipio { get; set; } = string.Empty;

    [JsonPropertyName("departamentoId")]
    public int DepartamentoId { get; set; }

    [JsonPropertyName("nombreDepartamento")]
    public string NombreDepartamento { get; set; } = string.Empty;

    [JsonPropertyName("paisId")]
    public int PaisId { get; set; }

    [JsonPropertyName("nombrePais")]
    public string NombrePais { get; set; } = string.Empty;
}

public sealed class ApiMensajeRespuesta
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;
}
