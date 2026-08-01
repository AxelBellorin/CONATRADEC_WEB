using System.ComponentModel.DataAnnotations;

namespace CONATRADEC.AdminWeb.Models;

public sealed class ExtraccionNutrientePaginaModel
{
    public List<ExtraccionNutrienteItemModel> Items { get; set; } = new();

    public int PaginaActual { get; set; } = 1;

    public int TamanoPagina { get; set; } = 20;

    public int TotalRegistros { get; set; }

    public int TotalPaginas { get; set; } = 1;
}

public sealed class ExtraccionNutrienteItemModel
{
    public int ParametroExtraccionNutrienteCafeId { get; set; }

    public int ElementoQuimicosId { get; set; }

    public string NombreElementoQuimico { get; set; } = string.Empty;

    public string SimboloElementoQuimico { get; set; } = string.Empty;

    public decimal CantidadExtraidaPorQQOro { get; set; }

    public string DescripcionParametro { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public string ElementoMostrar =>
        string.IsNullOrWhiteSpace(SimboloElementoQuimico)
            ? NombreElementoQuimico
            : $"{SimboloElementoQuimico} · {NombreElementoQuimico}";
}

public sealed class ExtraccionNutrienteFormModel
{
    public int ParametroExtraccionNutrienteCafeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un elemento químico.")]
    public int ElementoQuimicosId { get; set; }

    [Range(
        typeof(decimal),
        "0.0001",
        "999999999",
        ErrorMessage = "La cantidad extraída debe ser mayor que cero.")]
    public decimal CantidadExtraidaPorQQOro { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [MaxLength(150, ErrorMessage = "La descripción admite hasta 150 caracteres.")]
    public string DescripcionParametro { get; set; } = string.Empty;
}

public sealed class ExtraccionElementoOptionModel
{
    public int ElementoQuimicosId { get; set; }

    public string SimboloElementoQuimico { get; set; } = string.Empty;

    public string NombreElementoQuimico { get; set; } = string.Empty;

    public decimal PesoEquivalenteElementoQuimico { get; set; }

    public bool Activo { get; set; }

    public string Texto =>
        string.IsNullOrWhiteSpace(SimboloElementoQuimico)
            ? NombreElementoQuimico
            : $"{SimboloElementoQuimico} · {NombreElementoQuimico}";
}

public sealed class ExtraccionMutationResponseModel
{
    public string Mensaje { get; set; } = string.Empty;

    public ExtraccionNutrienteItemModel? Data { get; set; }
}

public sealed class ExtraccionCatalogoResponseModel<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}

public sealed class ExtraccionEliminadaItemModel
{
    public int Id { get; set; }

    public string Catalogo { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string Subtitulo { get; set; } = string.Empty;

    public string Detalle { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
