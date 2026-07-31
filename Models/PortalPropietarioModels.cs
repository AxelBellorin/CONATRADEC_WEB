namespace CONATRADEC.AdminWeb.Models;

public sealed class PortalPropietarioResumen
{
    public bool Vinculado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public PortalPropietarioDatos? Propietario { get; set; }
    public PortalPropietarioTotales Resumen { get; set; } = new();
    public List<PortalPropietarioTerreno> Terrenos { get; set; } = [];
}

public sealed class PortalPropietarioDatos
{
    public int PropietarioId { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
}

public sealed class PortalPropietarioTotales
{
    public int TotalTerrenos { get; set; }
    public decimal TotalManzanas { get; set; }
    public int TotalPlantas { get; set; }
    public decimal ProduccionEstimadaQuintales { get; set; }
    public int TotalAnalisis { get; set; }
}

public sealed class PortalPropietarioTerreno
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public decimal ExtensionManzanas { get; set; }
    public DateTime FechaIngreso { get; set; }
    public int CantidadPlantas { get; set; }
    public decimal CantidadQuintalesOro { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public int TotalAnalisis { get; set; }
    public DateTime? FechaUltimoAnalisis { get; set; }

    public string Ubicacion =>
        string.Join(
            ", ",
            new[] { Municipio, Departamento }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

    public string UltimoAnalisisTexto =>
        FechaUltimoAnalisis.HasValue
            ? FechaUltimoAnalisis.Value.ToLocalTime().ToString("dd/MM/yyyy")
            : "Sin análisis";
}

public sealed class PortalCentroGeoespacialResponse
{
    public bool Vinculado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime ActualizadoUtc { get; set; }
    public PortalPropietarioDatos? Propietario { get; set; }
    public PortalCentroGeoespacialResumen Resumen { get; set; } = new();
    public List<PortalCentroTerreno> Terrenos { get; set; } = [];
    public List<PortalCentroNutriente> NutrientesDisponibles { get; set; } = [];
    public PortalCentroClima Clima { get; set; } = new();
}

public sealed class PortalCentroGeoespacialResumen
{
    public int TotalTerrenos { get; set; }
    public decimal ExtensionTotalManzanas { get; set; }
    public int EstadoCritico { get; set; }
    public int RequierenAtencion { get; set; }
    public int EstadoEstable { get; set; }
    public int SinAnalisis { get; set; }
    public int AlertasActivas { get; set; }
    public decimal? TemperaturaPromedioLocal { get; set; }
    public decimal? HumedadPromedioLocal { get; set; }
    public decimal? PrecipitacionMaximaLocal { get; set; }
    public decimal? VientoMaximoLocal { get; set; }
}

public sealed class PortalCentroTerreno
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public decimal ExtensionManzanas { get; set; }
    public DateTime FechaIngreso { get; set; }
    public int CantidadPlantas { get; set; }
    public decimal CantidadQuintalesOro { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public int MunicipioId { get; set; }
    public string Municipio { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public string Departamento { get; set; } = string.Empty;
    public int TotalAnalisis { get; set; }
    public int? AnalisisSueloCalculoId { get; set; }
    public DateTime? FechaUltimoAnalisis { get; set; }
    public decimal? Ph { get; set; }
    public decimal? MateriaOrganica { get; set; }
    public decimal? AcidezTotal { get; set; }
    public decimal? Cice { get; set; }
    public decimal? SaturacionBases { get; set; }
    public string RecomendacionGeneral { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public string Nivel { get; set; } = "SIN_ANALISIS";
    public string Estado { get; set; } = "Sin análisis";
    public string ColorEstado { get; set; } = "#94A3B8";
    public List<PortalCentroElemento> Elementos { get; set; } = [];
    public List<PortalCentroAlerta> Alertas { get; set; } = [];

    public bool TieneCoordenadas => Latitud != 0 && Longitud != 0;

    public string Ubicacion =>
        string.Join(
            ", ",
            new[] { Municipio, Departamento }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

    public string FechaUltimoAnalisisTexto =>
        FechaUltimoAnalisis.HasValue
            ? FechaUltimoAnalisis.Value.ToLocalTime().ToString("dd/MM/yyyy")
            : "Sin análisis";
}

public sealed class PortalCentroElemento
{
    public int ElementoQuimicosId { get; set; }
    public string Simbolo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public string Clasificacion { get; set; } = string.Empty;
}

public sealed class PortalCentroAlerta
{
    public string Clave { get; set; } = string.Empty;
    public string Nivel { get; set; } = "ATENCION";
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
    public decimal? Umbral { get; set; }
}

public sealed class PortalCentroNutriente
{
    public int ElementoQuimicosId { get; set; }
    public string Simbolo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
}

public sealed class PortalCentroClima
{
    public bool Disponible { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string Licencia { get; set; } = string.Empty;
    public DateTime ActualizadoUtc { get; set; }
    public string UnidadTemperatura { get; set; } = "°C";
    public string UnidadPrecipitacion { get; set; } = "mm";
    public string UnidadViento { get; set; } = "km/h";
    public decimal? TemperaturaMinima { get; set; }
    public decimal? TemperaturaMaxima { get; set; }
    public decimal? HumedadMinima { get; set; }
    public decimal? HumedadMaxima { get; set; }
    public decimal? PrecipitacionMaxima { get; set; }
    public decimal? VientoMaximo { get; set; }
    public List<PortalCentroClimaPunto> Puntos { get; set; } = [];
}

public sealed class PortalCentroClimaPunto
{
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? TemperaturaAparente { get; set; }
    public decimal? HumedadRelativa { get; set; }
    public decimal? Precipitacion { get; set; }
    public decimal? VelocidadViento { get; set; }
    public decimal? Nubosidad { get; set; }
    public int? CodigoClima { get; set; }
    public DateTimeOffset? FechaObservacion { get; set; }
}

public sealed class PortalHistorialTerreno
{
    public int TerrenoId { get; set; }
    public string CodigoTerreno { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public decimal ExtensionManzanas { get; set; }
    public decimal ProduccionQuintalesOro { get; set; }
    public List<PortalHistorialAnalisis> Analisis { get; set; } = [];
}

public sealed class PortalHistorialAnalisis
{
    public int AnalisisSueloCalculoId { get; set; }
    public int AnalisisSueloId { get; set; }
    public string Identificador { get; set; } = string.Empty;
    public DateOnly FechaLaboratorio { get; set; }
    public DateTime FechaRegistro { get; set; }
    public decimal Ph { get; set; }
    public decimal? MateriaOrganica { get; set; }
    public decimal? AcidezTotal { get; set; }
    public decimal? Cice { get; set; }
    public decimal? SaturacionBases { get; set; }
    public string Nivel { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string RecomendacionGeneral { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public List<PortalCentroElemento> Elementos { get; set; } = [];
}
