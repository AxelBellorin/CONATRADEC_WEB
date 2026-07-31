namespace CONATRADEC.AdminWeb.Models;

/// <summary>
/// Elemento sencillo utilizado para mostrar catálogos
/// con identificador y nombre, por ejemplo departamentos.
/// </summary>
public sealed class CatalogoSimple
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
}
