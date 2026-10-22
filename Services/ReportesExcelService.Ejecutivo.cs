using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace CONATRADEC.AdminWeb.Services;

/// <summary>
/// Ampliación de los XLSX existentes: resumen, distribuciones y detalle.
/// Las celdas de texto no se interpretan como fórmulas; solo columnas numéricas
/// previamente designadas se guardan como números de Excel.
/// </summary>
public static partial class ReportesExcelService
{
    public static byte[] GenerarEjecutivo(
        string titulo,
        string filtros,
        IReadOnlyList<(string Etiqueta, string Valor)> indicadores,
        string tituloPrimario,
        IReadOnlyList<(string Etiqueta, int Cantidad)> primario,
        string tituloSecundario,
        IReadOnlyList<(string Etiqueta, int Cantidad)> secundario,
        IReadOnlyList<(string Etiqueta, int Cantidad)> evolucion,
        IReadOnlyList<string> encabezados,
        IReadOnlyList<string[]> filas,
        IReadOnlySet<int> columnasNumericas)
    {
        ArgumentNullException.ThrowIfNull(indicadores);
        ArgumentNullException.ThrowIfNull(primario);
        ArgumentNullException.ThrowIfNull(secundario);
        ArgumentNullException.ThrowIfNull(evolucion);
        ArgumentNullException.ThrowIfNull(encabezados);
        ArgumentNullException.ThrowIfNull(filas);
        ArgumentNullException.ThrowIfNull(columnasNumericas);
        if (encabezados.Count == 0 || encabezados.Count > 100)
            throw new ArgumentException("Cantidad de columnas inválida.", nameof(encabezados));
        if (filas.Any(x => x.Length != encabezados.Count))
            throw new ArgumentException("Las filas no coinciden con los encabezados.", nameof(filas));
        if (columnasNumericas.Any(x => x < 0 || x >= encabezados.Count))
            throw new ArgumentException("Columna numérica fuera del rango.", nameof(columnasNumericas));

        using var salida = new MemoryStream();
        using (var zip = new ZipArchive(salida, ZipArchiveMode.Create, leaveOpen: true))
        {
            GuardarXml(zip, "[Content_Types].xml", xml =>
            {
                xml.WriteStartElement("Types", TiposContenido);
                ContenidoPredeterminado(xml, "rels", "application/vnd.openxmlformats-package.relationships+xml");
                ContenidoPredeterminado(xml, "xml", "application/xml");
                TipoHoja(xml, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                for (int i = 1; i <= 3; i++)
                    TipoHoja(xml, $"/xl/worksheets/sheet{i}.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
                TipoHoja(xml, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
                xml.WriteEndElement();
            });
            GuardarXml(zip, "_rels/.rels", xml =>
            {
                xml.WriteStartElement("Relationships", RelacionesPaquete);
                Relacion(xml, "rId1", RelacionesDoc + "/officeDocument", "xl/workbook.xml");
                xml.WriteEndElement();
            });
            GuardarXml(zip, "xl/workbook.xml", xml =>
            {
                xml.WriteStartElement("workbook", HojaNs);
                xml.WriteAttributeString("xmlns", "r", null, RelacionesDoc);
                xml.WriteStartElement("sheets", HojaNs);
                string[] nombres = ["Resumen", "Distribuciones", "Detalle"];
                for (int i = 0; i < nombres.Length; i++)
                {
                    xml.WriteStartElement("sheet", HojaNs);
                    xml.WriteAttributeString("name", nombres[i]);
                    xml.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
                    xml.WriteAttributeString("r", "id", RelacionesDoc, $"rId{i + 1}");
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
                xml.WriteEndElement();
            });
            GuardarXml(zip, "xl/_rels/workbook.xml.rels", xml =>
            {
                xml.WriteStartElement("Relationships", RelacionesPaquete);
                for (int i = 1; i <= 3; i++)
                    Relacion(xml, $"rId{i}", RelacionesDoc + "/worksheet", $"worksheets/sheet{i}.xml");
                Relacion(xml, "rId4", RelacionesDoc + "/styles", "styles.xml");
                xml.WriteEndElement();
            });
            GuardarXml(zip, "xl/styles.xml", EscribirEstilosEjecutivos);
            GuardarXml(zip, "xl/worksheets/sheet1.xml", xml =>
            {
                AbrirHoja(xml, 2, congelar: false);
                xml.WriteStartElement("sheetData", HojaNs);
                EscribirFilaInforme(xml, 1, [(titulo, 2)], 2);
                EscribirFilaInforme(xml, 2, [(filtros, 0)], 2);
                EscribirFilaInforme(xml, 3, [($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", 0)], 2);
                EscribirFilaInforme(xml, 5, [("INDICADORES", 3), ("VALOR", 3)], 2);
                int filaActual = 6;
                foreach (var indicador in indicadores)
                    EscribirFilaInforme(xml, filaActual++, [(indicador.Etiqueta, 0), (indicador.Valor, 2)], 2);
                filaActual++;
                EscribirFilaInforme(xml, filaActual++, [("DESCRIPCIÓN", 3), ("VALOR", 3)], 2);
                EscribirFilaInforme(xml, filaActual++, [("Tipo de informe", 0), (titulo, 0)], 2);
                EscribirFilaInforme(xml, filaActual++, [("Registros exportados", 0), (filas.Count.ToString(CultureInfo.InvariantCulture), 0)], 2);
                EscribirFilaInforme(xml, filaActual, [("Fuente", 0), ("Consultas autorizadas de CONATRADEC", 0)], 2);
                xml.WriteEndElement();
                xml.WriteEndElement();
            });
            GuardarXml(zip, "xl/worksheets/sheet2.xml", xml =>
            {
                AbrirHoja(xml, 2, congelar: false);
                xml.WriteStartElement("sheetData", HojaNs);
                int filaActual = 1;
                EscribirGrupo(xml, ref filaActual, tituloPrimario, primario);
                filaActual += 2;
                EscribirGrupo(xml, ref filaActual, tituloSecundario, secundario);
                filaActual += 2;
                EscribirGrupo(xml, ref filaActual, "Registros por mes", evolucion);
                xml.WriteEndElement();
                xml.WriteEndElement();
            });
            GuardarXml(zip, "xl/worksheets/sheet3.xml", xml =>
            {
                AbrirHoja(xml, encabezados.Count, congelar: true);
                xml.WriteStartElement("sheetData", HojaNs);
                EscribirFilaInforme(xml, 1, encabezados.Select(x => (x, 1)).ToArray(), encabezados.Count);
                for (int f = 0; f < filas.Count; f++)
                {
                    var celdas = filas[f].Select((valor, c) =>
                        (Valor: valor ?? "", Estilo: columnasNumericas.Contains(c) ? 4 : 0)).ToArray();
                    EscribirFilaInforme(xml, f + 2, celdas, encabezados.Count, columnasNumericas);
                }
                xml.WriteEndElement();
                xml.WriteStartElement("autoFilter", HojaNs);
                xml.WriteAttributeString("ref", $"A1:{LetraColumna(encabezados.Count)}{filas.Count + 1}");
                xml.WriteEndElement();
                xml.WriteEndElement();
            });
        }
        return salida.ToArray();
    }

    private static void AbrirHoja(XmlWriter xml, int columnas, bool congelar)
    {
        xml.WriteStartElement("worksheet", HojaNs);
        if (congelar)
        {
            xml.WriteStartElement("sheetViews", HojaNs);
            xml.WriteStartElement("sheetView", HojaNs);
            xml.WriteAttributeString("workbookViewId", "0");
            xml.WriteStartElement("pane", HojaNs);
            xml.WriteAttributeString("ySplit", "1");
            xml.WriteAttributeString("topLeftCell", "A2");
            xml.WriteAttributeString("activePane", "bottomLeft");
            xml.WriteAttributeString("state", "frozen");
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        xml.WriteStartElement("cols", HojaNs);
        for (int c = 1; c <= columnas; c++)
        {
            xml.WriteStartElement("col", HojaNs);
            xml.WriteAttributeString("min", c.ToString(CultureInfo.InvariantCulture));
            xml.WriteAttributeString("max", c.ToString(CultureInfo.InvariantCulture));
            xml.WriteAttributeString("width", c == 1 && columnas <= 2 ? "46" : "25");
            xml.WriteAttributeString("customWidth", "1");
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    private static void EscribirGrupo(XmlWriter xml, ref int filaActual,
        string titulo, IReadOnlyList<(string Etiqueta, int Cantidad)> datos)
    {
        EscribirFilaInforme(xml, filaActual++, [(titulo, 3), ("Registros", 3)], 2);
        if (datos.Count == 0)
            EscribirFilaInforme(xml, filaActual++, [("Sin registros", 0), ("0", 4)], 2, new HashSet<int> { 1 });
        foreach (var dato in datos)
            EscribirFilaInforme(xml, filaActual++, [(dato.Etiqueta, 0),
                (dato.Cantidad.ToString(CultureInfo.InvariantCulture), 4)], 2, new HashSet<int> { 1 });
    }

    private static void EscribirFilaInforme(XmlWriter xml, int fila,
        IReadOnlyList<(string Valor, int Estilo)> celdas, int columnas,
        IReadOnlySet<int>? columnasNumericas = null)
    {
        xml.WriteStartElement("row", HojaNs);
        xml.WriteAttributeString("r", fila.ToString(CultureInfo.InvariantCulture));
        for (int c = 0; c < Math.Min(columnas, celdas.Count); c++)
        {
            string valor = celdas[c].Valor ?? "";
            int estilo = celdas[c].Estilo;
            xml.WriteStartElement("c", HojaNs);
            xml.WriteAttributeString("r", $"{LetraColumna(c + 1)}{fila}");
            bool esNumero = columnasNumericas?.Contains(c) == true &&
                (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.GetCultureInfo("es-NI"), out _) ||
                 decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out _));
            if (esNumero)
            {
                decimal.TryParse(valor, NumberStyles.Number,
                    CultureInfo.GetCultureInfo("es-NI"), out decimal numero);
                if (!decimal.TryParse(valor, NumberStyles.Number,
                    CultureInfo.GetCultureInfo("es-NI"), out numero))
                    decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out numero);
                xml.WriteAttributeString("s", estilo.ToString(CultureInfo.InvariantCulture));
                xml.WriteElementString("v", HojaNs, numero.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                xml.WriteAttributeString("t", "inlineStr");
                if (estilo != 0)
                    xml.WriteAttributeString("s", estilo.ToString(CultureInfo.InvariantCulture));
                xml.WriteStartElement("is", HojaNs);
                xml.WriteStartElement("t", HojaNs);
                xml.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                if (valor.Length > 32767) valor = valor[..32767];
                xml.WriteString(new string(valor.Where(XmlConvert.IsXmlChar).ToArray()));
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    private static void EscribirEstilosEjecutivos(XmlWriter xml)
    {
        xml.WriteStartElement("styleSheet", HojaNs);
        xml.WriteStartElement("fonts", HojaNs);
        xml.WriteAttributeString("count", "3");
        Fuente(xml, negrita: false, claro: false, tamano: 11);
        Fuente(xml, negrita: true, claro: true, tamano: 11);
        Fuente(xml, negrita: true, claro: false, tamano: 13);
        xml.WriteEndElement();
        xml.WriteStartElement("fills", HojaNs);
        xml.WriteAttributeString("count", "4");
        foreach (string patron in new[] { "none", "gray125" })
        {
            xml.WriteStartElement("fill", HojaNs);
            xml.WriteStartElement("patternFill", HojaNs);
            xml.WriteAttributeString("patternType", patron);
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        Relleno(xml, "FF213F53");
        Relleno(xml, "FFE6F2F6");
        xml.WriteEndElement();
        xml.WriteStartElement("borders", HojaNs);
        xml.WriteAttributeString("count", "1");
        xml.WriteStartElement("border", HojaNs);
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteStartElement("cellStyleXfs", HojaNs);
        xml.WriteAttributeString("count", "1");
        Estilo(xml, 0, 0, 0);
        xml.WriteEndElement();
        xml.WriteStartElement("cellXfs", HojaNs);
        xml.WriteAttributeString("count", "5");
        Estilo(xml, 0, 0, 0);
        Estilo(xml, 1, 2, 0);
        Estilo(xml, 2, 0, 0);
        Estilo(xml, 2, 3, 0);
        Estilo(xml, 0, 0, 4);
        xml.WriteEndElement();
        xml.WriteEndElement();
    }

    private static void Estilo(XmlWriter xml, int fuente, int relleno, int formato)
    {
        xml.WriteStartElement("xf", HojaNs);
        xml.WriteAttributeString("numFmtId", formato.ToString(CultureInfo.InvariantCulture));
        xml.WriteAttributeString("fontId", fuente.ToString(CultureInfo.InvariantCulture));
        xml.WriteAttributeString("fillId", relleno.ToString(CultureInfo.InvariantCulture));
        xml.WriteAttributeString("borderId", "0");
        xml.WriteAttributeString("xfId", "0");
        if (formato != 0) xml.WriteAttributeString("applyNumberFormat", "1");
        xml.WriteEndElement();
    }

    private static void Fuente(XmlWriter xml, bool negrita, bool claro, int tamano)
    {
        xml.WriteStartElement("font", HojaNs);
        if (negrita) xml.WriteElementString("b", HojaNs, "");
        xml.WriteStartElement("sz", HojaNs);
        xml.WriteAttributeString("val", tamano.ToString(CultureInfo.InvariantCulture));
        xml.WriteEndElement();
        if (claro)
        {
            xml.WriteStartElement("color", HojaNs);
            xml.WriteAttributeString("rgb", "FFFFFFFF");
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    private static void Relleno(XmlWriter xml, string color)
    {
        xml.WriteStartElement("fill", HojaNs);
        xml.WriteStartElement("patternFill", HojaNs);
        xml.WriteAttributeString("patternType", "solid");
        xml.WriteStartElement("fgColor", HojaNs);
        xml.WriteAttributeString("rgb", color);
        xml.WriteEndElement();
        xml.WriteStartElement("bgColor", HojaNs);
        xml.WriteAttributeString("indexed", "64");
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteEndElement();
    }
}
