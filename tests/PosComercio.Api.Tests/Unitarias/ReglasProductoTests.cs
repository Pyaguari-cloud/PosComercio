using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Services.Catalogo;

namespace PosComercio.Api.Tests.Unitarias;

public class ReglasProductoTests
{
    private static VarianteNuevaRequest Variante(
        string? talla = null, string? color = null, string? sku = null,
        decimal costo = 30m, decimal lista = 55m, decimal minimo = 48m) =>
        new(talla, color, sku, costo, lista, minimo,
            StockPercha: 0, StockBodega: 0, StockMinimoAlerta: 1,
            UbicacionBodegaId: null, ReferenciaBodega: null, ReferenciaPercha: null);

    [Fact]
    public void ValidarVariantesNuevas_ProductoConTallasCorrecto_NoDevuelveError()
    {
        var variantes = new[] { Variante("38", "Blanco", "PU-38"), Variante("39", "Blanco", "PU-39") };

        Assert.Null(ReglasProducto.ValidarVariantesNuevas(tieneVariantes: true, variantes));
    }

    [Fact]
    public void ValidarVariantesNuevas_ProductoSimpleConDosVariantes_DevuelveError()
    {
        var variantes = new[] { Variante(), Variante() };

        Assert.Equal(ErroresCatalogo.ProductoSimpleUnaVariante,
            ReglasProducto.ValidarVariantesNuevas(tieneVariantes: false, variantes));
    }

    [Fact]
    public void ValidarVariantesNuevas_ProductoSimpleConTalla_DevuelveError()
    {
        var variantes = new[] { Variante(talla: "40") };

        Assert.Equal(ErroresCatalogo.ProductoSimpleSinAtributos,
            ReglasProducto.ValidarVariantesNuevas(tieneVariantes: false, variantes));
    }

    [Fact]
    public void ValidarVariantesNuevas_VarianteSinTallaNiColor_DevuelveError()
    {
        var variantes = new[] { Variante("40", "Negro"), Variante() };

        Assert.Equal(ErroresCatalogo.VarianteSinAtributos,
            ReglasProducto.ValidarVariantesNuevas(tieneVariantes: true, variantes));
    }

    [Fact]
    public void ValidarVariantesNuevas_MismaTallaYColorConMayusculasYEspacios_DevuelveVarianteRepetida()
    {
        var variantes = new[] { Variante("40", "Negro"), Variante(" 40 ", "negro") };

        var error = ReglasProducto.ValidarVariantesNuevas(tieneVariantes: true, variantes);

        Assert.NotNull(error);
        Assert.Equal("catalogo.variante_repetida", error.Codigo);
    }

    [Fact]
    public void ValidarVariantesNuevas_SkuRepetidoConDistintoFormato_DevuelveError()
    {
        var variantes = new[] { Variante("38", sku: "pu-1"), Variante("39", sku: " PU-1 ") };

        Assert.Equal(ErroresCatalogo.SkuRepetidoEnPeticion,
            ReglasProducto.ValidarVariantesNuevas(tieneVariantes: true, variantes));
    }

    [Fact]
    public void ValidarPrecios_MinimoMayorQueLista_DevuelveError()
    {
        Assert.Equal(ErroresCatalogo.MinimoMayorQueLista,
            ReglasProducto.ValidarPrecios(costo: 30m, lista: 55m, minimo: 60m));
    }
}