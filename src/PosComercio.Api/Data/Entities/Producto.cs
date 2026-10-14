using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class Producto
{
    public int ProductoId { get; set; }

    public int NegocioId { get; set; }

    public int? CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Descripcion { get; set; }

    public string? ImagenUrl { get; set; }

    public bool TieneVariantes { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Categoria? Categoria { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<ProductoVariante> ProductoVariantes { get; set; } = new List<ProductoVariante>();
}
