using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

/// <summary>
/// Obtiene la última versión publicada utilizando el endpoint público que ya
/// consume la aplicación. El portal usa este servicio para mantener enlaces
/// estables, aunque cambie el identificador de la versión publicada.
/// </summary>
public sealed class DescargasPublicasService
{
    private readonly HttpClient httpClient;

    public DescargasPublicasService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<DescargaPublicaWeb?> ObtenerUltimaAsync(
        string plataforma,
        string canal = "PRODUCCION",
        CancellationToken cancellationToken = default)
    {
        string plataformaNormalizada =
            NormalizarPlataforma(plataforma);

        string canalNormalizado =
            NormalizarCanal(canal);

        if (string.IsNullOrWhiteSpace(plataformaNormalizada))
        {
            throw new ArgumentException(
                "La plataforma debe ser Android o Windows.",
                nameof(plataforma));
        }

        if (string.IsNullOrWhiteSpace(canalNormalizado))
        {
            throw new ArgumentException(
                "El canal debe ser Producción o Pruebas.",
                nameof(canal));
        }

        string ruta =
            "api/actualizaciones/comprobar" +
            $"?plataforma={Uri.EscapeDataString(plataformaNormalizada)}" +
            "&versionCodigo=0" +
            $"&canal={Uri.EscapeDataString(canalNormalizado)}";

        using HttpResponseMessage response =
            await httpClient.GetAsync(
                ruta,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        string contenido =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                ExtraerMensaje(contenido),
                null,
                response.StatusCode);
        }

        using JsonDocument document =
            JsonDocument.Parse(contenido);

        JsonElement raiz =
            document.RootElement;

        bool disponible =
            raiz.TryGetProperty(
                "actualizacionDisponible",
                out JsonElement disponibleElement) &&
            (disponibleElement.ValueKind is
                JsonValueKind.True or
                JsonValueKind.False) &&
            disponibleElement.GetBoolean();

        if (!disponible ||
            !raiz.TryGetProperty(
                "data",
                out JsonElement data) ||
            data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        long actualizacionId =
            LeerEntero(
                data,
                "actualizacionAplicacionId");

        string urlDescarga =
            LeerTexto(data, "urlDescarga");

        Uri urlAbsoluta =
            ResolverUrlDescarga(
                actualizacionId,
                urlDescarga);

        return new DescargaPublicaWeb
        {
            ActualizacionAplicacionId = actualizacionId,
            Plataforma =
                LeerTexto(data, "plataforma", plataformaNormalizada),
            Canal =
                LeerTexto(data, "canal", canalNormalizado),
            VersionNombre =
                LeerTexto(data, "versionNombre", "Sin versión"),
            VersionCodigo =
                LeerEntero(data, "versionCodigo"),
            NotasVersion =
                LeerTexto(data, "notasVersion"),
            NombreArchivo =
                LeerTexto(data, "nombreArchivo"),
            TipoContenido =
                LeerTexto(data, "tipoContenido"),
            TamanoBytes =
                LeerEntero(data, "tamanoBytes"),
            HashSha256 =
                LeerTexto(data, "hashSha256"),
            UrlDescarga =
                urlAbsoluta.AbsoluteUri,
            FechaPublicacionUtc =
                LeerFecha(data, "fechaPublicacionUtc")
        };
    }

    public static string NormalizarPlataforma(
        string? valor)
    {
        string normalizado =
            (valor ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        return normalizado switch
        {
            "ANDROID" => "ANDROID",
            "WINDOWS" => "WINDOWS",
            "WINUI" => "WINDOWS",
            _ => string.Empty
        };
    }

    private Uri ResolverUrlDescarga(
        long actualizacionId,
        string valorApi)
    {
        /*
         * Siempre que la API entregue el identificador, construimos la ruta
         * desde ApiSettings:BaseUrl. Esto evita que un proxy inverso genere
         * una URL con un esquema o host interno.
         */
        if (actualizacionId > 0 &&
            httpClient.BaseAddress != null)
        {
            return new Uri(
                httpClient.BaseAddress,
                $"api/actualizaciones/descargar/{actualizacionId}");
        }

        if (Uri.TryCreate(
                valorApi,
                UriKind.Absolute,
                out Uri? absoluta))
        {
            if (absoluta.Scheme is not ("http" or "https"))
            {
                throw new InvalidOperationException(
                    "La API devolvió una URL de descarga no permitida.");
            }

            return absoluta;
        }

        if (httpClient.BaseAddress == null ||
            string.IsNullOrWhiteSpace(valorApi))
        {
            throw new InvalidOperationException(
                "La versión publicada no contiene una URL de descarga válida.");
        }

        return new Uri(
            httpClient.BaseAddress,
            valorApi.TrimStart('/'));
    }

    private static string NormalizarCanal(
        string? valor)
    {
        string normalizado =
            (valor ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        return normalizado switch
        {
            "PRODUCCION" => "PRODUCCION",
            "PRODUCCIÓN" => "PRODUCCION",
            "PRUEBAS" => "PRUEBAS",
            _ => string.Empty
        };
    }

    private static string LeerTexto(
        JsonElement elemento,
        string propiedad,
        string valorPredeterminado = "")
    {
        if (!elemento.TryGetProperty(
                propiedad,
                out JsonElement valor) ||
            valor.ValueKind != JsonValueKind.String)
        {
            return valorPredeterminado;
        }

        return valor.GetString() ??
               valorPredeterminado;
    }

    private static long LeerEntero(
        JsonElement elemento,
        string propiedad)
    {
        if (!elemento.TryGetProperty(
                propiedad,
                out JsonElement valor))
        {
            return 0;
        }

        if (valor.ValueKind == JsonValueKind.Number &&
            valor.TryGetInt64(out long numero))
        {
            return numero;
        }

        return valor.ValueKind == JsonValueKind.String &&
               long.TryParse(
                   valor.GetString(),
                   out numero)
            ? numero
            : 0;
    }

    private static DateTime? LeerFecha(
        JsonElement elemento,
        string propiedad)
    {
        if (!elemento.TryGetProperty(
                propiedad,
                out JsonElement valor) ||
            valor.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.TryParse(
            valor.GetString(),
            out DateTime fecha)
            ? fecha
            : null;
    }

    private static string ExtraerMensaje(
        string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return
                "La API no devolvió detalles del error.";
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(contenido);

            JsonElement raiz =
                document.RootElement;

            foreach (string propiedad in new[]
                     {
                         "message",
                         "mensaje",
                         "title",
                         "error"
                     })
            {
                if (raiz.TryGetProperty(
                        propiedad,
                        out JsonElement valor) &&
                    valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString() ??
                           contenido;
                }
            }
        }
        catch (JsonException)
        {
            // La API también puede devolver texto plano.
        }

        return contenido.Trim().Trim('"');
    }
}

public sealed class DescargaPublicaWeb
{
    public long ActualizacionAplicacionId { get; set; }

    public string Plataforma { get; set; } = string.Empty;

    public string Canal { get; set; } = string.Empty;

    public string VersionNombre { get; set; } = string.Empty;

    public long VersionCodigo { get; set; }

    public string NotasVersion { get; set; } = string.Empty;

    public string NombreArchivo { get; set; } = string.Empty;

    public string TipoContenido { get; set; } = string.Empty;

    public long TamanoBytes { get; set; }

    public string HashSha256 { get; set; } = string.Empty;

    public string UrlDescarga { get; set; } = string.Empty;

    public DateTime? FechaPublicacionUtc { get; set; }

    public string TamanoVisible =>
        FormatearTamano(TamanoBytes);

    private static string FormatearTamano(
        long bytes)
    {
        string[] unidades =
            ["B", "KB", "MB", "GB"];

        double valor =
            Math.Max(0, bytes);

        int indice = 0;

        while (valor >= 1024 &&
               indice < unidades.Length - 1)
        {
            valor /= 1024;
            indice++;
        }

        return $"{valor:0.##} {unidades[indice]}";
    }
}
