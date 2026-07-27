using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class TerrenoMapaItem
{
    public int TerrenoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Productor { get; set; } = string.Empty;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int MunicipioId { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public decimal ExtensionManzanas { get; set; }
    public decimal ProduccionQuintalesOro { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string NivelAlerta { get; set; } = string.Empty;
    public decimal? UltimoPh { get; set; }
    public decimal? MateriaOrganica { get; set; }
    public decimal? AcidezTotal { get; set; }
    public DateTime? FechaUltimoAnalisis { get; set; }
    public List<string> Alertas { get; set; } = [];
    public string GoogleMapsUrl { get; set; } = string.Empty;
}

public sealed class MapaResumen
{
    public int TotalTerrenos { get; set; }
    public int ConAnalisis { get; set; }
    public int SinAnalisis { get; set; }
    public int Criticos { get; set; }
    public int Atencion { get; set; }
    public int Normales { get; set; }
    public decimal ExtensionVisibleManzanas { get; set; }
}

public sealed class MapaInteligenteRespuesta
{
    public MapaResumen Resumen { get; set; } = new();
    public List<TerrenoMapaItem> Terrenos { get; set; } = [];
}

public sealed class MapaFiltro
{
    public string Texto { get; set; } = string.Empty;
    public int? DepartamentoId { get; set; }
    public int? MunicipioId { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Indicador { get; set; } = string.Empty;
}

public sealed class AlertaAgricolaItem
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int MunicipioId { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public DateTime? FechaAnalisis { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string GoogleMapsUrl { get; set; } = string.Empty;
}

public sealed class AlertasAgricolasPaginada
{
    public List<AlertaAgricolaItem> Items { get; set; } = [];
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public int Criticas { get; set; }
    public int Atencion { get; set; }
    public int SinAnalisis { get; set; }
}
