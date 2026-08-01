using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class CatalogoPaginaRespuesta<T>
{
    public List<T> Items { get; set; } = [];
    public int PaginaActual { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; } = 1;
}

public sealed class CatalogoOperacionRespuesta<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public sealed class CatalogoEliminadoItemWeb
{
    public int Id { get; set; }
    public string Catalogo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string? Codigo { get; set; }
    public bool Activo { get; set; }
}

public sealed class ElementoQuimicoWebItem
{
    public int? ElementoQuimicosId { get; set; }
    public string SimboloElementoQuimico { get; set; } = string.Empty;
    public string NombreElementoQuimico { get; set; } = string.Empty;
    public decimal? PesoEquivalenteElementoQuimico { get; set; }
    public bool Activo { get; set; }
}

public sealed class ElementoQuimicoWebFormulario
{
    public int? ElementoQuimicosId { get; set; }

    [Required(ErrorMessage = "Ingrese el símbolo del elemento químico.")]
    [StringLength(
        10,
        ErrorMessage = "El símbolo no puede superar 10 caracteres.")]
    public string SimboloElementoQuimico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el nombre del elemento químico.")]
    [StringLength(
        100,
        ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string NombreElementoQuimico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el peso equivalente.")]
    [Range(
        typeof(decimal),
        "0.01",
        "99999999.99",
        ErrorMessage = "El peso equivalente debe estar entre 0.01 y 99,999,999.99.")]
    public decimal? PesoEquivalenteElementoQuimico { get; set; }
}

public sealed class TipoCultivoWebItem
{
    public int TipoCultivoId { get; set; }
    public string? NombreTipoCultivo { get; set; }
    public string? TipoCultivo { get; set; }
    public string? DescripcionTipoCultivo { get; set; }
    public bool Activo { get; set; }
    public int CantidadRangosActivos { get; set; }
    public int CantidadAnalisis { get; set; }

    public string NombreMostrar =>
        !string.IsNullOrWhiteSpace(TipoCultivo)
            ? TipoCultivo.Trim()
            : NombreTipoCultivo?.Trim() ?? string.Empty;

    public string DescripcionMostrar =>
        string.IsNullOrWhiteSpace(DescripcionTipoCultivo)
            ? "Sin descripción registrada."
            : DescripcionTipoCultivo.Trim();
}

public sealed class TipoCultivoWebFormulario
{
    [JsonIgnore]
    public int TipoCultivoId { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre del tipo de cultivo.")]
    [StringLength(
        80,
        ErrorMessage = "El nombre no puede superar 80 caracteres.")]
    public string NombreTipoCultivo { get; set; } = string.Empty;

    [StringLength(
        150,
        ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string DescripcionTipoCultivo { get; set; } = string.Empty;
}

public sealed class TipoAnalisisSueloWebItem
{
    public int TipoAnalisisSueloId { get; set; }
    public string? CodigoTipoAnalisisSuelo { get; set; }
    public string? NombreTipoAnalisisSuelo { get; set; }
    public string? DescripcionTipoAnalisisSuelo { get; set; }
    public bool Activo { get; set; }
    public int CantidadAnalisis { get; set; }
    public bool EsTipoSistema { get; set; }
    public bool PuedeEliminar { get; set; }

    public string NombreMostrar =>
        NombreTipoAnalisisSuelo?.Trim() ?? string.Empty;

    public string CodigoMostrar =>
        CodigoTipoAnalisisSuelo?.Trim() ?? string.Empty;

    public string DescripcionMostrar =>
        string.IsNullOrWhiteSpace(DescripcionTipoAnalisisSuelo)
            ? "Sin descripción registrada."
            : DescripcionTipoAnalisisSuelo.Trim();
}

public sealed class TipoAnalisisSueloWebFormulario
{
    [JsonIgnore]
    public int TipoAnalisisSueloId { get; set; }

    [JsonIgnore]
    public string CodigoTipoAnalisisSuelo { get; set; } = string.Empty;

    [JsonIgnore]
    public bool EsTipoSistema { get; set; }

    [Required(ErrorMessage = "Ingrese el nombre del tipo de análisis.")]
    [StringLength(
        100,
        ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string NombreTipoAnalisisSuelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la descripción del tipo de análisis.")]
    [StringLength(
        200,
        ErrorMessage = "La descripción no puede superar 200 caracteres.")]
    public string DescripcionTipoAnalisisSuelo { get; set; } = string.Empty;
}
