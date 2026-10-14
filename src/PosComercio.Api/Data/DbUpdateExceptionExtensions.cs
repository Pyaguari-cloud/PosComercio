using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PosComercio.Api.Data;

public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// True si SQL Server rechazó el guardado por un índice o constraint UNIQUE
    /// (errores 2601 y 2627).
    /// </summary>
    public static bool EsViolacionDeUnicidad(this DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}