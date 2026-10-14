using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class CajaMovimiento
{
    public long CajaMovimientoId { get; set; }

    public int NegocioId { get; set; }

    public long TurnoId { get; set; }

    public int UsuarioId { get; set; }

    public string Tipo { get; set; } = null!;

    public decimal Monto { get; set; }

    public string Concepto { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public Guid? ClientGuid { get; set; }

    public virtual CajaTurno CajaTurno { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
