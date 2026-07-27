namespace CONATRADEC.AdminWeb.Models;

public sealed class DashboardResumen
{
    public int TotalTerrenos { get; set; }
    public int TotalAnalisis { get; set; }
    public int UsuariosActivos { get; set; }
    public int TotalDiagnosticos { get; set; }
    public bool ApiDisponible { get; set; }
    public List<AnalisisMesItem> AnalisisPorMes { get; set; } = [];

    public static DashboardResumen CrearDemostracion() =>
        new()
        {
            TotalTerrenos = 0,
            TotalAnalisis = 0,
            UsuariosActivos = 0,
            TotalDiagnosticos = 0,
            ApiDisponible = false,
            AnalisisPorMes =
            [
                new("Feb", 0),
                new("Mar", 0),
                new("Abr", 0),
                new("May", 0),
                new("Jun", 0),
                new("Jul", 0)
            ]
        };
}

public sealed record AnalisisMesItem(string Mes, int Cantidad);

public sealed record ResultadoDatos<T>(T Datos, string? Mensaje = null);
