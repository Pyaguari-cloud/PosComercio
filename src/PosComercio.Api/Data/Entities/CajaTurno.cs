using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class CajaTurno
{
    public long TurnoId { get; set; }

    public int NegocioId { get; set; }

    public int UsuarioAbreId { get; set; }

    public DateTime FechaApertura { get; set; }

    public decimal MontoInicial { get; set; }

    public string Estado { get; set; } = null!;

    public int? UsuarioCierraId { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal? TotalVentas { get; set; }

    public decimal? TotalEfectivoVentas { get; set; }

    public decimal? TotalTransferencias { get; set; }

    public decimal? TotalTarjeta { get; set; }

    public decimal? TotalGastosRetiros { get; set; }

    public decimal? TotalIngresosExtra { get; set; }

    public decimal? GananciaDia { get; set; }

    public decimal? EfectivoEsperado { get; set; }

    public decimal? EfectivoContado { get; set; }

    public decimal? Diferencia { get; set; }

    public string? Nota { get; set; }

    public virtual ICollection<CajaMovimiento> CajaMovimientos { get; set; } = new List<CajaMovimiento>();

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Usuario? UsuarioNavigation { get; set; }

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
