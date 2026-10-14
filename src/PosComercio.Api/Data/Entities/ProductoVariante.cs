using System;
using System.Collections.Generic;

namespace PosComercio.Api.Data.Entities;

public partial class ProductoVariante
{
    public int VarianteId { get; set; }

    public int NegocioId { get; set; }

    public int ProductoId { get; set; }

    public int? UbicacionBodegaId { get; set; }

    public string? ReferenciaBodega { get; set; }

    public string? ReferenciaPercha { get; set; }

    public string? CodigoSku { get; set; }

    public bool EsVarianteUnica { get; set; }

    public string? AtributosResumen { get; set; }

    public string? AtributosJson { get; set; }

    public string? Talla { get; set; }

    public string? Color { get; set; }

    public string? ImagenVarianteUrl { get; set; }

    public decimal PrecioCosto { get; set; }

    public decimal PrecioVentaLista { get; set; }

    public decimal PrecioMinimoRegateo { get; set; }

    public int StockPercha { get; set; }

    public int StockBodega { get; set; }

    public int? StockTotal { get; set; }

    public int StockMinimoAlerta { get; set; }

    public bool Activo { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();

    public virtual Producto Producto { get; set; } = null!;

    public virtual UbicacionesBodega? UbicacionesBodega { get; set; }

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
}
