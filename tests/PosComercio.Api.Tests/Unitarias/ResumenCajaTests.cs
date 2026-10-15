using PosComercio.Api.Services.Caja;
namespace PosComercio.Api.Tests.Unitarias;

public class ResumenCajaTests
{
    [Fact]
    public void EfectivoEsperado_EscenarioDeLosDatosDePrueba_Da48Con50()
    {
        // Sencillo $20 + venta en efectivo $30 (los otros $28 fueron por transferencia) − gasto $1.50
        var resumen = new ResumenCaja(
            CantidadVentas: 1, TotalVentas: 58m, Efectivo: 30m, Transferencias: 28m, Tarjeta: 0m,
            GastosRetiros: 1.50m, IngresosExtra: 0m, Ganancia: 20m);

        Assert.Equal(48.50m, resumen.EfectivoEsperado(montoInicial: 20m));
    }

    [Fact]
    public void EfectivoEsperado_TransferenciasYTarjeta_NoSumanAlEfectivo()
    {
        var resumen = new ResumenCaja(
            CantidadVentas: 2, TotalVentas: 150m, Efectivo: 0m, Transferencias: 100m, Tarjeta: 50m,
            GastosRetiros: 0m, IngresosExtra: 0m, Ganancia: 40m);

        Assert.Equal(20m, resumen.EfectivoEsperado(montoInicial: 20m));
    }

    [Fact]
    public void EfectivoEsperado_IngresosSumanYRetirosRestan()
    {
        var resumen = new ResumenCaja(
            CantidadVentas: 3, TotalVentas: 107m, Efectivo: 107m, Transferencias: 0m, Tarjeta: 0m,
            GastosRetiros: 30m, IngresosExtra: 10m, Ganancia: 50m);

        Assert.Equal(107m, resumen.EfectivoEsperado(montoInicial: 20m));   // 20 + 107 + 10 − 30
    }
}