using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Dtos.Productos;

namespace PosComercio.Api.Services.Catalogo;

/// <summary>Reglas del catálogo que se validan sin ir a la base de datos.</summary>
public static class ReglasProducto
{
    public static Error? ValidarVariantesNuevas(bool tieneVariantes, IReadOnlyList<VarianteNuevaRequest> variantes)
    {
        if (!tieneVariantes)
        {
            if (variantes.Count != 1)
            {
                return ErroresCatalogo.ProductoSimpleUnaVariante;
            }

            if (Atributos.Limpiar(variantes[0].Talla) is not null || Atributos.Limpiar(variantes[0].Color) is not null)
            {
                return ErroresCatalogo.ProductoSimpleSinAtributos;
            }
        }
        else
        {
            if (variantes.Any(v => Atributos.Limpiar(v.Talla) is null && Atributos.Limpiar(v.Color) is null))
            {
                return ErroresCatalogo.VarianteSinAtributos;
            }

            var repetida = variantes
                .GroupBy(v => Atributos.Clave(v.Talla, v.Color))
                .FirstOrDefault(g => g.Count() > 1);

            if (repetida is not null)
            {
                var ejemplo = repetida.First();
                return ErroresCatalogo.VarianteRepetida(ejemplo.Talla, ejemplo.Color);
            }
        }

        foreach (var variante in variantes)
        {
            if (ValidarPrecios(variante.PrecioCosto, variante.PrecioVentaLista, variante.PrecioMinimoRegateo) is { } error)
            {
                return error;
            }
        }

        var skus = variantes
            .Select(v => Atributos.NormalizarSku(v.CodigoSku))
            .OfType<string>()          // descarta los null
            .ToList();

        if (skus.Count != skus.Distinct().Count())
        {
            return ErroresCatalogo.SkuRepetidoEnPeticion;
        }

        return null;
    }

    public static Error? ValidarPrecios(decimal costo, decimal lista, decimal minimo)
    {
        if (new[] { costo, lista, minimo }.Any(p => decimal.Round(p, 2) != p))
        {
            return ErroresCatalogo.MasDeDosDecimales;
        }

        if (minimo > lista)
        {
            return ErroresCatalogo.MinimoMayorQueLista;
        }

        return null;
    }
}