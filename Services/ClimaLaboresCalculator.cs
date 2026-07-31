using CONATRADEC.AdminWeb.Models;
using System.Globalization;

namespace CONATRADEC.AdminWeb.Services;

public static class ClimaLaboresCalculator
{
    public static List<VentanaLaborAgricola>
        CalcularVentanas(
            PronosticoClimaTerreno? pronostico,
            ReglaAgricolaClima? regla)
    {
        if (pronostico is null ||
            regla is null ||
            !pronostico.Disponible)
        {
            return [];
        }

        var resultado = new List<VentanaLaborAgricola>();

        foreach (PronosticoClimaDia dia
                 in pronostico.Dias
                     .OrderBy(item => item.Fecha))
        {
            foreach (PronosticoClimaPeriodo periodo
                     in dia.Periodos)
            {
                resultado.Add(
                    Evaluar(
                        dia,
                        periodo,
                        regla));
            }
        }

        return resultado;
    }

    public static List<AlertaClimaLabor>
        ConstruirAlertas(
            PronosticoClimaTerreno? pronostico,
            ReglaAgricolaClima? regla,
            IReadOnlyCollection<VentanaLaborAgricola> ventanas)
    {
        var alertas = new List<AlertaClimaLabor>();

        if (pronostico is null)
            return alertas;

        foreach (PronosticoClimaAlerta alerta
                 in pronostico.Alertas)
        {
            alertas.Add(new AlertaClimaLabor
            {
                Nivel = alerta.Nivel,
                Titulo = alerta.Titulo,
                Mensaje = alerta.Mensaje,
                Fecha = alerta.FechaInicio
            });
        }

        foreach (PronosticoClimaDia dia
                 in pronostico.Dias.Take(3))
        {
            foreach (PronosticoClimaAlerta alerta
                     in dia.Alertas)
            {
                alertas.Add(new AlertaClimaLabor
                {
                    Nivel = alerta.Nivel,
                    Titulo = alerta.Titulo,
                    Mensaje = alerta.Mensaje,
                    Fecha = dia.Fecha
                });
            }
        }

        bool hayVentanaProxima =
            ventanas
                .Where(item =>
                    item.Fecha <=
                    DateOnly.FromDateTime(
                        DateTime.Today.AddDays(2)))
                .Any(item => item.Recomendada);

        if (!hayVentanaProxima &&
            regla is not null &&
            ventanas.Count > 0)
        {
            alertas.Insert(
                0,
                new AlertaClimaLabor
                {
                    Nivel = "ATENCION",
                    Titulo =
                        $"Sin ventana favorable para {regla.Nombre.ToLowerInvariant()}",
                    Mensaje =
                        "Durante las próximas 48 horas no se encontró " +
                        "un periodo que cumpla todos los umbrales configurados."
                });
        }

        return alertas
            .GroupBy(item =>
                $"{item.Nivel}|{item.Titulo}|{item.Fecha}")
            .Select(item => item.First())
            .Take(12)
            .ToList();
    }

    public static List<PasoTimelineClima>
        ConstruirTimeline(
            IReadOnlyCollection<PortalPropietarioTerreno> terrenos,
            IReadOnlyDictionary<int, PronosticoClimaTerreno> pronosticos)
    {
        var pasos = new Dictionary<
            string,
            PasoTimelineClima>(
                StringComparer.OrdinalIgnoreCase);

        foreach (PortalPropietarioTerreno terreno
                 in terrenos.Where(item =>
                     item.Latitud != 0 &&
                     item.Longitud != 0))
        {
            if (!pronosticos.TryGetValue(
                    terreno.TerrenoId,
                    out PronosticoClimaTerreno? pronostico) ||
                !pronostico.Disponible)
            {
                continue;
            }

            foreach (PronosticoClimaDia dia
                     in pronostico.Dias)
            {
                foreach (PronosticoClimaPeriodo periodo
                         in dia.Periodos)
                {
                    string clave =
                        $"{dia.Fecha:yyyy-MM-dd}|" +
                        $"{periodo.Clave}";

                    if (!pasos.TryGetValue(
                            clave,
                            out PasoTimelineClima? paso))
                    {
                        paso = new PasoTimelineClima
                        {
                            Fecha = dia.Fecha,
                            PeriodoClave = periodo.Clave,
                            Etiqueta =
                                $"{NombreDia(dia.Fecha)} " +
                                $"{dia.Fecha:dd/MM} · " +
                                periodo.Nombre,
                            RangoHorario =
                                periodo.RangoHorario
                        };

                        pasos.Add(clave, paso);
                    }

                    paso.Puntos.Add(new PuntoTimelineClima
                    {
                        TerrenoId = terreno.TerrenoId,
                        Codigo = terreno.CodigoTerreno,
                        Ubicacion = terreno.Ubicacion,
                        Latitud = terreno.Latitud,
                        Longitud = terreno.Longitud,
                        Temperatura =
                            periodo.TemperaturaPromedio,
                        Humedad = periodo.HumedadPromedio,
                        ProbabilidadLluvia =
                            periodo
                                .ProbabilidadPrecipitacionMaxima,
                        Precipitacion =
                            periodo.PrecipitacionTotal,
                        Viento =
                            periodo.VelocidadVientoMaxima,
                        Rafaga = periodo.RafagaMaxima,
                        CodigoClima =
                            periodo.CodigoClima,
                        Condicion =
                            periodo.Condicion,
                        RiesgoTormenta =
                            periodo.RiesgoTormenta
                    });
                }
            }
        }

        List<PasoTimelineClima> resultado =
            pasos.Values
                .OrderBy(item => item.Fecha)
                .ThenBy(item =>
                    OrdenPeriodo(item.PeriodoClave))
                .ToList();

        for (int indice = 0;
             indice < resultado.Count;
             indice++)
        {
            resultado[indice].Indice = indice;
        }

        return resultado;
    }

    private static VentanaLaborAgricola Evaluar(
        PronosticoClimaDia dia,
        PronosticoClimaPeriodo periodo,
        ReglaAgricolaClima regla)
    {
        var condiciones =
            new List<EvaluacionCondicionLabor>();

        AgregarMaximo(
            condiciones,
            "Probabilidad de lluvia",
            periodo.ProbabilidadPrecipitacionMaxima,
            regla.ProbabilidadLluviaMaxima,
            "%");

        AgregarMaximo(
            condiciones,
            "Precipitación",
            periodo.PrecipitacionTotal,
            regla.PrecipitacionMaximaMm,
            "mm");

        AgregarMaximo(
            condiciones,
            "Velocidad del viento",
            periodo.VelocidadVientoMaxima,
            regla.VientoMaximoKmh,
            "km/h");

        AgregarMaximo(
            condiciones,
            "Ráfaga de viento",
            periodo.RafagaMaxima,
            regla.RafagaMaximaKmh,
            "km/h");

        AgregarRango(
            condiciones,
            "Temperatura",
            periodo.TemperaturaPromedio,
            regla.TemperaturaMinimaC,
            regla.TemperaturaMaximaC,
            "°C");

        AgregarRango(
            condiciones,
            "Humedad relativa",
            periodo.HumedadPromedio,
            regla.HumedadMinimaPct,
            regla.HumedadMaximaPct,
            "%");

        AgregarMaximo(
            condiciones,
            "Índice UV diario",
            dia.IndiceUvMaximo,
            regla.IndiceUvMaximo,
            string.Empty);

        AgregarDuracion(
            condiciones,
            periodo.RangoHorario,
            regla.DuracionMinimaHoras);

        if (regla.BloquearTormentaMedia)
        {
            string riesgo =
                periodo.RiesgoTormenta
                    .Trim()
                    .ToUpperInvariant();

            condiciones.Add(
                new EvaluacionCondicionLabor
                {
                    Indicador = "Riesgo de tormenta",
                    Valor = riesgo,
                    Limite = "Solo BAJO",
                    Cumple = riesgo is "" or "BAJO",
                    TieneDato =
                        !string.IsNullOrWhiteSpace(
                            periodo.RiesgoTormenta)
                });
        }

        List<EvaluacionCondicionLabor> evaluables =
            condiciones
                .Where(item => item.TieneDato)
                .ToList();

        int cumplidas =
            evaluables.Count(item => item.Cumple);

        int puntuacion =
            evaluables.Count == 0
                ? 0
                : Convert.ToInt32(
                    Math.Round(
                        cumplidas * 100m /
                        evaluables.Count,
                        MidpointRounding.AwayFromZero));

        string estado =
            evaluables.Count > 0 &&
            evaluables.All(item => item.Cumple)
                ? "FAVORABLE"
                : puntuacion >= 65
                    ? "PRECAUCION"
                    : "NO_FAVORABLE";

        return new VentanaLaborAgricola
        {
            Fecha = dia.Fecha,
            PeriodoClave = periodo.Clave,
            PeriodoNombre = periodo.Nombre,
            RangoHorario = periodo.RangoHorario,
            CondicionClimatica = periodo.Condicion,
            Estado = estado,
            Puntuacion = puntuacion,
            Temperatura =
                periodo.TemperaturaPromedio,
            Humedad = periodo.HumedadPromedio,
            ProbabilidadLluvia =
                periodo.ProbabilidadPrecipitacionMaxima,
            Precipitacion =
                periodo.PrecipitacionTotal,
            Viento =
                periodo.VelocidadVientoMaxima,
            Rafaga = periodo.RafagaMaxima,
            IndiceUv = dia.IndiceUvMaximo,
            RiesgoTormenta =
                periodo.RiesgoTormenta,
            Condiciones = condiciones,
            Mensaje =
                estado == "FAVORABLE"
                    ? regla.MensajeFavorable
                    : regla.MensajeNoFavorable
        };
    }

    private static void AgregarDuracion(
        ICollection<EvaluacionCondicionLabor> condiciones,
        string rangoHorario,
        int duracionMinimaHoras)
    {
        if (duracionMinimaHoras <= 0)
            return;

        decimal? duracion =
            CalcularDuracionHoras(rangoHorario);

        condiciones.Add(
            new EvaluacionCondicionLabor
            {
                Indicador = "Duración disponible",
                Valor = duracion.HasValue
                    ? $"{duracion.Value:0.#} h"
                    : "Sin dato",
                Limite = $"≥ {duracionMinimaHoras} h",
                Cumple =
                    duracion.HasValue &&
                    duracion.Value >=
                    duracionMinimaHoras,
                TieneDato = duracion.HasValue
            });
    }

    private static decimal? CalcularDuracionHoras(
        string rangoHorario)
    {
        if (string.IsNullOrWhiteSpace(rangoHorario))
            return null;

        string normalizado =
            rangoHorario
                .Replace('–', '-')
                .Replace('—', '-');

        string[] partes =
            normalizado.Split(
                '-',
                StringSplitOptions.TrimEntries |
                StringSplitOptions.RemoveEmptyEntries);

        if (partes.Length != 2 ||
            !TimeOnly.TryParse(
                partes[0],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out TimeOnly inicio) ||
            !TimeOnly.TryParse(
                partes[1],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out TimeOnly fin))
        {
            return null;
        }

        TimeSpan diferencia =
            fin.ToTimeSpan() -
            inicio.ToTimeSpan();

        if (diferencia <= TimeSpan.Zero)
            diferencia += TimeSpan.FromDays(1);

        return Convert.ToDecimal(
            diferencia.TotalHours);
    }

    private static void AgregarMaximo(
        ICollection<EvaluacionCondicionLabor> condiciones,
        string indicador,
        int? valor,
        int? maximo,
        string unidad)
    {
        AgregarMaximo(
            condiciones,
            indicador,
            valor.HasValue
                ? Convert.ToDecimal(valor.Value)
                : null,
            maximo.HasValue
                ? Convert.ToDecimal(maximo.Value)
                : null,
            unidad);
    }

    private static void AgregarMaximo(
        ICollection<EvaluacionCondicionLabor> condiciones,
        string indicador,
        decimal? valor,
        decimal? maximo,
        string unidad)
    {
        if (!maximo.HasValue)
            return;

        condiciones.Add(
            new EvaluacionCondicionLabor
            {
                Indicador = indicador,
                Valor = Formato(valor, unidad),
                Limite =
                    $"≤ {Formato(maximo, unidad)}",
                Cumple =
                    valor.HasValue &&
                    valor.Value <= maximo.Value,
                TieneDato = valor.HasValue
            });
    }

    private static void AgregarRango(
        ICollection<EvaluacionCondicionLabor> condiciones,
        string indicador,
        decimal? valor,
        decimal? minimo,
        decimal? maximo,
        string unidad)
    {
        if (!minimo.HasValue &&
            !maximo.HasValue)
        {
            return;
        }

        bool cumple =
            valor.HasValue &&
            (!minimo.HasValue ||
             valor.Value >= minimo.Value) &&
            (!maximo.HasValue ||
             valor.Value <= maximo.Value);

        string limite =
            minimo.HasValue &&
            maximo.HasValue
                ? $"{Formato(minimo, unidad)} – " +
                  $"{Formato(maximo, unidad)}"
                : minimo.HasValue
                    ? $"≥ {Formato(minimo, unidad)}"
                    : $"≤ {Formato(maximo, unidad)}";

        condiciones.Add(
            new EvaluacionCondicionLabor
            {
                Indicador = indicador,
                Valor = Formato(valor, unidad),
                Limite = limite,
                Cumple = cumple,
                TieneDato = valor.HasValue
            });
    }

    private static string Formato(
        decimal? valor,
        string unidad) =>
        valor.HasValue
            ? valor.Value.ToString(
                "0.#",
                CultureInfo.InvariantCulture) +
              (string.IsNullOrWhiteSpace(unidad)
                  ? string.Empty
                  : $" {unidad}")
            : "Sin dato";

    private static string NombreDia(
        DateOnly fecha)
    {
        string nombre =
            CultureInfo
                .GetCultureInfo("es-NI")
                .DateTimeFormat
                .GetAbbreviatedDayName(
                    fecha.DayOfWeek);

        return char.ToUpperInvariant(nombre[0]) +
               nombre[1..];
    }

    private static int OrdenPeriodo(
        string clave) =>
        clave.Trim().ToUpperInvariant() switch
        {
            "MANANA" => 1,
            "MAÑANA" => 1,
            "TARDE" => 2,
            "NOCHE" => 3,
            _ => 9
        };
}
