using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class DispositivosConexionResumen
{
    public int TotalConectados { get; set; }
    public int UsuariosConectados { get; set; }
    public int AndroidConectados { get; set; }
    public int WindowsConectados { get; set; }
    public int OtrosConectados { get; set; }
    public int ConectadosConUbicacion { get; set; }
    public int TotalDispositivosConUbicacion { get; set; }
    public int TotalDispositivosRegistrados { get; set; }
    public int TotalSesionesRegistradas { get; set; }
    public int DispositivosActivosUltimas24Horas { get; set; }
    public int MinutosTolerancia { get; set; }
    public DateTime FechaConsultaUtc { get; set; }
    public DateTime? UltimoLatidoRecibidoUtc { get; set; }
    public DateTime? UltimaUbicacionRecibidaUtc { get; set; }
}

public sealed class DispositivosConexionPaginada
{
    public List<DispositivoConexionItem> Items { get; set; } = [];
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; } = 1;
    public int MinutosTolerancia { get; set; } = 2;
    public DateTime FechaConsultaUtc { get; set; }
}

public sealed class DispositivoConexionItem
{
    public int DispositivoConexionId { get; set; }
    public string InstalacionId { get; set; } = string.Empty;
    public string SesionId { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string CorreoUsuario { get; set; } = string.Empty;
    public string RolNombre { get; set; } = string.Empty;
    public string Plataforma { get; set; } = string.Empty;
    public string TipoDispositivo { get; set; } = string.Empty;
    public string Fabricante { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string NombreDispositivo { get; set; } = string.Empty;
    public string SistemaOperativo { get; set; } = string.Empty;
    public string VersionSistema { get; set; } = string.Empty;
    public string VersionApp { get; set; } = string.Empty;
    public string BuildApp { get; set; } = string.Empty;
    public string Idioma { get; set; } = string.Empty;
    public string TipoConexion { get; set; } = string.Empty;
    public string PaginaActual { get; set; } = string.Empty;
    public string DireccionIp { get; set; } = string.Empty;
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public decimal? PrecisionMetros { get; set; }
    public DateTime? FechaUbicacionUtc { get; set; }
    public string OrigenUbicacion { get; set; } = string.Empty;
    public string EstadoPermisoUbicacion { get; set; } = string.Empty;
    public bool? UbicacionSimulada { get; set; }
    public bool TieneUbicacion { get; set; }
    public DateTime FechaRegistroUtc { get; set; }
    public DateTime FechaInicioSesionUtc { get; set; }
    public DateTime UltimoLatidoUtc { get; set; }
    public DateTime? FechaDesconexionUtc { get; set; }
    public bool Conectado { get; set; }
    public int SegundosDesdeUltimoLatido { get; set; }
    public int CantidadSesiones { get; set; }
}

public sealed class DispositivoConexionFiltro
{
    public string Estado { get; set; } = "conectados";
    public string Ubicacion { get; set; } = string.Empty;
    public string Plataforma { get; set; } = string.Empty;
    public string VersionApp { get; set; } = string.Empty;
    public string Buscar { get; set; } = string.Empty;
    public int MinutosActivo { get; set; } = 2;
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}


public sealed class DispositivoConexionMapaItem
{
    public int DispositivoConexionId { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string Plataforma { get; set; } = string.Empty;
    public string NombreDispositivo { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public decimal? PrecisionMetros { get; set; }
    public DateTime FechaUbicacionUtc { get; set; }
    public string EstadoPermisoUbicacion { get; set; } = string.Empty;
    public bool? UbicacionSimulada { get; set; }
    public bool Conectado { get; set; }
    public DateTime UltimoLatidoUtc { get; set; }
}
