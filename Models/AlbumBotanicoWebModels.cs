using System.ComponentModel.DataAnnotations;

namespace CONATRADEC.AdminWeb.Models;

public sealed class AlbumApiRespuesta<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public sealed class AlbumCategoriaItem
{
    public int CategoriaAlbumBotanicoId { get; set; }
    public string NombreCategoria { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? RutaImagenPortada { get; set; }
    public bool Activo { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalRegistrosActivos { get; set; }
}

public sealed class AlbumCategoriaFormulario
{
    public int CategoriaAlbumBotanicoId { get; set; }

    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string NombreCategoria { get; set; } = string.Empty;

    [StringLength(
        500,
        ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class AlbumRegistroResumen
{
    public int AlbumBotanicoCafeId { get; set; }
    public int CategoriaAlbumBotanicoId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? NombreCientifico { get; set; }
    public string DescripcionCorta { get; set; } = string.Empty;
    public string? FotoPortada { get; set; }
    public int TotalFotos { get; set; }
    public bool Activo { get; set; }
    public bool CategoriaActiva { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class AlbumRegistroDetalle
{
    public int AlbumBotanicoCafeId { get; set; }
    public int CategoriaAlbumBotanicoId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public bool CategoriaActiva { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? NombreCientifico { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Caracteristicas { get; set; }
    public string? Sintomas { get; set; }
    public string? Causas { get; set; }
    public string? Recomendaciones { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<AlbumFotoItem> Fotos { get; set; } = [];
}

public sealed class AlbumRegistroFormulario
{
    public int AlbumBotanicoCafeId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Seleccione una categoría.")]
    public int CategoriaAlbumBotanicoId { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(
        200,
        ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(
        200,
        ErrorMessage = "El nombre científico no puede superar los 200 caracteres.")]
    public string? NombreCientifico { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    public string? Caracteristicas { get; set; }
    public string? Sintomas { get; set; }
    public string? Causas { get; set; }
    public string? Recomendaciones { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class AlbumFotoItem
{
    public int AlbumBotanicoCafeFotoId { get; set; }
    public string RutaFoto { get; set; } = string.Empty;
    public string? DescripcionFoto { get; set; }
    public bool EsPortada { get; set; }
    public int Orden { get; set; }
}

public sealed class AlbumFotoFormulario
{
    public int AlbumBotanicoCafeFotoId { get; set; }

    [StringLength(
        500,
        ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? DescripcionFoto { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "El orden debe ser mayor que cero.")]
    public int Orden { get; set; } = 1;
}

public sealed class AlbumCrearIdRespuesta
{
    public int AlbumBotanicoCafeId { get; set; }
}

public sealed class AlbumFotoSubidaRespuesta
{
    public int AlbumBotanicoCafeFotoId { get; set; }
    public string RutaFoto { get; set; } = string.Empty;
    public bool EsPortada { get; set; }
    public int Orden { get; set; }
}
