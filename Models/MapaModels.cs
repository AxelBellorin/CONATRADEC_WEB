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

public sealed class CentroGeoespacialCapasRespuesta
{
    public DateTime ActualizadoUtc { get; set; }
    public List<MapaCapaDisponible> Capas { get; set; } = [];
}

public sealed class MapaCapaDisponible
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TipoVisualizacion { get; set; } = string.Empty;
    public bool Disponible { get; set; }
    public bool ActivaPorDefecto { get; set; }
    public int Orden { get; set; }
    public string? Mensaje { get; set; }
}

public sealed class ClimaMapaRespuesta
{
    public bool Disponible { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string Licencia { get; set; } = string.Empty;
    public DateTime ActualizadoUtc { get; set; }
    public string UnidadTemperatura { get; set; } = "°C";
    public string UnidadPrecipitacion { get; set; } = "mm";
    public string UnidadViento { get; set; } = "km/h";
    public decimal? TemperaturaMinima { get; set; }
    public decimal? TemperaturaMaxima { get; set; }
    public decimal? HumedadMinima { get; set; }
    public decimal? HumedadMaxima { get; set; }
    public decimal? PrecipitacionMaxima { get; set; }
    public decimal? VientoMaximo { get; set; }
    public List<ClimaPuntoMapa> Puntos { get; set; } = [];
}

public sealed class ClimaPuntoMapa
{
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? TemperaturaAparente { get; set; }
    public decimal? HumedadRelativa { get; set; }
    public decimal? Precipitacion { get; set; }
    public decimal? VelocidadViento { get; set; }
    public decimal? Nubosidad { get; set; }
    public int? CodigoClima { get; set; }
    public DateTimeOffset? FechaObservacion { get; set; }
}


public sealed class CapaSueloMapaRespuesta
{
    public bool Disponible { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string NivelAgrupacion { get; set; } = "DEPARTAMENTO";
    public DateTime ActualizadoUtc { get; set; }
    public decimal? Minimo { get; set; }
    public decimal? Maximo { get; set; }
    public int TotalRegiones { get; set; }
    public int TotalTerrenosAnalizados { get; set; }
    public List<RangoLeyendaMapa> Leyenda { get; set; } = [];
    public List<ResumenTerritorialSueloMapa> Regiones { get; set; } = [];

    // Compatibilidad temporal con respuestas anteriores.
    public int TotalPuntos { get; set; }
    public List<PuntoSueloMapa> Puntos { get; set; } = [];
}

public sealed class ResumenTerritorialSueloMapa
{
    public string TipoTerritorio { get; set; } = string.Empty;
    public string NombreTerritorio { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int? MunicipioId { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public decimal Promedio { get; set; }
    public decimal Minimo { get; set; }
    public decimal Maximo { get; set; }
    public int TerrenosAnalizados { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B655B";
    public DateTime FechaMasReciente { get; set; }
    public bool MuestraLimitada { get; set; }
}

public sealed class PuntoSueloMapa
{
    public int TerrenoId { get; set; }
    public int AnalisisSueloCalculoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Productor { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int MunicipioId { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public decimal Valor { get; set; }
    public string Clasificacion { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B655B";
    public DateTime FechaAnalisis { get; set; }
}

public sealed class RangoLeyendaMapa
{
    public string Etiqueta { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B655B";
    public decimal? Desde { get; set; }
    public decimal? Hasta { get; set; }
}

public sealed class HistorialTerrenoMapa
{
    public int TerrenoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Productor { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public decimal ExtensionManzanas { get; set; }
    public decimal ProduccionQuintalesOro { get; set; }
    public List<AnalisisTerrenoMapa> Analisis { get; set; } = [];
}

public sealed class AnalisisTerrenoMapa
{
    public int AnalisisSueloCalculoId { get; set; }
    public int AnalisisSueloId { get; set; }
    public string Identificador { get; set; } = string.Empty;
    public DateOnly FechaLaboratorio { get; set; }
    public DateTime FechaRegistro { get; set; }
    public decimal Ph { get; set; }
    public decimal? MateriaOrganica { get; set; }
    public decimal? AcidezTotal { get; set; }
    public decimal? Cice { get; set; }
    public decimal? SaturacionBases { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string RecomendacionGeneral { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public List<ElementoAnalisisTerrenoMapa> Elementos { get; set; } = [];
}

public sealed class ElementoAnalisisTerrenoMapa
{
    public int ElementoQuimicosId { get; set; }
    public string Simbolo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public string Clasificacion { get; set; } = string.Empty;
}
