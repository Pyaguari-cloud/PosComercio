using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class UbicacionesBodega
{
    public int UbicacionId { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<ProductoVariante> ProductoVariantes { get; set; } = new List<ProductoVariante>();
}
