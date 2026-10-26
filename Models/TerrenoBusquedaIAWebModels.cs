using System.Globalization;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

/// <summary>
/// Contrato de búsqueda de terrenos compartido con la aplicación .NET MAUI.
/// La API devuelve los resultados dentro de ApiEnvelopeWeb.
/// </summary>
public sealed class TerrenoBusquedaIAPaginaWeb
{
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
    public List<TerrenoBusquedaIAItemWeb> Items { get; set; } = [];
}

public sealed class TerrenoBusquedaIAItemWeb
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string DireccionTerreno { get; set; } = string.Empty;
    public decimal ExtensionManzanaTerreno { get; set; }
    public int? PropietarioId { get; set; }
    public string IdentificacionPropietario { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;

    [JsonIgnore]
    public string UbicacionTexto => string.Join(
        ", ",
        new[] { Municipio, Departamento, Pais }
            .Where(valor => !string.IsNullOrWhiteSpace(valor)));

    [JsonIgnore]
    public string PropietarioTexto => string.IsNullOrWhiteSpace(IdentificacionPropietario)
        ? Propietario
        : $"{Propietario} · {IdentificacionPropietario}";

    [JsonIgnore]
    public string ExtensionTexto =>
        $"{ExtensionManzanaTerreno.ToString("N2", CultureInfo.CurrentCulture)} manzanas";
}

public sealed class TerrenoBusquedaIAFiltroWeb
{
    public string Texto { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string IdentificacionPropietario { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public decimal? ExtensionMinima { get; set; }
    public decimal? ExtensionMaxima { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}
