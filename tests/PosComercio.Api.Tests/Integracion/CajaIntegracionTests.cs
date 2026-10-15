using System.Net;
using System.Net.Http.Json;
using PosComercio.Api.Dtos.Caja;
using PosComercio.Api.Tests.Integracion.Infraestructura;

namespace PosComercio.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class CajaIntegracionTests(ApiFactory factory) : PruebaIntegracionBase(factory)
{
    [Fact]
    public async Task CierreDeCaja_CuadraElEfectivo_YBloqueaNuevasVentas()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        (await AbrirCajaAsync(vendedor, montoInicial: 20m)).EnsureSuccessStatusCode();

        (await VenderAsync(vendedor, escenario.VarianteConStockId, 1, 50m)).EnsureSuccessStatusCode();
        (await vendedor.PostAsJsonAsync("/api/caja/movimientos",
            new { tipo = "GASTO", monto = 2m, concepto = "Fundas" })).EnsureSuccessStatusCode();

        var cierre = await vendedor.PostAsJsonAsync("/api/caja/cerrar", new { efectivoContado = 68m });

        Assert.Equal(HttpStatusCode.OK, cierre.StatusCode);
        var resultado = await cierre.Content.ReadFromJsonAsync<CierreCajaDto>();
        Assert.Equal(68m, resultado!.EfectivoEsperado);               // 20 + 50 − 2
        Assert.Equal(0m, resultado.Diferencia);

        var ventaDespues = await VenderAsync(vendedor, escenario.VarianteConStockId, 1, 50m);
        Assert.Equal(HttpStatusCode.Conflict, ventaDespues.StatusCode);
        Assert.Equal("caja.sin_caja_abierta", await LeerCodigoErrorAsync(ventaDespues));
    }

    [Fact]
    public async Task AbrirUnaSegundaCaja_SeRechaza()
    {
        var escenario = await DatosDePrueba.CrearNegocioAsync();
        var vendedor = await IniciarSesionAsync(escenario.VendedorLogin);
        (await AbrirCajaAsync(vendedor)).EnsureSuccessStatusCode();

        var segunda = await AbrirCajaAsync(vendedor);

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        Assert.Equal("caja.ya_abierta", await LeerCodigoErrorAsync(segunda));
    }
}