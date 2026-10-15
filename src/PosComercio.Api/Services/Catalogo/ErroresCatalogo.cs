using PosComercio.Api.Common.Resultados;

namespace PosComercio.Api.Services.Catalogo;

public static class ErroresCatalogo
{
    // ---- No encontrados (404) ----
    public static readonly Error ProductoNoEncontrado = new(
        "catalogo.producto_no_encontrado", "El producto no existe o está inactivo.", StatusCodes.Status404NotFound);

    public static readonly Error VarianteNoEncontrada = new(
        "catalogo.variante_no_encontrada", "La variante no existe.", StatusCodes.Status404NotFound);

    public static readonly Error CategoriaNoEncontrada = new(
        "catalogo.categoria_no_encontrada", "La categoría no existe o está inactiva.", StatusCodes.Status404NotFound);

    public static readonly Error UbicacionNoEncontrada = new(
        "catalogo.ubicacion_no_encontrada", "La ubicación de bodega no existe o está inactiva.", StatusCodes.Status404NotFound);

    // ---- Datos mal formados (400) ----
    public static readonly Error ProductoSimpleUnaVariante = new(
        "catalogo.producto_simple_una_variante",
        "Un producto sin tallas ni colores debe tener exactamente una variante.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ProductoSimpleSinAtributos = new(
        "catalogo.producto_simple_sin_atributos",
        "Un producto sin tallas ni colores no puede llevar talla ni color.",
        StatusCodes.Status400BadRequest);

    public static readonly Error VarianteSinAtributos = new(
        "catalogo.variante_sin_atributos",
        "Cada variante debe tener al menos talla o color.",
        StatusCodes.Status400BadRequest);

    public static Error VarianteRepetida(string? talla, string? color) => new(
        "catalogo.variante_repetida",
        $"La variante '{Atributos.Resumen(talla, color)}' está repetida en la lista.",
        StatusCodes.Status400BadRequest);

    public static readonly Error SkuRepetidoEnPeticion = new(
        "catalogo.sku_repetido",
        "Hay códigos SKU repetidos en la lista.",
        StatusCodes.Status400BadRequest);

    public static readonly Error MinimoMayorQueLista = new(
        "catalogo.minimo_mayor_que_lista",
        "El precio mínimo de regateo no puede ser mayor que el precio de lista.",
        StatusCodes.Status400BadRequest);

    public static readonly Error MasDeDosDecimales = new(
        "catalogo.mas_de_dos_decimales",
        "Los precios pueden tener máximo 2 decimales.",
        StatusCodes.Status400BadRequest);

    // ---- Conflictos con lo que ya existe (409) ----
    public static Error VarianteYaExiste(string? talla, string? color) => new(
        "catalogo.variante_ya_existe",
        $"El producto ya tiene la variante '{Atributos.Resumen(talla, color)}' (puede estar desactivada).",
        StatusCodes.Status409Conflict);

    public static readonly Error ProductoSinVariantes = new(
        "catalogo.producto_sin_variantes",
        "Este producto no maneja tallas ni colores, así que no se le pueden agregar variantes.",
        StatusCodes.Status409Conflict);

    public static readonly Error SkuDuplicado = new(
        "catalogo.sku_duplicado",
        "Ese código SKU ya lo usa otro producto.",
        StatusCodes.Status409Conflict);

    public static readonly Error CategoriaDuplicada = new(
        "catalogo.categoria_duplicada", "Ya existe una categoría con ese nombre.", StatusCodes.Status409Conflict);

    public static readonly Error UbicacionDuplicada = new(
        "catalogo.ubicacion_duplicada", "Ya existe una ubicación con ese nombre.", StatusCodes.Status409Conflict);

    public static readonly Error ModificadaPorOtro = new(
        "catalogo.modificada_por_otro",
        "Alguien modificó este producto al mismo tiempo. Vuelve a cargarlo e intenta de nuevo.",
        StatusCodes.Status409Conflict);
}