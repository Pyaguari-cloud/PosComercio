using Microsoft.Extensions.Time.Testing;
using PosComercio.Api.Common.Tiempo;

namespace PosComercio.Api.Tests.Unitarias;

public class RangoFechasTests
{
    private const string Guayaquil = "America/Guayaquil";   // UTC-5, sin horario de verano

    [Fact]
    public void DiaLocalEnUtc_Guayaquil_DesplazaCincoHoras()
    {
        var (desde, hasta) = RangoFechas.DiaLocalEnUtc(new DateOnly(2026, 10, 14), Guayaquil);

        Assert.Equal(new DateTime(2026, 10, 14, 5, 0, 0, DateTimeKind.Utc), desde);
        Assert.Equal(new DateTime(2026, 10, 15, 5, 0, 0, DateTimeKind.Utc), hasta);
    }

    [Fact]
    public void HoyLocal_NocheEnEcuador_SigueSiendoElDiaAnterior()
    {
        // 03:30 UTC del 15 de octubre = 22:30 del 14 de octubre en Ecuador
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 3, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 14), RangoFechas.HoyLocal(reloj, Guayaquil));
    }

    [Fact]
    public void HoyLocal_MediodiaEnEcuador_EsElMismoDia()
    {
        // 17:00 UTC = 12:00 en Ecuador
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 17, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 10, 15), RangoFechas.HoyLocal(reloj, Guayaquil));
    }
}