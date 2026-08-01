using System.ComponentModel.DataAnnotations;

namespace CONATRADEC.AdminWeb.Models;

public sealed class RangoCultivoPaginaModel
{
    public List<RangoCultivoResumenModel> Items { get; set; } = new();

    public int PaginaActual { get; set; } = 1;

    public int TamanoPagina { get; set; } = 20;

    public int TotalRegistros { get; set; }

    public int TotalPaginas { get; set; } = 1;
}

public sealed class RangoCultivoResumenModel
{
    public int TipoCultivoId { get; set; }

    public string NombreCategoria { get; set; } = string.Empty;

    public string DescripcionCategoria { get; set; } = string.Empty;

    public int CantidadAportes { get; set; }

    public string NombreMostrar =>
        string.IsNullOrWhiteSpace(NombreCategoria)
            ? "Cultivo sin nombre"
            : NombreCategoria;

    public string DescripcionMostrar =>
        string.IsNullOrWhiteSpace(DescripcionCategoria)
            ? "Sin descripción registrada."
            : DescripcionCategoria;
}

public sealed class RangoNutrientePaginaModel
{
    public List<RangoNutrienteItemModel> Items { get; set; } = new();

    public int PaginaActual { get; set; } = 1;

    public int TamanoPagina { get; set; } = 20;

    public int TotalRegistros { get; set; }

    public int TotalPaginas { get; set; } = 1;
}

public sealed class RangoNutrienteItemModel
{
    public int ParametroRangoNutrienteCultivoId { get; set; }

    public int TipoCultivoId { get; set; }

    public string NombreTipoCultivo { get; set; } = string.Empty;

    public int ElementoQuimicosId { get; set; }

    public string NombreElementoQuimico { get; set; } = string.Empty;

    public string SimboloElementoQuimico { get; set; } = string.Empty;

    public decimal ValorMinimo { get; set; }

    public decimal ValorMaximo { get; set; }

    public string UnidadBase { get; set; } = "lb/Mz";

    public string DescripcionParametro { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public decimal Amplitud =>
        Math.Max(
            0m,
            ValorMaximo - ValorMinimo);

    public string ElementoMostrar =>
        string.IsNullOrWhiteSpace(SimboloElementoQuimico)
            ? NombreElementoQuimico
            : $"{SimboloElementoQuimico} · {NombreElementoQuimico}";
}

public sealed class ElementoRangoDisponibleModel
{
    public int ElementoQuimicosId { get; set; }

    public string NombreElementoQuimico { get; set; } = string.Empty;

    public string SimboloElementoQuimico { get; set; } = string.Empty;

    public string Texto =>
        string.IsNullOrWhiteSpace(SimboloElementoQuimico)
            ? NombreElementoQuimico
            : $"{SimboloElementoQuimico} · {NombreElementoQuimico}";
}

public sealed class RangoCultivoFormModel
{
    [Required(
        ErrorMessage = "El nombre del cultivo es obligatorio.")]
    [MaxLength(
        80,
        ErrorMessage = "El nombre admite hasta 80 caracteres.")]
    public string NombreTipoCultivo { get; set; } = string.Empty;

    [MaxLength(
        150,
        ErrorMessage = "La descripción admite hasta 150 caracteres.")]
    public string DescripcionTipoCultivo { get; set; } = string.Empty;
}

public sealed class RangoNutrienteFormModel
{
    public int ParametroRangoNutrienteCultivoId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Seleccione un tipo de cultivo.")]
    public int TipoCultivoId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Seleccione un elemento químico.")]
    public int ElementoQuimicosId { get; set; }

    [Range(
        typeof(decimal),
        "0.0001",
        "999999999",
        ErrorMessage = "El valor mínimo debe ser mayor que cero.")]
    public decimal ValorMinimo { get; set; }

    [Range(
        typeof(decimal),
        "0.0001",
        "999999999",
        ErrorMessage = "El valor máximo debe ser mayor que cero.")]
    public decimal ValorMaximo { get; set; }

    public string UnidadBase { get; set; } = "lb/Mz";

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [MaxLength(
        150,
        ErrorMessage = "La descripción admite hasta 150 caracteres.")]
    public string DescripcionParametro { get; set; } = string.Empty;
}

public sealed class RangoMutationResponseModel
{
    public string Mensaje { get; set; } = string.Empty;

    public RangoNutrienteItemModel? Data { get; set; }
}

public sealed class RangoCatalogoResponseModel<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}

public sealed class RangoEliminadoItemModel
{
    public int Id { get; set; }

    public string Catalogo { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string Subtitulo { get; set; } = string.Empty;

    public string Detalle { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
