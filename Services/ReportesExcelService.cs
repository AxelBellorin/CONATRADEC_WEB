using System.IO.Compression;
using System.Text;
using System.Xml;

namespace CONATRADEC.AdminWeb.Services;

/// <summary>
/// Crea un libro XLSX OpenXML sin agregar dependencias NuGet.
/// Todos los valores se guardan como texto (inlineStr), nunca como fórmulas.
/// </summary>
public static partial class ReportesExcelService
{
    private const string HojaNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelacionesDoc = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string RelacionesPaquete = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string TiposContenido = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Generar(string titulo, IReadOnlyList<string> encabezados, IReadOnlyList<string[]> filas)
    {
        ArgumentNullException.ThrowIfNull(encabezados);
        ArgumentNullException.ThrowIfNull(filas);
        if (encabezados.Count == 0 || encabezados.Count > 100)
            throw new ArgumentException("El reporte debe tener entre 1 y 100 columnas.", nameof(encabezados));
        if (filas.Any(fila => fila.Length != encabezados.Count))
            throw new ArgumentException("Todas las filas deben tener la misma cantidad de columnas.", nameof(filas));

        using var salida = new MemoryStream();
        using (var zip = new ZipArchive(salida, ZipArchiveMode.Create, leaveOpen: true))
        {
            GuardarXml(zip, "[Content_Types].xml", xml =>
            {
                xml.WriteStartElement("Types", TiposContenido);
                ContenidoPredeterminado(xml, "rels", "application/vnd.openxmlformats-package.relationships+xml");
                ContenidoPredeterminado(xml, "xml", "application/xml");
                TipoHoja(xml, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                TipoHoja(xml, "/xl/worksheets/sheet1.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
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
                xml.WriteStartElement("sheet", HojaNs);
                xml.WriteAttributeString("name", NombreHoja(titulo));
                xml.WriteAttributeString("sheetId", "1");
                xml.WriteAttributeString("r", "id", RelacionesDoc, "rId1");
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();
            });
            GuardarXml(zip, "xl/_rels/workbook.xml.rels", xml =>
            {
                xml.WriteStartElement("Relationships", RelacionesPaquete);
                Relacion(xml, "rId1", RelacionesDoc + "/worksheet", "worksheets/sheet1.xml");
                Relacion(xml, "rId2", RelacionesDoc + "/styles", "styles.xml");
                xml.WriteEndElement();
            });
            GuardarXml(zip, "xl/styles.xml", EscribirEstilos);
            GuardarXml(zip, "xl/worksheets/sheet1.xml", xml =>
            {
                xml.WriteStartElement("worksheet", HojaNs);
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
                xml.WriteStartElement("cols", HojaNs);
                for (int c = 1; c <= encabezados.Count; c++)
                {
                    xml.WriteStartElement("col", HojaNs);
                    xml.WriteAttributeString("min", c.ToString());
                    xml.WriteAttributeString("max", c.ToString());
                    xml.WriteAttributeString("width", "24");
                    xml.WriteAttributeString("customWidth", "1");
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
                xml.WriteStartElement("sheetData", HojaNs);
                EscribirFila(xml, 1, encabezados, encabezado: true);
                for (int indice = 0; indice < filas.Count; indice++)
                    EscribirFila(xml, indice + 2, filas[indice], encabezado: false);
                xml.WriteEndElement();
                xml.WriteStartElement("autoFilter", HojaNs);
                xml.WriteAttributeString("ref", $"A1:{LetraColumna(encabezados.Count)}{filas.Count + 1}");
                xml.WriteEndElement();
                xml.WriteEndElement();
            });
        }
        return salida.ToArray();
    }

    private static void GuardarXml(ZipArchive zip, string ruta, Action<XmlWriter> escribir)
    {
        ZipArchiveEntry archivo = zip.CreateEntry(ruta, CompressionLevel.Fastest);
        using Stream stream = archivo.Open();
        using var xml = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            CloseOutput = false
        });
        xml.WriteStartDocument();
        escribir(xml);
        xml.WriteEndDocument();
    }

    private static void ContenidoPredeterminado(XmlWriter xml, string extension, string tipo)
    {
        xml.WriteStartElement("Default", TiposContenido);
        xml.WriteAttributeString("Extension", extension);
        xml.WriteAttributeString("ContentType", tipo);
        xml.WriteEndElement();
    }

    private static void TipoHoja(XmlWriter xml, string ruta, string tipo)
    {
        xml.WriteStartElement("Override", TiposContenido);
        xml.WriteAttributeString("PartName", ruta);
        xml.WriteAttributeString("ContentType", tipo);
        xml.WriteEndElement();
    }

    private static void Relacion(XmlWriter xml, string id, string tipo, string destino)
    {
        xml.WriteStartElement("Relationship", RelacionesPaquete);
        xml.WriteAttributeString("Id", id);
        xml.WriteAttributeString("Type", tipo);
        xml.WriteAttributeString("Target", destino);
        xml.WriteEndElement();
    }

    private static void EscribirEstilos(XmlWriter xml)
    {
        xml.WriteStartElement("styleSheet", HojaNs);
        xml.WriteStartElement("fonts", HojaNs);
        xml.WriteAttributeString("count", "2");
        xml.WriteStartElement("font", HojaNs);
        xml.WriteStartElement("sz", HojaNs);
        xml.WriteAttributeString("val", "11");
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteStartElement("font", HojaNs);
        xml.WriteElementString("b", HojaNs, "");
        xml.WriteStartElement("color", HojaNs);
        xml.WriteAttributeString("rgb", "FFFFFFFF");
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteStartElement("fills", HojaNs);
        xml.WriteAttributeString("count", "3");
        foreach (string pattern in new[] { "none", "gray125" })
        {
            xml.WriteStartElement("fill", HojaNs);
            xml.WriteStartElement("patternFill", HojaNs);
            xml.WriteAttributeString("patternType", pattern);
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        xml.WriteStartElement("fill", HojaNs);
        xml.WriteStartElement("patternFill", HojaNs);
        xml.WriteAttributeString("patternType", "solid");
        xml.WriteStartElement("fgColor", HojaNs);
        xml.WriteAttributeString("rgb", "FF245879");
        xml.WriteEndElement();
        xml.WriteStartElement("bgColor", HojaNs);
        xml.WriteAttributeString("indexed", "64");
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteStartElement("borders", HojaNs);
        xml.WriteAttributeString("count", "1");
        xml.WriteStartElement("border", HojaNs);
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteStartElement("cellStyleXfs", HojaNs);
        xml.WriteAttributeString("count", "1");
        Xf(xml, "0", "0");
        xml.WriteEndElement();
        xml.WriteStartElement("cellXfs", HojaNs);
        xml.WriteAttributeString("count", "2");
        Xf(xml, "0", "0");
        Xf(xml, "1", "2");
        xml.WriteEndElement();
        xml.WriteEndElement();
    }

    private static void Xf(XmlWriter xml, string fontId, string fillId)
    {
        xml.WriteStartElement("xf", HojaNs);
        xml.WriteAttributeString("numFmtId", "0");
        xml.WriteAttributeString("fontId", fontId);
        xml.WriteAttributeString("fillId", fillId);
        xml.WriteAttributeString("borderId", "0");
        xml.WriteAttributeString("xfId", "0");
        xml.WriteEndElement();
    }

    private static void EscribirFila(XmlWriter xml, int indice, IReadOnlyList<string> valores, bool encabezado)
    {
        xml.WriteStartElement("row", HojaNs);
        xml.WriteAttributeString("r", indice.ToString());
        for (int c = 0; c < valores.Count; c++)
        {
            xml.WriteStartElement("c", HojaNs);
            xml.WriteAttributeString("r", LetraColumna(c + 1) + indice);
            xml.WriteAttributeString("t", "inlineStr");
            if (encabezado)
                xml.WriteAttributeString("s", "1");
            xml.WriteStartElement("is", HojaNs);
            xml.WriteStartElement("t", HojaNs);
            xml.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
            string valor = valores[c] ?? "";
            if (valor.Length > 32767)
                valor = valor[..32767];
            xml.WriteString(new string(valor.Where(XmlConvert.IsXmlChar).ToArray()));
            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }

    private static string LetraColumna(int indice)
    {
        var letras = new StringBuilder();
        while (indice > 0)
        {
            indice--;
            letras.Insert(0, (char)('A' + indice % 26));
            indice /= 26;
        }
        return letras.ToString();
    }

    private static string NombreHoja(string titulo)
    {
        string nombre = new string((titulo ?? "Reporte")
            .Where(c => "[]:*?/\\".IndexOf(c) < 0).Take(31).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(nombre) ? "Reporte" : nombre.Trim('\'');
    }
}
