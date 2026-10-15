using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PosComercio.Api.Common.Resultados;
using PosComercio.Api.Common.Seguridad;
using PosComercio.Api.Data;
using PosComercio.Api.Data.Entities;
using PosComercio.Api.Dtos.Admin;
using PosComercio.Api.Services.Usuarios;

namespace PosComercio.Api.Services.Admin;

public static class ErroresAdmin
{
    public static readonly Error NegocioNoEncontrado = new(
        "admin.negocio_no_encontrado", "El negocio no existe.", StatusCodes.Status404NotFound);
}

public interface IAdminService
{
    Task<IReadOnlyList<NegocioAdminDto>> ListarNegociosAsync(CancellationToken ct);
    Task<Resultado<NegocioAdminDto>> CrearNegocioAsync(CrearNegocioRequest request, CancellationToken ct);
    Task<Resultado<NegocioAdminDto>> CambiarPlanAsync(int negocioId, CambiarPlanRequest request, CancellationToken ct);
}

public sealed class AdminService(
    PosComercioDbContext db,
    IUsuarioActual usuarioActual,
    IPasswordHasher<Usuario> hasher,
    TimeProvider tiempo,
    ILogger<AdminService> logger) : IAdminService
{
    // Configuración con la que arranca todo negocio nuevo. El dueño podrá cambiarla después.
    private const string ConfiguracionInicial =
        """{"usaVariantes":true,"separaPerchaBodega":true,"permiteRegateo":true,"cierreCiego":true,"montoInicialSugerido":20}""";

    public async Task<IReadOnlyList<NegocioAdminDto>> ListarNegociosAsync(CancellationToken ct) =>
        await db.Negocios
            .AsNoTracking()
            .Where(n => n.TipoNegocio != "Interno")
            .OrderByDescending(n => n.FechaCreacion)
            .Select(ANegocioDto)
            .ToListAsync(ct);

    public async Task<Resultado<NegocioAdminDto>> CrearNegocioAsync(CrearNegocioRequest request, CancellationToken ct)
    {
        if (ReglasPassword.Validar(request.Dueno.Password) is { } errorPassword)
        {
            return errorPassword;
        }

        var login = Logins.Normalizar(request.Dueno.UsuarioLogin);
        if (await db.Usuarios.AnyAsync(u => u.UsuarioLogin == login, ct))
        {
            return ErroresUsuarios.LoginEnUso;
        }

        var ahora = tiempo.GetUtcNow().UtcDateTime;

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // 1. El negocio
        var negocio = new Negocio
        {
            NombreComercial = request.NombreComercial.Trim(),
            TipoNegocio = request.TipoNegocio.Trim(),
            TelefonoContacto = request.TelefonoContacto?.Trim(),
            Moneda = "USD",
            ZonaHoraria = "America/Guayaquil",
            ConfiguracionJson = ConfiguracionInicial,
            PlanActual = "PRUEBA",
            FechaFinPrueba = DateOnly.FromDateTime(ahora).AddDays(request.DiasPrueba),
            Activo = true,
            FechaCreacion = ahora
        };

        db.Negocios.Add(negocio);
        await db.SaveChangesAsync(ct);

        // 2. Su dueño
        var dueno = new Usuario
        {
            NegocioId = negocio.NegocioId,
            Nombre = request.Dueno.Nombre.Trim(),
            UsuarioLogin = login,
            PasswordHash = string.Empty,
            Rol = Roles.Dueno,
            Activo = true,
            FechaCreacion = ahora
        };
        dueno.PasswordHash = hasher.HashPassword(dueno, request.Dueno.Password);
        db.Usuarios.Add(dueno);

        // 3. Datos mínimos para que pueda empezar a cargar productos de inmediato
        db.Add(new Categoria { NegocioId = negocio.NegocioId, Nombre = "General", ColorHex = "#3B82F6", Activo = true });
        db.Add(new UbicacionesBodega { NegocioId = negocio.NegocioId, Nombre = "Bodega principal", Activo = true });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.EsViolacionDeUnicidad())
        {
            return ErroresUsuarios.LoginEnUso;
        }

        await transaccion.CommitAsync(ct);

        logger.LogInformation("Negocio {NegocioId} '{Nombre}' creado por el admin {AdminId}",
            negocio.NegocioId, negocio.NombreComercial, usuarioActual.UsuarioId);

        return await ObtenerAsync(negocio.NegocioId, ct);
    }

    public async Task<Resultado<NegocioAdminDto>> CambiarPlanAsync(
        int negocioId, CambiarPlanRequest request, CancellationToken ct)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n =>
            n.NegocioId == negocioId && n.TipoNegocio != "Interno", ct);

        if (negocio is null)
        {
            return ErroresAdmin.NegocioNoEncontrado;
        }

        var planAnterior = negocio.PlanActual;
        negocio.PlanActual = request.Plan;
        await db.SaveChangesAsync(ct);

        logger.LogWarning("Plan del negocio {NegocioId}: {Anterior} → {Nuevo}, por el admin {AdminId}",
            negocioId, planAnterior, request.Plan, usuarioActual.UsuarioId);

        return await ObtenerAsync(negocioId, ct);
    }

    private async Task<Resultado<NegocioAdminDto>> ObtenerAsync(int negocioId, CancellationToken ct)
    {
        var dto = await db.Negocios
            .AsNoTracking()
            .Where(n => n.NegocioId == negocioId)
            .Select(ANegocioDto)
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return ErroresAdmin.NegocioNoEncontrado;
        }

        return dto;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Negocio, NegocioAdminDto>> ANegocioDto = n =>
        new NegocioAdminDto(
            n.NegocioId,
            n.NombreComercial,
            n.TipoNegocio,
            n.TelefonoContacto,
            n.PlanActual,
            n.FechaFinPrueba,
            n.Activo,
            n.Usuarios.Count(),
            n.FechaCreacion);
}