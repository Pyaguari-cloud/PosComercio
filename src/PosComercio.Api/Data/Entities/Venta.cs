using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class Venta
{
    public long VentaId { get; set; }

    public int NegocioId { get; set; }

    public int NumeroVenta { get; set; }

    public int UsuarioId { get; set; }

    public long TurnoId { get; set; }

    public DateTime FechaVenta { get; set; }

    public decimal TotalVenta { get; set; }

    public string Estado { get; set; } = null!;

    public string? NotaVenta { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public int? UsuarioAnulaId { get; set; }

    public string? MotivoAnulacion { get; set; }

    public Guid? ClientGuid { get; set; }

    public virtual CajaTurno CajaTurno { get; set; } = null!;

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual Usuario? UsuarioAnula { get; set; }

    public virtual Usuario Vendedor { get; set; } = null!;

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();

    public virtual ICollection<VentaPago> VentaPagos { get; set; } = new List<VentaPago>();
}
