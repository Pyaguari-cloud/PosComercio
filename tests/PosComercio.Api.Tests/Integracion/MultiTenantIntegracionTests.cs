using System.Net;
using System.Net.Http.Json;
using PosComercio.Api.Dtos.Comunes;
using PosComercio.Api.Dtos.Productos;
using PosComercio.Api.Tests.Integracion.Infraestructura;

namespace PosComercio.Api.Tests.Integracion;

[Collection(ColeccionApi.Nombre)]
public class MultiTenantIntegracionTests(ApiFactory factory) : PruebaIntegracionBase(factory)
{
    [Fact]
    public async Task UnNegocio_NoVeLosProductosDeOtro()
    {
        var negocioA = await DatosDePrueba.CrearNegocioAsync();
        var negocioB = await DatosDePrueba.CrearNegocioAsync();
        var clienteB = await IniciarSesionAsync(negocioB.DuenoLogin);

        var productoAjeno = await clienteB.GetAsync($"/api/productos/{negocioA.ProductoId}");
        var lista = await clienteB.GetFromJsonAsync<PaginaDto<ProductoDto>>("/api/productos");

        Assert.Equal(HttpStatusCode.NotFound, productoAjeno.StatusCode);
        Assert.Equal(negocioB.ProductoId, Assert.Single(lista!.Items).ProductoId);
    }

    [Fact]
    public async Task UnNegocio_NoPuedeVenderVariantesDeOtro()
    {
        var negocioA = await DatosDePrueba.CrearNegocioAsync();
        var negocioB = await DatosDePrueba.CrearNegocioAsync();
        var clienteB = await IniciarSesionAsync(negocioB.VendedorLogin);
        (await AbrirCajaAsync(clienteB)).EnsureSuccessStatusCode();

        var respuesta = await VenderAsync(clienteB, negocioA.VarianteConStockId, 1, 50m);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("ventas.variante_no_encontrada", await LeerCodigoErrorAsync(respuesta));
    }

    [Fact]
    public async Task SinToken_TodoEstaProtegido()
    {
        var anonimo = CrearCliente();

        var respuesta = await anonimo.GetAsync("/api/productos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}