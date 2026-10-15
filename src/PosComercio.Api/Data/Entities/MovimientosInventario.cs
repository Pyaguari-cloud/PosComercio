using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class MovimientosInventario
{
    public long MovimientoId { get; set; }

    public int NegocioId { get; set; }

    public int VarianteId { get; set; }

    public int UsuarioId { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public string? UbicacionOrigen { get; set; }

    public string? UbicacionDestino { get; set; }

    public int Cantidad { get; set; }

    public int StockPerchaResultante { get; set; }

    public int StockBodegaResultante { get; set; }

    public long? VentaId { get; set; }

    public string? NotaExplicativa { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public Guid? ClientGuid { get; set; }

    public virtual ProductoVariante Variante { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Venta? Venta { get; set; }
}
