using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class TerrenoAdminPagina
{
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; } = 1;
    public List<TerrenoAdminItem> Data { get; set; } = [];
}

public sealed class TerrenoAdminItem
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public int? PropietarioId { get; set; }
    public PropietarioTerrenoCatalogoItem? Propietario { get; set; }
    public string DireccionTerreno { get; set; } = string.Empty;
    public decimal ExtensionManzanaTerreno { get; set; }
    public DateOnly FechaIngresoTerreno { get; set; }
    public int CantidadPlantasTerreno { get; set; }
    public int MunicipioId { get; set; }
    public decimal CantidadQuintalesOro { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public bool Activo { get; set; }
    public TerrenoUbicacionItem Ubicacion { get; set; } = new();

    public string PropietarioNombre =>
        Propietario?.NombreCompleto ?? "Sin propietario";

    public string UbicacionTexto =>
        string.Join(
            ", ",
            new[]
            {
                Ubicacion.NombreMunicipio,
                Ubicacion.NombreDepartamento
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed class PropietarioTerrenoCatalogoItem
{
    public int PropietarioId { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public int TotalTerrenos { get; set; }
}

public sealed class TerrenoUbicacionItem
{
    public int PaisId { get; set; }
    public string NombrePais { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string NombreDepartamento { get; set; } = string.Empty;
    public int MunicipioId { get; set; }
    public string NombreMunicipio { get; set; } = string.Empty;
}

public sealed class TerrenoFormularioWeb
{
    [JsonIgnore]
    public int TerrenoId { get; set; }

    [JsonIgnore]
    public string CodigoTerreno { get; set; } = string.Empty;

    [JsonIgnore]
    public DateOnly? FechaIngresoTerreno { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un propietario.")]
    public int PropietarioId { get; set; }

    [Required(ErrorMessage = "Ingrese la dirección del terreno.")]
    [StringLength(300, ErrorMessage = "La dirección no puede superar 300 caracteres.")]
    public string DireccionTerreno { get; set; } = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999",
        ErrorMessage = "La extensión debe ser mayor que cero.")]
    public decimal ExtensionManzanaTerreno { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un municipio.")]
    public int MunicipioId { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "9999999999",
        ErrorMessage = "La producción no puede ser negativa.")]
    public decimal CantidadQuintalesOro { get; set; }

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "La cantidad de plantas no puede ser negativa.")]
    public int CantidadPlantasTerreno { get; set; }

    [Range(typeof(decimal), "-90", "90", ErrorMessage = "La latitud no es válida.")]
    public decimal Latitud { get; set; }

    [Range(typeof(decimal), "-180", "180", ErrorMessage = "La longitud no es válida.")]
    public decimal Longitud { get; set; }

    [JsonIgnore]
    public int? PaisId { get; set; }

    [JsonIgnore]
    public int? DepartamentoId { get; set; }
}

public sealed class TerrenoOperacionWeb<T>
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public string MensajeMostrar =>
        Mensaje ?? Message ?? string.Empty;
}

public sealed class TerrenoPoligonoVerticeWeb
{
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
}

public sealed class TerrenoPoligonoGuardarWeb
{
    public List<TerrenoPoligonoVerticeWeb> Vertices { get; set; } = [];
}

public sealed class TerrenoPoligonoWeb
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public bool TienePoligono { get; set; }
    public decimal LatitudPunto { get; set; }
    public decimal LongitudPunto { get; set; }
    public decimal ExtensionRegistradaManzanas { get; set; }
    public List<TerrenoPoligonoVerticeWeb> Vertices { get; set; } = [];
    public decimal AreaMetrosCuadrados { get; set; }
    public decimal AreaHectareas { get; set; }
    public decimal AreaManzanasCalculada { get; set; }
    public decimal DiferenciaManzanas { get; set; }
    public decimal? DiferenciaPorcentaje { get; set; }
    public bool PuntoDentroPoligono { get; set; } = true;
    public DateTime? FechaActualizacionUtc { get; set; }
}

public sealed class TerrenoEditorCambio
{
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public List<TerrenoPoligonoVerticeWeb> Vertices { get; set; } = [];
    public decimal AreaMetrosCuadrados { get; set; }
    public decimal AreaHectareas { get; set; }
    public decimal AreaManzanas { get; set; }
    public bool PuntoDentroPoligono { get; set; } = true;
}

public sealed class TerrenoFotoWeb
{
    public int FotoTerrenoId { get; set; }
    public int TerrenoId { get; set; }
    public string UrlFotoTerreno { get; set; } = string.Empty;
}

public sealed class TerrenoEliminadoWeb
{
    public int Id { get; set; }
    public string Catalogo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Codigo { get; set; }
    public bool Activo { get; set; }
}

public sealed class TerrenoEliminadosRespuesta
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<TerrenoEliminadoWeb> Data { get; set; } = [];
}
