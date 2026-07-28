using System.ComponentModel.DataAnnotations;

namespace CONATRADEC.AdminWeb.Models;

public sealed class SeguimientoAlertaItem
{
    public int SeguimientoAlertaAgricolaId
    {
        get;
        set;
    }

    public int TerrenoId { get; set; }

    public string CodigoTerreno { get; set; } =
        string.Empty;

    public string Propietario { get; set; } =
        string.Empty;

    public string Direccion { get; set; } =
        string.Empty;

    public string Municipio { get; set; } =
        string.Empty;

    public string Departamento { get; set; } =
        string.Empty;

    public string TipoAlerta { get; set; } =
        string.Empty;

    public string Nivel { get; set; } =
        string.Empty;

    public string Estado { get; set; } =
        string.Empty;

    public int? UsuarioAsignadoId { get; set; }

    public string? UsuarioAsignado { get; set; }

    public string Observacion { get; set; } =
        string.Empty;

    public DateTime FechaCreacionUtc { get; set; }

    public DateTime FechaUltimaModificacionUtc
    {
        get;
        set;
    }

    public DateTime? FechaCierreUtc { get; set; }

    public string Ubicacion =>
        string.Join(
            ", ",
            new[]
            {
                Municipio,
                Departamento
            }
            .Where(item =>
                !string.IsNullOrWhiteSpace(item)));
}

public sealed class CrearSeguimientoAlerta
{
    [Range(1, int.MaxValue)]
    public int TerrenoId { get; set; }

    [Required, MaxLength(80)]
    public string TipoAlerta { get; set; } =
        string.Empty;

    [Required, MaxLength(20)]
    public string Nivel { get; set; } =
        string.Empty;

    public int? UsuarioAsignadoId { get; set; }

    [MaxLength(1000)]
    public string Observacion { get; set; } =
        string.Empty;

    [Range(1, int.MaxValue)]
    public int UsuarioAccionId { get; set; }
}

public sealed class ActualizarSeguimientoAlerta
{
    [Required]
    public string Estado { get; set; } =
        "PENDIENTE";

    public int? UsuarioAsignadoId { get; set; }

    [MaxLength(1000)]
    public string Observacion { get; set; } =
        string.Empty;

    [Range(1, int.MaxValue)]
    public int UsuarioAccionId { get; set; }
}

public sealed class ConfiguracionAlertaItem
{
    public int ConfiguracionAlertaAgricolaId
    {
        get;
        set;
    }

    public string Clave { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public decimal Valor { get; set; }

    public string Operador { get; set; } =
        string.Empty;

    public string Unidad { get; set; } =
        string.Empty;

    public string Descripcion { get; set; } =
        string.Empty;
}

public sealed class HistorialAlertaItem
{
    public int HistorialAlertaAgricolaId
    {
        get;
        set;
    }

    public string Accion { get; set; } =
        string.Empty;

    public string Detalle { get; set; } =
        string.Empty;

    public int UsuarioId { get; set; }

    public string Usuario { get; set; } =
        string.Empty;

    public DateTime FechaUtc { get; set; }
}

public sealed class TecnicoAlertaItem
{
    public int UsuarioId { get; set; }

    public string NombreCompleto { get; set; } =
        string.Empty;

    public string NombreUsuario { get; set; } =
        string.Empty;

    public string Rol { get; set; } =
        string.Empty;

    public string Procedencia { get; set; } =
        string.Empty;

    public string Etiqueta =>
        $"{NombreCompleto} · {Rol} · {Procedencia}";
}

public sealed class ActualizarUmbralAlerta
{
    public decimal Valor { get; set; }

    public int UsuarioAccionId { get; set; }
}

public sealed class ResumenReporteAlertas
{
    public int Total { get; set; }
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
    public int Atendidas { get; set; }
    public int Descartadas { get; set; }
    public int Criticas { get; set; }
}
