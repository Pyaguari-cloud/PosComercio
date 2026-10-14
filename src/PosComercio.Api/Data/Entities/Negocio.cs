using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class Negocio
{
    public int NegocioId { get; set; }

    public string NombreComercial { get; set; } = null!;

    public string TipoNegocio { get; set; } = null!;

    public string? TelefonoContacto { get; set; }

    public string Moneda { get; set; } = null!;

    public string ZonaHoraria { get; set; } = null!;

    public string? ConfiguracionJson { get; set; }

    public string PlanActual { get; set; } = null!;

    public DateOnly? FechaFinPrueba { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Categoria> Categoria { get; set; } = new List<Categoria>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual ICollection<UbicacionesBodega> UbicacionesBodegas { get; set; } = new List<UbicacionesBodega>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
