using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class UsuariosInactivosService
{
    private readonly ApiClientService apiClient;
    private readonly AuthStateService authState;

    public UsuariosInactivosService(
        ApiClientService apiClient,
        AuthStateService authState)
    {
        this.apiClient = apiClient;
        this.authState = authState;
    }

    public async Task<List<CatalogoEliminadoItem>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        CatalogoEliminadoRespuesta? respuesta =
            await apiClient.GetAsync<CatalogoEliminadoRespuesta>(
                "api/catalogos-eliminados/usuario",
                Encabezados(),
                cancellationToken);

        return respuesta?.Data ?? [];
    }

    public Task ReactivarAsync(
        int usuarioId,
        CancellationToken cancellationToken = default) =>
        apiClient.PutSinContenidoAsync(
            $"api/catalogos-eliminados/usuario/{usuarioId}/reactivar",
            Encabezados(),
            cancellationToken);

    private IReadOnlyDictionary<string, string>
        Encabezados() =>
        authState.CrearEncabezadosSesion();
}
