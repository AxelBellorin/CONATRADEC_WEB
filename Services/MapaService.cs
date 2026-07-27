using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class MapaService
{
    private readonly ApiClientService apiClient;

    public MapaService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<ResultadoDatos<List<TerrenoMapaItem>>> ObtenerTerrenosAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            /*
             * Endpoint liviano recomendado para el mapa:
             * GET api/mapa/terrenos
             *
             * No debe devolver fotografías ni análisis completos.
             */
            var datos = await apiClient.GetAsync<List<TerrenoMapaItem>>(
                "api/mapa/terrenos",
                cancellationToken);

            return new ResultadoDatos<List<TerrenoMapaItem>>(
                datos ?? []);
        }
        catch (Exception)
        {
            return new ResultadoDatos<List<TerrenoMapaItem>>(
                CrearDatosDemostracion(),
                "El mapa utiliza puntos demostrativos hasta que agreguemos el endpoint api/mapa/terrenos al backend.");
        }
    }

    private static List<TerrenoMapaItem> CrearDatosDemostracion() =>
    [
        new()
        {
            TerrenoId = 1,
            Codigo = "DEMO-MAT-001",
            Nombre = "Terreno demostrativo Matagalpa",
            Productor = "Dato demostrativo",
            Latitud = 12.9256,
            Longitud = -85.9175,
            Departamento = "Matagalpa",
            Municipio = "Matagalpa",
            ExtensionManzanas = 8.5m,
            Estado = "Normal",
            UltimoPh = 5.8m
        },
        new()
        {
            TerrenoId = 2,
            Codigo = "DEMO-JIN-001",
            Nombre = "Terreno demostrativo Jinotega",
            Productor = "Dato demostrativo",
            Latitud = 13.0910,
            Longitud = -86.0004,
            Departamento = "Jinotega",
            Municipio = "Jinotega",
            ExtensionManzanas = 12m,
            Estado = "Atención",
            UltimoPh = 4.9m
        }
    ];
}
