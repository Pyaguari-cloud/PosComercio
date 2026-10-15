using PosComercio.Api.Services.Admin;
using PosComercio.Api.Services.Auth;
using PosComercio.Api.Services.Autorizacion;
using PosComercio.Api.Services.Caja;
using PosComercio.Api.Services.Catalogo;
using PosComercio.Api.Services.Fotos;
using PosComercio.Api.Services.Inventario;
using PosComercio.Api.Services.Negocios;
using PosComercio.Api.Services.Productos;
using PosComercio.Api.Services.Usuarios;
using PosComercio.Api.Services.Ventas;

namespace PosComercio.Api.Configuracion;

/// <summary>Servicios con la lógica de negocio. Todos Scoped: uno por petición.</summary>
public static class ServiciosAplicacion
{
    public static IServiceCollection AddServiciosAplicacion(this IServiceCollection services)
    {
        // Acceso y usuarios
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IAutorizacionDuenoService, AutorizacionDuenoService>();
        services.AddScoped<IAdminService, AdminService>();

        // Negocio y configuración
        services.AddScoped<IConfiguracionNegocioService, ConfiguracionNegocioService>();

        // Catálogo
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IProductoEdicionService, ProductoEdicionService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IFotoService, FotoService>();

        // Inventario
        services.AddScoped<IKardexService, KardexService>();
        services.AddScoped<IInventarioService, InventarioService>();

        // Dinero
        services.AddScoped<ICajaService, CajaService>();
        services.AddScoped<IVentaService, VentaService>();

        return services;
    }
}