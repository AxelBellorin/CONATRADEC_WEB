namespace CONATRADEC.AdminWeb.Models;

public class ResultadoPaginado<T>
{
    public List<T> Items { get; set; } = [];
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
    public bool TienePaginaAnterior { get; set; }
    public bool TienePaginaSiguiente { get; set; }
}

public sealed class ResumenSeguimientoAlertas
{
    public int Total { get; set; }
    public int Pendientes { get; set; }
    public int EnProceso { get; set; }
    public int Atendidas { get; set; }
    public int Descartadas { get; set; }
    public int Cerradas { get; set; }
}

public sealed class SeguimientosPaginados
    : ResultadoPaginado<SeguimientoAlertaItem>
{
    public ResumenSeguimientoAlertas Resumen { get; set; } = new();
}
