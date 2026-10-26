using System.Globalization;

namespace CONATRADEC.AdminWeb.Services;

/// <summary>
/// Indicadores descriptivos construidos únicamente con las filas que el usuario
/// ha podido consultar. No infiere datos históricos ni combina fuentes ajenas.
/// </summary>
public static class ReportesAnaliticaService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-NI");

    public sealed record Registro(string[] Columnas, DateTime? Fecha);
    public sealed record Indicador(string Etiqueta, string Valor, string Aclaracion);
    public sealed record Categoria(string Etiqueta, int Cantidad, decimal Porcentaje);
    public sealed record Mes(string Etiqueta, int Cantidad);
    public sealed record Analisis(
        IReadOnlyList<Indicador> Indicadores,
        string TituloPrimario,
        IReadOnlyList<Categoria> Primario,
        string TituloSecundario,
        IReadOnlyList<Categoria> Secundario,
        IReadOnlyList<Mes> Evolucion,
        string Hallazgo,
        string Nota);

    public static Analisis Crear(string tipo, IReadOnlyList<Registro> filas)
    {
        ArgumentNullException.ThrowIfNull(filas);
        int total = filas.Count;
        var kpis = new List<Indicador>();
        string tituloPrincipal;
        string tituloSecundario;
        IReadOnlyList<Categoria> principal;
        IReadOnlyList<Categoria> secundario;
        string hallazgo;

        switch (tipo)
        {
            case "propietarios":
            {
                int conTerreno = filas.Count(f => Numero(f, 4) > 0);
                int sinTerreno = total - conTerreno;
                decimal relaciones = filas.Sum(f => Numero(f, 4));
                int activos = filas.Count(f => Valor(f, 6).Equals("Activo", StringComparison.OrdinalIgnoreCase));
                kpis.Add(new("Propietarios", Formato(total), "Registros que coinciden con los filtros"));
                kpis.Add(new("Con terrenos", Formato(conTerreno), "Con al menos un terreno asociado"));
                kpis.Add(new("Sin terrenos", Formato(sinTerreno), "Registrados con cero terrenos"));
                kpis.Add(new("Terrenos asociados", Formato(relaciones), "Suma de los terrenos de los propietarios consultados"));
                tituloPrincipal = "Relación con terrenos";
                principal = DesdeConteos([("Con terrenos", conTerreno), ("Sin terrenos", sinTerreno)], total);
                tituloSecundario = "Estado del propietario";
                secundario = DesdeConteos([("Activos", activos), ("Inactivos", total - activos)], total);
                hallazgo = total == 0 ? "Sin registros para analizar."
                    : $"{sinTerreno:N0} de {total:N0} propietarios figuran sin terrenos asociados ({Porcentaje(sinTerreno, total):N1} %).";
                break;
            }
            case "terrenos":
            {
                decimal superficie = filas.Sum(f => Numero(f, 4));
                decimal plantas = filas.Sum(f => Numero(f, 5));
                int conSuperficie = filas.Count(f => Numero(f, 4) > 0);
                kpis.Add(new("Terrenos", Formato(total), "Terrenos incluidos en la consulta"));
                kpis.Add(new("Superficie registrada", $"{superficie.ToString("N2", Cultura)} mz", "Suma de manzanas de los terrenos"));
                kpis.Add(new("Plantas registradas", Formato(plantas), "Suma de plantas informadas"));
                kpis.Add(new("Superficie promedio", $"{(total == 0 ? 0 : superficie / total).ToString("N2", Cultura)} mz", "Promedio por terreno consultado"));
                tituloPrincipal = "Distribución por superficie";
                principal = DesdeConteos([
                    ("Sin superficie positiva", total - conSuperficie),
                    ("Hasta 5 mz", filas.Count(f => Numero(f, 4) > 0 && Numero(f, 4) <= 5)),
                    ("Más de 5 a 20 mz", filas.Count(f => Numero(f, 4) > 5 && Numero(f, 4) <= 20)),
                    ("Más de 20 mz", filas.Count(f => Numero(f, 4) > 20))
                ], total);
                tituloSecundario = "Ubicaciones con más registros";
                secundario = DesdeAgrupacion(filas, 2);
                hallazgo = total == 0 ? "Sin registros para analizar."
                    : $"{conSuperficie:N0} de {total:N0} terrenos tienen una superficie positiva registrada. Extensión total: {superficie.ToString("N2", Cultura)} mz.";
                break;
            }
            case "asignaciones":
            {
                int principales = filas.Count(f => Valor(f, 4).Equals("Sí", StringComparison.OrdinalIgnoreCase));
                int usuarios = Distintos(filas, 0);
                int terrenos = Distintos(filas, 1);
                kpis.Add(new("Asignaciones", Formato(total), "Asignaciones registradas"));
                kpis.Add(new("Responsables principales", Formato(principales), "Asignaciones marcadas como principales"));
                kpis.Add(new("Usuarios distintos", Formato(usuarios), "Agrupados por nombre visible"));
                kpis.Add(new("Terrenos distintos", Formato(terrenos), "Agrupados por código"));
                tituloPrincipal = "Tipos de asignación";
                principal = DesdeAgrupacion(filas, 3);
                tituloSecundario = "Responsabilidad principal";
                secundario = DesdeConteos([("Principal", principales), ("Otra", total - principales)], total);
                hallazgo = total == 0 ? "Sin registros para analizar."
                    : $"{principales:N0} de {total:N0} asignaciones están marcadas como responsables principales ({Porcentaje(principales, total):N1} %).";
                break;
            }
            case "coberturas":
            {
                int departamentos = Distintos(filas, 2);
                int municipios = filas.Select(f => $"{Valor(f, 2)}|{Valor(f, 3)}")
                    .Where(s => s != "|").Distinct(StringComparer.OrdinalIgnoreCase).Count();
                int usuarios = Distintos(filas, 0);
                kpis.Add(new("Coberturas", Formato(total), "Registros de cobertura consultados"));
                kpis.Add(new("Departamentos", Formato(departamentos), "Nombres distintos informados"));
                kpis.Add(new("Municipios", Formato(municipios), "Pares departamento y municipio distintos"));
                kpis.Add(new("Usuarios", Formato(usuarios), "Agrupados por nombre visible"));
                tituloPrincipal = "Registros por departamento";
                principal = DesdeAgrupacion(filas, 2);
                tituloSecundario = "Tipos de cobertura";
                secundario = DesdeAgrupacion(filas, 1);
                hallazgo = total == 0 ? "Sin registros para analizar."
                    : $"La consulta comprende {departamentos:N0} departamentos y {municipios:N0} combinaciones de municipio y departamento.";
                break;
            }
            case "suelo":
            {
                var phDisponibles = filas.Select(f => Numero(f, 4))
                    .Where(v => v > 0 && v <= 14).ToArray();
                int distintosTerrenos = Distintos(filas, 1);
                int distintosLaboratorios = Distintos(filas, 3);
                kpis.Add(new("Análisis", Formato(total), "Análisis registrados en el período"));
                kpis.Add(new("Terrenos analizados", Formato(distintosTerrenos), "Códigos de terreno diferentes"));
                kpis.Add(new("Laboratorios", Formato(distintosLaboratorios), "Laboratorios informados"));
                kpis.Add(new("pH promedio", phDisponibles.Length == 0 ? "—" :
                    phDisponibles.Average().ToString("N2", Cultura), "Calculado solo para valores de pH entre 0 y 14"));
                tituloPrincipal = "Estado de los análisis";
                principal = DesdeAgrupacion(filas, 5);
                tituloSecundario = "Distribución por laboratorio";
                secundario = DesdeAgrupacion(filas, 3);
                hallazgo = total == 0 ? "Sin registros para analizar."
                    : $"Se registraron {total:N0} análisis correspondientes a {distintosTerrenos:N0} terrenos. El pH promedio utiliza {phDisponibles.Length:N0} valores válidos.";
                break;
            }
            case "fertilizacion":
            {
                int formulas = filas.Count(f => Valor(f, 3) == "Sí");
                int enmiendas = filas.Count(f => Valor(f, 4) == "Sí");
                int mixtas = filas.Count(f => Valor(f, 5) == "Sí");
                int combinados = filas.Count(f =>
                    (Valor(f, 3) == "Sí" ? 1 : 0) +
                    (Valor(f, 4) == "Sí" ? 1 : 0) +
                    (Valor(f, 5) == "Sí" ? 1 : 0) > 1);
                kpis.Add(new("Análisis con propuestas", Formato(total), "Con fórmula, enmienda y/o fertilización mixta"));
                kpis.Add(new("Con fórmula", Formato(formulas), "Análisis con fórmula calculada"));
                kpis.Add(new("Con enmienda", Formato(enmiendas), "Análisis con enmienda calculada"));
                kpis.Add(new("Con fertilización mixta", Formato(mixtas), "Análisis con propuesta mixta"));
                tituloPrincipal = "Tipos de propuestas por análisis";
                principal = DesdeConteos([
                    ("Solo fórmula", filas.Count(f => Valor(f, 3) == "Sí" && Valor(f, 4) != "Sí" && Valor(f, 5) != "Sí")),
                    ("Solo enmienda", filas.Count(f => Valor(f, 3) != "Sí" && Valor(f, 4) == "Sí" && Valor(f, 5) != "Sí")),
                    ("Solo mixta", filas.Count(f => Valor(f, 3) != "Sí" && Valor(f, 4) != "Sí" && Valor(f, 5) == "Sí")),
                    ("Combinadas", combinados)
                ], total);
                tituloSecundario = "Terrenos con propuestas";
                secundario = DesdeAgrupacion(filas, 1);
                hallazgo = total == 0 ? "Sin propuestas para analizar."
                    : $"De {total:N0} análisis con propuesta registrada, {combinados:N0} contienen más de un tipo de recomendación.";
                break;
            }
            case "fitosanitario":
            {
                int cerradas = filas.Count(f => Valor(f, 7) == "Cerrada");
                decimal fotos = filas.Sum(f => Numero(f, 8));
                int tecnicos = Distintos(filas, 5);
                kpis.Add(new("Inspecciones", Formato(total), "Expedientes fitosanitarios consultados"));
                kpis.Add(new("Cerradas", Formato(cerradas), "Expedientes cerrados definitivamente"));
                kpis.Add(new("Fotografías", Formato(fotos), "Fotografías registradas en los expedientes"));
                kpis.Add(new("Técnicos", Formato(tecnicos), "Agrupados por nombre visible"));
                tituloPrincipal = "Estados de inspección";
                principal = DesdeAgrupacion(filas, 6);
                tituloSecundario = "Por departamento";
                secundario = DesdeAgrupacion(filas, 4);
                hallazgo = total == 0 ? "Sin inspecciones para analizar."
                    : $"{cerradas:N0} de {total:N0} inspecciones están cerradas definitivamente ({Porcentaje(cerradas, total):N1} %).";
                break;
            }
            case "actividad":
            {
                int exitosos = filas.Count(f => Valor(f, 6) == "Exitoso");
                int fallidos = total - exitosos;
                int usuarios = Distintos(filas, 1);
                kpis.Add(new("Eventos", Formato(total), "Operaciones registradas en la bitácora"));
                kpis.Add(new("Exitosos", Formato(exitosos), "Eventos con resultado exitoso"));
                kpis.Add(new("Fallidos", Formato(fallidos), "Eventos con resultado fallido"));
                kpis.Add(new("Usuarios", Formato(usuarios), "Nombres diferentes en bitácora"));
                tituloPrincipal = "Actividad por módulo";
                principal = DesdeAgrupacion(filas, 3);
                tituloSecundario = "Acciones más frecuentes";
                secundario = DesdeAgrupacion(filas, 4);
                hallazgo = total == 0 ? "Sin eventos en el período consultado."
                    : $"Se registraron {total:N0} eventos; {fallidos:N0} figuran como fallidos ({Porcentaje(fallidos, total):N1} %).";
                break;
            }
            default:
                throw new ArgumentException("Tipo de reporte no admitido.", nameof(tipo));
        }

        var fechas = filas.Where(f => f.Fecha.HasValue).Select(f => f.Fecha!.Value.Date).ToArray();
        string nota = fechas.Length == 0
            ? "No se dispone de fechas para elaborar una serie mensual."
            : "La serie mensual representa registros fechados, no cambios históricos del inventario.";
        if (tipo is "asignaciones" or "coberturas")
            nota += " Las cantidades de personas y zonas distintas se agrupan por los textos disponibles, no por identificadores únicos.";
        if (tipo == "terrenos")
            nota += " Los valores de superficie se expresan en manzanas (mz).";
        if (tipo == "fertilizacion")
            nota += " Se identifica la existencia de propuestas; no se calculan dosis ni se reemplaza la recomendación técnica individual.";
        if (tipo == "suelo")
            nota += " El promedio de pH es descriptivo, no determina por sí mismo necesidades de fertilización.";
        if (tipo == "fitosanitario")
            nota += " La distribución corresponde a expedientes, no a casos confirmados de enfermedades.";
        if (tipo == "actividad")
            nota += " Se omiten de la exportación tokens, parámetros e información privada de solicitudes.";
        return new(kpis, tituloPrincipal, principal, tituloSecundario, secundario,
            CrearMeses(fechas), hallazgo, nota);
    }

    private static decimal Numero(Registro fila, int indice)
    {
        string texto = Valor(fila, indice);
        return decimal.TryParse(texto, NumberStyles.Number, Cultura, out decimal resultado)
            ? resultado
            : decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out resultado)
                ? resultado : 0m;
    }

    private static string Valor(Registro fila, int indice) =>
        indice >= 0 && indice < fila.Columnas.Length ? fila.Columnas[indice]?.Trim() ?? "" : "";

    private static int Distintos(IReadOnlyList<Registro> filas, int indice) => filas
        .Select(f => Valor(f, indice)).Where(s => !string.IsNullOrWhiteSpace(s))
        .Distinct(StringComparer.OrdinalIgnoreCase).Count();

    private static string Formato(decimal numero) => numero.ToString("N0", Cultura);
    private static decimal Porcentaje(int cantidad, int total) => total == 0 ? 0 : 100m * cantidad / total;

    private static IReadOnlyList<Categoria> DesdeConteos(IEnumerable<(string Etiqueta, int Cantidad)> grupos, int total) =>
        grupos.Where(x => x.Cantidad > 0).Select(x => new Categoria(x.Etiqueta, x.Cantidad, Porcentaje(x.Cantidad, total))).ToArray();

    private static IReadOnlyList<Categoria> DesdeAgrupacion(IReadOnlyList<Registro> filas, int indice)
    {
        int total = filas.Count;
        var agrupados = filas.GroupBy(f => !string.IsNullOrWhiteSpace(Valor(f, indice)) ? Valor(f, indice) : "Sin especificar", StringComparer.OrdinalIgnoreCase)
            .Select(grupo => (Etiqueta: grupo.Key, Cantidad: grupo.Count()))
            .OrderByDescending(x => x.Cantidad).ThenBy(x => x.Etiqueta, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        if (agrupados.Count > 6)
        {
            int otros = agrupados.Skip(5).Sum(x => x.Cantidad);
            agrupados = agrupados.Take(5).Append(("Otros", otros)).ToList();
        }
        return DesdeConteos(agrupados, total);
    }

    private static IReadOnlyList<Mes> CrearMeses(DateTime[] fechas)
    {
        if (fechas.Length == 0)
            return [];
        DateTime fechaMaxima = fechas.Max();
        var mesFinal = new DateTime(fechaMaxima.Year, fechaMaxima.Month, 1);
        return Enumerable.Range(0, 6).Select(offset => mesFinal.AddMonths(offset - 5))
            .Select(inicio => new Mes(inicio.ToString("MMM yyyy", Cultura),
                fechas.Count(fecha => fecha.Year == inicio.Year && fecha.Month == inicio.Month)))
            .ToArray();
    }
}
