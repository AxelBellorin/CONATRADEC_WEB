using System.ComponentModel.DataAnnotations;

namespace CONATRADEC.AdminWeb.Models;

public sealed class PropietarioAccesoItem
{
    public int PropietarioId { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistroUtc { get; set; }
    public int TotalTerrenos { get; set; }
    public int? UsuarioPortalId { get; set; }
    public string? UsuarioPortal { get; set; }

    public override string ToString() =>
        $"{NombreCompleto} · {Identificacion}";
}

public sealed class PropietarioAccesoFormulario
{
    public int PropietarioId { get; set; }

    [Required(ErrorMessage = "La identificación es obligatoria.")]
    [MaxLength(50)]
    public string Identificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [MaxLength(25)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [MaxLength(150)]
    public string? Correo { get; set; }

    [MaxLength(300)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;
}

public sealed class PropietarioDetalleRespuesta
{
    public PropietarioAccesoItem Propietario { get; set; } = new();
    public List<TerrenoPropietarioItem> Terrenos { get; set; } = [];
}

public sealed class TerrenoPropietarioItem
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string DireccionTerreno { get; set; } = string.Empty;
    public decimal ExtensionManzanas { get; set; }
    public decimal QuintalesOro { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaAsignacionUtc { get; set; }
}

public sealed class UsuarioAccesoCatalogo
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompletoUsuario { get; set; } = string.Empty;
    public string CorreoUsuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;

    public override string ToString() =>
        $"{NombreCompletoUsuario} ({NombreUsuario})";
}

public sealed class TerrenoAccesoCatalogo
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string PropietarioActual { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;

    public override string ToString() =>
        $"{CodigoTerreno} · {PropietarioActual}";
}

public sealed class DepartamentoAccesoCatalogo
{
    public int DepartamentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public override string ToString() => Nombre;
}

public sealed class MunicipioAccesoCatalogo
{
    public int MunicipioId { get; set; }
    public int DepartamentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public override string ToString() => Nombre;
}

public sealed class AsignacionTerrenoItem
{
    public int UsuarioTerrenoAsignacionId { get; set; }
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompletoUsuario { get; set; } = string.Empty;
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string TipoAsignacion { get; set; } = string.Empty;
    public bool EsResponsablePrincipal { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaInicioUtc { get; set; }
}

public sealed class CoberturaTerritorialItem
{
    public int UsuarioCoberturaTerritorialId { get; set; }
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompletoUsuario { get; set; } = string.Empty;
    public string TipoCobertura { get; set; } = string.Empty;
    public int? DepartamentoId { get; set; }
    public string? Departamento { get; set; }
    public int? MunicipioId { get; set; }
    public string? Municipio { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaInicioUtc { get; set; }
}

public sealed class VincularTerrenoPropietarioRequest
{
    public int TerrenoId { get; set; }
}

public sealed class VincularUsuarioPropietarioRequest
{
    public int UsuarioId { get; set; }
    public int PropietarioId { get; set; }
}

public sealed class AsignarUsuarioTerrenoRequest
{
    public int UsuarioId { get; set; }
    public int TerrenoId { get; set; }
    public string TipoAsignacion { get; set; } = "TECNICO";
    public bool EsResponsablePrincipal { get; set; }
    public string? Observacion { get; set; }
}

public sealed class GuardarCoberturaTerritorialRequest
{
    public int UsuarioId { get; set; }
    public string TipoCobertura { get; set; } = "DEPARTAMENTO";
    public int? DepartamentoId { get; set; }
    public int? MunicipioId { get; set; }
    public string? Observacion { get; set; }
}

public sealed class ApiOperacionAccesoRespuesta
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
