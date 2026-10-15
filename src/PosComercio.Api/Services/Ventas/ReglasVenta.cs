using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Dtos.Ventas;

namespace PosComercio.Api.Services.Ventas;

/// <summary>
/// Reglas que se validan SIN ir a la base de datos.
/// Son funciones puras: misma entrada → misma salida. Ideales para pruebas unitarias.
/// </summary>
public static class ReglasVenta
{
    public static decimal CalcularTotal(IEnumerable<ItemVentaRequest> items) =>
        items.Sum(i => i.Cantidad * i.PrecioVendidoUnitario);

    /// <summary>Devuelve el primer error encontrado, o null si todo está bien.</summary>
    public static Error? Validar(RegistrarVentaRequest request)
    {
        var hayDuplicados = request.Items
            .GroupBy(i => (i.VarianteId, i.SalioDe))
            .Any(g => g.Count() > 1);

        if (hayDuplicados)
        {
            return ErroresVentas.ItemsDuplicados;
        }

        var montos = request.Items.Select(i => i.PrecioVendidoUnitario)
            .Concat(request.Pagos.Select(p => p.Monto));

        if (montos.Any(m => decimal.Round(m, 2) != m))
        {
            return ErroresVentas.MasDeDosDecimales;
        }

        var total = CalcularTotal(request.Items);
        var pagado = request.Pagos.Sum(p => p.Monto);

        if (total != pagado)
        {
            return ErroresVentas.PagosNoCuadran(total, pagado);
        }

        return null;
    }
}