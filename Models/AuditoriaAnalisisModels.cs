using System.Text.Json.Serialization;

namespace CONATRADEC.AdminWeb.Models;

public sealed class AuditoriaApiRespuesta<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class AuditoriaAnalisisFiltro
{
    public string Buscar { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }
    public string Laboratorio { get; set; } = string.Empty;
    public DateOnly? FechaLaboratorioDesde { get; set; }
    public DateOnly? FechaLaboratorioHasta { get; set; }
    public DateTime? FechaRegistroDesde { get; set; }
    public DateTime? FechaRegistroHasta { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool? TieneFormula { get; set; }
    public bool? TieneEnmienda { get; set; }
    public bool? TieneMixta { get; set; }
    public decimal? PhMinimo { get; set; }
    public decimal? PhMaximo { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public sealed class AuditoriaAnalisisPaginada
{
    [JsonPropertyName("items")]
    public List<AuditoriaAnalisisItem> Items { get; set; } = [];

    [JsonPropertyName("pagina")]
    public int Pagina { get; set; } = 1;

    [JsonPropertyName("tamanoPagina")]
    public int TamanoPagina { get; set; } = 25;

    [JsonPropertyName("totalRegistros")]
    public int TotalRegistros { get; set; }

    [JsonPropertyName("totalPaginas")]
    public int TotalPaginas { get; set; } = 1;

    [JsonPropertyName("resumen")]
    public AuditoriaAnalisisResumen Resumen { get; set; } = new();
}

public sealed class AuditoriaAnalisisResumen
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("activos")]
    public int Activos { get; set; }

    [JsonPropertyName("inactivos")]
    public int Inactivos { get; set; }

    [JsonPropertyName("inconsistentes")]
    public int Inconsistentes { get; set; }

    [JsonPropertyName("online")]
    public int Online { get; set; }

    [JsonPropertyName("offline")]
    public int Offline { get; set; }
}

public sealed class AuditoriaAnalisisItem
{
    [JsonPropertyName("analisisSueloId")]
    public int AnalisisSueloId { get; set; }

    [JsonPropertyName("analisisSueloCalculoId")]
    public int AnalisisSueloCalculoId { get; set; }

    [JsonPropertyName("identificador")]
    public string Identificador { get; set; } = string.Empty;

    [JsonPropertyName("laboratorio")]
    public string Laboratorio { get; set; } = string.Empty;

    [JsonPropertyName("fechaLaboratorio")]
    public DateOnly FechaLaboratorio { get; set; }

    [JsonPropertyName("fechaRegistro")]
    public DateTime FechaRegistro { get; set; }

    [JsonPropertyName("fechaCalculo")]
    public DateTime FechaCalculo { get; set; }

    [JsonPropertyName("terrenoId")]
    public int TerrenoId { get; set; }

    [JsonPropertyName("codigoTerreno")]
    public string CodigoTerreno { get; set; } = string.Empty;

    [JsonPropertyName("propietario")]
    public string Propietario { get; set; } = string.Empty;

    [JsonPropertyName("usuarioId")]
    public int? UsuarioId { get; set; }

    [JsonPropertyName("usuarioNombre")]
    public string UsuarioNombre { get; set; } = string.Empty;

    [JsonPropertyName("ph")]
    public decimal Ph { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("origen")]
    public string Origen { get; set; } = string.Empty;

    [JsonPropertyName("tipoOperacionOffline")]
    public string? TipoOperacionOffline { get; set; }

    [JsonPropertyName("fechaSincronizacionUtc")]
    public DateTime? FechaSincronizacionUtc { get; set; }

    [JsonPropertyName("tieneFormula")]
    public bool TieneFormula { get; set; }

    [JsonPropertyName("tieneEnmienda")]
    public bool TieneEnmienda { get; set; }

    [JsonPropertyName("tieneMixta")]
    public bool TieneMixta { get; set; }

    [JsonPropertyName("tieneInconsistencia")]
    public bool TieneInconsistencia { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("alertas")]
    public List<string> Alertas { get; set; } = [];
}

public sealed class AuditoriaAnalisisCatalogos
{
    [JsonPropertyName("usuarios")]
    public List<AuditoriaUsuarioOpcion> Usuarios { get; set; } = [];

    [JsonPropertyName("laboratorios")]
    public List<string> Laboratorios { get; set; } = [];

    [JsonPropertyName("origenes")]
    public List<AuditoriaOpcion> Origenes { get; set; } = [];

    [JsonPropertyName("estados")]
    public List<AuditoriaOpcion> Estados { get; set; } = [];
}

public sealed class AuditoriaUsuarioOpcion
{
    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

public sealed class AuditoriaOpcion
{
    [JsonPropertyName("valor")]
    public string Valor { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}

public sealed class AuditoriaAnalisisDetalle
{
    [JsonPropertyName("resumen")]
    public AuditoriaDetalleResumen Resumen { get; set; } = new();

    [JsonPropertyName("procedenciaOffline")]
    public AuditoriaProcedenciaOffline? ProcedenciaOffline { get; set; }

    [JsonPropertyName("modulos")]
    public AuditoriaModulos Modulos { get; set; } = new();

    [JsonPropertyName("elementosIngresados")]
    public List<AuditoriaElementoIngresado> ElementosIngresados { get; set; } = [];

    [JsonPropertyName("elementosCalculados")]
    public List<AuditoriaElementoCalculado> ElementosCalculados { get; set; } = [];

    [JsonPropertyName("inconsistencias")]
    public List<string> Inconsistencias { get; set; } = [];

    [JsonPropertyName("historial")]
    public List<AuditoriaHistorialItem> Historial { get; set; } = [];
}

public sealed class AuditoriaDetalleResumen
{
    [JsonPropertyName("analisisSueloId")]
    public int AnalisisSueloId { get; set; }

    [JsonPropertyName("analisisSueloCalculoId")]
    public int AnalisisSueloCalculoId { get; set; }

    [JsonPropertyName("identificador")]
    public string Identificador { get; set; } = string.Empty;

    [JsonPropertyName("laboratorio")]
    public string Laboratorio { get; set; } = string.Empty;

    [JsonPropertyName("fechaLaboratorio")]
    public DateOnly FechaLaboratorio { get; set; }

    [JsonPropertyName("fechaRegistro")]
    public DateTime FechaRegistro { get; set; }

    [JsonPropertyName("fechaCalculo")]
    public DateTime FechaCalculo { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("origen")]
    public string Origen { get; set; } = string.Empty;

    [JsonPropertyName("usuarioId")]
    public int? UsuarioId { get; set; }

    [JsonPropertyName("usuarioNombre")]
    public string UsuarioNombre { get; set; } = string.Empty;

    [JsonPropertyName("terreno")]
    public AuditoriaTerreno Terreno { get; set; } = new();

    [JsonPropertyName("tipoCultivo")]
    public string TipoCultivo { get; set; } = string.Empty;

    [JsonPropertyName("tipoAnalisis")]
    public string TipoAnalisis { get; set; } = string.Empty;

    [JsonPropertyName("ph")]
    public decimal Ph { get; set; }

    [JsonPropertyName("materiaOrganica")]
    public decimal? MateriaOrganica { get; set; }

    [JsonPropertyName("acidezTotal")]
    public decimal? AcidezTotal { get; set; }

    [JsonPropertyName("cantidadQuintalesOro")]
    public decimal CantidadQuintalesOro { get; set; }

    [JsonPropertyName("tamanoFinca")]
    public decimal TamanoFinca { get; set; }

    [JsonPropertyName("recomendacionGeneral")]
    public string? RecomendacionGeneral { get; set; }

    [JsonPropertyName("observacion")]
    public string? Observacion { get; set; }
}

public sealed class AuditoriaTerreno
{
    [JsonPropertyName("terrenoId")]
    public int TerrenoId { get; set; }

    [JsonPropertyName("codigoTerreno")]
    public string CodigoTerreno { get; set; } = string.Empty;

    [JsonPropertyName("propietario")]
    public string Propietario { get; set; } = string.Empty;

    [JsonPropertyName("direccion")]
    public string Direccion { get; set; } = string.Empty;

    [JsonPropertyName("extensionManzanas")]
    public decimal ExtensionManzanas { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public sealed class AuditoriaProcedenciaOffline
{
    [JsonPropertyName("identificadorAnalisis")]
    public string IdentificadorAnalisis { get; set; } = string.Empty;

    [JsonPropertyName("tipoOperacion")]
    public string TipoOperacion { get; set; } = string.Empty;

    [JsonPropertyName("versionMotor")]
    public string VersionMotor { get; set; } = string.Empty;

    [JsonPropertyName("hashPaquete")]
    public string HashPaquete { get; set; } = string.Empty;

    [JsonPropertyName("fechaCalculoLocalUtc")]
    public DateTime? FechaCalculoLocalUtc { get; set; }

    [JsonPropertyName("fechaRecepcionUtc")]
    public DateTime? FechaRecepcionUtc { get; set; }

    [JsonPropertyName("fechaCompletadoUtc")]
    public DateTime? FechaCompletadoUtc { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;
}

public sealed class AuditoriaModulos
{
    [JsonPropertyName("requerimientoAnual")]
    public bool RequerimientoAnual { get; set; }

    [JsonPropertyName("formulaNutricional")]
    public AuditoriaFormula? FormulaNutricional { get; set; }

    [JsonPropertyName("enmiendaCalcarea")]
    public AuditoriaEnmienda? EnmiendaCalcarea { get; set; }

    [JsonPropertyName("fertilizacionMixta")]
    public AuditoriaMixta? FertilizacionMixta { get; set; }
}

public sealed class AuditoriaFormula
{
    [JsonPropertyName("formulaNutricionalId")]
    public int FormulaNutricionalId { get; set; }

    [JsonPropertyName("nombreFormula")]
    public string NombreFormula { get; set; } = string.Empty;

    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("totalLibras")]
    public decimal TotalLibras { get; set; }

    [JsonPropertyName("mezclaTotalQq")]
    public decimal MezclaTotalQq { get; set; }

    [JsonPropertyName("precioTotalFormula")]
    public decimal PrecioTotalFormula { get; set; }

    [JsonPropertyName("totalPlantas")]
    public int TotalPlantas { get; set; }

    [JsonPropertyName("totalAplicaciones")]
    public int TotalAplicaciones { get; set; }

    [JsonPropertyName("esComplementoFertilizacionMixta")]
    public bool EsComplementoFertilizacionMixta { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("cantidadDetalles")]
    public int CantidadDetalles { get; set; }
}

public sealed class AuditoriaEnmienda
{
    [JsonPropertyName("enmiendaCalcareaId")]
    public int EnmiendaCalcareaId { get; set; }

    [JsonPropertyName("nombreAnalisis")]
    public string NombreAnalisis { get; set; } = string.Empty;

    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("ph")]
    public decimal Ph { get; set; }

    [JsonPropertyName("cice")]
    public decimal Cice { get; set; }

    [JsonPropertyName("saturacionActual")]
    public decimal SaturacionActual { get; set; }

    [JsonPropertyName("saturacionDeseada")]
    public decimal SaturacionDeseada { get; set; }

    [JsonPropertyName("necesidadEncaladoLbMz")]
    public decimal NecesidadEncaladoLbMz { get; set; }

    [JsonPropertyName("dosisPlantaAnualOz")]
    public decimal DosisPlantaAnualOz { get; set; }

    [JsonPropertyName("totalPlantas")]
    public int TotalPlantas { get; set; }

    [JsonPropertyName("totalAplicaciones")]
    public int TotalAplicaciones { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public sealed class AuditoriaMixta
{
    [JsonPropertyName("fertilizacionMixtaId")]
    public int FertilizacionMixtaId { get; set; }

    [JsonPropertyName("fechaCalculo")]
    public DateTime FechaCalculo { get; set; }

    [JsonPropertyName("observacion")]
    public string? Observacion { get; set; }

    [JsonPropertyName("esComplementoBalance")]
    public bool EsComplementoBalance { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }

    [JsonPropertyName("cantidadFuentes")]
    public int CantidadFuentes { get; set; }

    [JsonPropertyName("cantidadDetalles")]
    public int CantidadDetalles { get; set; }
}

public sealed class AuditoriaElementoIngresado
{
    [JsonPropertyName("analisisSueloElementoQuimicoId")]
    public int AnalisisSueloElementoQuimicoId { get; set; }

    [JsonPropertyName("elementoQuimicosId")]
    public int ElementoQuimicosId { get; set; }

    [JsonPropertyName("simbolo")]
    public string Simbolo { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; set; }

    [JsonPropertyName("unidad")]
    public string Unidad { get; set; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public sealed class AuditoriaElementoCalculado
{
    [JsonPropertyName("analisisSueloCalculoElementoQuimicoId")]
    public int AnalisisSueloCalculoElementoQuimicoId { get; set; }

    [JsonPropertyName("elementoQuimicosId")]
    public int ElementoQuimicosId { get; set; }

    [JsonPropertyName("simbolo")]
    public string Simbolo { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("cantidadIngresada")]
    public decimal CantidadIngresada { get; set; }

    [JsonPropertyName("cantidadConvertidaLbMz")]
    public decimal? CantidadConvertidaLbMz { get; set; }

    [JsonPropertyName("requerimientoCalculado")]
    public decimal? RequerimientoCalculado { get; set; }

    [JsonPropertyName("clasificacion")]
    public string? Clasificacion { get; set; }

    [JsonPropertyName("observacion")]
    public string? Observacion { get; set; }

    [JsonPropertyName("incluirCalculosComplementarios")]
    public bool IncluirCalculosComplementarios { get; set; }

    [JsonPropertyName("activo")]
    public bool Activo { get; set; }
}

public sealed class AuditoriaHistorialItem
{
    [JsonPropertyName("bitacoraDetalleId")]
    public long BitacoraDetalleId { get; set; }

    [JsonPropertyName("fechaHoraUtc")]
    public DateTime FechaHoraUtc { get; set; }

    [JsonPropertyName("entidad")]
    public string Entidad { get; set; } = string.Empty;

    [JsonPropertyName("entidadId")]
    public string EntidadId { get; set; } = string.Empty;

    [JsonPropertyName("operacion")]
    public string Operacion { get; set; } = string.Empty;

    [JsonPropertyName("valoresAnteriores")]
    public string ValoresAnteriores { get; set; } = string.Empty;

    [JsonPropertyName("valoresNuevos")]
    public string ValoresNuevos { get; set; } = string.Empty;

    [JsonPropertyName("propiedadesModificadas")]
    public string PropiedadesModificadas { get; set; } = string.Empty;

    [JsonPropertyName("bitacoraId")]
    public Guid BitacoraId { get; set; }

    [JsonPropertyName("usuarioId")]
    public int? UsuarioId { get; set; }

    [JsonPropertyName("usuarioNombre")]
    public string UsuarioNombre { get; set; } = string.Empty;

    [JsonPropertyName("rolNombre")]
    public string RolNombre { get; set; } = string.Empty;

    [JsonPropertyName("accion")]
    public string Accion { get; set; } = string.Empty;

    [JsonPropertyName("modulo")]
    public string Modulo { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("paginaOrigen")]
    public string PaginaOrigen { get; set; } = string.Empty;

    [JsonPropertyName("exitoso")]
    public bool Exitoso { get; set; }

    [JsonPropertyName("codigoEstado")]
    public int CodigoEstado { get; set; }

    [JsonPropertyName("dispositivo")]
    public string Dispositivo { get; set; } = string.Empty;

    [JsonPropertyName("plataforma")]
    public string Plataforma { get; set; } = string.Empty;

    [JsonPropertyName("versionApp")]
    public string VersionApp { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;
}
