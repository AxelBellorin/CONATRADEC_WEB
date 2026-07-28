using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public static class PermisosWeb
{
    public const string Portal =
        "PortalAdministrativoWeb";

    public const string Roles =
        "AdministrarRolesWeb";

    public const string Matriz =
        "AdministrarMatrizPermisosWeb";

    public const string Alertas =
        "CentroAlertasWeb";

    public const string SeguimientoAlertas =
        "SeguimientoAlertasWeb";

    public const string ConfiguracionAlertas =
        "ConfiguracionAlertasWeb";
}

public sealed class RolWebItem
{
    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } =
        string.Empty;

    [JsonPropertyName("descripcionRol")]
    public string DescripcionRol { get; set; } =
        string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("esAdministrador")]
    public bool EsAdministrador { get; set; }
}

public sealed class RolFormularioWeb
{
    [Required(
        ErrorMessage =
            "El nombre del rol es obligatorio.")]
    [MaxLength(50)]
    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } =
        string.Empty;

    [MaxLength(500)]
    [JsonPropertyName("descripcionRol")]
    public string DescripcionRol { get; set; } =
        string.Empty;
}

public sealed class RolLiteWeb
{
    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("nombreRol")]
    public string NombreRol { get; set; } =
        string.Empty;

    [JsonPropertyName("esAdministrador")]
    public bool EsAdministrador { get; set; }
}

public sealed class InterfazPermisoWeb
{
    [JsonPropertyName("interfazId")]
    public int InterfazId { get; set; }

    [JsonPropertyName("nombreInterfaz")]
    public string NombreInterfaz { get; set; } =
        string.Empty;

    [JsonPropertyName("nombreAmigableInterfaz")]
    public string NombreAmigableInterfaz
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("leer")]
    public bool Leer { get; set; }

    [JsonPropertyName("agregar")]
    public bool Agregar { get; set; }

    [JsonPropertyName("actualizar")]
    public bool Actualizar { get; set; }

    [JsonPropertyName("eliminar")]
    public bool Eliminar { get; set; }

    [JsonIgnore]
    public string NombreVisible =>
        string.IsNullOrWhiteSpace(
            NombreAmigableInterfaz)
                ? NombreInterfaz
                : NombreAmigableInterfaz;
}

public sealed class RolConPermisosWeb
{
    [JsonPropertyName("rol")]
    public RolLiteWeb Rol { get; set; } =
        new();

    [JsonPropertyName("interfaz")]
    public List<InterfazPermisoWeb> Interfaz
    {
        get;
        set;
    } = [];
}
