using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class MapaRelacionesAnalisisPayload
{
    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } =
        string.Empty;

    [JsonPropertyName("subtitulo")]
    public string Subtitulo { get; set; } =
        string.Empty;

    [JsonPropertyName("estadoGeneral")]
    public string EstadoGeneral { get; set; } =
        "NEUTRAL";

    [JsonPropertyName("nodos")]
    public List<MapaRelacionesAnalisisNodo> Nodos
    {
        get;
        set;
    } = [];

    [JsonPropertyName("relaciones")]
    public List<MapaRelacionesAnalisisArista> Relaciones
    {
        get;
        set;
    } = [];
}

public sealed class MapaRelacionesAnalisisNodo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } =
        string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } =
        string.Empty;

    [JsonPropertyName("grupo")]
    public string Grupo { get; set; } =
        string.Empty;

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } =
        string.Empty;

    [JsonPropertyName("subtitulo")]
    public string Subtitulo { get; set; } =
        string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } =
        "NEUTRAL";

    [JsonPropertyName("padreId")]
    public string? PadreId { get; set; }

    [JsonPropertyName("expandible")]
    public bool Expandible { get; set; }

    [JsonPropertyName("ocultoInicial")]
    public bool OcultoInicial { get; set; }

    [JsonPropertyName("detalles")]
    public Dictionary<string, string> Detalles
    {
        get;
        set;
    } = [];
}

public sealed class MapaRelacionesAnalisisArista
{
    [JsonPropertyName("id")]
    public string Id { get; set; } =
        string.Empty;

    [JsonPropertyName("origen")]
    public string Origen { get; set; } =
        string.Empty;

    [JsonPropertyName("destino")]
    public string Destino { get; set; } =
        string.Empty;

    [JsonPropertyName("etiqueta")]
    public string Etiqueta { get; set; } =
        string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } =
        "NORMAL";

    [JsonPropertyName("ocultaInicial")]
    public bool OcultaInicial { get; set; }
}

public sealed class MapaRelacionesDetalleTecnico
{
    [JsonPropertyName("analisisSueloId")]
    public int AnalisisSueloId { get; set; }

    [JsonPropertyName("analisisSueloCalculoId")]
    public int AnalisisSueloCalculoId { get; set; }

    [JsonPropertyName("formula")]
    public MapaRelacionesFormula? Formula { get; set; }

    [JsonPropertyName("fertilizacionMixta")]
    public MapaRelacionesMixta? FertilizacionMixta
    {
        get;
        set;
    }
}

public sealed class MapaRelacionesFormula
{
    [JsonPropertyName("formulaNutricionalId")]
    public int FormulaNutricionalId { get; set; }

    [JsonPropertyName("nombreFormula")]
    public string NombreFormula { get; set; } =
        string.Empty;

    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("totalLibras")]
    public decimal TotalLibras { get; set; }

    [JsonPropertyName("mezclaTotalQq")]
    public decimal MezclaTotalQq { get; set; }

    [JsonPropertyName("precioTotalFormula")]
    public decimal PrecioTotalFormula { get; set; }

    [JsonPropertyName("totalPlantas")]
    public int TotalPlantas { get; set; }

    [JsonPropertyName("totalAplicaciones")]
    public int TotalAplicaciones { get; set; }

    [JsonPropertyName("esComplementoMixta")]
    public bool EsComplementoMixta { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("fuentes")]
    public List<MapaRelacionesFormulaFuente> Fuentes
    {
        get;
        set;
    } = [];
}

public sealed class MapaRelacionesFormulaFuente
{
    [JsonPropertyName("fuenteNutrientesId")]
    public int FuenteNutrientesId { get; set; }

    [JsonPropertyName("nombreFuente")]
    public string NombreFuente { get; set; } =
        string.Empty;

    [JsonPropertyName("cantidadDetalles")]
    public int CantidadDetalles { get; set; }

    [JsonPropertyName("libras")]
    public decimal Libras { get; set; }

    [JsonPropertyName("qq")]
    public decimal Qq { get; set; }

    [JsonPropertyName("requerimientoLibras")]
    public decimal RequerimientoLibras { get; set; }

    [JsonPropertyName("precioPorQuintal")]
    public decimal PrecioPorQuintal { get; set; }

    [JsonPropertyName("subtotalFuente")]
    public decimal SubtotalFuente { get; set; }

    [JsonPropertyName("onzasAnuales")]
    public decimal OnzasAnuales { get; set; }

    [JsonPropertyName("onzasPorAplicacion")]
    public decimal OnzasPorAplicacion { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("elementosObjetivo")]
    public List<MapaRelacionesElementoReferencia>
        ElementosObjetivo { get; set; } = [];

    [JsonPropertyName("aportes")]
    public List<MapaRelacionesFormulaAporte> Aportes
    {
        get;
        set;
    } = [];
}

public sealed class MapaRelacionesFormulaAporte
{
    [JsonPropertyName("elementoQuimicosId")]
    public int ElementoQuimicosId { get; set; }

    [JsonPropertyName("simbolo")]
    public string Simbolo { get; set; } =
        string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } =
        string.Empty;

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public sealed class MapaRelacionesElementoReferencia
{
    [JsonPropertyName("elementoQuimicosId")]
    public int ElementoQuimicosId { get; set; }

    [JsonPropertyName("simbolo")]
    public string Simbolo { get; set; } =
        string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } =
        string.Empty;
}

public sealed class MapaRelacionesMixta
{
    [JsonPropertyName("fertilizacionMixtaId")]
    public int FertilizacionMixtaId { get; set; }

    [JsonPropertyName("fechaCalculo")]
    public DateTime FechaCalculo { get; set; }

    [JsonPropertyName("observacion")]
    public string? Observacion { get; set; }

    [JsonPropertyName("esComplementoBalance")]
    public bool EsComplementoBalance { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("fuentes")]
    public List<MapaRelacionesMixtaFuente> Fuentes
    {
        get;
        set;
    } = [];

    [JsonPropertyName("resultados")]
    public List<MapaRelacionesMixtaResultado> Resultados
    {
        get;
        set;
    } = [];
}

public sealed class MapaRelacionesMixtaFuente
{
    [JsonPropertyName("fertilizacionMixtaFuenteId")]
    public int FertilizacionMixtaFuenteId { get; set; }

    [JsonPropertyName("fuenteNutrientesId")]
    public int FuenteNutrientesId { get; set; }

    [JsonPropertyName("nombreFuente")]
    public string NombreFuente { get; set; } =
        string.Empty;

    [JsonPropertyName("cantidadQq")]
    public decimal CantidadQq { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("aportes")]
    public List<MapaRelacionesMixtaAporteFuente>
        Aportes { get; set; } = [];
}

public sealed class MapaRelacionesMixtaAporteFuente
{
    [JsonPropertyName("elementoQuimicosId")]
    public int ElementoQuimicosId { get; set; }

    [JsonPropertyName("simbolo")]
    public string Simbolo { get; set; } =
        string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } =
        string.Empty;

    [JsonPropertyName("aportePorUnidad")]
    public decimal AportePorUnidad { get; set; }

    [JsonPropertyName("cantidadQq")]
    public decimal CantidadQq { get; set; }

    [JsonPropertyName("aporteTotal")]
    public decimal AporteTotal { get; set; }
}

public sealed class MapaRelacionesMixtaResultado
{
    [JsonPropertyName("fertilizacionMixtaDetalleId")]
    public int FertilizacionMixtaDetalleId { get; set; }

    [JsonPropertyName("elementoQuimicosId")]
    public int ElementoQuimicosId { get; set; }

    [JsonPropertyName("simbolo")]
    public string Simbolo { get; set; } =
        string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } =
        string.Empty;

    [JsonPropertyName("requerimientoOriginal")]
    public decimal RequerimientoOriginal { get; set; }

    [JsonPropertyName("aporteOrganico")]
    public decimal AporteOrganico { get; set; }

    [JsonPropertyName("diferencia")]
    public decimal Diferencia { get; set; }

    [JsonPropertyName("deficit")]
    public decimal Deficit { get; set; }

    [JsonPropertyName("sobrante")]
    public decimal Sobrante { get; set; }

    [JsonPropertyName("aporteReconstruido")]
    public decimal AporteReconstruido { get; set; }

    [JsonPropertyName("diferenciaReconstruccion")]
    public decimal DiferenciaReconstruccion { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}
