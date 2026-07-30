using CONATRADEC.AdminWeb.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class BrowserSessionService
{
    /*
     * v5 incorpora JWT, expiración absoluta y tiempo de inactividad.
     */
    private const string ClaveSesion =
        "conatradec.portal.session.v5";

    private static readonly string[] ClavesAnteriores =
    [
        "conatradec.portal.session.v4",
        "conatradec.portal.session.v3"
    ];

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

        await EliminarClavesAnterioresAsync();
    }

    public async Task<UsuarioSesion?> LeerAsync()
    {
        string? json =
            await jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                ClaveSesion);

        if (string.IsNullOrWhiteSpace(json))
        {
            await EliminarClavesAnterioresAsync();
            return null;
        }

        try
        {
            SesionPersistida? sesion =
                JsonSerializer.Deserialize<SesionPersistida>(
                    json,
                    opciones);

            if (sesion is null ||
                sesion.VersionSesion <= 0 ||
                string.IsNullOrWhiteSpace(sesion.Token) ||
                !sesion.ExpiraTokenUtc.HasValue ||
                sesion.ExpiraTokenUtc.Value <= DateTime.UtcNow)
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

        await EliminarClavesAnterioresAsync();
    }

    private async ValueTask EliminarClavesAnterioresAsync()
    {
        foreach (string clave in ClavesAnteriores)
        {
            await jsRuntime.InvokeVoidAsync(
                "localStorage.removeItem",
                clave);
        }
    }
}
