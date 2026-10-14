using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class VentaDetalle
{
    public long VentaDetalleId { get; set; }

    public int NegocioId { get; set; }

    public long VentaId { get; set; }

    public int VarianteId { get; set; }

    public string NombreProductoHistorico { get; set; } = null!;

    public string SalioDeUbicacion { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal PrecioCostoHistorico { get; set; }

    public decimal PrecioListaUnitario { get; set; }

    public decimal PrecioMinimoHistorico { get; set; }

    public decimal PrecioVendidoUnitario { get; set; }

    public int? AutorizadoPorUsuarioId { get; set; }

    public decimal? DescuentoRegateoUnitario { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? GananciaLinea { get; set; }

    public virtual ProductoVariante ProductoVariante { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; }

    public virtual Venta Venta { get; set; } = null!;
}
