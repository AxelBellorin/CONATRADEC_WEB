namespace CONATRADEC.AdminWeb.Models;

public sealed class DashboardResumen
{
    public DateTime FechaConsultaUtc { get; set; }

    public int TotalTerrenos { get; set; }
    public int TerrenosConAnalisis { get; set; }
    public int TerrenosSinAnalisis { get; set; }

    public int TotalAnalisis { get; set; }
    public int AnalisisMesActual { get; set; }
    public int AnalisisUltimos30Dias { get; set; }

    public int UsuariosActivos { get; set; }
    public int UsuariosInternos { get; set; }
    public int UsuariosExternos { get; set; }

    public int DispositivosConectados { get; set; }
    public int UsuariosConectados { get; set; }

    public decimal ExtensionTotalManzanas { get; set; }
    public decimal ProduccionEstimadaQuintalesOro { get; set; }

    public int AlertasCriticas { get; set; }
    public int AlertasAtencion { get; set; }

    public int TerrenosPhCritico { get; set; }
    public int TerrenosMateriaOrganicaBaja { get; set; }
    public int TerrenosAcidezAlta { get; set; }

    public decimal PorcentajeTerrenosAnalizados { get; set; }
    public decimal PorcentajePhCritico { get; set; }

    public bool ApiDisponible { get; set; }

    public List<AnalisisMesItem> AnalisisPorMes { get; set; } = [];
    public List<DepartamentoResumenItem> Departamentos { get; set; } = [];
    public List<AlertaTerrenoItem> AlertasRecientes { get; set; } = [];
    public List<IndicadorAlertaItem> DistribucionAlertas { get; set; } = [];

    public static DashboardResumen Vacio() =>
        new()
        {
            AnalisisPorMes =
            [
                new() { Mes = "Feb", Cantidad = 0 },
                new() { Mes = "Mar", Cantidad = 0 },
                new() { Mes = "Abr", Cantidad = 0 },
                new() { Mes = "May", Cantidad = 0 },
                new() { Mes = "Jun", Cantidad = 0 },
                new() { Mes = "Jul", Cantidad = 0 }
            ]
        };
}

public sealed class AnalisisMesItem
{
    public string Mes { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public sealed class DepartamentoResumenItem
{
    public string Departamento { get; set; } = string.Empty;
    public int Terrenos { get; set; }
    public int TerrenosAnalizados { get; set; }
    public decimal ExtensionManzanas { get; set; }
    public decimal CoberturaAnalisisPorcentaje { get; set; }
}

public sealed class AlertaTerrenoItem
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
}

public sealed class IndicadorAlertaItem
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public sealed record ResultadoDatos<T>(
    T Datos,
    string? Mensaje = null);
