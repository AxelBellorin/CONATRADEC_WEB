using CONATRADEC.AdminWeb.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.AdminWeb.Services;

public sealed class InspeccionFitosanitariaWebService
{
    private const long MaximoBytesFoto = 12L * 1024L * 1024L;
    private readonly ApiClientService apiClient;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AuthStateService authState;
    private readonly JsonSerializerOptions jsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public InspeccionFitosanitariaWebService(
        ApiClientService apiClient,
        IHttpClientFactory httpClientFactory,
        AuthStateService authState)
    {
        this.apiClient = apiClient;
        this.httpClientFactory = httpClientFactory;
        this.authState = authState;
    }

    public async Task<List<TipoFotografiaIAWeb>> ObtenerTiposFotografiaAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<TipoFotografiaIAWeb>>(
            "api/configuracion/tipos-fotografia-ia/activos",
            cancellationToken) ?? [];

    public async Task<List<TipoFotografiaIAWeb>> ObtenerTiposFotografiaConfiguracionAsync(
        bool incluirInactivos = true,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<TipoFotografiaIAWeb>>(
            $"api/configuracion/tipos-fotografia-ia?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}",
            cancellationToken) ?? [];

    public Task<DiagnosticoIAConfiguracionWeb> ObtenerConfiguracionIAAsync(
        CancellationToken cancellationToken = default) =>
        GetDataAsync<DiagnosticoIAConfiguracionWeb>(
            "api/diagnostico-ia/configuracion",
            cancellationToken);

    public async Task<DiagnosticoIAConfiguracionWeb> GuardarConfiguracionIAAsync(
        int maximoRevisionesGemini,
        bool revisionesIlimitadas,
        CancellationToken cancellationToken = default)
    {
        ApiEnvelopeWeb<DiagnosticoIAConfiguracionWeb>? envelope =
            await apiClient.PutAsync<object, ApiEnvelopeWeb<DiagnosticoIAConfiguracionWeb>>(
                "api/diagnostico-ia/configuracion",
                new { maximoRevisionesGemini, revisionesIlimitadas },
                cancellationToken);

        return envelope?.Data ?? throw new InvalidOperationException(
            "La API no devolvió la configuración actualizada.");
    }

    public async Task<TipoFotografiaIAWeb> CrearTipoFotografiaAsync(
        TipoFotografiaIAGuardarWeb request,
        CancellationToken cancellationToken = default)
    {
        ApiEnvelopeWeb<TipoFotografiaIAWeb>? envelope =
            await apiClient.PostAsync<TipoFotografiaIAGuardarWeb, ApiEnvelopeWeb<TipoFotografiaIAWeb>>(
                "api/configuracion/tipos-fotografia-ia",
                request,
                cancellationToken);

        return envelope?.Data ?? throw new InvalidOperationException(
            "La API no devolvió el tipo de fotografía creado.");
    }

    public async Task<TipoFotografiaIAWeb> ActualizarTipoFotografiaAsync(
        int id,
        TipoFotografiaIAGuardarWeb request,
        CancellationToken cancellationToken = default)
    {
        ApiEnvelopeWeb<TipoFotografiaIAWeb>? envelope =
            await apiClient.PutAsync<TipoFotografiaIAGuardarWeb, ApiEnvelopeWeb<TipoFotografiaIAWeb>>(
                $"api/configuracion/tipos-fotografia-ia/{id}",
                request,
                cancellationToken);

        return envelope?.Data ?? throw new InvalidOperationException(
            "La API no devolvió el tipo de fotografía actualizado.");
    }

    public Task DesactivarTipoFotografiaAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        apiClient.PutSinContenidoAsync(
            $"api/configuracion/tipos-fotografia-ia/{id}/eliminar",
            cancellationToken: cancellationToken);

    public Task RecuperarTipoFotografiaAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        apiClient.PutSinContenidoAsync(
            $"api/configuracion/tipos-fotografia-ia/{id}/recuperar",
            cancellationToken: cancellationToken);

    public async Task<InspeccionFitosanitariaBandejaWeb> ObtenerBandejaAsync(
        string modo,
        string? buscar,
        string? propietario,
        int? tecnicoId,
        string? departamento,
        string? tipoFotografia,
        string? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        DateTime? ultimaFechaUtc,
        int? ultimoId,
        int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        string modoNormalizado = (modo ?? "mis").Trim().ToLowerInvariant();
        bool revision = modoNormalizado is
            "analizador" or "analizador-disponibles" or "analizador-revisadas" or
            "aprobador" or "aprobador-disponibles" or "aprobador-revisadas";

        string rutaBase = revision
            ? "api/revision-fitosanitaria/bandeja-paginada"
            : "api/inspecciones-fitosanitarias/bandeja-paginada";

        var parametros = new List<string>
        {
            $"modo={Uri.EscapeDataString(modoNormalizado)}",
            $"tamanoPagina={Math.Clamp(tamanoPagina, 10, 50)}",
            $"desfaseHorarioMinutos={(int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes}"
        };

        Agregar(parametros, "buscar", buscar);
        Agregar(parametros, "propietario", propietario);
        Agregar(parametros, "departamento", departamento);
        Agregar(parametros, "tipoFotografia", tipoFotografia);
        Agregar(parametros, "estado", estado);

        if (tecnicoId is > 0)
            parametros.Add($"tecnicoId={tecnicoId.Value}");
        if (fechaDesde.HasValue)
            parametros.Add($"fechaDesde={fechaDesde.Value:yyyy-MM-dd}");
        if (fechaHasta.HasValue)
            parametros.Add($"fechaHasta={fechaHasta.Value:yyyy-MM-dd}");
        if (ultimaFechaUtc.HasValue && ultimoId is > 0)
        {
            parametros.Add(
                "ultimaFechaUtc=" +
                Uri.EscapeDataString(ultimaFechaUtc.Value.ToUniversalTime().ToString("O")));
            parametros.Add($"ultimoId={ultimoId.Value}");
        }

        ApiEnvelopeWeb<InspeccionFitosanitariaBandejaWeb>? envelope =
            await apiClient.GetAsync<ApiEnvelopeWeb<InspeccionFitosanitariaBandejaWeb>>(
                rutaBase + "?" + string.Join("&", parametros),
                cancellationToken);

        return envelope?.Data ?? new();
    }

    /// <summary>
    /// Toma explícitamente un expediente disponible para la etapa indicada.
    /// La API realiza la asignación de forma atómica; si otro usuario lo tomó
    /// antes, la respuesta de conflicto se propaga al cliente.
    /// </summary>
    public Task TomarExpedienteAsync(
        int inspeccionId,
        string modo,
        CancellationToken cancellationToken = default)
    {
        string etapa = (modo ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "aprobador" or "aprobador-disponibles" => "aprobador",
            _ => "analizador"
        };

        return apiClient.PostSinContenidoAsync(
            $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/tomar?modo={Uri.EscapeDataString(etapa)}",
            cancellationToken);
    }

    public async Task<InspeccionFitosanitariaDetalleWeb> ObtenerDetalleAsync(
        int inspeccionId,
        CancellationToken cancellationToken = default)
    {
        ApiEnvelopeWeb<InspeccionFitosanitariaDetalleWeb>? envelope =
            await apiClient.GetAsync<ApiEnvelopeWeb<InspeccionFitosanitariaDetalleWeb>>(
                $"api/inspecciones-fitosanitarias/{inspeccionId}",
                cancellationToken);

        return envelope?.Data ?? throw new InvalidOperationException(
            "La API no devolvió el detalle de la inspección.");
    }

    public Task<InspeccionFitosanitariaDetalleWeb> CrearAsync(
        string codigoTerreno,
        string nombreInspeccion,
        string observacion,
        IReadOnlyCollection<FotoFitosanitariaCargaWeb> fotos,
        CancellationToken cancellationToken = default) =>
        EnviarFotosAsync(
            "api/inspecciones-fitosanitarias",
            codigoTerreno,
            nombreInspeccion,
            observacion,
            fotos,
            cancellationToken);

    public Task<InspeccionFitosanitariaDetalleWeb> AgregarFotosAsync(
        int inspeccionId,
        IReadOnlyCollection<FotoFitosanitariaCargaWeb> fotos,
        CancellationToken cancellationToken = default) =>
        EnviarFotosAsync(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/fotografias",
            null,
            null,
            null,
            fotos,
            cancellationToken);

    public Task<InspeccionOperacionMasivaWeb> ProcesarFotosAsync(
        int inspeccionId,
        IReadOnlyCollection<int> fotografiaIds,
        CancellationToken cancellationToken = default) =>
        PostDataAsync<InspeccionOperacionMasivaWeb>(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/procesar-fotografias",
            new { fotografiaIds },
            cancellationToken);

    public Task<InspeccionOperacionMasivaWeb> SolicitarRevisionIAAsync(
        int inspeccionId,
        int fotografiaId,
        string retroalimentacion,
        string? diagnosticoPropuesto,
        CancellationToken cancellationToken = default) =>
        PostDataAsync<InspeccionOperacionMasivaWeb>(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/solicitar-revision-ia",
            new
            {
                fotografiaIds = new[] { fotografiaId },
                retroalimentacion,
                diagnosticoPropuesto
            },
            cancellationToken);

    public Task<InspeccionOperacionMasivaWeb> EnviarAnalizadorAsync(
        int inspeccionId,
        IReadOnlyCollection<int> fotografiaIds,
        CancellationToken cancellationToken = default) =>
        PostDataAsync<InspeccionOperacionMasivaWeb>(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/enviar-analizador",
            new { fotografiaIds },
            cancellationToken);

    public Task<InspeccionOperacionMasivaWeb> DescartarFotoAsync(
        int inspeccionId,
        int fotografiaId,
        string motivo,
        CancellationToken cancellationToken = default) =>
        PostDataAsync<InspeccionOperacionMasivaWeb>(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/descartar-fotografias",
            new
            {
                fotografiaIds = new[] { fotografiaId },
                motivo
            },
            cancellationToken);

    public async Task FinalizarEtapaTecnicaAsync(
        int inspeccionId,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/finalizar-etapa-tecnica",
            new { },
            cancellationToken);
    }

    public Task<InspeccionOperacionMasivaWeb> GuardarAnalisisHumanoAsync(
        int inspeccionId,
        InspeccionFotoAnalisisHumanoRequestWeb fotografia,
        CancellationToken cancellationToken = default) =>
        PostDataAsync<InspeccionOperacionMasivaWeb>(
            $"api/inspecciones-fitosanitarias-flujo/{inspeccionId}/analisis-humano-individual",
            new
            {
                fotografias = new[] { fotografia },
                enviarAprobacion = false
            },
            cancellationToken);

    public async Task EnviarAprobadorAsync(
        int inspeccionId,
        IReadOnlyCollection<int> fotografiaIds,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/revision-fitosanitaria/{inspeccionId}/enviar-aprobador",
            new { fotografiaIds },
            cancellationToken);
    }

    public Task<InspeccionOperacionMasivaWeb> RegistrarAprobacionAsync(
        int inspeccionId,
        InspeccionFotoAprobacionRequestWeb fotografia,
        CancellationToken cancellationToken = default) =>
        PostDataAsync<InspeccionOperacionMasivaWeb>(
            $"api/inspecciones-fitosanitarias-flujo/{inspeccionId}/aprobacion-individual",
            new { fotografias = new[] { fotografia } },
            cancellationToken);

    public async Task CerrarDefinitivamenteAsync(
        int inspeccionId,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/cerrar-definitivo",
            new { },
            cancellationToken);
    }

    public async Task<InspeccionBloqueoWeb> AdquirirBloqueoAsync(
        int inspeccionId,
        string modo,
        CancellationToken cancellationToken = default) =>
        await PostDataAsync<InspeccionBloqueoWeb>(
            $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/adquirir?modo=" +
            Uri.EscapeDataString(modo),
            new { },
            cancellationToken);

    public async Task<InspeccionBloqueoWeb> RenovarBloqueoAsync(
        int inspeccionId,
        string modo,
        string token,
        CancellationToken cancellationToken = default) =>
        await PostDataAsync<InspeccionBloqueoWeb>(
            $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/renovar?modo=" +
            Uri.EscapeDataString(modo),
            new { token },
            cancellationToken);

    public async Task LiberarBloqueoAsync(
        int inspeccionId,
        string modo,
        string token,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/liberar?modo=" +
            Uri.EscapeDataString(modo),
            new { token },
            cancellationToken);
    }

    public async Task<List<MotivoDevolucionTecnicoWeb>> ObtenerMotivosDevolucionAsync(
        CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<List<MotivoDevolucionTecnicoWeb>>(
            "api/configuracion/motivos-devolucion-tecnico/activos",
            cancellationToken) ?? [];

    public async Task DevolverTecnicoAsync(
        int inspeccionId,
        int fotografiaId,
        int motivoDevolucionTecnicoId,
        string instrucciones,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/revision-fitosanitaria/{inspeccionId}/devolver-tecnico",
            new { fotografiaId, motivoDevolucionTecnicoId, instrucciones },
            cancellationToken);
    }

    public async Task ResolverDevolucionAsync(
        int inspeccionId,
        int fotografiaId,
        string tipoFotografia,
        DateTime fechaIdentificacionCampo,
        string respuestaTecnico,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/revision-fitosanitaria/{inspeccionId}/resolver-devolucion",
            new
            {
                fotografiaId,
                tipoFotografia,
                fechaIdentificacionCampo,
                respuestaTecnico
            },
            cancellationToken);
    }

    public async Task FinalizarAnalizadorAsync(
        int inspeccionId,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/revision-fitosanitaria/{inspeccionId}/finalizar-analizador",
            new { },
            cancellationToken);
    }

    public async Task<List<InspeccionAlbumCategoriaWeb>> ObtenerCatalogoAlbumAsync(
        CancellationToken cancellationToken = default)
    {
        ApiEnvelopeWeb<List<InspeccionAlbumCategoriaWeb>>? envelope =
            await apiClient.GetAsync<ApiEnvelopeWeb<List<InspeccionAlbumCategoriaWeb>>>(
                "api/inspecciones-fitosanitarias/catalogo-album",
                cancellationToken);
        return envelope?.Data ?? [];
    }

    public async Task PublicarAlbumAsync(
        int inspeccionId,
        int fotografiaId,
        int categoriaAlbumBotanicoId,
        int albumBotanicoCafeId,
        string descripcion,
        bool esPortada,
        int orden,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/inspecciones-fitosanitarias/{inspeccionId}/fotografias/{fotografiaId}/publicar-album",
            new
            {
                categoriaAlbumBotanicoId,
                albumBotanicoCafeId,
                descripcion,
                esPortada,
                orden
            },
            cancellationToken);
    }

    public Task<ControlFitosanitarioResumenWeb> ObtenerResumenControlAsync(
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default) =>
        GetDataAsync<ControlFitosanitarioResumenWeb>(
            "api/control-fitosanitario/resumen" +
            ConstruirFechas(fechaDesde, fechaHasta),
            cancellationToken);

    public Task<ControlFitosanitarioRendimientoIAWeb> ObtenerRendimientoIAAsync(
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default) =>
        GetDataAsync<ControlFitosanitarioRendimientoIAWeb>(
            "api/control-fitosanitario/rendimiento-ia" +
            ConstruirFechas(fechaDesde, fechaHasta),
            cancellationToken);

    public async Task<ControlFitosanitarioPaginaWeb> ObtenerControlInspeccionesAsync(
        string? buscar,
        string? estado,
        int? tecnicoId,
        string? departamento,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default)
    {
        var parametros = new List<string>
        {
            $"pagina={Math.Max(1, pagina)}",
            $"tamanoPagina={Math.Clamp(tamanoPagina, 10, 100)}"
        };
        Agregar(parametros, "buscar", buscar);
        Agregar(parametros, "estado", estado);
        Agregar(parametros, "departamento", departamento);
        if (tecnicoId is > 0)
            parametros.Add($"tecnicoId={tecnicoId.Value}");
        if (fechaDesde.HasValue)
            parametros.Add($"fechaDesde={fechaDesde.Value:yyyy-MM-dd}");
        if (fechaHasta.HasValue)
            parametros.Add($"fechaHasta={fechaHasta.Value:yyyy-MM-dd}");

        return await GetDataAsync<ControlFitosanitarioPaginaWeb>(
            "api/control-fitosanitario/inspecciones?" + string.Join("&", parametros),
            cancellationToken);
    }

    public Task<ControlFitosanitarioAuditoriaWeb> ObtenerAuditoriaAsync(
        int inspeccionId,
        CancellationToken cancellationToken = default) =>
        GetDataAsync<ControlFitosanitarioAuditoriaWeb>(
            $"api/control-fitosanitario/{inspeccionId}/auditoria",
            cancellationToken);

    public Task<List<ControlFitosanitarioUsuarioWeb>> ObtenerUsuariosEtapaAsync(
        string etapa,
        CancellationToken cancellationToken = default) =>
        GetDataAsync<List<ControlFitosanitarioUsuarioWeb>>(
            "api/control-fitosanitario/usuarios?etapa=" +
            Uri.EscapeDataString(etapa),
            cancellationToken);

    public async Task ReasignarAsync(
        int inspeccionId,
        string etapa,
        int usuarioNuevoId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/control-fitosanitario/{inspeccionId}/reasignar",
            new { etapa, usuarioNuevoId, motivo },
            cancellationToken);
    }

    public async Task LiberarBloqueoAdministrativoAsync(
        int inspeccionId,
        string etapa,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        await PostDataAsync<object>(
            $"api/control-fitosanitario/{inspeccionId}/liberar-bloqueo",
            new { etapa, motivo },
            cancellationToken);
    }

    private async Task<InspeccionFitosanitariaDetalleWeb> EnviarFotosAsync(
        string ruta,
        string? codigoTerreno,
        string? nombreInspeccion,
        string? observacion,
        IReadOnlyCollection<FotoFitosanitariaCargaWeb> fotos,
        CancellationToken cancellationToken)
    {
        if (fotos.Count == 0)
            throw new InvalidOperationException("Debe seleccionar al menos una fotografía.");
        if (fotos.Count > 40)
            throw new InvalidOperationException("Puede cargar como máximo 40 fotografías por inspección.");

        HttpClient client = httpClientFactory.CreateClient(
            "CONATRADEC_API_AUTENTICADA");
        client.Timeout = TimeSpan.FromMinutes(10);

        string? token = authState.Usuario?.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("La sesión no contiene un token válido.");

        using var request = new HttpRequestMessage(HttpMethod.Post, ruta);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var contenido = new MultipartFormDataContent();
        var streams = new List<Stream>();

        try
        {
            if (!string.IsNullOrWhiteSpace(codigoTerreno))
                contenido.Add(new StringContent(codigoTerreno.Trim(), Encoding.UTF8), "CodigoTerreno");
            if (!string.IsNullOrWhiteSpace(nombreInspeccion))
                contenido.Add(new StringContent(nombreInspeccion.Trim(), Encoding.UTF8), "NombreInspeccion");
            if (!string.IsNullOrWhiteSpace(observacion))
                contenido.Add(new StringContent(observacion.Trim(), Encoding.UTF8), "Observacion");

            foreach (FotoFitosanitariaCargaWeb foto in fotos)
            {
                Stream stream = foto.Archivo.OpenReadStream(MaximoBytesFoto, cancellationToken);
                streams.Add(stream);
                var archivoContent = new StreamContent(stream);
                archivoContent.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(foto.Archivo.ContentType)
                        ? "image/jpeg"
                        : foto.Archivo.ContentType);
                contenido.Add(archivoContent, "Fotos", foto.Archivo.Name);
                contenido.Add(new StringContent(foto.TipoFotografia, Encoding.UTF8), "TiposFotografia");
                contenido.Add(new StringContent(
                    foto.FechaIdentificacionCampo.ToString("yyyy-MM-dd"),
                    Encoding.UTF8), "FechasIdentificacionCampo");
            }

            request.Content = contenido;
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            ApiEnvelopeWeb<InspeccionFitosanitariaDetalleWeb>? envelope =
                string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<ApiEnvelopeWeb<InspeccionFitosanitariaDetalleWeb>>(
                        json, jsonOptions);

            if (!response.IsSuccessStatusCode)
                throw CrearExcepcion(response.StatusCode, envelope?.Message, json);

            return envelope?.Data ?? throw new InvalidOperationException(
                "La API no devolvió la inspección registrada.");
        }
        finally
        {
            foreach (Stream stream in streams)
                await stream.DisposeAsync();
        }
    }

    private async Task<T> GetDataAsync<T>(
        string ruta,
        CancellationToken cancellationToken)
        where T : class
    {
        ApiEnvelopeWeb<T>? envelope =
            await apiClient.GetAsync<ApiEnvelopeWeb<T>>(ruta, cancellationToken);
        return envelope?.Data ?? throw new InvalidOperationException(
            envelope?.Message ?? "La API no devolvió datos para la operación.");
    }

    private async Task<T> PostDataAsync<T>(
        string ruta,
        object payload,
        CancellationToken cancellationToken)
        where T : class
    {
        ApiEnvelopeWeb<T>? envelope =
            await apiClient.PostAsync<object, ApiEnvelopeWeb<T>>(
                ruta,
                payload,
                cancellationToken);

        if (envelope?.Data is not null)
            return envelope.Data;

        if (typeof(T) == typeof(object) && envelope?.Success == true)
            return (T)(object)new object();

        throw new InvalidOperationException(
            envelope?.Message ?? "La API no devolvió el resultado esperado.");
    }

    private static void Agregar(
        ICollection<string> parametros,
        string nombre,
        string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
            parametros.Add($"{nombre}={Uri.EscapeDataString(valor.Trim())}");
    }

    private static string ConstruirFechas(DateTime? desde, DateTime? hasta)
    {
        var partes = new List<string>();
        if (desde.HasValue)
            partes.Add($"fechaDesde={desde.Value:yyyy-MM-dd}");
        if (hasta.HasValue)
            partes.Add($"fechaHasta={hasta.Value:yyyy-MM-dd}");
        return partes.Count == 0 ? string.Empty : "?" + string.Join("&", partes);
    }

    private static Exception CrearExcepcion(
        System.Net.HttpStatusCode status,
        string? mensaje,
        string contenido)
    {
        string texto = string.IsNullOrWhiteSpace(mensaje)
            ? string.IsNullOrWhiteSpace(contenido)
                ? "No fue posible completar la operación."
                : contenido
            : mensaje;

        return status == System.Net.HttpStatusCode.Unauthorized
            ? new UnauthorizedAccessException(texto)
            : new HttpRequestException(texto, null, status);
    }
}

public sealed class FotoFitosanitariaCargaWeb
{
    public required IBrowserFile Archivo { get; init; }
    public string TipoFotografia { get; set; } = "EVIDENCIA";
    public DateTime FechaIdentificacionCampo { get; set; } = DateTime.Today;
}
