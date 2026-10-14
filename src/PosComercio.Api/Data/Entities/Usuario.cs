using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class Usuario
{
    public int UsuarioId { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string UsuarioLogin { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PinHash { get; set; }

    public string Rol { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<CajaMovimiento> CajaMovimientos { get; set; } = new List<CajaMovimiento>();

    public virtual ICollection<CajaTurno> CajaTurnoUsuarioNavigations { get; set; } = new List<CajaTurno>();

    public virtual ICollection<CajaTurno> CajaTurnoUsuarios { get; set; } = new List<CajaTurno>();

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();

    public virtual ICollection<VentaPago> VentaPagos { get; set; } = new List<VentaPago>();

    public virtual ICollection<Venta> VentaUsuarioNavigations { get; set; } = new List<Venta>();

    public virtual ICollection<Venta> VentaUsuarios { get; set; } = new List<Venta>();
}
