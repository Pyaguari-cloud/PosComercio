/* =========================================================================
   POS COMERCIO - MVP
   Script 04: Seguridad multi-tenant (Row-Level Security) y rol de la app

   ¡NO lo ejecutes todavía! Lo activamos cuando el backend esté listo.
   Una vez activo, SQL Server solo muestra las filas del negocio fijado
   en la sesión. Si consultas desde SSMS sin fijarlo, verás las tablas
   VACÍAS (no es que se haya borrado nada).

   Para consultar desde SSMS con RLS activo:
       EXEC sp_set_session_context @key = N'NegocioId', @value = 1;
   Para desactivarlo temporalmente:
       ALTER SECURITY POLICY Seguridad.PoliticaNegocio WITH (STATE = OFF);
   ========================================================================= */

USE PosComercio;
GO

CREATE SCHEMA Seguridad;
GO

-- Devuelve una fila (permitido) si el NegocioId coincide con el de la sesión,
-- o si la sesión es de un administrador del SaaS.
CREATE FUNCTION Seguridad.fn_FiltroNegocio (@NegocioId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS Permitido
    WHERE @NegocioId = CAST(SESSION_CONTEXT(N'NegocioId') AS INT)
       OR CAST(SESSION_CONTEXT(N'EsAdminSaaS') AS BIT) = 1;
GO

-- FILTER: oculta filas de otros negocios al leer.
-- BLOCK (sin operación específica): impide insertar, modificar o borrar
-- filas de otro negocio, o mover una fila a otro negocio.
-- Negocios y Usuarios quedan fuera: el login ocurre antes de conocer el negocio.
CREATE SECURITY POLICY Seguridad.PoliticaNegocio
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.Categorias,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.Categorias,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.UbicacionesBodega,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.UbicacionesBodega,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.Productos,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.Productos,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.ProductoVariantes,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.ProductoVariantes,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.CajaTurnos,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.CajaTurnos,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.CajaMovimientos,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.CajaMovimientos,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.Ventas,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.Ventas,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.VentaDetalles,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.VentaDetalles,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.VentaPagos,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.VentaPagos,
    ADD FILTER PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.MovimientosInventario,
    ADD BLOCK  PREDICATE Seguridad.fn_FiltroNegocio(NegocioId) ON dbo.MovimientosInventario
WITH (STATE = ON);
GO

/* ---------- Rol con permisos mínimos para la aplicación ----------
   - Puede leer, insertar y actualizar.
   - No puede borrar nada (se usa Activo = 0 o se anula).
   - El kardex no se puede modificar: solo se agregan movimientos nuevos. */
CREATE ROLE rol_app_pos;
GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO rol_app_pos;
DENY UPDATE ON dbo.MovimientosInventario TO rol_app_pos;
GO

/* ---------- Usuario de conexión del backend ----------
   Descomenta, cambia la contraseña y ejecuta cuando configuremos el backend.
   La app NUNCA debe conectarse como 'sa'. */
-- CREATE LOGIN pos_app WITH PASSWORD = N'CambiaEstaClave_MuySegura_2026!';
-- CREATE USER pos_app FOR LOGIN pos_app;
-- ALTER ROLE rol_app_pos ADD MEMBER pos_app;
GO
