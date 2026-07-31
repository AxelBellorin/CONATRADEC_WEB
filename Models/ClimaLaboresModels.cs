namespace CONATRADEC.AdminWeb.Models;

public sealed class ReglaAgricolaClima
{
    public int ReglaAgricolaClimaId { get; set; }

    public string Clave { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Icono { get; set; } =
        "fa-solid fa-seedling";

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;

    public int? ProbabilidadLluviaMaxima { get; set; }

    public decimal? PrecipitacionMaximaMm { get; set; }

    public decimal? VientoMaximoKmh { get; set; }

    public decimal? RafagaMaximaKmh { get; set; }

    public decimal? TemperaturaMinimaC { get; set; }

    public decimal? TemperaturaMaximaC { get; set; }

    public decimal? HumedadMinimaPct { get; set; }

    public decimal? HumedadMaximaPct { get; set; }

    public decimal? IndiceUvMaximo { get; set; }

    public bool BloquearTormentaMedia { get; set; } = true;

    public int DuracionMinimaHoras { get; set; } = 3;

    public string MensajeFavorable { get; set; } =
        "Condiciones adecuadas para realizar la labor.";

    public string MensajeNoFavorable { get; set; } =
        "Conviene reprogramar la labor.";

    public DateTime FechaRegistroUtc { get; set; }

    public DateTime? FechaActualizacionUtc { get; set; }

    public ReglaAgricolaClimaGuardar CrearRequest() =>
        new()
        {
            Clave = Clave,
            Nombre = Nombre,
            Descripcion = Descripcion,
            Icono = Icono,
            Orden = Orden,
            Activo = Activo,
            ProbabilidadLluviaMaxima =
                ProbabilidadLluviaMaxima,
            PrecipitacionMaximaMm =
                PrecipitacionMaximaMm,
            VientoMaximoKmh = VientoMaximoKmh,
            RafagaMaximaKmh = RafagaMaximaKmh,
            TemperaturaMinimaC = TemperaturaMinimaC,
            TemperaturaMaximaC = TemperaturaMaximaC,
            HumedadMinimaPct = HumedadMinimaPct,
            HumedadMaximaPct = HumedadMaximaPct,
            IndiceUvMaximo = IndiceUvMaximo,
            BloquearTormentaMedia =
                BloquearTormentaMedia,
            DuracionMinimaHoras =
                DuracionMinimaHoras,
            MensajeFavorable = MensajeFavorable,
            MensajeNoFavorable = MensajeNoFavorable
        };
}

public sealed class ReglaAgricolaClimaGuardar
{
    public string Clave { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Icono { get; set; } =
        "fa-solid fa-seedling";

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;

    public int? ProbabilidadLluviaMaxima { get; set; }

    public decimal? PrecipitacionMaximaMm { get; set; }

    public decimal? VientoMaximoKmh { get; set; }

    public decimal? RafagaMaximaKmh { get; set; }

    public decimal? TemperaturaMinimaC { get; set; }

    public decimal? TemperaturaMaximaC { get; set; }

    public decimal? HumedadMinimaPct { get; set; }

    public decimal? HumedadMaximaPct { get; set; }

    public decimal? IndiceUvMaximo { get; set; }

    public bool BloquearTormentaMedia { get; set; } = true;

    public int DuracionMinimaHoras { get; set; } = 3;

    public string MensajeFavorable { get; set; } =
        "Condiciones adecuadas para realizar la labor.";

    public string MensajeNoFavorable { get; set; } =
        "Conviene reprogramar la labor.";
}

public sealed class VentanaLaborAgricola
{
    public DateOnly Fecha { get; set; }

    public string PeriodoClave { get; set; } = string.Empty;

    public string PeriodoNombre { get; set; } = string.Empty;

    public string RangoHorario { get; set; } = string.Empty;

    public string CondicionClimatica { get; set; } =
        string.Empty;

    public string Estado { get; set; } = "NO_FAVORABLE";

    public int Puntuacion { get; set; }

    public bool Recomendada =>
        string.Equals(
            Estado,
            "FAVORABLE",
            StringComparison.OrdinalIgnoreCase);

    public decimal? Temperatura { get; set; }

    public decimal? Humedad { get; set; }

    public int? ProbabilidadLluvia { get; set; }

    public decimal? Precipitacion { get; set; }

    public decimal? Viento { get; set; }

    public decimal? Rafaga { get; set; }

    public decimal? IndiceUv { get; set; }

    public string RiesgoTormenta { get; set; } = "BAJO";

    public List<EvaluacionCondicionLabor> Condiciones
    {
        get;
        set;
    } = [];

    public string Mensaje { get; set; } = string.Empty;

    public string ClaseCss =>
        Estado switch
        {
            "FAVORABLE" => "favorable",
            "PRECAUCION" => "precaucion",
            _ => "no-favorable"
        };

    public string EstadoTexto =>
        Estado switch
        {
            "FAVORABLE" => "Favorable",
            "PRECAUCION" => "Con precaución",
            _ => "No favorable"
        };
}

public sealed class EvaluacionCondicionLabor
{
    public string Indicador { get; set; } = string.Empty;

    public string Valor { get; set; } = string.Empty;

    public string Limite { get; set; } = string.Empty;

    public bool Cumple { get; set; }

    public bool TieneDato { get; set; } = true;
}

public sealed class AlertaClimaLabor
{
    public string Nivel { get; set; } = "ATENCION";

    public string Titulo { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public DateOnly? Fecha { get; set; }

    public string ClaseCss =>
        Nivel.Equals(
            "CRITICA",
            StringComparison.OrdinalIgnoreCase)
            ? "critical"
            : "attention";
}

public sealed class PasoTimelineClima
{
    public int Indice { get; set; }

    public DateOnly Fecha { get; set; }

    public string PeriodoClave { get; set; } = string.Empty;

    public string Etiqueta { get; set; } = string.Empty;

    public string RangoHorario { get; set; } = string.Empty;

    public List<PuntoTimelineClima> Puntos { get; set; } = [];
}

public sealed class PuntoTimelineClima
{
    public int TerrenoId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Ubicacion { get; set; } = string.Empty;

    public decimal Latitud { get; set; }

    public decimal Longitud { get; set; }

    public decimal? Temperatura { get; set; }

    public decimal? Humedad { get; set; }

    public int? ProbabilidadLluvia { get; set; }

    public decimal? Precipitacion { get; set; }

    public decimal? Viento { get; set; }

    public decimal? Rafaga { get; set; }

    public int? CodigoClima { get; set; }

    public string Condicion { get; set; } = string.Empty;

    public string RiesgoTormenta { get; set; } = "BAJO";
}
