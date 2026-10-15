using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PosComercio.Api.Dtos.Auth;
using PosComercio.Api.Dtos.Productos;

namespace PosComercio.Api.Tests.Integracion.Infraestructura;

/// <summary>Atajos para que las pruebas se lean como el flujo real del negocio.</summary>
public abstract class PruebaIntegracionBase(ApiFactory factory)
{
    protected HttpClient CrearCliente() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    /// <summary>Un cliente HTTP con la sesión ya iniciada.</summary>
    protected async Task<HttpClient> IniciarSesionAsync(string login)
    {
        var cliente = CrearCliente();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { usuarioLogin = login, password = DatosDePrueba.Password });
        respuesta.EnsureSuccessStatusCode();

        var sesion = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion!.AccessToken);

        return cliente;
    }

    protected static Task<HttpResponseMessage> AbrirCajaAsync(HttpClient cliente, decimal montoInicial = 20m) =>
        cliente.PostAsJsonAsync("/api/caja/abrir", new { montoInicial });

    /// <summary>Venta de un solo producto, pagada en efectivo por el total exacto.</summary>
    protected static Task<HttpResponseMessage> VenderAsync(
        HttpClient cliente, int varianteId, int cantidad, decimal precio,
        string? pinAutorizacion = null, Guid? clientGuid = null) =>
        cliente.PostAsJsonAsync("/api/ventas", new
        {
            items = new[] { new { varianteId, cantidad, salioDe = "PERCHA", precioVendidoUnitario = precio } },
            pagos = new[] { new { metodoPago = "EFECTIVO", monto = cantidad * precio } },
            pinAutorizacion,
            clientGuid
        });

    protected static async Task<VarianteDto> LeerVarianteAsync(HttpClient cliente, int productoId, int varianteId)
    {
        var producto = await cliente.GetFromJsonAsync<ProductoDto>($"/api/productos/{productoId}");
        return producto!.Variantes.Single(v => v.VarianteId == varianteId);
    }

    /// <summary>Lee el "codigo" de un error ProblemDetails (ej. "ventas.stock_insuficiente").</summary>
    protected static async Task<string?> LeerCodigoErrorAsync(HttpResponseMessage respuesta)
    {
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("codigo", out var codigo) ? codigo.GetString() : null;
    }
}