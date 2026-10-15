using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Fotos;

public static class ErroresFotos
{
    public static readonly Error ArchivoVacio = new(
        "fotos.archivo_vacio",
        "El archivo está vacío.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ArchivoMuyGrande = new(
        "fotos.archivo_muy_grande",
        "La foto pesa demasiado. El máximo es 10 MB.",
        StatusCodes.Status413PayloadTooLarge);

    public static readonly Error NoEsImagen = new(
        "fotos.no_es_imagen",
        "El archivo no es una imagen válida.",
        StatusCodes.Status400BadRequest);

    public static readonly Error FormatoNoSoportado = new(
        "fotos.formato_no_soportado",
        "Formato no soportado. Usa una foto JPG, PNG o WebP.",
        StatusCodes.Status415UnsupportedMediaType);

    public static readonly Error DemasiadosPixeles = new(
        "fotos.demasiados_pixeles",
        "La imagen tiene dimensiones demasiado grandes.",
        StatusCodes.Status400BadRequest);
}