namespace CONATRADEC.AdminWeb.Models;

public sealed class DashboardEjecutivoResumen
{
    public DateTime FechaConsultaUtc { get; set; }
    public int TotalTerrenos { get; set; }
    public int TerrenosConAnalisis { get; set; }
    public int TerrenosSinAnalisis { get; set; }
    public decimal ExtensionTotalManzanas { get; set; }
    public decimal ProduccionEstimadaQuintalesOro { get; set; }
    public int TotalAnalisis { get; set; }
    public int AnalisisMesActual { get; set; }
    public int AnalisisUltimos30Dias { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosInternos { get; set; }
    public int UsuariosExternos { get; set; }
    public int DispositivosConectados { get; set; }
    public int UsuariosConectados { get; set; }
    public int AlertasCriticas { get; set; }
    public int AlertasAtencion { get; set; }
    public int TerrenosNormales { get; set; }
    public int SeguimientosPendientes { get; set; }
    public int SeguimientosEnProceso { get; set; }
    public int SeguimientosAtendidos { get; set; }
    public int SeguimientosDescartados { get; set; }
    public int SeguimientosSinAsignar { get; set; }
    public decimal PorcentajeTerrenosAnalizados { get; set; }
    public decimal PorcentajeSeguimientosCerrados { get; set; }
    public List<DashboardSerieMesItem> AnalisisPorMes { get; set; } = [];
    public List<DashboardDepartamentoItem> Departamentos { get; set; } = [];
    public List<DashboardAlertaItem> AlertasRecientes { get; set; } = [];
    public List<DashboardIndicadorAlertaItem> DistribucionAlertas { get; set; } = [];
    public List<DashboardTecnicoItem> Tecnicos { get; set; } = [];
}

public sealed class DashboardSerieMesItem
{
    public string Mes { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public sealed class DashboardDepartamentoItem
{
    public string Departamento { get; set; } = string.Empty;
    public int Terrenos { get; set; }
    public int TerrenosAnalizados { get; set; }
    public decimal ExtensionManzanas { get; set; }
    public decimal CoberturaAnalisisPorcentaje { get; set; }
}

public sealed class DashboardAlertaItem
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public DateTime FechaAnalisis { get; set; }
    public int? SeguimientoId { get; set; }
    public string? EstadoSeguimiento { get; set; }
    public string? Responsable { get; set; }
}

public sealed class DashboardIndicadorAlertaItem
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class DashboardTecnicoItem
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
    public int Atendidos { get; set; }
    public int TotalAbiertos { get; set; }
}
