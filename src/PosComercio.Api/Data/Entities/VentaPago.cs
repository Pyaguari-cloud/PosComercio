using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class VentaPago
{
    public long PagoId { get; set; }

    public int NegocioId { get; set; }

    public long VentaId { get; set; }

    public int UsuarioId { get; set; }

    public string MetodoPago { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? ReferenciaTransferencia { get; set; }

    public DateTime FechaPago { get; set; }

    public Guid? ClientGuid { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Venta Venta { get; set; } = null!;
}
