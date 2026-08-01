namespace CONATRADEC.AdminWeb.Models;

public sealed class ResumenTerritorialMapaRespuesta
{
    public bool Disponible { get; set; }

    public string Mensaje { get; set; } =
        string.Empty;

    public string NivelAgrupacion { get; set; } =
        "DEPARTAMENTO";

    public DateTime ActualizadoUtc { get; set; }

    public int TotalRegiones { get; set; }

    public int TotalTerrenos { get; set; }

    public List<ResumenTerritorialRegionMapa> Regiones
    {
        get;
        set;
    } = [];
}

public sealed class ResumenTerritorialRegionMapa
{
    public string TipoTerritorio { get; set; } =
        string.Empty;

    public int DepartamentoId { get; set; }

    public string Departamento { get; set; } =
        string.Empty;

    public int? MunicipioId { get; set; }

    public string Municipio { get; set; } =
        string.Empty;

    public string NombreTerritorio { get; set; } =
        string.Empty;

    public int TotalTerrenos { get; set; }

    public int TotalPropietarios { get; set; }

    public decimal ExtensionTotalManzanas { get; set; }

    public int ConAnalisis { get; set; }

    public int SinAnalisis { get; set; }

    public decimal CoberturaAnalisisPorcentaje { get; set; }

    public int Criticos { get; set; }

    public int Atencion { get; set; }

    public int Normales { get; set; }

    public decimal CriticosPorcentaje { get; set; }

    public decimal AtencionPorcentaje { get; set; }

    public decimal NormalesPorcentaje { get; set; }

    public decimal SinAnalisisPorcentaje { get; set; }

    public string EstadoTerritorial { get; set; } =
        "SIN_ANALISIS";

    public string EstadoTexto { get; set; } =
        "Sin información";

    public string Color { get; set; } =
        "#64748B";

    public decimal? PhPromedio { get; set; }

    public decimal? MateriaOrganicaPromedio { get; set; }

    public decimal? AcidezTotalPromedio { get; set; }

    public decimal? CicePromedio { get; set; }

    public decimal? SaturacionBasesPromedio { get; set; }

    public DateTime? FechaAnalisisMasReciente { get; set; }

    public bool MuestraLimitada { get; set; }

    public List<ResumenTerritorialNutrienteMapa> Nutrientes
    {
        get;
        set;
    } = [];
}

public sealed class ResumenTerritorialNutrienteMapa
{
    public int ElementoQuimicoId { get; set; }

    public string Simbolo { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string Unidad { get; set; } =
        string.Empty;

    public decimal Promedio { get; set; }

    public int TerrenosConDato { get; set; }

    public int Bajos { get; set; }

    public int Medios { get; set; }

    public int Altos { get; set; }

    public decimal PorcentajeBajo { get; set; }
}
