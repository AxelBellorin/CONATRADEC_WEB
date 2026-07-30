using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class ControlAnalisisMotivoRequest
{
    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class ControlAnalisisEstadoEliminacion
{
    [JsonPropertyName("eliminacionId")]
    public long EliminacionId { get; set; }

    [JsonPropertyName("fechaEliminacionUtc")]
    public DateTime FechaEliminacionUtc { get; set; }

    [JsonPropertyName("motivoEliminacion")]
    public string MotivoEliminacion { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("fechaRecuperacionUtc")]
    public DateTime? FechaRecuperacionUtc { get; set; }

    [JsonPropertyName("motivoRecuperacion")]
    public string? MotivoRecuperacion { get; set; }

    [JsonPropertyName("tieneManifiesto")]
    public bool TieneManifiesto { get; set; }

    [JsonPropertyName("tienePdf")]
    public bool TienePdf { get; set; }
}
