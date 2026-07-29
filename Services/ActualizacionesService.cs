using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class ActualizacionesService
{
    public const long TamanoMaximoArchivo =
        1024L * 1024L * 1024L;

    private readonly HttpClient httpClient;
    private readonly AuthStateService authState;

    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web);

    public ActualizacionesService(
        HttpClient httpClient,
        AuthStateService authState)
    {
        this.httpClient = httpClient;
        this.authState = authState;
    }

    public async Task<List<ActualizacionWebItem>> ListarAsync(
        int usuarioId,
        string? plataforma = null,
        string? canal = null,
        string? estado = null,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>();

        AgregarParametro(parametros, "plataforma", plataforma);
        AgregarParametro(parametros, "canal", canal);
        AgregarParametro(parametros, "estado", estado);

        string ruta = "api/actualizaciones/administrar";

        if (parametros.Count > 0)
            ruta += "?" + string.Join("&", parametros);

        RespuestaApi<List<ActualizacionWebItem>>? respuesta =
            await EnviarAsync<RespuestaApi<List<ActualizacionWebItem>>>(
                HttpMethod.Get,
                ruta,
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data ?? [];
    }

    public async Task<SiguienteVersionWeb?> ObtenerSiguienteAsync(
        int usuarioId,
        string plataforma,
        string canal,
        CancellationToken cancellationToken = default)
    {
        string ruta =
            "api/actualizaciones/siguiente-version" +
            $"?plataforma={Uri.EscapeDataString(plataforma)}" +
            $"&canal={Uri.EscapeDataString(canal)}";

        RespuestaApi<SiguienteVersionWeb>? respuesta =
            await EnviarAsync<RespuestaApi<SiguienteVersionWeb>>(
                HttpMethod.Get,
                ruta,
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task<ActualizacionWebItem?> SubirAsync(
        int usuarioId,
        ActualizacionNuevaWeb modelo,
        Func<ProgresoSubidaArchivo, Task>? reportarProgreso = null,
        CancellationToken cancellationToken = default)
    {
        if (modelo.Archivo == null)
        {
            throw new InvalidOperationException(
                "Debe seleccionar un archivo.");
        }

        using var contenido =
            new MultipartFormDataContent();

        await using Stream stream =
            modelo.Archivo.OpenReadStream(
                TamanoMaximoArchivo,
                cancellationToken);

        using var archivoContenido =
            new ProgressStreamContent(
                stream,
                modelo.Archivo.Size,
                reportarProgreso,
                cancellationToken);

        if (!string.IsNullOrWhiteSpace(
                modelo.Archivo.ContentType))
        {
            archivoContenido.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    modelo.Archivo.ContentType);
        }

        contenido.Add(
            archivoContenido,
            "Archivo",
            modelo.Archivo.Name);

        contenido.Add(
            new StringContent(modelo.Plataforma),
            "Plataforma");

        contenido.Add(
            new StringContent(modelo.Canal),
            "Canal");

        contenido.Add(
            new StringContent(modelo.VersionNombre),
            "VersionNombre");

        contenido.Add(
            new StringContent(
                modelo.VersionCodigo.ToString(
                    CultureInfo.InvariantCulture)),
            "VersionCodigo");

        contenido.Add(
            new StringContent(
                modelo.NotasVersion ??
                string.Empty),
            "NotasVersion");

        contenido.Add(
            new StringContent(
                modelo.Obligatoria.ToString(
                    CultureInfo.InvariantCulture)),
            "Obligatoria");

        if (modelo.DefinirVersionMinima &&
            modelo.VersionMinimaCodigo.HasValue)
        {
            contenido.Add(
                new StringContent(
                    modelo.VersionMinimaCodigo.Value.ToString(
                        CultureInfo.InvariantCulture)),
                "VersionMinimaCodigo");
        }

        await ReportarAsync(
            reportarProgreso,
            new ProgresoSubidaArchivo(
                0,
                modelo.Archivo.Size,
                0,
                "Preparando la transferencia..."));

        RespuestaApi<ActualizacionWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<ActualizacionWebItem>>(
                HttpMethod.Post,
                "api/actualizaciones/subir",
                usuarioId,
                contenido,
                cancellationToken);

        await ReportarAsync(
            reportarProgreso,
            new ProgresoSubidaArchivo(
                modelo.Archivo.Size,
                modelo.Archivo.Size,
                100,
                "Carga completada."));

        return respuesta?.Data;
    }

    public Task<ActualizacionWebItem?> PublicarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default) =>
        EjecutarAccionAsync(
            usuarioId,
            id,
            "publicar",
            cancellationToken);

    public Task<ActualizacionWebItem?> RevocarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default) =>
        EjecutarAccionAsync(
            usuarioId,
            id,
            "revocar",
            cancellationToken);

    public async Task<ActualizacionWebItem?> ConfigurarAsync(
        int usuarioId,
        int id,
        ActualizacionConfiguracionWeb modelo,
        CancellationToken cancellationToken = default)
    {
        using JsonContent contenido =
            JsonContent.Create(modelo);

        RespuestaApi<ActualizacionWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<ActualizacionWebItem>>(
                HttpMethod.Put,
                $"api/actualizaciones/{id}/configuracion",
                usuarioId,
                contenido,
                cancellationToken);

        return respuesta?.Data;
    }

    public async Task EliminarAsync(
        int usuarioId,
        int id,
        CancellationToken cancellationToken = default)
    {
        await EnviarAsync<object>(
            HttpMethod.Delete,
            $"api/actualizaciones/{id}",
            usuarioId,
            null,
            cancellationToken);
    }

    private async Task<ActualizacionWebItem?>
        EjecutarAccionAsync(
            int usuarioId,
            int id,
            string accion,
            CancellationToken cancellationToken)
    {
        RespuestaApi<ActualizacionWebItem>? respuesta =
            await EnviarAsync<RespuestaApi<ActualizacionWebItem>>(
                HttpMethod.Put,
                $"api/actualizaciones/{id}/{accion}",
                usuarioId,
                null,
                cancellationToken);

        return respuesta?.Data;
    }

    private async Task<T?> EnviarAsync<T>(
        HttpMethod metodo,
        string ruta,
        int usuarioId,
        HttpContent? contenido,
        CancellationToken cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                metodo,
                ruta);

        /*
         * VersionSesionMiddleware exige ambas cabeceras cuando se identifica
         * al usuario. Antes este servicio enviaba solamente X-Usuario-Id y la
         * API interpretaba todas las llamadas como una sesión antigua.
         */
        IReadOnlyDictionary<string, string> encabezados =
            authState.CrearEncabezadosSesion();

        string usuarioSesion =
            encabezados["X-Usuario-Id"];

        if (!string.Equals(
                usuarioSesion,
                usuarioId.ToString(
                    CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                "La sesión activa no corresponde al usuario solicitado.");
        }

        foreach (
            KeyValuePair<string, string> encabezado
            in encabezados)
        {
            request.Headers.TryAddWithoutValidation(
                encabezado.Key,
                encabezado.Value);
        }

        request.Content =
            contenido;

        using HttpResponseMessage response =
            await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        string texto =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        /*
         * También procesa una invalidación real de sesión para conservar el
         * mismo comportamiento del resto del portal: limpiar sesión y volver
         * automáticamente al login.
         */
        if (EsSesionInvalidada(
                response,
                texto))
        {
            await authState
                .InvalidarSesionDesdeApiAsync();

            throw new UnauthorizedAccessException(
                ExtraerMensaje(texto));
        }

        if (!response.IsSuccessStatusCode)
        {
            string mensaje =
                ExtraerMensaje(texto);

            if (response.StatusCode is
                HttpStatusCode.Unauthorized or
                HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    mensaje);
            }

            throw new HttpRequestException(
                mensaje,
                null,
                response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(texto))
            return default;

        return JsonSerializer.Deserialize<T>(
            texto,
            jsonOptions);
    }

    private static bool EsSesionInvalidada(
        HttpResponseMessage response,
        string contenido)
    {
        bool porEncabezado =
            response.Headers.TryGetValues(
                "X-Sesion-Invalidada",
                out IEnumerable<string>? valores) &&
            valores.Any(valor =>
                string.Equals(
                    valor,
                    "true",
                    StringComparison.OrdinalIgnoreCase));

        if (porEncabezado)
            return true;

        if (string.IsNullOrWhiteSpace(contenido))
            return false;

        return contenido.Contains(
            "SESSION_INVALIDATED",
            StringComparison.OrdinalIgnoreCase);
    }

    private static void AgregarParametro(
        ICollection<string> parametros,
        string nombre,
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return;

        parametros.Add(
            $"{nombre}={Uri.EscapeDataString(valor.Trim())}");
    }

    private static string ExtraerMensaje(
        string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return
                "La API no devolvió detalles del error.";
        }

        try
        {
            using JsonDocument documento =
                JsonDocument.Parse(texto);

            JsonElement raiz =
                documento.RootElement;

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
                    valor.ValueKind ==
                        JsonValueKind.String)
                {
                    return valor.GetString() ??
                           texto;
                }
            }
        }
        catch (JsonException)
        {
            // La API también puede devolver texto plano.
        }

        return texto.Trim().Trim('"');
    }

    private static Task ReportarAsync(
        Func<ProgresoSubidaArchivo, Task>? reportar,
        ProgresoSubidaArchivo progreso) =>
        reportar?.Invoke(progreso) ?? Task.CompletedTask;

    private sealed class ProgressStreamContent : HttpContent
    {
        private const int TamanoBuffer = 256 * 1024;

        private readonly Stream origen;
        private readonly long longitud;
        private readonly Func<ProgresoSubidaArchivo, Task>? reportar;
        private readonly CancellationToken cancellationTokenInicial;

        public ProgressStreamContent(
            Stream origen,
            long longitud,
            Func<ProgresoSubidaArchivo, Task>? reportar,
            CancellationToken cancellationTokenInicial)
        {
            this.origen = origen;
            this.longitud = longitud;
            this.reportar = reportar;
            this.cancellationTokenInicial = cancellationTokenInicial;

            Headers.ContentLength = longitud;
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) =>
            CopiarAsync(
                stream,
                cancellationTokenInicial);

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken) =>
            CopiarAsync(
                stream,
                cancellationToken);

        protected override bool TryComputeLength(
            out long length)
        {
            length = longitud;
            return true;
        }

        private async Task CopiarAsync(
            Stream destino,
            CancellationToken cancellationToken)
        {
            byte[] buffer =
                ArrayPool<byte>.Shared.Rent(TamanoBuffer);

            long enviados = 0;
            int ultimoPorcentaje = -1;

            try
            {
                while (true)
                {
                    int leidos = await origen.ReadAsync(
                        buffer.AsMemory(0, TamanoBuffer),
                        cancellationToken);

                    if (leidos == 0)
                        break;

                    await destino.WriteAsync(
                        buffer.AsMemory(0, leidos),
                        cancellationToken);

                    enviados += leidos;

                    int porcentaje = longitud <= 0
                        ? 0
                        : (int)Math.Min(
                            99,
                            enviados * 100L / longitud);

                    if (porcentaje <= ultimoPorcentaje)
                        continue;

                    ultimoPorcentaje = porcentaje;

                    await ReportarAsync(
                        reportar,
                        new ProgresoSubidaArchivo(
                            enviados,
                            longitud,
                            porcentaje,
                            "Subiendo el archivo al servidor..."));
                }

                await destino.FlushAsync(cancellationToken);

                await ReportarAsync(
                    reportar,
                    new ProgresoSubidaArchivo(
                        longitud,
                        longitud,
                        100,
                        "Archivo transferido. Calculando SHA-256 y guardando el borrador..."));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}

public sealed record ProgresoSubidaArchivo(
    long BytesEnviados,
    long TotalBytes,
    int Porcentaje,
    string Estado);
