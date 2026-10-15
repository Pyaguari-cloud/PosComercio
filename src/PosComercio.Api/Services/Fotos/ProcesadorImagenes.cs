using PosComercio.Api.Common.Resultados;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PosComercio.Api.Services.Fotos;

/// <summary>Imagen ya validada, enderezada, reducida y sin metadatos.</summary>
public sealed record ImagenProcesada(
    MemoryStream Contenido,
    string ContentType,
    string Extension,
    int Ancho,
    int Alto);

public interface IProcesadorImagenes
{
    Task<Resultado<ImagenProcesada>> ProcesarAsync(Stream entrada, CancellationToken ct);
}

public sealed class ProcesadorImagenes(ILogger<ProcesadorImagenes> logger) : IProcesadorImagenes
{
    public const long TamanoMaximoBytes = 10 * 1024 * 1024;   // 10 MB
    private const long PixelesMaximos = 40_000_000;          // 40 megapíxeles
    private const int LadoMaximo = 1200;
    private const int CalidadWebp = 80;

    public async Task<Resultado<ImagenProcesada>> ProcesarAsync(Stream entrada, CancellationToken ct)
    {
        // Copiamos a memoria para poder leer el archivo dos veces (identificar y luego cargar).
        // El tamaño máximo ya lo limita el controller, antes de llegar aquí.
        using var original = new MemoryStream();
        await entrada.CopyToAsync(original, ct);

        if (original.Length == 0)
        {
            return ErroresFotos.ArchivoVacio;
        }

        if (original.Length > TamanoMaximoBytes)
        {
            return ErroresFotos.ArchivoMuyGrande;
        }

        // 1. Identificar: lee SOLO la cabecera del archivo (formato y dimensiones), sin abrirlo entero
        original.Position = 0;
        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(original, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            return ErroresFotos.NoEsImagen;
        }

        if (info.Metadata.DecodedImageFormat is not (JpegFormat or PngFormat or WebpFormat))
        {
            return ErroresFotos.FormatoNoSoportado;
        }

        // 2. Protección contra "bombas de descompresión"
        if ((long)info.Width * info.Height > PixelesMaximos)
        {
            return ErroresFotos.DemasiadosPixeles;
        }

        // 3. Ahora sí, cargar la imagen completa
        original.Position = 0;
        Image imagen;
        try
        {
            imagen = await Image.LoadAsync(original, ct);
        }
        catch (InvalidImageContentException)
        {
            return ErroresFotos.NoEsImagen;
        }

        using (imagen)
        {
            // Endereza según la orientación que guardó la cámara
            imagen.Mutate(x => x.AutoOrient());

            // Reduce solo si es más grande que el máximo (nunca agranda)
            if (imagen.Width > LadoMaximo || imagen.Height > LadoMaximo)
            {
                imagen.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,                         // mantiene la proporción
                    Size = new Size(LadoMaximo, LadoMaximo)
                }));
            }

            // Borra metadatos: GPS, modelo del celular, fecha, etc.
            imagen.Metadata.ExifProfile = null;
            imagen.Metadata.IptcProfile = null;
            imagen.Metadata.XmpProfile = null;

            var salida = new MemoryStream();
            await imagen.SaveAsWebpAsync(salida, new WebpEncoder { Quality = CalidadWebp }, ct);
            salida.Position = 0;

            logger.LogInformation("Imagen procesada: {AnchoOriginal}x{AltoOriginal} ({BytesOriginal} B) → {Ancho}x{Alto} ({Bytes} B)",
                info.Width, info.Height, original.Length, imagen.Width, imagen.Height, salida.Length);

            return new ImagenProcesada(salida, "image/webp", ".webp", imagen.Width, imagen.Height);
        }
    }
}