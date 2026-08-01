namespace CONATRADEC.AdminWeb.Models;

/// <summary>
/// Respuesta paginada del catálogo administrativo de fuentes.
/// </summary>
public sealed class FuenteNutrientePaginaModel
{
    public List<FuenteNutrienteAdminItemModel> Items { get; set; } = new();

    public int PaginaActual { get; set; } = 1;

    public int TamanoPagina { get; set; } = 12;

    public int TotalRegistros { get; set; }

    public int TotalPaginas { get; set; } = 1;
}

/// <summary>
/// Fuente de nutrientes utilizada por listado, detalle y formulario.
/// </summary>
public sealed class FuenteNutrienteAdminItemModel
{
    public int FuenteNutrientesId { get; set; }

    public string NombreNutriente { get; set; } = string.Empty;

    public string DescripcionNutriente { get; set; } = string.Empty;

    public decimal PrecioNutriente { get; set; }

    public bool Activo { get; set; }

    public bool HabilitadaEnmiendaCalcarea { get; set; }

    public bool HabilitadaFertilizacionMixta { get; set; }

    public decimal? Prnt { get; set; }

    public string? DescripcionParametro { get; set; }

    public List<ElementoFuenteAdminModel> ElementosQuimicos { get; set; } =
        new();

    public List<ParametroEnmiendaFuenteModel>
        ParametrosEnmiendaCalcarea { get; set; } = new();
}

public sealed class ElementoFuenteAdminModel
{
    public int FuenteNutrienteElementoQuimicoId { get; set; }

    public int ElementoQuimicosId { get; set; }

    public string NombreElementoQuimico { get; set; } = string.Empty;

    public string SimboloElementoQuimico { get; set; } = string.Empty;

    public decimal CantidadAporte { get; set; }
}

public sealed class ParametroEnmiendaFuenteModel
{
    public decimal Prnt { get; set; }

    public string? DescripcionParametro { get; set; }
}

public sealed class ElementoQuimicoFuenteOptionModel
{
    public int ElementoQuimicosId { get; set; }

    public string SimboloElementoQuimico { get; set; } = string.Empty;

    public string NombreElementoQuimico { get; set; } = string.Empty;

    public decimal PesoEquivalenteElementoQuimico { get; set; }

    public bool Activo { get; set; }
}

public sealed class FuenteNutrienteFormModel
{
    public int? FuenteNutrientesId { get; set; }

    public string NombreNutriente { get; set; } = string.Empty;

    public string DescripcionNutriente { get; set; } = string.Empty;

    public decimal PrecioNutriente { get; set; }

    public bool HabilitadaEnmiendaCalcarea { get; set; }

    public bool HabilitadaFertilizacionMixta { get; set; }

    public decimal Prnt { get; set; } = 80m;

    public string DescripcionParametro { get; set; } =
        "Fuente utilizada para cálculo de enmienda calcárea";

    public bool EnmiendaOriginal { get; set; }

    public bool FertilizacionMixtaOriginal { get; set; }

    public List<ElementoFuenteFormModel> Elementos { get; set; } = new();
}

public sealed class ElementoFuenteFormModel
{
    public int ElementoQuimicosId { get; set; }

    public string Simbolo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public bool Seleccionado { get; set; }

    public decimal CantidadAporte { get; set; }
}

public sealed class FuenteNutrienteGuardarRequestModel
{
    public string NombreNutriente { get; set; } = string.Empty;

    public string DescripcionNutriente { get; set; } = string.Empty;

    public decimal PrecioNutriente { get; set; }

    public List<ElementoFuenteGuardarRequestModel>
        ElementosQuimicos { get; set; } = new();
}

public sealed class ElementoFuenteGuardarRequestModel
{
    public int ElementoQuimicosId { get; set; }

    public decimal CantidadAporte { get; set; }
}

public sealed class HabilitarEnmiendaCalcareaRequestModel
{
    public decimal Prnt { get; set; }

    public string DescripcionParametro { get; set; } = string.Empty;
}

public sealed class FuenteNutrienteMutationResponseModel
{
    public string Mensaje { get; set; } = string.Empty;

    public FuenteNutrienteAdminItemModel? Data { get; set; }
}
