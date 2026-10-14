/* =========================================================================
   POS COMERCIO - MVP
   Script 02: Datos de prueba ("Zapatería Demo")

   Crea: 1 negocio, 2 usuarios, categorías, ubicaciones, 6 productos con
   tallas (y una mochila sin tallas), el inventario inicial con su kardex,
   una caja abierta, 1 venta con regateo y pago mixto, y 1 gasto de caja.

   Ejecutar DESPUÉS del script 01, una sola vez (todo va en una transacción:
   si algo falla, no se guarda nada).
   ========================================================================= */

USE PosComercio;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;   -- cualquier error deshace toda la transacción

BEGIN TRANSACTION;

DECLARE @NegocioId INT, @DuenoId INT, @VendedorId INT;
DECLARE @CatMarca INT, @CatEconomico INT, @CatEscolar INT, @CatMochilas INT;
DECLARE @UbiA INT, @UbiB INT, @UbiAltillo INT, @UbiEscalera INT;
DECLARE @P1 INT, @P2 INT, @P3 INT, @P4 INT, @P5 INT, @P6 INT;

/* ---------- Negocio ---------- */
INSERT INTO dbo.Negocios (NombreComercial, TipoNegocio, TelefonoContacto, ConfiguracionJson, PlanActual, FechaFinPrueba)
VALUES (N'Zapatería Demo', N'Zapateria', N'0990000000',
        N'{"usaVariantes":true,"separaPerchaBodega":true,"permiteRegateo":true,"cierreCiego":true,"montoInicialSugerido":20}',
        'PRUEBA', DATEADD(DAY, 30, CAST(SYSUTCDATETIME() AS DATE)));
SET @NegocioId = SCOPE_IDENTITY();

/* ---------- Usuarios ----------
   PasswordHash y PinHash son marcadores. Cuando hagamos el backend,
   los reemplazamos por hashes reales generados desde .NET. */
INSERT INTO dbo.Usuarios (NegocioId, Nombre, UsuarioLogin, PasswordHash, Rol)
VALUES (@NegocioId, N'Dueño Demo', N'dueno@demo.com', N'PENDIENTE_HASH', 'Dueno');
SET @DuenoId = SCOPE_IDENTITY();

INSERT INTO dbo.Usuarios (NegocioId, Nombre, UsuarioLogin, PasswordHash, Rol)
VALUES (@NegocioId, N'Vendedor Demo', N'vendedor@demo.com', N'PENDIENTE_HASH', 'Vendedor');
SET @VendedorId = SCOPE_IDENTITY();

/* ---------- Categorías ---------- */
INSERT INTO dbo.Categorias (NegocioId, Nombre, ColorHex) VALUES (@NegocioId, N'Zapatos de Marca',   '#2563EB'); SET @CatMarca     = SCOPE_IDENTITY();
INSERT INTO dbo.Categorias (NegocioId, Nombre, ColorHex) VALUES (@NegocioId, N'Calzado Económico',  '#16A34A'); SET @CatEconomico = SCOPE_IDENTITY();
INSERT INTO dbo.Categorias (NegocioId, Nombre, ColorHex) VALUES (@NegocioId, N'Escolar',            '#EA580C'); SET @CatEscolar   = SCOPE_IDENTITY();
INSERT INTO dbo.Categorias (NegocioId, Nombre, ColorHex) VALUES (@NegocioId, N'Mochilas',           '#9333EA'); SET @CatMochilas  = SCOPE_IDENTITY();

/* ---------- Ubicaciones de bodega ---------- */
INSERT INTO dbo.UbicacionesBodega (NegocioId, Nombre, Descripcion) VALUES (@NegocioId, N'Estante A',        N'Entrando a la bodega, a mano derecha'); SET @UbiA        = SCOPE_IDENTITY();
INSERT INTO dbo.UbicacionesBodega (NegocioId, Nombre, Descripcion) VALUES (@NegocioId, N'Estante B',        N'Al fondo, junto a la ventana');         SET @UbiB        = SCOPE_IDENTITY();
INSERT INTO dbo.UbicacionesBodega (NegocioId, Nombre, Descripcion) VALUES (@NegocioId, N'Altillo',          N'Subiendo la escalera de madera');       SET @UbiAltillo  = SCOPE_IDENTITY();
INSERT INTO dbo.UbicacionesBodega (NegocioId, Nombre, Descripcion) VALUES (@NegocioId, N'Bodega Escalera',  N'Debajo de la escalera');                SET @UbiEscalera = SCOPE_IDENTITY();

/* ---------- Productos ---------- */
INSERT INTO dbo.Productos (NegocioId, CategoriaId, Nombre, Marca, TieneVariantes) VALUES (@NegocioId, @CatMarca,     N'Zapatilla Urbana',         N'Nike',     1); SET @P1 = SCOPE_IDENTITY();
INSERT INTO dbo.Productos (NegocioId, CategoriaId, Nombre, Marca, TieneVariantes) VALUES (@NegocioId, @CatMarca,     N'Zapatilla Running',        N'Adidas',   1); SET @P2 = SCOPE_IDENTITY();
INSERT INTO dbo.Productos (NegocioId, CategoriaId, Nombre, Marca, TieneVariantes) VALUES (@NegocioId, @CatEscolar,   N'Zapato Escolar Clásico',   N'Genérico', 1); SET @P3 = SCOPE_IDENTITY();
INSERT INTO dbo.Productos (NegocioId, CategoriaId, Nombre, Marca, TieneVariantes) VALUES (@NegocioId, @CatEconomico, N'Bota Casual de Cuero',     N'Genérico', 1); SET @P4 = SCOPE_IDENTITY();
INSERT INTO dbo.Productos (NegocioId, CategoriaId, Nombre, Marca, TieneVariantes) VALUES (@NegocioId, @CatEconomico, N'Sandalia Dama Verano',     N'Genérico', 1); SET @P5 = SCOPE_IDENTITY();
INSERT INTO dbo.Productos (NegocioId, CategoriaId, Nombre, Marca, TieneVariantes) VALUES (@NegocioId, @CatMochilas,  N'Mochila Escolar Reforzada', N'Totto',   0); SET @P6 = SCOPE_IDENTITY();

/* ---------- Variantes ----------
   Algunas tienen StockPercha = 0 y stock en bodega para probar "Reponer percha". */
INSERT INTO dbo.ProductoVariantes
    (NegocioId, ProductoId, UbicacionBodegaId, ReferenciaBodega, ReferenciaPercha,
     CodigoSku, EsVarianteUnica, AtributosResumen, AtributosJson,
     PrecioCosto, PrecioVentaLista, PrecioMinimoRegateo,
     StockPercha, StockBodega, StockMinimoAlerta)
VALUES
-- Zapatilla Urbana (Nike)
(@NegocioId, @P1, @UbiA, N'Cajas negras, fila 1',   N'Pared central, repisa 1', N'NK-URB-39-NEG', 0, N'Talla 39 | Negro',  N'{"talla":"39","color":"Negro"}',  38.00, 65.00, 55.00, 1, 2, 1),
(@NegocioId, @P1, @UbiA, N'Cajas negras, fila 1',   N'Pared central, repisa 1', N'NK-URB-40-NEG', 0, N'Talla 40 | Negro',  N'{"talla":"40","color":"Negro"}',  38.00, 65.00, 55.00, 2, 3, 1),
(@NegocioId, @P1, @UbiA, N'Cajas negras, fila 1',   N'Pared central, repisa 1', N'NK-URB-41-NEG', 0, N'Talla 41 | Negro',  N'{"talla":"41","color":"Negro"}',  38.00, 65.00, 55.00, 0, 2, 1),
(@NegocioId, @P1, @UbiA, N'Cajas negras, fila 2',   N'Pared central, repisa 1', N'NK-URB-40-BLA', 0, N'Talla 40 | Blanco', N'{"talla":"40","color":"Blanco"}', 38.00, 65.00, 55.00, 1, 1, 1),
-- Zapatilla Running (Adidas)
(@NegocioId, @P2, @UbiB, N'Cajas azules, arriba',   N'Pared central, repisa 2', N'AD-RUN-40-AZU', 0, N'Talla 40 | Azul',   N'{"talla":"40","color":"Azul"}',   45.00, 80.00, 68.00, 1, 1, 1),
(@NegocioId, @P2, @UbiB, N'Cajas azules, arriba',   N'Pared central, repisa 2', N'AD-RUN-42-AZU', 0, N'Talla 42 | Azul',   N'{"talla":"42","color":"Azul"}',   45.00, 80.00, 68.00, 0, 2, 1),
(@NegocioId, @P2, @UbiB, N'Cajas azules, arriba',   N'Pared central, repisa 2', N'AD-RUN-42-GRI', 0, N'Talla 42 | Gris',   N'{"talla":"42","color":"Gris"}',   45.00, 80.00, 68.00, 1, 0, 1),
-- Zapato Escolar
(@NegocioId, @P3, @UbiAltillo, N'Cajas cafés, lado izquierdo', N'Vitrina de entrada', N'ESC-CLA-34-NEG', 0, N'Talla 34 | Negro', N'{"talla":"34","color":"Negro"}', 12.00, 25.00, 20.00, 2, 6, 2),
(@NegocioId, @P3, @UbiAltillo, N'Cajas cafés, lado izquierdo', N'Vitrina de entrada', N'ESC-CLA-36-NEG', 0, N'Talla 36 | Negro', N'{"talla":"36","color":"Negro"}', 12.00, 25.00, 20.00, 3, 8, 2),
(@NegocioId, @P3, @UbiAltillo, N'Cajas cafés, lado derecho',   N'Vitrina de entrada', N'ESC-CLA-38-NEG', 0, N'Talla 38 | Negro', N'{"talla":"38","color":"Negro"}', 12.00, 25.00, 20.00, 0, 5, 2),
-- Bota Casual
(@NegocioId, @P4, @UbiEscalera, N'Bolsa plástica grande', N'Repisa baja, izquierda', N'BOT-CUE-38-CAF', 0, N'Talla 38 | Café', N'{"talla":"38","color":"Café"}', 22.00, 45.00, 38.00, 1, 1, 1),
(@NegocioId, @P4, @UbiEscalera, N'Bolsa plástica grande', N'Repisa baja, izquierda', N'BOT-CUE-39-CAF', 0, N'Talla 39 | Café', N'{"talla":"39","color":"Café"}', 22.00, 45.00, 38.00, 1, 0, 1),
-- Sandalia Dama
(@NegocioId, @P5, @UbiB, N'Caja blanca en el piso', N'Exhibidor giratorio', N'SAN-VER-36-BEI', 0, N'Talla 36 | Beige', N'{"talla":"36","color":"Beige"}', 6.00, 15.00, 12.00, 2, 3, 2),
(@NegocioId, @P5, @UbiB, N'Caja blanca en el piso', N'Exhibidor giratorio', N'SAN-VER-37-BEI', 0, N'Talla 37 | Beige', N'{"talla":"37","color":"Beige"}', 6.00, 15.00, 12.00, 2, 2, 2),
-- Mochila (producto simple: una sola variante invisible para el dueño)
(@NegocioId, @P6, @UbiEscalera, N'Apiladas junto a la puerta', N'Colgadas en la entrada', N'MOC-ESC-UNI', 1, NULL, NULL, 14.00, 30.00, 25.00, 3, 4, 2);

/* ---------- Kardex: carga inicial ----------
   Primero lo que entra a percha y luego lo que entra a bodega,
   para que el stock resultante de cada fila sea coherente. */
INSERT INTO dbo.MovimientosInventario
    (NegocioId, VarianteId, UsuarioId, TipoMovimiento, UbicacionOrigen, UbicacionDestino,
     Cantidad, StockPerchaResultante, StockBodegaResultante, NotaExplicativa)
SELECT NegocioId, VarianteId, @DuenoId, 'INICIAL', NULL, 'PERCHA',
       StockPercha, StockPercha, 0, N'Carga inicial de inventario'
FROM dbo.ProductoVariantes
WHERE NegocioId = @NegocioId AND StockPercha > 0;

INSERT INTO dbo.MovimientosInventario
    (NegocioId, VarianteId, UsuarioId, TipoMovimiento, UbicacionOrigen, UbicacionDestino,
     Cantidad, StockPerchaResultante, StockBodegaResultante, NotaExplicativa)
SELECT NegocioId, VarianteId, @DuenoId, 'INICIAL', NULL, 'BODEGA',
       StockBodega, StockPercha, StockBodega, N'Carga inicial de inventario'
FROM dbo.ProductoVariantes
WHERE NegocioId = @NegocioId AND StockBodega > 0;

/* ---------- Caja: el vendedor abre con $20 de sencillo ---------- */
DECLARE @TurnoId BIGINT;
INSERT INTO dbo.CajaTurnos (NegocioId, UsuarioAbreId, MontoInicial)
VALUES (@NegocioId, @VendedorId, 20.00);
SET @TurnoId = SCOPE_IDENTITY();

/* ---------- Venta de ejemplo ----------
   Zapatilla Urbana talla 40 negro, de percha.
   Lista $65, mínimo $55, se cerró en $58 tras el regateo.
   El cliente paga $30 en efectivo y $28 por transferencia.

   Este bloque es exactamente el patrón que replicará el backend:
   1) descontar stock de forma atómica, 2) cabecera, 3) detalle,
   4) pagos, 5) kardex. Todo dentro de la misma transacción. */
DECLARE @VarId INT, @Costo DECIMAL(18,2), @Lista DECIMAL(18,2), @Minimo DECIMAL(18,2), @NombreHist NVARCHAR(250);
DECLARE @VentaId BIGINT, @NumeroVenta INT, @PrecioVendido DECIMAL(18,2) = 58.00;

SELECT @VarId      = v.VarianteId,
       @Costo      = v.PrecioCosto,
       @Lista      = v.PrecioVentaLista,
       @Minimo     = v.PrecioMinimoRegateo,
       @NombreHist = p.Nombre + ISNULL(N' - ' + v.AtributosResumen, N'')
FROM dbo.ProductoVariantes v
JOIN dbo.Productos p ON p.NegocioId = v.NegocioId AND p.ProductoId = v.ProductoId
WHERE v.NegocioId = @NegocioId AND v.CodigoSku = N'NK-URB-40-NEG';

-- 1) Descontar stock (solo si hay; si no, error y se deshace todo)
UPDATE dbo.ProductoVariantes
SET StockPercha = StockPercha - 1
WHERE NegocioId = @NegocioId AND VarianteId = @VarId AND StockPercha >= 1;

IF @@ROWCOUNT = 0
    THROW 50001, N'No hay stock en percha para esta variante.', 1;

-- 2) Cabecera con número correlativo por negocio
SELECT @NumeroVenta = ISNULL(MAX(NumeroVenta), 0) + 1
FROM dbo.Ventas WITH (UPDLOCK, HOLDLOCK)
WHERE NegocioId = @NegocioId;

INSERT INTO dbo.Ventas (NegocioId, NumeroVenta, UsuarioId, TurnoId, TotalVenta, NotaVenta, ClientGuid)
VALUES (@NegocioId, @NumeroVenta, @VendedorId, @TurnoId, @PrecioVendido, N'Cliente regateó desde $65', NEWID());
SET @VentaId = SCOPE_IDENTITY();

-- 3) Detalle con la auditoría del regateo
INSERT INTO dbo.VentaDetalles
    (NegocioId, VentaId, VarianteId, NombreProductoHistorico, SalioDeUbicacion, Cantidad,
     PrecioCostoHistorico, PrecioListaUnitario, PrecioMinimoHistorico, PrecioVendidoUnitario)
VALUES
    (@NegocioId, @VentaId, @VarId, @NombreHist, 'PERCHA', 1,
     @Costo, @Lista, @Minimo, @PrecioVendido);

-- 4) Pago mixto
INSERT INTO dbo.VentaPagos (NegocioId, VentaId, UsuarioId, MetodoPago, Monto, ReferenciaTransferencia, ClientGuid)
VALUES (@NegocioId, @VentaId, @VendedorId, 'EFECTIVO',      30.00, NULL,              NEWID()),
       (@NegocioId, @VentaId, @VendedorId, 'TRANSFERENCIA', 28.00, N'Comprobante 4521', NEWID());

-- 5) Kardex con el stock resultante real
INSERT INTO dbo.MovimientosInventario
    (NegocioId, VarianteId, UsuarioId, TipoMovimiento, UbicacionOrigen, UbicacionDestino,
     Cantidad, StockPerchaResultante, StockBodegaResultante, VentaId, ClientGuid)
SELECT NegocioId, VarianteId, @VendedorId, 'VENTA', 'PERCHA', NULL,
       1, StockPercha, StockBodega, @VentaId, NEWID()
FROM dbo.ProductoVariantes
WHERE NegocioId = @NegocioId AND VarianteId = @VarId;

/* ---------- Gasto de caja ---------- */
INSERT INTO dbo.CajaMovimientos (NegocioId, TurnoId, UsuarioId, Tipo, Monto, Concepto, ClientGuid)
VALUES (@NegocioId, @TurnoId, @VendedorId, 'GASTO', 1.50, N'Fundas plásticas', NEWID());

COMMIT TRANSACTION;

PRINT N'Datos de prueba cargados. NegocioId = ' + CAST(@NegocioId AS NVARCHAR(10));
PRINT N'Efectivo esperado en caja: 20.00 inicial + 30.00 venta - 1.50 gasto = 48.50';
GO
