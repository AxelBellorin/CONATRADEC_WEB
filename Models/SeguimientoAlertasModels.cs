namespace CONATRADEC.AdminWeb.Models;

public sealed class SeguimientoAlertaItem
{
    public int SeguimientoAlertaAgricolaId { get; set; }
    public int TerrenoId { get; set; }
    public string TipoAlerta { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? UsuarioAsignadoId { get; set; }
    public string? UsuarioAsignado { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime FechaUltimaModificacionUtc { get; set; }
    public DateTime? FechaCierreUtc { get; set; }
}

public sealed class CrearSeguimientoAlerta
{
    public int TerrenoId { get; set; }
    public string TipoAlerta { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public int? UsuarioAsignadoId { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public int UsuarioAccionId { get; set; }
}

public sealed class ActualizarSeguimientoAlerta
{
    public string Estado { get; set; } = "PENDIENTE";
    public int? UsuarioAsignadoId { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public int UsuarioAccionId { get; set; }
}

public sealed class ConfiguracionAlertaItem
{
    public int ConfiguracionAlertaAgricolaId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Operador { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
