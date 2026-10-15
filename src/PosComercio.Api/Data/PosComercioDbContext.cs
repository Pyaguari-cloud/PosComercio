using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Data.Entities;

namespace PosComercio.Api.Data;

public partial class PosComercioDbContext : DbContext
{
    public PosComercioDbContext(DbContextOptions<PosComercioDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CajaMovimiento> CajaMovimientos { get; set; }

    public virtual DbSet<CajaTurno> CajaTurnos { get; set; }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<MovimientosInventario> MovimientosInventarios { get; set; }

    public virtual DbSet<Negocio> Negocios { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ProductoVariante> ProductoVariantes { get; set; }

    public virtual DbSet<UbicacionesBodega> UbicacionesBodegas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Venta> Ventas { get; set; }

    public virtual DbSet<VentaDetalle> VentaDetalles { get; set; }

    public virtual DbSet<VentaPago> VentaPagos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CajaMovimiento>(entity =>
        {
            entity.HasIndex(e => new { e.NegocioId, e.TurnoId }, "IX_CajaMov_Turno");

            entity.HasIndex(e => e.ClientGuid, "UX_CajaMov_ClientGuid")
                .IsUnique()
                .HasFilter("([ClientGuid] IS NOT NULL)");

            entity.Property(e => e.Concepto).HasMaxLength(200);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Tipo)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.CajaTurno).WithMany(p => p.CajaMovimientos)
                .HasPrincipalKey(p => new { p.NegocioId, p.TurnoId })
                .HasForeignKey(d => new { d.NegocioId, d.TurnoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CajaMov_Turno");

            entity.HasOne(d => d.Usuario).WithMany(p => p.CajaMovimientos)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.UsuarioId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CajaMov_Usuario");
        });

        modelBuilder.Entity<CajaTurno>(entity =>
        {
            entity.HasKey(e => e.TurnoId);

            entity.HasIndex(e => new { e.NegocioId, e.TurnoId }, "UQ_Turnos_Negocio").IsUnique();

            entity.HasIndex(e => e.NegocioId, "UX_Turnos_UnAbierto")
                .IsUnique()
                .HasFilter("([Estado]='ABIERTO')");

            entity.Property(e => e.Diferencia)
                .HasComputedColumnSql("([EfectivoContado]-[EfectivoEsperado])", false)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.EfectivoContado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EfectivoEsperado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Estado)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ABIERTO");
            entity.Property(e => e.FechaApertura).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GananciaDia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontoInicial).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Nota).HasMaxLength(250);
            entity.Property(e => e.TotalEfectivoVentas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalGastosRetiros).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalIngresosExtra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalTarjeta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalTransferencias).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalVentas).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.UsuarioAbre).WithMany(p => p.CajaTurnoUsuarios)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.UsuarioAbreId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Turnos_Abre");

            entity.HasOne(d => d.UsuarioCierra).WithMany(p => p.CajaTurnoUsuarioNavigations)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.UsuarioCierraId })
                .HasConstraintName("FK_Turnos_Cierra");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasIndex(e => new { e.NegocioId, e.CategoriaId }, "UQ_Categorias_Negocio").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ColorHex)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValue("#3B82F6");
            entity.Property(e => e.Nombre).HasMaxLength(100);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Categoria)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categorias_Negocios");
        });

        modelBuilder.Entity<MovimientosInventario>(entity =>
        {
            entity.HasKey(e => e.MovimientoId);

            entity.ToTable("MovimientosInventario");

            entity.HasIndex(e => new { e.NegocioId, e.VarianteId, e.FechaMovimiento }, "IX_Mov_Variante").IsDescending(false, false, true);

            entity.HasIndex(e => e.ClientGuid, "UX_Mov_ClientGuid")
                .IsUnique()
                .HasFilter("([ClientGuid] IS NOT NULL)");

            entity.Property(e => e.FechaMovimiento).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.NotaExplicativa).HasMaxLength(250);
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UbicacionDestino)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.UbicacionOrigen)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Usuario).WithMany(p => p.MovimientosInventarios)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.UsuarioId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mov_Usuarios");

            entity.HasOne(d => d.Variante).WithMany(p => p.MovimientosInventarios)
                .HasPrincipalKey(p => new { p.NegocioId, p.VarianteId })
                .HasForeignKey(d => new { d.NegocioId, d.VarianteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mov_Variantes");

            entity.HasOne(d => d.Venta).WithMany(p => p.MovimientosInventarios)
                .HasPrincipalKey(p => new { p.NegocioId, p.VentaId })
                .HasForeignKey(d => new { d.NegocioId, d.VentaId })
                .HasConstraintName("FK_Mov_Ventas");
        });

        modelBuilder.Entity<Negocio>(entity =>
        {
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Moneda)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("USD")
                .IsFixedLength();
            entity.Property(e => e.NombreComercial).HasMaxLength(150);
            entity.Property(e => e.PlanActual)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PRUEBA");
            entity.Property(e => e.TelefonoContacto).HasMaxLength(30);
            entity.Property(e => e.TipoNegocio)
                .HasMaxLength(80)
                .HasDefaultValue("General");
            entity.Property(e => e.ZonaHoraria)
                .HasMaxLength(50)
                .HasDefaultValue("America/Guayaquil");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasIndex(e => new { e.NegocioId, e.Activo, e.Nombre }, "IX_Productos_Busqueda");

            entity.HasIndex(e => new { e.NegocioId, e.ProductoId }, "UQ_Productos_Negocio").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ImagenUrl).HasMaxLength(500);
            entity.Property(e => e.Marca).HasMaxLength(100);
            entity.Property(e => e.Nombre).HasMaxLength(150);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Productos)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Negocios");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos)
                .HasPrincipalKey(p => new { p.NegocioId, p.CategoriaId })
                .HasForeignKey(d => new { d.NegocioId, d.CategoriaId })
                .HasConstraintName("FK_Productos_Categorias");
        });

        modelBuilder.Entity<ProductoVariante>(entity =>
        {
            entity.HasKey(e => e.VarianteId);

            entity.HasIndex(e => new { e.NegocioId, e.ProductoId }, "IX_Variantes_Producto");

            entity.HasIndex(e => e.NegocioId, "IX_Variantes_ReponerPercha").HasFilter("([StockPercha]=(0) AND [StockBodega]>(0) AND [Activo]=(1))");

            entity.HasIndex(e => new { e.NegocioId, e.Talla }, "IX_Variantes_Talla");

            entity.HasIndex(e => new { e.NegocioId, e.VarianteId }, "UQ_Variantes_Negocio").IsUnique();

            entity.HasIndex(e => new { e.NegocioId, e.CodigoSku }, "UX_Variantes_Sku")
                .IsUnique()
                .HasFilter("([CodigoSku] IS NOT NULL)");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.AtributosResumen).HasMaxLength(150);
            entity.Property(e => e.CodigoSku).HasMaxLength(80);
            entity.Property(e => e.Color)
                .HasMaxLength(40)
                .HasComputedColumnSql("(CONVERT([nvarchar](40),json_value([AtributosJson],'$.color')))", false);
            entity.Property(e => e.EsVarianteUnica).HasDefaultValue(true);
            entity.Property(e => e.ImagenVarianteUrl).HasMaxLength(500);
            entity.Property(e => e.PrecioCosto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioMinimoRegateo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVentaLista).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenciaBodega).HasMaxLength(150);
            entity.Property(e => e.ReferenciaPercha).HasMaxLength(100);
            entity.Property(e => e.StockMinimoAlerta).HasDefaultValue(1);
            entity.Property(e => e.StockTotal).HasComputedColumnSql("([StockPercha]+[StockBodega])", false);
            entity.Property(e => e.Talla)
                .HasMaxLength(20)
                .HasComputedColumnSql("(CONVERT([nvarchar](20),json_value([AtributosJson],'$.talla')))", false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Producto).WithMany(p => p.ProductoVariantes)
                .HasPrincipalKey(p => new { p.NegocioId, p.ProductoId })
                .HasForeignKey(d => new { d.NegocioId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Variantes_Productos");

            entity.HasOne(d => d.UbicacionesBodega).WithMany(p => p.ProductoVariantes)
                .HasPrincipalKey(p => new { p.NegocioId, p.UbicacionId })
                .HasForeignKey(d => new { d.NegocioId, d.UbicacionBodegaId })
                .HasConstraintName("FK_Variantes_Ubicaciones");
        });

        modelBuilder.Entity<UbicacionesBodega>(entity =>
        {
            entity.HasKey(e => e.UbicacionId);

            entity.ToTable("UbicacionesBodega");

            entity.HasIndex(e => new { e.NegocioId, e.UbicacionId }, "UQ_Ubicaciones_Negocio").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Nombre).HasMaxLength(100);

            entity.HasOne(d => d.Negocio).WithMany(p => p.UbicacionesBodegas)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ubicaciones_Negocios");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.UsuarioLogin, "UQ_Usuarios_Login").IsUnique();

            entity.HasIndex(e => new { e.NegocioId, e.UsuarioId }, "UQ_Usuarios_Negocio").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PinHash).HasMaxLength(255);
            entity.Property(e => e.Rol)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Vendedor");
            entity.Property(e => e.UsuarioLogin).HasMaxLength(80);

            entity.HasOne(d => d.Negocio).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Negocios");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasIndex(e => new { e.NegocioId, e.FechaVenta }, "IX_Ventas_Cuaderno").IsDescending(false, true);

            entity.HasIndex(e => new { e.NegocioId, e.TurnoId }, "IX_Ventas_Turno");

            entity.HasIndex(e => new { e.NegocioId, e.VentaId }, "UQ_Ventas_Negocio").IsUnique();

            entity.HasIndex(e => new { e.NegocioId, e.NumeroVenta }, "UQ_Ventas_Numero").IsUnique();

            entity.HasIndex(e => e.ClientGuid, "UX_Ventas_ClientGuid")
                .IsUnique()
                .HasFilter("([ClientGuid] IS NOT NULL)");

            entity.Property(e => e.Estado)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("COMPLETADA");
            entity.Property(e => e.FechaVenta).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(250);
            entity.Property(e => e.NotaVenta).HasMaxLength(250);
            entity.Property(e => e.TotalVenta).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Venta)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Negocios");

            entity.HasOne(d => d.CajaTurno).WithMany(p => p.Venta)
                .HasPrincipalKey(p => new { p.NegocioId, p.TurnoId })
                .HasForeignKey(d => new { d.NegocioId, d.TurnoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Turno");

            entity.HasOne(d => d.UsuarioAnula).WithMany(p => p.VentaUsuarios)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.UsuarioAnulaId })
                .HasConstraintName("FK_Ventas_UsuarioAnula");

            entity.HasOne(d => d.Vendedor).WithMany(p => p.VentaUsuarioNavigations)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.UsuarioId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Usuarios");
        });

        modelBuilder.Entity<VentaDetalle>(entity =>
        {
            entity.HasIndex(e => new { e.NegocioId, e.VentaId }, "IX_Detalles_Venta");

            entity.HasIndex(e => new { e.NegocioId, e.VentaDetalleId }, "UQ_Detalles_Negocio").IsUnique();

            entity.Property(e => e.DescuentoRegateoUnitario)
                .HasComputedColumnSql("([PrecioListaUnitario]-[PrecioVendidoUnitario])", false)
                .HasColumnType("decimal(19, 2)");
            entity.Property(e => e.GananciaLinea)
                .HasComputedColumnSql("([Cantidad]*([PrecioVendidoUnitario]-[PrecioCostoHistorico]))", false)
                .HasColumnType("decimal(30, 2)");
            entity.Property(e => e.NombreProductoHistorico).HasMaxLength(250);
            entity.Property(e => e.PrecioCostoHistorico).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioListaUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioMinimoHistorico).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVendidoUnitario).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SalioDeUbicacion)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Subtotal)
                .HasComputedColumnSql("([Cantidad]*[PrecioVendidoUnitario])", false)
                .HasColumnType("decimal(29, 2)");

            entity.HasOne(d => d.AutorizadoPor).WithMany(p => p.VentaDetalles)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.AutorizadoPorUsuarioId })
                .HasConstraintName("FK_Detalles_Autoriza");

            entity.HasOne(d => d.ProductoVariante).WithMany(p => p.VentaDetalles)
                .HasPrincipalKey(p => new { p.NegocioId, p.VarianteId })
                .HasForeignKey(d => new { d.NegocioId, d.VarianteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Detalles_Variantes");

            entity.HasOne(d => d.Venta).WithMany(p => p.VentaDetalles)
                .HasPrincipalKey(p => new { p.NegocioId, p.VentaId })
                .HasForeignKey(d => new { d.NegocioId, d.VentaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Detalles_Ventas");
        });

        modelBuilder.Entity<VentaPago>(entity =>
        {
            entity.HasKey(e => e.PagoId);

            entity.HasIndex(e => new { e.NegocioId, e.VentaId }, "IX_Pagos_Venta");

            entity.HasIndex(e => e.ClientGuid, "UX_Pagos_ClientGuid")
                .IsUnique()
                .HasFilter("([ClientGuid] IS NOT NULL)");

            entity.Property(e => e.FechaPago).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MetodoPago)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenciaTransferencia).HasMaxLength(100);

            entity.HasOne(d => d.Usuario).WithMany(p => p.VentaPagos)
                .HasPrincipalKey(p => new { p.NegocioId, p.UsuarioId })
                .HasForeignKey(d => new { d.NegocioId, d.UsuarioId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Usuarios");

            entity.HasOne(d => d.Venta).WithMany(p => p.VentaPagos)
                .HasPrincipalKey(p => new { p.NegocioId, p.VentaId })
                .HasForeignKey(d => new { d.NegocioId, d.VentaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Ventas");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
