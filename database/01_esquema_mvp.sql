/* =========================================================================
   POS COMERCIO - MVP
   Script 01: Esquema de base de datos
   Motor: SQL Server 2016 o superior

   Ejecutar completo en SSMS (F5).
   Para empezar de cero durante el desarrollo:
       USE master;
       ALTER DATABASE PosComercio SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
       DROP DATABASE PosComercio;
   ========================================================================= */

IF DB_ID(N'PosComercio') IS NULL
    CREATE DATABASE PosComercio;
GO

USE PosComercio;
GO

/* =========================================================================
   MÓDULO 1: NÚCLEO SAAS Y USUARIOS
   ========================================================================= */

CREATE TABLE dbo.Negocios (
    NegocioId           INT IDENTITY(1,1) NOT NULL,
    NombreComercial     NVARCHAR(150) NOT NULL,
    TipoNegocio         NVARCHAR(80)  NOT NULL DEFAULT N'General',   -- Zapateria, Libreria, Bazar...
    TelefonoContacto    NVARCHAR(30)  NULL,
    Moneda              CHAR(3)       NOT NULL DEFAULT 'USD',
    ZonaHoraria         NVARCHAR(50)  NOT NULL DEFAULT N'America/Guayaquil', -- formato IANA (lo usa el backend)
    -- {"usaVariantes":true,"separaPerchaBodega":true,"permiteRegateo":true,
    --  "cierreCiego":true,"montoInicialSugerido":20}
    ConfiguracionJson   NVARCHAR(MAX) NULL,
    PlanActual          VARCHAR(20)   NOT NULL DEFAULT 'PRUEBA',
    FechaFinPrueba      DATE          NULL,
    Activo              BIT           NOT NULL DEFAULT 1,
    FechaCreacion       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Negocios PRIMARY KEY (NegocioId),
    CONSTRAINT CK_Negocios_Config CHECK (ConfiguracionJson IS NULL OR ISJSON(ConfiguracionJson) = 1),
    CONSTRAINT CK_Negocios_Plan   CHECK (PlanActual IN ('PRUEBA','BASICO','PRO','SUSPENDIDO'))
);
GO

CREATE TABLE dbo.Usuarios (
    UsuarioId       INT IDENTITY(1,1) NOT NULL,
    NegocioId       INT           NOT NULL,
    Nombre          NVARCHAR(100) NOT NULL,
    UsuarioLogin    NVARCHAR(80)  NOT NULL,          -- correo o celular, único en todo el sistema
    PasswordHash    NVARCHAR(255) NOT NULL,          -- lo genera el backend (nunca texto plano)
    PinHash         NVARCHAR(255) NULL,              -- PIN rápido de 4 dígitos, hasheado
    Rol             VARCHAR(20)   NOT NULL DEFAULT 'Vendedor',
    Activo          BIT           NOT NULL DEFAULT 1,
    FechaCreacion   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Usuarios PRIMARY KEY (UsuarioId),
    CONSTRAINT UQ_Usuarios_Login   UNIQUE (UsuarioLogin),
    CONSTRAINT UQ_Usuarios_Negocio UNIQUE (NegocioId, UsuarioId),   -- para FKs compuestas
    CONSTRAINT FK_Usuarios_Negocios FOREIGN KEY (NegocioId) REFERENCES dbo.Negocios(NegocioId),
    CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN ('AdminSaaS','Dueno','Vendedor'))
);
GO

/* =========================================================================
   MÓDULO 2: CATÁLOGO, PERCHA Y BODEGA
   ========================================================================= */

CREATE TABLE dbo.Categorias (
    CategoriaId INT IDENTITY(1,1) NOT NULL,
    NegocioId   INT           NOT NULL,
    Nombre      NVARCHAR(100) NOT NULL,
    ColorHex    VARCHAR(7)    NOT NULL DEFAULT '#3B82F6',
    Activo      BIT           NOT NULL DEFAULT 1,

    CONSTRAINT PK_Categorias PRIMARY KEY (CategoriaId),
    CONSTRAINT UQ_Categorias_Negocio UNIQUE (NegocioId, CategoriaId),
    CONSTRAINT FK_Categorias_Negocios FOREIGN KEY (NegocioId) REFERENCES dbo.Negocios(NegocioId)
);
GO

CREATE TABLE dbo.UbicacionesBodega (
    UbicacionId INT IDENTITY(1,1) NOT NULL,
    NegocioId   INT           NOT NULL,
    Nombre      NVARCHAR(100) NOT NULL,          -- 'Estante A', 'Altillo'
    Descripcion NVARCHAR(250) NULL,              -- 'Entrando a la bodega a mano derecha'
    Activo      BIT           NOT NULL DEFAULT 1,

    CONSTRAINT PK_UbicacionesBodega PRIMARY KEY (UbicacionId),
    CONSTRAINT UQ_Ubicaciones_Negocio UNIQUE (NegocioId, UbicacionId),
    CONSTRAINT FK_Ubicaciones_Negocios FOREIGN KEY (NegocioId) REFERENCES dbo.Negocios(NegocioId)
);
GO

CREATE TABLE dbo.Productos (
    ProductoId      INT IDENTITY(1,1) NOT NULL,
    NegocioId       INT           NOT NULL,
    CategoriaId     INT           NULL,
    Nombre          NVARCHAR(150) NOT NULL,
    Marca           NVARCHAR(100) NULL,
    Descripcion     NVARCHAR(300) NULL,
    ImagenUrl       NVARCHAR(500) NULL,
    TieneVariantes  BIT           NOT NULL DEFAULT 0,   -- 0 = producto simple, 1 = tallas/colores
    Activo          BIT           NOT NULL DEFAULT 1,
    FechaCreacion   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Productos PRIMARY KEY (ProductoId),
    CONSTRAINT UQ_Productos_Negocio UNIQUE (NegocioId, ProductoId),
    CONSTRAINT FK_Productos_Negocios   FOREIGN KEY (NegocioId) REFERENCES dbo.Negocios(NegocioId),
    CONSTRAINT FK_Productos_Categorias FOREIGN KEY (NegocioId, CategoriaId)
        REFERENCES dbo.Categorias(NegocioId, CategoriaId)
);
GO

CREATE TABLE dbo.ProductoVariantes (
    VarianteId          INT IDENTITY(1,1) NOT NULL,
    NegocioId           INT           NOT NULL,
    ProductoId          INT           NOT NULL,

    -- Ubicación física
    UbicacionBodegaId   INT           NULL,
    ReferenciaBodega    NVARCHAR(150) NULL,      -- 'Caja azul en la segunda fila'
    ReferenciaPercha    NVARCHAR(100) NULL,      -- 'Pared central, repisa 2'

    -- Identificación y atributos
    CodigoSku           NVARCHAR(80)  NULL,
    EsVarianteUnica     BIT           NOT NULL DEFAULT 1,   -- 1 cuando el producto no tiene tallas
    AtributosResumen    NVARCHAR(150) NULL,                 -- 'Talla 40 | Negro'
    AtributosJson       NVARCHAR(MAX) NULL,                 -- {"talla":"40","color":"Negro"}
    Talla AS CAST(JSON_VALUE(AtributosJson, '$.talla') AS NVARCHAR(20)),
    Color AS CAST(JSON_VALUE(AtributosJson, '$.color') AS NVARCHAR(40)),
    ImagenVarianteUrl   NVARCHAR(500) NULL,

    -- Esquema de 3 precios
    PrecioCosto         DECIMAL(18,2) NOT NULL DEFAULT 0,
    PrecioVentaLista    DECIMAL(18,2) NOT NULL,
    PrecioMinimoRegateo DECIMAL(18,2) NOT NULL,

    -- Stock dividido
    StockPercha         INT NOT NULL DEFAULT 0,
    StockBodega         INT NOT NULL DEFAULT 0,
    StockTotal AS (StockPercha + StockBodega),
    StockMinimoAlerta   INT NOT NULL DEFAULT 1,

    Activo              BIT NOT NULL DEFAULT 1,
    VersionFila         ROWVERSION,                          -- control de concurrencia (EF Core)

    CONSTRAINT PK_ProductoVariantes PRIMARY KEY (VarianteId),
    CONSTRAINT UQ_Variantes_Negocio UNIQUE (NegocioId, VarianteId),
    CONSTRAINT FK_Variantes_Productos   FOREIGN KEY (NegocioId, ProductoId)
        REFERENCES dbo.Productos(NegocioId, ProductoId),
    CONSTRAINT FK_Variantes_Ubicaciones FOREIGN KEY (NegocioId, UbicacionBodegaId)
        REFERENCES dbo.UbicacionesBodega(NegocioId, UbicacionId),
    CONSTRAINT CK_Variantes_Atributos CHECK (AtributosJson IS NULL OR ISJSON(AtributosJson) = 1),
    CONSTRAINT CK_Variantes_Stock     CHECK (StockPercha >= 0 AND StockBodega >= 0 AND StockMinimoAlerta >= 0),
    CONSTRAINT CK_Variantes_Precios   CHECK (PrecioCosto >= 0
                                             AND PrecioMinimoRegateo >= 0
                                             AND PrecioMinimoRegateo <= PrecioVentaLista)
);
GO

/* =========================================================================
   MÓDULO 3: CAJA (antes que Ventas, porque toda venta pertenece a un turno)
   ========================================================================= */

CREATE TABLE dbo.CajaTurnos (
    TurnoId             BIGINT IDENTITY(1,1) NOT NULL,
    NegocioId           INT           NOT NULL,
    UsuarioAbreId       INT           NOT NULL,
    FechaApertura       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    MontoInicial        DECIMAL(18,2) NOT NULL DEFAULT 0,
    Estado              VARCHAR(10)   NOT NULL DEFAULT 'ABIERTO',

    -- Foto del resumen al momento de cerrar
    UsuarioCierraId     INT           NULL,
    FechaCierre         DATETIME2     NULL,
    TotalVentas         DECIMAL(18,2) NULL,
    TotalEfectivoVentas DECIMAL(18,2) NULL,
    TotalTransferencias DECIMAL(18,2) NULL,
    TotalTarjeta        DECIMAL(18,2) NULL,
    TotalGastosRetiros  DECIMAL(18,2) NULL,
    TotalIngresosExtra  DECIMAL(18,2) NULL,
    GananciaDia         DECIMAL(18,2) NULL,          -- solo visible para el dueño
    EfectivoEsperado    DECIMAL(18,2) NULL,
    EfectivoContado     DECIMAL(18,2) NULL,
    Diferencia AS (EfectivoContado - EfectivoEsperado),
    Nota                NVARCHAR(250) NULL,

    CONSTRAINT PK_CajaTurnos PRIMARY KEY (TurnoId),
    CONSTRAINT UQ_Turnos_Negocio UNIQUE (NegocioId, TurnoId),
    CONSTRAINT FK_Turnos_Abre   FOREIGN KEY (NegocioId, UsuarioAbreId)   REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT FK_Turnos_Cierra FOREIGN KEY (NegocioId, UsuarioCierraId) REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT CK_Turnos_Monto  CHECK (MontoInicial >= 0),
    CONSTRAINT CK_Turnos_Estado CHECK (Estado IN ('ABIERTO','CERRADO')),
    CONSTRAINT CK_Turnos_Cierre CHECK (Estado = 'ABIERTO'
        OR (FechaCierre IS NOT NULL AND UsuarioCierraId IS NOT NULL
            AND EfectivoContado IS NOT NULL AND EfectivoEsperado IS NOT NULL))
);
GO

CREATE TABLE dbo.CajaMovimientos (
    CajaMovimientoId BIGINT IDENTITY(1,1) NOT NULL,
    NegocioId        INT              NOT NULL,
    TurnoId          BIGINT           NOT NULL,
    UsuarioId        INT              NOT NULL,
    Tipo             VARCHAR(15)      NOT NULL,      -- GASTO, RETIRO, INGRESO_EXTRA
    Monto            DECIMAL(18,2)    NOT NULL,
    Concepto         NVARCHAR(200)    NOT NULL,      -- 'Fundas', 'Pago al cargador'
    Fecha            DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientGuid       UNIQUEIDENTIFIER NULL,

    CONSTRAINT PK_CajaMovimientos PRIMARY KEY (CajaMovimientoId),
    CONSTRAINT FK_CajaMov_Turno   FOREIGN KEY (NegocioId, TurnoId)   REFERENCES dbo.CajaTurnos(NegocioId, TurnoId),
    CONSTRAINT FK_CajaMov_Usuario FOREIGN KEY (NegocioId, UsuarioId) REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT CK_CajaMov_Tipo  CHECK (Tipo IN ('GASTO','RETIRO','INGRESO_EXTRA')),
    CONSTRAINT CK_CajaMov_Monto CHECK (Monto > 0)
);
GO

/* =========================================================================
   MÓDULO 4: VENTAS ("CUADERNO DIGITAL") Y REGATEO
   ========================================================================= */

CREATE TABLE dbo.Ventas (
    VentaId         BIGINT IDENTITY(1,1) NOT NULL,
    NegocioId       INT              NOT NULL,
    NumeroVenta     INT              NOT NULL,       -- "Venta #154", correlativo por negocio
    UsuarioId       INT              NOT NULL,
    TurnoId         BIGINT           NOT NULL,
    FechaVenta      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    TotalVenta      DECIMAL(18,2)    NOT NULL,
    Estado          VARCHAR(15)      NOT NULL DEFAULT 'COMPLETADA',
    NotaVenta       NVARCHAR(250)    NULL,
    FechaAnulacion  DATETIME2        NULL,
    UsuarioAnulaId  INT              NULL,
    MotivoAnulacion NVARCHAR(250)    NULL,
    ClientGuid      UNIQUEIDENTIFIER NULL,           -- idempotencia (evita ventas duplicadas)

    CONSTRAINT PK_Ventas PRIMARY KEY (VentaId),
    CONSTRAINT UQ_Ventas_Negocio UNIQUE (NegocioId, VentaId),
    CONSTRAINT UQ_Ventas_Numero  UNIQUE (NegocioId, NumeroVenta),
    CONSTRAINT FK_Ventas_Negocios     FOREIGN KEY (NegocioId) REFERENCES dbo.Negocios(NegocioId),
    CONSTRAINT FK_Ventas_Usuarios     FOREIGN KEY (NegocioId, UsuarioId)      REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT FK_Ventas_UsuarioAnula FOREIGN KEY (NegocioId, UsuarioAnulaId) REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT FK_Ventas_Turno        FOREIGN KEY (NegocioId, TurnoId)        REFERENCES dbo.CajaTurnos(NegocioId, TurnoId),
    CONSTRAINT CK_Ventas_Total     CHECK (TotalVenta >= 0),
    CONSTRAINT CK_Ventas_Estado    CHECK (Estado IN ('COMPLETADA','ANULADA')),
    CONSTRAINT CK_Ventas_Anulacion CHECK (Estado <> 'ANULADA'
        OR (FechaAnulacion IS NOT NULL AND UsuarioAnulaId IS NOT NULL))
);
GO

CREATE TABLE dbo.VentaDetalles (
    VentaDetalleId          BIGINT IDENTITY(1,1) NOT NULL,
    NegocioId               INT           NOT NULL,
    VentaId                 BIGINT        NOT NULL,
    VarianteId              INT           NOT NULL,
    NombreProductoHistorico NVARCHAR(250) NOT NULL,      -- 'Zapatilla Urbana - Talla 40 | Negro'
    SalioDeUbicacion        VARCHAR(10)   NOT NULL,      -- PERCHA o BODEGA
    Cantidad                INT           NOT NULL,

    -- Auditoría del regateo y ganancia real
    PrecioCostoHistorico    DECIMAL(18,2) NOT NULL,
    PrecioListaUnitario     DECIMAL(18,2) NOT NULL,
    PrecioMinimoHistorico   DECIMAL(18,2) NOT NULL,
    PrecioVendidoUnitario   DECIMAL(18,2) NOT NULL,
    AutorizadoPorUsuarioId  INT           NULL,          -- dueño que autorizó bajar del mínimo

    DescuentoRegateoUnitario AS (PrecioListaUnitario - PrecioVendidoUnitario),
    Subtotal                 AS (Cantidad * PrecioVendidoUnitario),
    GananciaLinea            AS (Cantidad * (PrecioVendidoUnitario - PrecioCostoHistorico)),

    CONSTRAINT PK_VentaDetalles PRIMARY KEY (VentaDetalleId),
    CONSTRAINT UQ_Detalles_Negocio UNIQUE (NegocioId, VentaDetalleId),
    CONSTRAINT FK_Detalles_Ventas    FOREIGN KEY (NegocioId, VentaId)    REFERENCES dbo.Ventas(NegocioId, VentaId),
    CONSTRAINT FK_Detalles_Variantes FOREIGN KEY (NegocioId, VarianteId) REFERENCES dbo.ProductoVariantes(NegocioId, VarianteId),
    CONSTRAINT FK_Detalles_Autoriza  FOREIGN KEY (NegocioId, AutorizadoPorUsuarioId) REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT CK_Detalles_Ubicacion CHECK (SalioDeUbicacion IN ('PERCHA','BODEGA')),
    CONSTRAINT CK_Detalles_Cantidad  CHECK (Cantidad > 0),
    CONSTRAINT CK_Detalles_Precio    CHECK (PrecioVendidoUnitario >= 0),
    -- Bajo el mínimo solo con autorización del dueño
    CONSTRAINT CK_Detalles_Minimo    CHECK (PrecioVendidoUnitario >= PrecioMinimoHistorico
                                            OR AutorizadoPorUsuarioId IS NOT NULL)
);
GO

-- Regla en el backend: SUM(VentaPagos.Monto) = Ventas.TotalVenta al guardar.
-- Se registra lo COBRADO, no el billete recibido (el vuelto no se guarda).
CREATE TABLE dbo.VentaPagos (
    PagoId                  BIGINT IDENTITY(1,1) NOT NULL,
    NegocioId               INT              NOT NULL,
    VentaId                 BIGINT           NOT NULL,
    UsuarioId               INT              NOT NULL,
    MetodoPago              VARCHAR(20)      NOT NULL,   -- EFECTIVO, TRANSFERENCIA, TARJETA
    Monto                   DECIMAL(18,2)    NOT NULL,
    ReferenciaTransferencia NVARCHAR(100)    NULL,
    FechaPago               DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientGuid              UNIQUEIDENTIFIER NULL,

    CONSTRAINT PK_VentaPagos PRIMARY KEY (PagoId),
    CONSTRAINT FK_Pagos_Ventas   FOREIGN KEY (NegocioId, VentaId)   REFERENCES dbo.Ventas(NegocioId, VentaId),
    CONSTRAINT FK_Pagos_Usuarios FOREIGN KEY (NegocioId, UsuarioId) REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT CK_Pagos_Metodo CHECK (MetodoPago IN ('EFECTIVO','TRANSFERENCIA','TARJETA')),
    CONSTRAINT CK_Pagos_Monto  CHECK (Monto > 0)
);
GO

/* =========================================================================
   MÓDULO 5: KARDEX (historial inmutable del stock)
   Cantidad siempre positiva. La dirección la dan origen y destino:
     origen NULL  = entra desde afuera (compra, inicial, encontrado)
     destino NULL = sale del local (venta, pérdida, dañado)
   ========================================================================= */

CREATE TABLE dbo.MovimientosInventario (
    MovimientoId          BIGINT IDENTITY(1,1) NOT NULL,
    NegocioId             INT              NOT NULL,
    VarianteId            INT              NOT NULL,
    UsuarioId             INT              NOT NULL,
    TipoMovimiento        VARCHAR(30)      NOT NULL,
    UbicacionOrigen       VARCHAR(10)      NULL,
    UbicacionDestino      VARCHAR(10)      NULL,
    Cantidad              INT              NOT NULL,
    StockPerchaResultante INT              NOT NULL,
    StockBodegaResultante INT              NOT NULL,
    VentaId               BIGINT           NULL,
    NotaExplicativa       NVARCHAR(250)    NULL,
    FechaMovimiento       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ClientGuid            UNIQUEIDENTIFIER NULL,

    CONSTRAINT PK_MovimientosInventario PRIMARY KEY (MovimientoId),
    CONSTRAINT FK_Mov_Variantes FOREIGN KEY (NegocioId, VarianteId) REFERENCES dbo.ProductoVariantes(NegocioId, VarianteId),
    CONSTRAINT FK_Mov_Usuarios  FOREIGN KEY (NegocioId, UsuarioId)  REFERENCES dbo.Usuarios(NegocioId, UsuarioId),
    CONSTRAINT FK_Mov_Ventas    FOREIGN KEY (NegocioId, VentaId)    REFERENCES dbo.Ventas(NegocioId, VentaId),
    CONSTRAINT CK_Mov_Tipo CHECK (TipoMovimiento IN (
        'INICIAL','COMPRA','VENTA','PASAR_A_PERCHA','DEVOLVER_A_BODEGA',
        'PERDIDA','ENCONTRADO','DANADO','AJUSTE_CONTEO','ANULACION')),
    CONSTRAINT CK_Mov_Origen    CHECK (UbicacionOrigen  IN ('PERCHA','BODEGA')),
    CONSTRAINT CK_Mov_Destino   CHECK (UbicacionDestino IN ('PERCHA','BODEGA')),
    CONSTRAINT CK_Mov_Direccion CHECK (UbicacionOrigen IS NOT NULL OR UbicacionDestino IS NOT NULL),
    CONSTRAINT CK_Mov_Cantidad  CHECK (Cantidad > 0),
    CONSTRAINT CK_Mov_Stock     CHECK (StockPerchaResultante >= 0 AND StockBodegaResultante >= 0)
);
GO

/* =========================================================================
   ÍNDICES
   ========================================================================= */

-- Catálogo y búsquedas
CREATE INDEX IX_Productos_Busqueda ON dbo.Productos (NegocioId, Activo, Nombre)
    INCLUDE (Marca, ImagenUrl, CategoriaId);

CREATE INDEX IX_Variantes_Producto ON dbo.ProductoVariantes (NegocioId, ProductoId);

CREATE INDEX IX_Variantes_Talla ON dbo.ProductoVariantes (NegocioId, Talla)
    INCLUDE (ProductoId, StockPercha, StockBodega);

CREATE UNIQUE INDEX UX_Variantes_Sku ON dbo.ProductoVariantes (NegocioId, CodigoSku)
    WHERE CodigoSku IS NOT NULL;

-- Lista "Reponer percha": 0 afuera, pero hay en bodega
CREATE INDEX IX_Variantes_ReponerPercha ON dbo.ProductoVariantes (NegocioId)
    INCLUDE (ProductoId, StockBodega, UbicacionBodegaId, ReferenciaBodega)
    WHERE StockPercha = 0 AND StockBodega > 0 AND Activo = 1;

-- Ventas y caja
CREATE INDEX IX_Ventas_Cuaderno ON dbo.Ventas (NegocioId, FechaVenta DESC)
    INCLUDE (NumeroVenta, Estado, TotalVenta, UsuarioId);

CREATE INDEX IX_Ventas_Turno ON dbo.Ventas (NegocioId, TurnoId)
    INCLUDE (Estado, TotalVenta);

CREATE INDEX IX_Detalles_Venta ON dbo.VentaDetalles (NegocioId, VentaId);

CREATE INDEX IX_Pagos_Venta ON dbo.VentaPagos (NegocioId, VentaId)
    INCLUDE (MetodoPago, Monto);

CREATE INDEX IX_CajaMov_Turno ON dbo.CajaMovimientos (NegocioId, TurnoId)
    INCLUDE (Tipo, Monto);

-- Solo una caja abierta por negocio a la vez
CREATE UNIQUE INDEX UX_Turnos_UnAbierto ON dbo.CajaTurnos (NegocioId)
    WHERE Estado = 'ABIERTO';

-- Kardex de una variante
CREATE INDEX IX_Mov_Variante ON dbo.MovimientosInventario (NegocioId, VarianteId, FechaMovimiento DESC);

-- Idempotencia: el mismo registro enviado dos veces desde el celular se rechaza
CREATE UNIQUE INDEX UX_Ventas_ClientGuid   ON dbo.Ventas (ClientGuid)                WHERE ClientGuid IS NOT NULL;
CREATE UNIQUE INDEX UX_Pagos_ClientGuid    ON dbo.VentaPagos (ClientGuid)            WHERE ClientGuid IS NOT NULL;
CREATE UNIQUE INDEX UX_CajaMov_ClientGuid  ON dbo.CajaMovimientos (ClientGuid)       WHERE ClientGuid IS NOT NULL;
CREATE UNIQUE INDEX UX_Mov_ClientGuid      ON dbo.MovimientosInventario (ClientGuid) WHERE ClientGuid IS NOT NULL;
GO

PRINT N'Esquema PosComercio creado correctamente.';
