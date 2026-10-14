/* =========================================================================
   POS COMERCIO - MVP
   Script 03: Consultas útiles (verificación y base para el backend)

   Ejecuta cada bloque por separado: selecciónalo y presiona F5.
   Cada consulta corresponde a una pantalla o función del MVP.
   ========================================================================= */

USE PosComercio;
GO

DECLARE @NegocioId INT = (SELECT TOP 1 NegocioId FROM dbo.Negocios WHERE NombreComercial = N'Zapatería Demo');

/* ---------- 1. Inventario completo (pantalla "Buscar producto") ---------- */
SELECT p.Nombre, p.Marca, c.Nombre AS Categoria,
       v.CodigoSku, v.Talla, v.Color,
       v.StockPercha, v.StockBodega, v.StockTotal,
       v.PrecioVentaLista, v.PrecioMinimoRegateo,
       u.Nombre AS UbicacionBodega, v.ReferenciaBodega, v.ReferenciaPercha
FROM dbo.ProductoVariantes v
JOIN dbo.Productos p        ON p.NegocioId = v.NegocioId AND p.ProductoId = v.ProductoId
LEFT JOIN dbo.Categorias c  ON c.NegocioId = p.NegocioId AND c.CategoriaId = p.CategoriaId
LEFT JOIN dbo.UbicacionesBodega u ON u.NegocioId = v.NegocioId AND u.UbicacionId = v.UbicacionBodegaId
WHERE v.NegocioId = @NegocioId AND v.Activo = 1
ORDER BY p.Nombre, v.Talla, v.Color;

/* ---------- 2. Buscar por talla ("¿qué tengo en 40?") ---------- */
SELECT p.Nombre, p.Marca, v.Color, v.StockPercha, v.StockBodega, v.PrecioVentaLista
FROM dbo.ProductoVariantes v
JOIN dbo.Productos p ON p.NegocioId = v.NegocioId AND p.ProductoId = v.ProductoId
WHERE v.NegocioId = @NegocioId AND v.Talla = N'40' AND v.Activo = 1 AND v.StockTotal > 0;

/* ---------- 3. Reponer percha (0 afuera, hay en bodega) ---------- */
SELECT p.Nombre, v.AtributosResumen, v.StockBodega,
       u.Nombre AS UbicacionBodega, v.ReferenciaBodega, v.ReferenciaPercha
FROM dbo.ProductoVariantes v
JOIN dbo.Productos p ON p.NegocioId = v.NegocioId AND p.ProductoId = v.ProductoId
LEFT JOIN dbo.UbicacionesBodega u ON u.NegocioId = v.NegocioId AND u.UbicacionId = v.UbicacionBodegaId
WHERE v.NegocioId = @NegocioId AND v.StockPercha = 0 AND v.StockBodega > 0 AND v.Activo = 1;

/* ---------- 4. Stock bajo (para pedir al proveedor) ---------- */
SELECT p.Nombre, v.AtributosResumen, v.StockTotal, v.StockMinimoAlerta
FROM dbo.ProductoVariantes v
JOIN dbo.Productos p ON p.NegocioId = v.NegocioId AND p.ProductoId = v.ProductoId
WHERE v.NegocioId = @NegocioId AND v.Activo = 1 AND v.StockTotal <= v.StockMinimoAlerta;

/* ---------- 5. Cuaderno del día ----------
   Las fechas se guardan en UTC. Aquí calculamos el rango "hoy" en hora local.
   SQL Server usa nombres de zona de Windows ('SA Pacific Standard Time' = UTC-5).
   En el backend este rango lo calculará .NET a partir de Negocios.ZonaHoraria. */
DECLARE @HoyLocal DATE = CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SA Pacific Standard Time' AS DATE);
DECLARE @DesdeUtc DATETIME2 = CAST((CAST(@HoyLocal AS DATETIME2) AT TIME ZONE 'SA Pacific Standard Time') AT TIME ZONE 'UTC' AS DATETIME2);
DECLARE @HastaUtc DATETIME2 = DATEADD(DAY, 1, @DesdeUtc);

SELECT v.NumeroVenta,
       CAST(v.FechaVenta AT TIME ZONE 'UTC' AT TIME ZONE 'SA Pacific Standard Time' AS DATETIME2(0)) AS HoraLocal,
       us.Nombre AS Vendedor,
       d.NombreProductoHistorico, d.PrecioListaUnitario, d.PrecioVendidoUnitario,
       d.DescuentoRegateoUnitario, v.TotalVenta, v.Estado
FROM dbo.Ventas v
JOIN dbo.Usuarios us      ON us.NegocioId = v.NegocioId AND us.UsuarioId = v.UsuarioId
JOIN dbo.VentaDetalles d  ON d.NegocioId = v.NegocioId AND d.VentaId = v.VentaId
WHERE v.NegocioId = @NegocioId AND v.FechaVenta >= @DesdeUtc AND v.FechaVenta < @HastaUtc
ORDER BY v.FechaVenta DESC;

/* ---------- 6. Resumen de caja abierta (base del cierre) ----------
   Efectivo esperado = inicial + ventas en efectivo + ingresos extra - gastos - retiros
   Con los datos de prueba debe dar 48.50 */
SELECT t.TurnoId,
       t.MontoInicial,
       vt.TotalVentas,
       pg.Efectivo        AS VentasEfectivo,
       pg.Transferencias,
       pg.Tarjeta,
       cm.GastosRetiros,
       cm.IngresosExtra,
       t.MontoInicial + pg.Efectivo + cm.IngresosExtra - cm.GastosRetiros AS EfectivoEsperado,
       vt.Ganancia        AS GananciaDia   -- solo para el dueño
FROM dbo.CajaTurnos t
OUTER APPLY (
    SELECT ISNULL(SUM(v.TotalVenta), 0) AS TotalVentas,
           ISNULL((SELECT SUM(d.GananciaLinea)
                   FROM dbo.VentaDetalles d
                   JOIN dbo.Ventas v2 ON v2.NegocioId = d.NegocioId AND v2.VentaId = d.VentaId
                   WHERE v2.NegocioId = t.NegocioId AND v2.TurnoId = t.TurnoId AND v2.Estado = 'COMPLETADA'), 0) AS Ganancia
    FROM dbo.Ventas v
    WHERE v.NegocioId = t.NegocioId AND v.TurnoId = t.TurnoId AND v.Estado = 'COMPLETADA'
) vt
OUTER APPLY (
    SELECT ISNULL(SUM(CASE WHEN p.MetodoPago = 'EFECTIVO'      THEN p.Monto END), 0) AS Efectivo,
           ISNULL(SUM(CASE WHEN p.MetodoPago = 'TRANSFERENCIA' THEN p.Monto END), 0) AS Transferencias,
           ISNULL(SUM(CASE WHEN p.MetodoPago = 'TARJETA'       THEN p.Monto END), 0) AS Tarjeta
    FROM dbo.VentaPagos p
    JOIN dbo.Ventas v ON v.NegocioId = p.NegocioId AND v.VentaId = p.VentaId
    WHERE v.NegocioId = t.NegocioId AND v.TurnoId = t.TurnoId AND v.Estado = 'COMPLETADA'
) pg
OUTER APPLY (
    SELECT ISNULL(SUM(CASE WHEN m.Tipo IN ('GASTO','RETIRO') THEN m.Monto END), 0) AS GastosRetiros,
           ISNULL(SUM(CASE WHEN m.Tipo = 'INGRESO_EXTRA'     THEN m.Monto END), 0) AS IngresosExtra
    FROM dbo.CajaMovimientos m
    WHERE m.NegocioId = t.NegocioId AND m.TurnoId = t.TurnoId
) cm
WHERE t.NegocioId = @NegocioId AND t.Estado = 'ABIERTO';

/* ---------- 7. Kardex de una variante ---------- */
SELECT m.FechaMovimiento, m.TipoMovimiento, m.UbicacionOrigen, m.UbicacionDestino,
       m.Cantidad, m.StockPerchaResultante, m.StockBodegaResultante,
       us.Nombre AS Usuario, m.NotaExplicativa
FROM dbo.MovimientosInventario m
JOIN dbo.Usuarios us ON us.NegocioId = m.NegocioId AND us.UsuarioId = m.UsuarioId
JOIN dbo.ProductoVariantes v ON v.NegocioId = m.NegocioId AND v.VarianteId = m.VarianteId
WHERE m.NegocioId = @NegocioId AND v.CodigoSku = N'NK-URB-40-NEG'
ORDER BY m.MovimientoId;

/* ---------- 8. Regateo por vendedor (cuánto rebaja cada uno) ---------- */
SELECT us.Nombre AS Vendedor,
       COUNT(*)                                   AS LineasVendidas,
       SUM(d.Cantidad * d.DescuentoRegateoUnitario) AS TotalRebajado,
       AVG(d.DescuentoRegateoUnitario)            AS RebajaPromedioPorPar,
       SUM(CASE WHEN d.AutorizadoPorUsuarioId IS NOT NULL THEN 1 ELSE 0 END) AS VentasBajoMinimo
FROM dbo.VentaDetalles d
JOIN dbo.Ventas v    ON v.NegocioId = d.NegocioId AND v.VentaId = d.VentaId
JOIN dbo.Usuarios us ON us.NegocioId = v.NegocioId AND us.UsuarioId = v.UsuarioId
WHERE d.NegocioId = @NegocioId AND v.Estado = 'COMPLETADA'
GROUP BY us.Nombre;
GO
