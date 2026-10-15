using PosComercio.Api.Dtos.Ventas;
using PosComercio.Api.Services.Ventas;

namespace PosComercio.Api.Tests.Unitarias;

public class ReglasVentaTests
{
    // ---- Ayudantes para armar datos de prueba de forma legible ----

    private static ItemVentaRequest Item(int varianteId, int cantidad, decimal precio, string salioDe = "PERCHA") =>
        new(varianteId, cantidad, salioDe, precio);

    private static PagoVentaRequest Pago(decimal monto, string metodo = "EFECTIVO") =>
        new(metodo, monto, null);

    private static RegistrarVentaRequest Venta(ItemVentaRequest[] items, params PagoVentaRequest[] pagos) =>
        new(items, pagos, Nota: null, PinAutorizacion: null, ClientGuid: null);

    // ---- Pruebas ----

    [Fact]
    public void CalcularTotal_VariosItems_SumaCantidadPorPrecio()
    {
        var items = new[] { Item(1, cantidad: 2, precio: 22.50m), Item(2, cantidad: 1, precio: 10m) };

        var total = ReglasVenta.CalcularTotal(items);

        Assert.Equal(55.00m, total);
    }

    [Fact]
    public void Validar_VentaCorrecta_NoDevuelveError()
    {
        var venta = Venta([Item(1, 1, 22m)], Pago(22m));

        Assert.Null(ReglasVenta.Validar(venta));
    }

    [Fact]
    public void Validar_PagosMixtosQueCuadran_NoDevuelveError()
    {
        var venta = Venta(
            [Item(1, 1, 60m), Item(15, 1, 28m, salioDe: "BODEGA")],
            Pago(50m), Pago(38m, "TRANSFERENCIA"));

        Assert.Null(ReglasVenta.Validar(venta));
    }

    [Fact]
    public void Validar_PagosNoCuadran_DevuelvePagosNoCuadran()
    {
        var venta = Venta([Item(9, 1, 25m)], Pago(20m));

        var error = ReglasVenta.Validar(venta);

        Assert.NotNull(error);
        Assert.Equal("ventas.pagos_no_cuadran", error.Codigo);
        Assert.Contains("$20.00", error.Mensaje);
        Assert.Contains("$25.00", error.Mensaje);
    }

    [Fact]
    public void Validar_MismaVarianteYUbicacionRepetida_DevuelveItemsDuplicados()
    {
        var venta = Venta([Item(1, 1, 30m), Item(1, 1, 30m)], Pago(60m));

        Assert.Equal(ErroresVentas.ItemsDuplicados, ReglasVenta.Validar(venta));
    }

    [Fact]
    public void Validar_MismaVarianteDesdePerchaYBodega_EsValida()
    {
        // Un par de la vitrina y otro de la bodega: es una venta legítima
        var venta = Venta(
            [Item(1, 1, 30m, salioDe: "PERCHA"), Item(1, 1, 30m, salioDe: "BODEGA")],
            Pago(60m));

        Assert.Null(ReglasVenta.Validar(venta));
    }

    [Fact]
    public void Validar_PrecioConTresDecimales_DevuelveMasDeDosDecimales()
    {
        var venta = Venta([Item(1, 1, 22.555m)], Pago(22.555m));

        Assert.Equal(ErroresVentas.MasDeDosDecimales, ReglasVenta.Validar(venta));
    }
}