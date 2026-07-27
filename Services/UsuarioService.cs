using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class UsuarioService
{
    private readonly ApiClientService apiClient;

    public UsuarioService(ApiClientService apiClient)
    {
        this.apiClient = apiClient;
    }

    public async Task<List<UsuarioListadoItem>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await apiClient.GetAsync<List<UsuarioListadoItem>>(
            "api/usuarios/listar",
            cancellationToken) ?? [];
    }

    public string ConstruirUrlImagen(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
            return string.Empty;

        if (Uri.TryCreate(ruta, UriKind.Absolute, out var absoluta))
            return absoluta.ToString();

        return new Uri(apiClient.BaseAddress!, ruta.TrimStart('/')).ToString();
    }
}
