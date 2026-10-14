using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class Categoria
{
    public int CategoriaId { get; set; }

    public int NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string ColorHex { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
