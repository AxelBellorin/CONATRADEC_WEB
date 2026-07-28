using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class DispositivosConexionService
{
    private readonly ApiClientService apiClient;
    private readonly AuthStateService authState;

    public DispositivosConexionService(
        ApiClientService apiClient,
        AuthStateService authState)
    {
        this.apiClient = apiClient;
        this.authState = authState;
    }

    public async Task<DispositivosConexionResumen> ObtenerResumenAsync(
        int minutosActivo = 2,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<DispositivosConexionResumen>(
            $"api/dispositivos-conectados/resumen?minutosActivo={Math.Clamp(minutosActivo, 1, 15)}",
            Encabezados(),
            cancellationToken) ?? new();

    public async Task<DispositivosConexionPaginada> ListarAsync(
        DispositivoConexionFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>
        {
            $"minutosActivo={Math.Clamp(filtro.MinutosActivo, 1, 15)}",
            $"pagina={Math.Max(1, filtro.Pagina)}",
            $"tamanoPagina={Math.Clamp(filtro.TamanoPagina, 10, 100)}"
        };

        if (filtro.Estado == "conectados")
            parametros.Add("conectado=true");
        else if (filtro.Estado == "desconectados")
            parametros.Add("conectado=false");

        if (filtro.Ubicacion == "con")
            parametros.Add("conUbicacion=true");
        else if (filtro.Ubicacion == "sin")
            parametros.Add("conUbicacion=false");

        Agregar(
            parametros,
            "plataforma",
            filtro.Plataforma);

        Agregar(
            parametros,
            "versionApp",
            filtro.VersionApp);

        Agregar(
            parametros,
            "buscar",
            filtro.Buscar);

        string ruta =
            "api/dispositivos-conectados?" +
            string.Join("&", parametros);

        return await apiClient.GetAsync<DispositivosConexionPaginada>(
            ruta,
            Encabezados(),
            cancellationToken) ?? new();
    }

    public async Task<List<DispositivoConexionMapaItem>> ObtenerMapaAsync(
        bool soloConectados = true,
        int minutosActivo = 2,
        int limite = 1000,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<DispositivoConexionMapaItem>>(
            "api/dispositivos-conectados/mapa" +
            $"?soloConectados={soloConectados.ToString().ToLowerInvariant()}" +
            $"&minutosActivo={Math.Clamp(minutosActivo, 1, 15)}" +
            $"&limite={Math.Clamp(limite, 1, 2000)}",
            Encabezados(),
            cancellationToken) ?? [];

    public async Task<DispositivoConexionItem?> ObtenerAsync(
        int id,
        int minutosActivo = 2,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<DispositivoConexionItem>(
            $"api/dispositivos-conectados/{id}?minutosActivo={Math.Clamp(minutosActivo, 1, 15)}",
            Encabezados(),
            cancellationToken);

    private static void Agregar(
        ICollection<string> parametros,
        string nombre,
        string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            parametros.Add(
                $"{nombre}={Uri.EscapeDataString(valor.Trim())}");
        }
    }

    private IReadOnlyDictionary<string, string>
        Encabezados() =>
        authState.CrearEncabezadosSesion();
}
