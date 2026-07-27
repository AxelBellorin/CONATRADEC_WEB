using CONATRADEC.AdminWeb.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class BrowserSessionService
{
    private const string ClaveSesion =
        "conatradec.portal.session";

    private readonly IJSRuntime jsRuntime;

    private readonly JsonSerializerOptions jsonOptions =
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

        string json =
            JsonSerializer.Serialize(
                usuario,
                jsonOptions);

        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            ClaveSesion,
            json);
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
            return JsonSerializer.Deserialize<UsuarioSesion>(
                json,
                jsonOptions);
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
    }
}
