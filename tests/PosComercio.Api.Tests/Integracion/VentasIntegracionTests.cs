using System.Net;
using System.Net.Http.Json;
using PosComercio.Api.Dtos.Caja;
using PosComercio.Api.Dtos.Ventas;
using PosComercio.Api.Tests.Integracion.Infraestructura;

namespace PosComercio.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class VentasIntegracionTests(ApiFactory factory) : PruebaIntegracionBase(factory)
{
    [Fact]
    public async Task VentaSimple_DescuentaStockYSeRefleja_EnLaCaja()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        var dueno = await IniciarSesionAsync(escenario.DuenoLogin);
        (await AbrirCajaAsync(vendedor, montoInicial: 20m)).EnsureSuccessStatusCode();

        var respuesta = await VenderAsync(vendedor, escenario.VarianteConStockId, cantidad: 2, precio: 45m);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var variante = await LeerVarianteAsync(dueno, escenario.ProductoId, escenario.VarianteConStockId);
        Assert.Equal(3, variante.StockPercha);                        // 5 − 2

        var caja = await dueno.GetFromJsonAsync<EstadoCajaDto>("/api/caja/actual");
        Assert.Equal(1, caja!.Turno!.CantidadVentas);
        Assert.Equal(90m, caja.Turno.TotalVentas);                    // 2 × 45
        Assert.Equal(110m, caja.Turno.EfectivoEsperado);              // 20 + 90
        Assert.Equal(50m, caja.Turno.Ganancia);                       // (45 − 20) × 2
    }

    [Fact]
    public async Task VentaConUnItemSinStock_NoDescuentaNingunItem()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        (await AbrirCajaAsync(vendedor)).EnsureSuccessStatusCode();

        // El primer ítem SÍ tiene stock; el segundo pide 2 cuando solo queda 1
        var respuesta = await vendedor.PostAsJsonAsync("/api/ventas", new
        {
            items = new[]
            {
                new { varianteId = escenario.VarianteConStockId,     cantidad = 1, salioDe = "PERCHA", precioVendidoUnitario = 50m },
                new { varianteId = escenario.VarianteUltimaUnidadId, cantidad = 2, salioDe = "PERCHA", precioVendidoUnitario = 30m }
            },
            pagos = new[] { new { metodoPago = "EFECTIVO", monto = 110m } }
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("ventas.stock_insuficiente", await LeerCodigoErrorAsync(respuesta));

        // La transacción deshizo TODO: el primer ítem tampoco se descontó
        var conStock = await LeerVarianteAsync(vendedor, escenario.ProductoId, escenario.VarianteConStockId);
        var ultima = await LeerVarianteAsync(vendedor, escenario.ProductoId, escenario.VarianteUltimaUnidadId);
        Assert.Equal(5, conStock.StockPercha);
        Assert.Equal(1, ultima.StockPercha);
    }

    [Fact]
    public async Task PrecioBajoMinimo_SinPinSeRechaza_ConPinDelDuenoSeRegistra()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        (await AbrirCajaAsync(vendedor)).EnsureSuccessStatusCode();

        // Mínimo 40, se intenta vender a 35
        var sinPin = await VenderAsync(vendedor, escenario.VarianteConStockId, 1, 35m);

        Assert.Equal(HttpStatusCode.Forbidden, sinPin.StatusCode);
        Assert.Equal("autorizacion.requerida", await LeerCodigoErrorAsync(sinPin));

        var conPin = await VenderAsync(vendedor, escenario.VarianteConStockId, 1, 35m,
            pinAutorizacion: DatosDePrueba.PinDueno);

        Assert.Equal(HttpStatusCode.Created, conPin.StatusCode);
        var venta = await conPin.Content.ReadFromJsonAsync<VentaDto>();
        Assert.Equal("Dueño", venta!.Detalles.Single().AutorizadoPor);
    }

    [Fact]
    public async Task MismaVentaEnviadaDosVeces_ConElMismoClientGuid_SeRegistraUnaSolaVez()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        var dueno = await IniciarSesionAsync(escenario.DuenoLogin);
        (await AbrirCajaAsync(vendedor)).EnsureSuccessStatusCode();
        var guid = Guid.NewGuid();

        var primera = await VenderAsync(vendedor, escenario.VarianteConStockId, 1, 50m, clientGuid: guid);
        var segunda = await VenderAsync(vendedor, escenario.VarianteConStockId, 1, 50m, clientGuid: guid);

        var ventaUno = await primera.Content.ReadFromJsonAsync<VentaDto>();
        var ventaDos = await segunda.Content.ReadFromJsonAsync<VentaDto>();
        Assert.Equal(ventaUno!.VentaId, ventaDos!.VentaId);

        var variante = await LeerVarianteAsync(dueno, escenario.ProductoId, escenario.VarianteConStockId);
        Assert.Equal(4, variante.StockPercha);                        // se descontó UNA sola vez

        var caja = await dueno.GetFromJsonAsync<EstadoCajaDto>("/api/caja/actual");
        Assert.Equal(1, caja!.Turno!.CantidadVentas);
    }

    [Fact]
    public async Task DosPersonasVendenLaUltimaUnidadAlMismoTiempo_SoloUnaLaVende()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        var dueno = await IniciarSesionAsync(escenario.DuenoLogin);
        (await AbrirCajaAsync(vendedor)).EnsureSuccessStatusCode();

        // Las dos ventas salen AL MISMO TIEMPO
        var respuestas = await Task.WhenAll(
            VenderAsync(vendedor, escenario.VarianteUltimaUnidadId, 1, 30m),
            VenderAsync(dueno, escenario.VarianteUltimaUnidadId, 1, 30m));

        var codigos = respuestas.Select(r => r.StatusCode).OrderBy(c => c).ToList();
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], codigos);

        var variante = await LeerVarianteAsync(dueno, escenario.ProductoId, escenario.VarianteUltimaUnidadId);
        Assert.Equal(0, variante.StockPercha);                        // nunca negativo
    }

    [Fact]
    public async Task AnularVenta_DevuelveElStockYLaSacaDeLaCaja()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        var dueno = await IniciarSesionAsync(escenario.DuenoLogin);
        (await AbrirCajaAsync(vendedor, montoInicial: 20m)).EnsureSuccessStatusCode();

        var vendida = await VenderAsync(vendedor, escenario.VarianteConStockId, 1, 50m);
        var venta = await vendida.Content.ReadFromJsonAsync<VentaDto>();

        var anulacion = await vendedor.PostAsJsonAsync($"/api/ventas/{venta!.VentaId}/anular", new
        {
            motivo = "El cliente se arrepintió",
            pinAutorizacion = DatosDePrueba.PinDueno
        });

        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
        var anulada = await anulacion.Content.ReadFromJsonAsync<VentaDto>();
        Assert.Equal("ANULADA", anulada!.Estado);

        var variante = await LeerVarianteAsync(dueno, escenario.ProductoId, escenario.VarianteConStockId);
        Assert.Equal(5, variante.StockPercha);                        // volvió a la percha

        var caja = await dueno.GetFromJsonAsync<EstadoCajaDto>("/api/caja/actual");
        Assert.Equal(0, caja!.Turno!.CantidadVentas);
        Assert.Equal(20m, caja.Turno.EfectivoEsperado);               // solo el sencillo
    }
}