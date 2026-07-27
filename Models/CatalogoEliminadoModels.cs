using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class CatalogoEliminadoRespuesta
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<CatalogoEliminadoItem> Data { get; set; } = [];
}

public sealed class CatalogoEliminadoItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("catalogo")]
    public string Catalogo { get; set; } = string.Empty;

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("subtitulo")]
    public string Subtitulo { get; set; } = string.Empty;

    [JsonPropertyName("detalle")]
    public string Detalle { get; set; } = string.Empty;

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}
