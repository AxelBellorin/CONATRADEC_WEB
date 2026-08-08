namespace CONATRADEC.AdminWeb.Models;

public static class EstadosFotoFitosanitariaWeb
{
    public const string Borrador = "BORRADOR";
    public const string PendienteIA = "PENDIENTE_IA";
    public const string AnalizandoIA = "ANALIZANDO_IA";
    public const string ErrorIA = "ERROR_IA";
    public const string PendienteDecisionTecnico = "PENDIENTE_DECISION_TECNICO";
    public const string PendienteAnalizador = "PENDIENTE_ANALIZADOR";
    public const string EnAnalisisHumano = "EN_ANALISIS_HUMANO";
    public const string PendienteAprobacion = "PENDIENTE_APROBACION";
    public const string DevueltaAnalizador = "DEVUELTA_AL_ANALIZADOR";
    public const string DevueltaTecnico = "DEVUELTA_AL_TECNICO";
    public const string Aprobada = "APROBADA";
    public const string AprobadaConCorreccion = "APROBADA_CON_CORRECCION";
    public const string Rechazada = "RECHAZADA";
    public const string NoConcluyente = "NO_CONCLUYENTE";
    public const string Descartada = "DESCARTADA";
    public const string PublicadaAlbum = "PUBLICADA_ALBUM";
}

public static class DecisionesAprobacionFitosanitariaWeb
{
    public const string Aprobar = "APROBAR_SIN_CAMBIOS";
    public const string AprobarConCorreccion = "APROBAR_CON_CORRECCION";
    public const string Devolver = "DEVOLVER_AL_ANALIZADOR";
    public const string Rechazar = "RECHAZAR_DIAGNOSTICO";
    public const string NoConcluyente = "MARCAR_NO_CONCLUYENTE";
}

public sealed class ApiEnvelopeWeb<T>
    where T : class
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public sealed class TipoFotografiaIAWeb
{
    public int TipoFotografiaIAId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string InstruccionIA { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; }
}

public sealed class InspeccionFitosanitariaBandejaWeb
{
    public List<InspeccionFitosanitariaBandejaItemWeb> Items { get; set; } = [];
    public bool HayMas { get; set; }
    public DateTime? SiguienteFechaUtc { get; set; }
    public int? SiguienteId { get; set; }
}

public sealed class InspeccionFitosanitariaBandejaItemWeb
{
    public int InspeccionId { get; set; }
    public string NombreInspeccion { get; set; } = string.Empty;
    public bool EtapaTecnicaFinalizada { get; set; }
    public DateTime? FechaFinEtapaTecnicaUtc { get; set; }
    public bool CerradaDefinitiva { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public int UsuarioTecnicoId { get; set; }
    public string TecnicoNombreCompleto { get; set; } = string.Empty;
    public string TecnicoUsuario { get; set; } = string.Empty;
    public DateTime FechaRegistroSistemaUtc { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int TotalFotografias { get; set; }
    public int Pendientes { get; set; }
    public int ConError { get; set; }
    public int Finalizadas { get; set; }
    public int RequierenDecisionTecnico { get; set; }
    public int EnviadasRevision { get; set; }
    public int PendientesAprobacion { get; set; }
    public int EnviadasAprobador { get; set; }
    public int Procesando { get; set; }
    public int Descartadas { get; set; }
    public string UrlMiniatura { get; set; } = string.Empty;
    public int? UsuarioAnalizadorAsignadoId { get; set; }
    public string AnalizadorAsignado { get; set; } = string.Empty;
    public int? UsuarioAprobadorAsignadoId { get; set; }
    public string AprobadorAsignado { get; set; } = string.Empty;
    public string VersionAsignacion { get; set; } = string.Empty;
}

public sealed class InspeccionFitosanitariaDetalleWeb
{
    public int InspeccionId { get; set; }
    public int? TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public int UsuarioSolicitanteId { get; set; }
    public string UsuarioSolicitante { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaRegistroSistemaUtc { get; set; }
    public bool EtapaTecnicaFinalizada { get; set; }
    public DateTime? FechaFinEtapaTecnicaUtc { get; set; }
    public int? UsuarioCierreEtapaTecnicaId { get; set; }
    public bool CerradaDefinitiva { get; set; }
    public DateTime? FechaCierreDefinitivoUtc { get; set; }
    public int? UsuarioCierreDefinitivoId { get; set; }
    public int? UsuarioAnalizadorAsignadoId { get; set; }
    public int? UsuarioAprobadorAsignadoId { get; set; }
    public string VersionAsignacion { get; set; } = string.Empty;
    public List<InspeccionFotoWeb> Fotografias { get; set; } = [];
    public bool PuedeGestionarSolicitud { get; set; }
    public bool PuedeCerrarInspeccion { get; set; }
    public string MotivoNoPuedeCerrar { get; set; } = string.Empty;
    public bool PuedeAnalizar { get; set; }
    public bool PuedeAprobar { get; set; }
    public bool PuedePublicarAlbum { get; set; }
}

public sealed class InspeccionFotoWeb
{
    public int FotografiaId { get; set; }
    public int Orden { get; set; }
    public string TipoFotografia { get; set; } = string.Empty;
    public string NombreArchivoOriginal { get; set; } = string.Empty;
    public string UrlImagen { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaIdentificacionCampo { get; set; }
    public DateTime FechaRegistroSistemaUtc { get; set; }
    public DateTime? FechaAnalisisIAUtc { get; set; }
    public DateTime? FechaAnalisisHumanoUtc { get; set; }
    public DateTime? FechaAprobacionUtc { get; set; }
    public string ModeloIAUtilizado { get; set; } = string.Empty;
    public int IntentosIA { get; set; }
    public string ErrorProcesamiento { get; set; } = string.Empty;
    public bool Descartada { get; set; }
    public string MotivoDescarte { get; set; } = string.Empty;
    public bool PublicadaAlbum { get; set; }
    public InspeccionFotoResultadoIAWeb? ResultadoIA { get; set; }
    public InspeccionFotoAnalisisHumanoWeb? UltimoAnalisisHumano { get; set; }
    public InspeccionFotoAprobacionWeb? UltimaAprobacion { get; set; }
    public List<InspeccionFotoHistorialWeb> Historial { get; set; } = [];
}

public sealed class InspeccionFotoResultadoIAWeb
{
    public bool ImagenValida { get; set; }
    public bool ParecePlantaCafe { get; set; }
    public bool ResultadoConcluyente { get; set; }
    public string PartePlanta { get; set; } = string.Empty;
    public string CalidadEvaluacion { get; set; } = string.Empty;
    public string EstadoGeneral { get; set; } = string.Empty;
    public string CategoriaPrincipal { get; set; } = string.Empty;
    public List<string> CategoriasSecundarias { get; set; } = [];
    public string DiagnosticoProbable { get; set; } = string.Empty;
    public string TipoDiagnostico { get; set; } = string.Empty;
    public string SeveridadVisual { get; set; } = string.Empty;
    public string NivelCerteza { get; set; } = string.Empty;
    public string ResumenImagen { get; set; } = string.Empty;
    public List<string> SintomasVisibles { get; set; } = [];
    public List<string> EvidenciasObservadas { get; set; } = [];
    public List<string> EvidenciasNoObservadas { get; set; } = [];
    public List<string> DiagnosticosAlternativos { get; set; } = [];
    public List<string> InformacionFaltante { get; set; } = [];
    public List<string> RecomendacionesCaptura { get; set; } = [];
    public List<string> Advertencias { get; set; } = [];
    public DateTime? FechaAnalisisIAUtc { get; set; }
}

public sealed class InspeccionFotoAnalisisHumanoWeb
{
    public int AnalisisHumanoId { get; set; }
    public int Version { get; set; }
    public int UsuarioAnalizadorId { get; set; }
    public string UsuarioAnalizador { get; set; } = string.Empty;
    public string EstadoRegistro { get; set; } = string.Empty;
    public string CalidadEvaluacion { get; set; } = string.Empty;
    public string EstadoGeneral { get; set; } = string.Empty;
    public string CategoriaPrincipal { get; set; } = string.Empty;
    public List<string> CategoriasSecundarias { get; set; } = [];
    public string Diagnostico { get; set; } = string.Empty;
    public string TipoDiagnostico { get; set; } = string.Empty;
    public string Severidad { get; set; } = string.Empty;
    public string NivelCerteza { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaEnvioUtc { get; set; }
}

public sealed class InspeccionFotoAprobacionWeb
{
    public int AprobacionId { get; set; }
    public int UsuarioAprobadorId { get; set; }
    public string UsuarioAprobador { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string DiagnosticoFinal { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public bool AutorizaPublicacionAlbum { get; set; }
    public bool MismoUsuarioQueAnalizo { get; set; }
    public DateTime FechaAprobacionUtc { get; set; }
}

public sealed class InspeccionFotoHistorialWeb
{
    public int HistorialId { get; set; }
    public int UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
}

public sealed class InspeccionOperacionMasivaWeb
{
    public int TotalSolicitadas { get; set; }
    public int TotalExitosas { get; set; }
    public int TotalConError { get; set; }
    public List<InspeccionOperacionItemWeb> Resultados { get; set; } = [];
}

public sealed class InspeccionOperacionItemWeb
{
    public int FotografiaId { get; set; }
    public bool Exitoso { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public sealed class InspeccionFotoAnalisisHumanoRequestWeb
{
    public int FotografiaId { get; set; }
    public string CalidadEvaluacion { get; set; } = "EVALUABLE";
    public string EstadoGeneral { get; set; } = "INDETERMINADA";
    public string CategoriaPrincipal { get; set; } = "AFECTACION_NO_DETERMINADA";
    public List<string> CategoriasSecundarias { get; set; } = [];
    public string Diagnostico { get; set; } = string.Empty;
    public string TipoDiagnostico { get; set; } = string.Empty;
    public string Severidad { get; set; } = "NO_EVALUABLE";
    public string NivelCerteza { get; set; } = "NO_DETERMINADO";
    public string Observaciones { get; set; } = string.Empty;
}

public sealed class InspeccionFotoAprobacionRequestWeb
{
    public int FotografiaId { get; set; }
    public string Decision { get; set; } = DecisionesAprobacionFitosanitariaWeb.Aprobar;
    public string CalidadEvaluacionFinal { get; set; } = string.Empty;
    public string EstadoGeneralFinal { get; set; } = string.Empty;
    public string CategoriaPrincipalFinal { get; set; } = string.Empty;
    public List<string> CategoriasSecundariasFinales { get; set; } = [];
    public string DiagnosticoFinal { get; set; } = string.Empty;
    public string TipoDiagnosticoFinal { get; set; } = string.Empty;
    public string SeveridadFinal { get; set; } = string.Empty;
    public string NivelCertezaFinal { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public bool AutorizaPublicacionAlbum { get; set; }
}

public sealed class InspeccionBloqueoWeb
{
    public bool Adquirido { get; set; }
    public int InspeccionId { get; set; }
    public string Modo { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime? FechaAdquisicionUtc { get; set; }
    public DateTime? UltimoHeartbeatUtc { get; set; }
    public DateTime? ExpiraUtc { get; set; }
    public int VigenciaSegundos { get; set; }
}

public sealed class InspeccionAlbumCategoriaWeb
{
    public int CategoriaAlbumBotanicoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<InspeccionAlbumFichaWeb> Fichas { get; set; } = [];
}

public sealed class InspeccionAlbumFichaWeb
{
    public int AlbumBotanicoCafeId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string NombreCientifico { get; set; } = string.Empty;
}

public sealed class ControlFitosanitarioResumenWeb
{
    public int TotalInspecciones { get; set; }
    public int Abiertas { get; set; }
    public int EtapaTecnicaAbierta { get; set; }
    public int PendientesAnalizador { get; set; }
    public int PendientesAprobacion { get; set; }
    public int Cerradas { get; set; }
    public int TotalFotografias { get; set; }
    public int FotografiasProcesando { get; set; }
    public int FotografiasErrorIA { get; set; }
    public int FotografiasDevueltas { get; set; }
    public int FotografiasNoConcluyentes { get; set; }
    public int FotografiasRechazadas { get; set; }
    public int FotografiasAprobadas { get; set; }
    public int FotografiasPublicadasAlbum { get; set; }
    public int Mas48HorasAnalizador { get; set; }
    public int Mas48HorasAprobador { get; set; }
    public int BloqueosActivos { get; set; }
    public decimal? PromedioHorasCierre { get; set; }
}

public sealed class ControlFitosanitarioPaginaWeb
{
    public List<ControlFitosanitarioItemWeb> Items { get; set; } = [];
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
}

public sealed class ControlFitosanitarioItemWeb
{
    public int InspeccionId { get; set; }
    public string NombreInspeccion { get; set; } = string.Empty;
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Propietario { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public int UsuarioTecnicoId { get; set; }
    public string Tecnico { get; set; } = string.Empty;
    public DateTime FechaRegistroSistemaUtc { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EtapaTecnicaFinalizada { get; set; }
    public bool CerradaDefinitiva { get; set; }
    public int TotalFotografias { get; set; }
    public int Pendientes { get; set; }
    public int ConError { get; set; }
    public int Finalizadas { get; set; }
    public int? UsuarioAnalizadorId { get; set; }
    public string Analizador { get; set; } = string.Empty;
    public int? UsuarioAprobadorId { get; set; }
    public string Aprobador { get; set; } = string.Empty;
    public int? BloqueoAnalizadorUsuarioId { get; set; }
    public string BloqueoAnalizadorUsuario { get; set; } = string.Empty;
    public DateTime? BloqueoAnalizadorExpiraUtc { get; set; }
    public int? BloqueoAprobadorUsuarioId { get; set; }
    public string BloqueoAprobadorUsuario { get; set; } = string.Empty;
    public DateTime? BloqueoAprobadorExpiraUtc { get; set; }
    public DateTime? UltimaActividadUtc { get; set; }
}

public sealed class ControlFitosanitarioUsuarioWeb
{
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
}

public sealed class ControlFitosanitarioAuditoriaWeb
{
    public int InspeccionId { get; set; }
    public string NombreInspeccion { get; set; } = string.Empty;
    public string CodigoTerreno { get; set; } = string.Empty;
    public int UsuarioTecnicoId { get; set; }
    public string Tecnico { get; set; } = string.Empty;
    public DateTime FechaRegistroSistemaUtc { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EtapaTecnicaFinalizada { get; set; }
    public DateTime? FechaFinEtapaTecnicaUtc { get; set; }
    public bool CerradaDefinitiva { get; set; }
    public DateTime? FechaCierreDefinitivoUtc { get; set; }
    public int? UsuarioAnalizadorId { get; set; }
    public string Analizador { get; set; } = string.Empty;
    public int? UsuarioAprobadorId { get; set; }
    public string Aprobador { get; set; } = string.Empty;
    public ControlFitosanitarioBloqueoWeb? BloqueoAnalizador { get; set; }
    public ControlFitosanitarioBloqueoWeb? BloqueoAprobador { get; set; }
    public List<ControlFitosanitarioFotoComparacionWeb> Fotografias { get; set; } = [];
    public List<ControlFitosanitarioEventoWeb> Eventos { get; set; } = [];
}

public sealed class ControlFitosanitarioBloqueoWeb
{
    public string Etapa { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public DateTime FechaAdquisicionUtc { get; set; }
    public DateTime UltimoHeartbeatUtc { get; set; }
    public DateTime ExpiraUtc { get; set; }
}

public sealed class ControlFitosanitarioFotoComparacionWeb
{
    public int FotografiaId { get; set; }
    public int Orden { get; set; }
    public string TipoFotografia { get; set; } = string.Empty;
    public string UrlImagen { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaIdentificacionCampo { get; set; }
    public DateTime? FechaAnalisisIAUtc { get; set; }
    public DateTime? FechaAnalisisHumanoUtc { get; set; }
    public DateTime? FechaAprobacionUtc { get; set; }
    public string ModeloIA { get; set; } = string.Empty;
    public int IntentosIA { get; set; }
    public string DiagnosticoIA { get; set; } = string.Empty;
    public string DiagnosticoHumano { get; set; } = string.Empty;
    public string DiagnosticoFinal { get; set; } = string.Empty;
    public string DecisionFinal { get; set; } = string.Empty;
    public bool CoincidenciaIAFinal { get; set; }
}

public sealed class ControlFitosanitarioEventoWeb
{
    public DateTime FechaUtc { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int? FotografiaId { get; set; }
    public int UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
}

public sealed class ControlFitosanitarioRendimientoIAWeb
{
    public int FotografiasAnalizadas { get; set; }
    public int FotografiasConResultadoFinal { get; set; }
    public int CoincidenciasExactas { get; set; }
    public int CorregidasPorHumano { get; set; }
    public int NoConcluyentes { get; set; }
    public int Rechazadas { get; set; }
    public int ErroresIA { get; set; }
    public decimal PorcentajeCoincidencia { get; set; }
    public List<ControlFitosanitarioModeloIAWeb> Modelos { get; set; } = [];
}

public sealed class ControlFitosanitarioModeloIAWeb
{
    public string Modelo { get; set; } = string.Empty;
    public int FotografiasAnalizadas { get; set; }
    public int ConResultadoFinal { get; set; }
    public int CoincidenciasExactas { get; set; }
    public int CorregidasPorHumano { get; set; }
    public decimal PorcentajeCoincidencia { get; set; }
}

public sealed class MotivoDevolucionTecnicoWeb
{
    public int MotivoDevolucionTecnicoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string InstruccionSugerida { get; set; } = string.Empty;
    public bool RequiereNuevaFotografia { get; set; }
    public bool PermiteCorregirMetadatos { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
}

public sealed class DiagnosticoIAConfiguracionWeb
{
    public int MaximoRevisionesGemini { get; set; } = 2;
    public bool RevisionesIlimitadas { get; set; }
    public DateTime FechaModificacionUtc { get; set; }
    public int? UsuarioModificacionId { get; set; }
    public string UsuarioModificacion { get; set; } = string.Empty;
    public List<DiagnosticoIAConfiguracionHistorialWeb> Historial { get; set; } = [];
}

public sealed class DiagnosticoIAConfiguracionHistorialWeb
{
    public int DiagnosticoIAConfiguracionHistorialId { get; set; }
    public int MaximoAnterior { get; set; }
    public bool IlimitadasAnterior { get; set; }
    public int MaximoNuevo { get; set; }
    public bool IlimitadasNuevo { get; set; }
    public int UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; }
}

public sealed class TipoFotografiaIAGuardarWeb
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string InstruccionIA { get; set; } = string.Empty;
    public int Orden { get; set; } = 1;
}
