using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class BitacoraPaginada
{
    [JsonPropertyName("items")]
    public List<BitacoraListadoItem> Items { get; set; } = [];

    [JsonPropertyName("pagina")]
    public int Pagina { get; set; } = 1;

    [JsonPropertyName("tamanoPagina")]
    public int TamanoPagina { get; set; } = 25;

    [JsonPropertyName("totalRegistros")]
    public int TotalRegistros { get; set; }

    [JsonPropertyName("totalPaginas")]
    public int TotalPaginas { get; set; } = 1;
}

public class BitacoraListadoItem
{
    [JsonPropertyName("bitacoraId")]
    public Guid BitacoraId { get; set; }

    [JsonPropertyName("fechaHoraUtc")]
    public DateTime FechaHoraUtc { get; set; }

    [JsonPropertyName("usuarioId")]
    public int? UsuarioId { get; set; }

    [JsonPropertyName("usuarioNombre")]
    public string UsuarioNombre { get; set; } = string.Empty;

    [JsonPropertyName("rolNombre")]
    public string RolNombre { get; set; } = string.Empty;

    [JsonPropertyName("modulo")]
    public string Modulo { get; set; } = string.Empty;

    [JsonPropertyName("accion")]
    public string Accion { get; set; } = string.Empty;

    [JsonPropertyName("metodoHttp")]
    public string MetodoHttp { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("paginaOrigen")]
    public string PaginaOrigen { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("codigoEstado")]
    public int CodigoEstado { get; set; }

    [JsonPropertyName("exitoso")]
    public bool Exitoso { get; set; }

    [JsonPropertyName("duracionMs")]
    public long DuracionMs { get; set; }

    [JsonPropertyName("cantidadCambios")]
    public int CantidadCambios { get; set; }
}

public sealed class BitacoraDetalle : BitacoraListadoItem
{
    [JsonPropertyName("parametros")]
    public string? Parametros { get; set; }

    [JsonPropertyName("direccionIp")]
    public string? DireccionIp { get; set; }

    [JsonPropertyName("dispositivo")]
    public string? Dispositivo { get; set; }

    [JsonPropertyName("plataforma")]
    public string? Plataforma { get; set; }

    [JsonPropertyName("versionApp")]
    public string? VersionApp { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("cambios")]
    public List<BitacoraCambio> Cambios { get; set; } = [];
}

public sealed class BitacoraCambio
{
    [JsonPropertyName("bitacoraDetalleId")]
    public long BitacoraDetalleId { get; set; }

    [JsonPropertyName("fechaHoraUtc")]
    public DateTime FechaHoraUtc { get; set; }

    [JsonPropertyName("entidad")]
    public string Entidad { get; set; } = string.Empty;

    [JsonPropertyName("entidadId")]
    public string? EntidadId { get; set; }

    [JsonPropertyName("operacion")]
    public string Operacion { get; set; } = string.Empty;

    [JsonPropertyName("valoresAnteriores")]
    public string? ValoresAnteriores { get; set; }

    [JsonPropertyName("valoresNuevos")]
    public string? ValoresNuevos { get; set; }

    [JsonPropertyName("propiedadesModificadas")]
    public string? PropiedadesModificadas { get; set; }
}

public sealed class BitacoraCatalogos
{
    [JsonPropertyName("acciones")]
    public List<string> Acciones { get; set; } = [];

    [JsonPropertyName("modulos")]
    public List<string> Modulos { get; set; } = [];

    [JsonPropertyName("usuarios")]
    public List<BitacoraUsuarioFiltro> Usuarios { get; set; } = [];
}

public sealed class BitacoraUsuarioFiltro
{
    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

public sealed class BitacoraFiltro
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Buscar { get; set; } = string.Empty;
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}
