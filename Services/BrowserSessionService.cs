using CONATRADEC.AdminWeb.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class BrowserSessionService
{
    /*
     * Se cambia a v4 porque las sesiones v3 no guardaban VersionSesion.
     * Así se obliga únicamente una vez a iniciar sesión de nuevo después
     * de publicar esta corrección.
     */
    private const string ClaveSesion =
        "conatradec.portal.session.v4";

    private const string ClaveSesionAnterior =
        "conatradec.portal.session.v3";

    private readonly IJSRuntime jsRuntime;

    private readonly JsonSerializerOptions opciones =
        new(JsonSerializerDefaults.Web);

    public BrowserSessionService(
        IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    public async Task GuardarAsync(
        UsuarioSesion usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        SesionPersistida sesion =
            SesionPersistida.DesdeUsuario(usuario);

        string json =
            JsonSerializer.Serialize(
                sesion,
                opciones);

        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            ClaveSesion,
            json);

        // La sesión antigua ya no es compatible con VersionSesion.
        await jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            ClaveSesionAnterior);
    }

    public async Task<UsuarioSesion?> LeerAsync()
    {
        string? json =
            await jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                ClaveSesion);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            SesionPersistida? sesion =
                JsonSerializer.Deserialize<
                    SesionPersistida>(
                    json,
                    opciones);

            if (sesion is null ||
                sesion.VersionSesion <= 0)
            {
                await EliminarAsync();
                return null;
            }

            return sesion.AUsuarioSesion();
        }
        catch (JsonException)
        {
            await EliminarAsync();
            return null;
        }
    }

    public async ValueTask EliminarAsync()
    {
        await jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            ClaveSesion);

        await jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            ClaveSesionAnterior);
    }
}
