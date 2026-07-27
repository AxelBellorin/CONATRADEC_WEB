using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class TerrenoMapaItem
{
    [JsonPropertyName("terrenoId")]
    public int TerrenoId { get; set; }

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("productor")]
    public string Productor { get; set; } = string.Empty;

    [JsonPropertyName("latitud")]
    public double Latitud { get; set; }

    [JsonPropertyName("longitud")]
    public double Longitud { get; set; }

    [JsonPropertyName("departamento")]
    public string? Departamento { get; set; }

    [JsonPropertyName("municipio")]
    public string? Municipio { get; set; }

    [JsonPropertyName("extensionManzanas")]
    public decimal? ExtensionManzanas { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "Normal";

    [JsonPropertyName("ultimoPh")]
    public decimal? UltimoPh { get; set; }
}

public sealed class MapaFiltro
{
    public string Texto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
}
