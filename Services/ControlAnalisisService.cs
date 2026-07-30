using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CONATRADEC.AdminWeb.Models;

namespace CONATRADEC.AdminWeb.Services;

public sealed class ControlAnalisisService
{
    private readonly ApiClientService apiClient;
    private readonly AuthStateService authState;

    public ControlAnalisisService(
        ApiClientService apiClient,
        AuthStateService authState)
    {
        this.apiClient = apiClient;
        this.authState = authState;
    }

    public async Task EliminarAsync(
        int analisisSueloId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        await EnviarMotivoAsync(
            $"api/control-analisis/{analisisSueloId}/eliminar",
            motivo,
            cancellationToken);
    }

    public async Task RecuperarAsync(
        int analisisSueloId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        await EnviarMotivoAsync(
            $"api/control-analisis/{analisisSueloId}/recuperar",
            motivo,
            cancellationToken);
    }

    public async Task<ControlAnalisisEstadoEliminacion?> ObtenerEstadoEliminacionAsync(
        int analisisSueloId,
        CancellationToken cancellationToken = default)
    {
        AuditoriaApiRespuesta<ControlAnalisisEstadoEliminacion>? respuesta =
            await apiClient.GetAsync<AuditoriaApiRespuesta<ControlAnalisisEstadoEliminacion>>(
                $"api/control-analisis/{analisisSueloId}/estado-eliminacion",
                authState.CrearEncabezadosSesion(),
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task<string> ObtenerPdfDataUriAsync(
        int analisisSueloCalculoId,
        CancellationToken cancellationToken = default)
    {
        UsuarioSesion usuario = authState.Usuario ??
            throw new UnauthorizedAccessException("No existe una sesión activa.");

        using HttpClient client = new()
        {
            BaseAddress = apiClient.BaseAddress,
            Timeout = TimeSpan.FromMinutes(2)
        };

        if (!string.IsNullOrWhiteSpace(usuario.Token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", usuario.Token);
        }

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Usuario-Id",
            usuario.UsuarioId.ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Version-Sesion",
            usuario.VersionSesion.ToString());

        byte[] bytes = await client.GetByteArrayAsync(
            $"api/control-analisis/{analisisSueloCalculoId}/pdf",
            cancellationToken);

        return $"data:application/pdf;base64,{Convert.ToBase64String(bytes)}";
    }

    private async Task EnviarMotivoAsync(
        string ruta,
        string motivo,
        CancellationToken cancellationToken)
    {
        UsuarioSesion usuario = authState.Usuario ??
            throw new UnauthorizedAccessException("No existe una sesión activa.");

        using HttpClient client = new()
        {
            BaseAddress = apiClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60)
        };

        if (!string.IsNullOrWhiteSpace(usuario.Token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", usuario.Token);
        }

        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Usuario-Id",
            usuario.UsuarioId.ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Version-Sesion",
            usuario.VersionSesion.ToString());

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            ruta,
            new ControlAnalisisMotivoRequest { Motivo = motivo },
            cancellationToken);

        string contenido = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        string mensaje = contenido;
        try
        {
            using JsonDocument document = JsonDocument.Parse(contenido);
            if (document.RootElement.TryGetProperty("message", out JsonElement value))
                mensaje = value.GetString() ?? contenido;
        }
        catch (JsonException)
        {
        }

        throw new HttpRequestException(mensaje.Trim('"'), null, response.StatusCode);
    }
}
