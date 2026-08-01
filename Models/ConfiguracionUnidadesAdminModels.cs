using System.ComponentModel.DataAnnotations;

namespace CONATRADEC.AdminWeb.Models;

public sealed class ConfiguracionUnidadesApiResponseModel<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}

public sealed class UnidadCatalogoAdminModel
{
    public int UnidadMedidaId { get; set; }

    public string NombreUnidadMedida { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public bool EsUnidadInternaKgHa =>
        string.Equals(
            NombreUnidadMedida.Trim(),
            "KG/HA",
            StringComparison.OrdinalIgnoreCase);
}

public sealed class UnidadMedidaFormModel
{
    public int UnidadMedidaId { get; set; }

    [Required(ErrorMessage = "El nombre de la unidad es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre admite hasta 50 caracteres.")]
    public string NombreUnidadMedida { get; set; } = string.Empty;
}

public sealed class UnidadMedidaMutationResponseModel
{
    public string Mensaje { get; set; } = string.Empty;

    public UnidadCatalogoAdminModel? Data { get; set; }
}

public sealed class FormulaConversionAdminModel
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public bool RequiereElementoQuimico { get; set; }

    public bool RequiereMateriaOrganica { get; set; }
}

public sealed class ElementoConfiguracionUnidadesAdminModel
{
    public int ElementoQuimicosId { get; set; }

    public string SimboloElementoQuimico { get; set; } = string.Empty;

    public string NombreElementoQuimico { get; set; } = string.Empty;

    public decimal PesoEquivalenteElementoQuimico { get; set; }

    public int? UnidadPredeterminadaId { get; set; }

    public List<UnidadConversionConfiguradaAdminModel> Unidades { get; set; } =
        new();

    public int UnidadesActivas =>
        Unidades.Count(item =>
            item.Activo &&
            !item.EsUnidadInternaKgHa);

    public string NombreMostrar =>
        string.IsNullOrWhiteSpace(SimboloElementoQuimico)
            ? NombreElementoQuimico
            : $"{SimboloElementoQuimico} · {NombreElementoQuimico}";
}

public sealed class UnidadConversionConfiguradaAdminModel
{
    public int ConfiguracionId { get; set; }

    public int UnidadMedidaId { get; set; }

    public string NombreUnidadMedida { get; set; } = string.Empty;

    public string CodigoFormulaConversion { get; set; } = "LINEAL";

    public decimal FactorPrincipal { get; set; } = 1m;

    public decimal FactorSecundario { get; set; } = 1m;

    public decimal FactorTerciario { get; set; } = 1m;

    public decimal Divisor { get; set; } = 1m;

    public decimal Desplazamiento { get; set; }

    public bool UnidadPredeterminada { get; set; }

    public bool VisibleEnFormulario { get; set; } = true;

    public int Orden { get; set; }

    public string Observacion { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public bool EsUnidadInternaKgHa =>
        string.Equals(
            NombreUnidadMedida.Trim(),
            "KG/HA",
            StringComparison.OrdinalIgnoreCase);
}

public sealed class UnidadConversionEdicionModel
{
    public int ConfiguracionId { get; set; }

    public int UnidadMedidaId { get; set; }

    public string NombreUnidadMedida { get; set; } = string.Empty;

    public bool UnidadCatalogoActiva { get; set; }

    public bool Seleccionada { get; set; }

    public string CodigoFormulaConversion { get; set; } = "LINEAL";

    public decimal FactorPrincipal { get; set; } = 1m;

    public decimal FactorSecundario { get; set; } = 1m;

    public decimal FactorTerciario { get; set; } = 1m;

    public decimal Divisor { get; set; } = 1m;

    public decimal Desplazamiento { get; set; }

    public bool UnidadPredeterminada { get; set; }

    public bool VisibleEnFormulario { get; set; } = true;

    public int Orden { get; set; }

    public string Observacion { get; set; } = string.Empty;

    public bool EsUnidadInternaKgHa =>
        string.Equals(
            NombreUnidadMedida.Trim(),
            "KG/HA",
            StringComparison.OrdinalIgnoreCase);
}

public sealed class GuardarConfiguracionUnidadesRequestModel
{
    public List<GuardarUnidadConversionRequestModel> Unidades { get; set; } =
        new();
}

public sealed class GuardarUnidadConversionRequestModel
{
    public int UnidadMedidaId { get; set; }

    public string CodigoFormulaConversion { get; set; } = "LINEAL";

    public decimal FactorPrincipal { get; set; } = 1m;

    public decimal FactorSecundario { get; set; } = 1m;

    public decimal FactorTerciario { get; set; } = 1m;

    public decimal Divisor { get; set; } = 1m;

    public decimal Desplazamiento { get; set; }

    public bool UnidadPredeterminada { get; set; }

    public bool VisibleEnFormulario { get; set; } = true;

    public int Orden { get; set; }

    public string Observacion { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}

public sealed class ProbarConversionUnidadRequestModel
{
    public string Contexto { get; set; } = "ELEMENTO";

    public int? ElementoQuimicosId { get; set; }

    public int UnidadMedidaId { get; set; }

    public decimal ValorReportado { get; set; }

    public decimal? MateriaOrganicaPorcentaje { get; set; }
}

public sealed class ResultadoPruebaConversionAdminModel
{
    public string Contexto { get; set; } = string.Empty;

    public int? ElementoQuimicosId { get; set; }

    public string Elemento { get; set; } = string.Empty;

    public int UnidadOrigenId { get; set; }

    public string UnidadOrigen { get; set; } = string.Empty;

    public int UnidadDestinoId { get; set; }

    public string UnidadDestino { get; set; } = string.Empty;

    public decimal ValorReportado { get; set; }

    public decimal ValorConvertido { get; set; }

    public string CodigoFormulaConversion { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;
}
