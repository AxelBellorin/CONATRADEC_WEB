namespace CONATRADEC.AdminWeb.Models;

/// <summary>
/// Credencial administrada por el gestor de contraseñas del navegador.
/// La aplicación no la guarda en la base de datos ni dentro de su sesión.
/// </summary>
public sealed class CredencialNavegadorWeb
{
    public string Usuario { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public bool Disponible { get; set; }
    public bool Encontrada { get; set; }
}

/// <summary>
/// Información detectada directamente en el navegador.
/// </summary>
public sealed class ContextoPresenciaNavegadorWeb
{
    public string InstalacionId { get; set; } = string.Empty;
    public string SesionId { get; set; } = string.Empty;
    public string Plataforma { get; set; } = "Web";
    public string TipoDispositivo { get; set; } = string.Empty;
    public string Fabricante { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string NombreDispositivo { get; set; } = string.Empty;
    public string SistemaOperativo { get; set; } = string.Empty;
    public string VersionSistema { get; set; } = string.Empty;
    public string VersionApp { get; set; } = "Portal Web";
    public string BuildApp { get; set; } = "1.0";
    public string Idioma { get; set; } = string.Empty;
    public string TipoConexion { get; set; } = string.Empty;

    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public double? PrecisionMetros { get; set; }
    public DateTime? FechaUbicacionUtc { get; set; }
    public string OrigenUbicacion { get; set; } = "NAVEGADOR";
    public string EstadoPermisoUbicacion { get; set; } =
        "NO_SOLICITADA";
    public bool? UbicacionSimulada { get; set; }
}

public sealed class HoraLocalNavegadorWeb
{
    public int Hora { get; set; }
    public int Minuto { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;
    public string Saludo { get; set; } = "Bienvenido";
}

public sealed class ReportarDispositivoConexionWebRequest
{
    public string InstalacionId { get; set; } = string.Empty;
    public string SesionId { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string Plataforma { get; set; } = "Web";
    public string TipoDispositivo { get; set; } = string.Empty;
    public string Fabricante { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string NombreDispositivo { get; set; } = string.Empty;
    public string SistemaOperativo { get; set; } = string.Empty;
    public string VersionSistema { get; set; } = string.Empty;
    public string VersionApp { get; set; } = "Portal Web";
    public string BuildApp { get; set; } = "1.0";
    public string Idioma { get; set; } = string.Empty;
    public string TipoConexion { get; set; } = string.Empty;
    public string PaginaActual { get; set; } = string.Empty;
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public double? PrecisionMetros { get; set; }
    public DateTime? FechaUbicacionUtc { get; set; }
    public string OrigenUbicacion { get; set; } = "NAVEGADOR";
    public string EstadoPermisoUbicacion { get; set; } =
        "NO_SOLICITADA";
    public bool? UbicacionSimulada { get; set; }
}

public sealed class ReportarDispositivoConexionWebResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DispositivoConexionId { get; set; }
    public DateTime UltimoLatidoUtc { get; set; }
    public DateTime ConsideradoConectadoHastaUtc { get; set; }
    public bool UbicacionActualizada { get; set; }
}

public sealed class DesconectarDispositivoConexionWebRequest
{
    public string InstalacionId { get; set; } = string.Empty;
    public string SesionId { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class DesconectarDispositivoConexionWebResponse
{
    public bool Success { get; set; }
    public bool Actualizado { get; set; }
    public string Message { get; set; } = string.Empty;
}
