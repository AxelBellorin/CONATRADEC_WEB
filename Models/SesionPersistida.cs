using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class SesionPersistida
{
    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("nombreUsuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [JsonPropertyName("nombreCompletoUsuario")]
    public string NombreCompletoUsuario { get; set; } = string.Empty;

    [JsonPropertyName("correoUsuario")]
    public string CorreoUsuario { get; set; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("rolNombre")]
    public string RolNombre { get; set; } = string.Empty;

    [JsonPropertyName("procedenciaId")]
    public int ProcedenciaId { get; set; }

    [JsonPropertyName("procedenciaNombre")]
    public string ProcedenciaNombre { get; set; } = string.Empty;

    [JsonPropertyName("esInterno")]
    public bool EsInterno { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("urlImagenUsuario")]
    public string UrlImagenUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Debe conservarse en localStorage para que todas las llamadas
    /// autenticadas envíen la misma versión recibida durante el login.
    /// </summary>
    [JsonPropertyName("versionSesion")]
    public int VersionSesion { get; set; }

    [JsonPropertyName("permisos")]
    public List<PermisoPersistido> Permisos { get; set; } = [];

    public static SesionPersistida DesdeUsuario(
        UsuarioSesion usuario)
    {
        return new SesionPersistida
        {
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompletoUsuario =
                usuario.NombreCompletoUsuario,
            CorreoUsuario = usuario.CorreoUsuario,
            Activo = usuario.Activo,
            RolId = usuario.RolId,
            RolNombre = usuario.RolNombre,
            ProcedenciaId = usuario.ProcedenciaId,
            ProcedenciaNombre =
                usuario.ProcedenciaNombre,
            EsInterno = usuario.EsInterno,
            Token = usuario.Token ?? string.Empty,
            UrlImagenUsuario =
                usuario.UrlImagenUsuario,
            VersionSesion = usuario.VersionSesion,
            Permisos = usuario.Permisos
                .Select(item =>
                    new PermisoPersistido
                    {
                        InterfazId =
                            item.InterfazId,
                        NombreInterfaz =
                            item.NombreInterfaz,
                        Leer =
                            item.Leer == true,
                        Agregar =
                            item.Agregar == true,
                        Actualizar =
                            item.Actualizar == true,
                        Eliminar =
                            item.Eliminar == true
                    })
                .ToList()
        };
    }

    public UsuarioSesion AUsuarioSesion()
    {
        return new UsuarioSesion
        {
            UsuarioId = UsuarioId,
            NombreUsuario = NombreUsuario,
            NombreCompletoUsuario =
                NombreCompletoUsuario,
            CorreoUsuario = CorreoUsuario,
            Activo = Activo,
            RolId = RolId,
            RolNombre = RolNombre,
            ProcedenciaId = ProcedenciaId,
            ProcedenciaNombre =
                ProcedenciaNombre,
            EsInterno = EsInterno,
            Token = Token,
            UrlImagenUsuario =
                UrlImagenUsuario,
            VersionSesion = VersionSesion,
            Permisos = Permisos
                .Select(item =>
                    new PermisoInterfaz
                    {
                        InterfazId =
                            item.InterfazId,
                        NombreInterfaz =
                            item.NombreInterfaz,
                        Leer = item.Leer,
                        Agregar = item.Agregar,
                        Actualizar =
                            item.Actualizar,
                        Eliminar =
                            item.Eliminar
                    })
                .ToList()
        };
    }
}

public sealed class PermisoPersistido
{
    [JsonPropertyName("interfazId")]
    public int InterfazId { get; set; }

    [JsonPropertyName("nombreInterfaz")]
    public string NombreInterfaz { get; set; } = string.Empty;

    [JsonPropertyName("leer")]
    public bool Leer { get; set; }

    [JsonPropertyName("agregar")]
    public bool Agregar { get; set; }

    [JsonPropertyName("actualizar")]
    public bool Actualizar { get; set; }

    [JsonPropertyName("eliminar")]
    public bool Eliminar { get; set; }
}
