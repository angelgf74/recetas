using System.Reflection;
using Recetas.Dominio.Puertos;

namespace Recetas.Arquitectura.Tests;

/// <summary>
/// La regla "las dependencias apuntan hacia dentro" de la constitución, verificada.
/// Una regla que nadie comprueba se incumple en cuanto aprieta la prisa.
/// </summary>
public class ReglaDeDependenciasTests
{
    private static readonly Assembly Dominio = typeof(IComprobadorDeAlmacen).Assembly;
    private static readonly Assembly Aplicacion = typeof(Recetas.Aplicacion.Salud.ConsultarSalud).Assembly;
    private static readonly Assembly Infraestructura = typeof(Recetas.Infraestructura.InyeccionDeDependencias).Assembly;
    private static readonly Assembly Contratos = typeof(Recetas.Contratos.Salud.RespuestaDeSalud).Assembly;

    /// <summary>Prefijos de ensamblados que son infraestructura y no pueden filtrarse hacia dentro.</summary>
    private static readonly string[] PrefijosDeInfraestructura =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql"
    ];

    [Fact]
    public void Dominio_NoDependeDeInfraestructura()
    {
        var prohibidas = ReferenciasDe(Dominio)
            .Where(EsInfraestructura)
            .ToArray();

        Assert.True(
            prohibidas.Length == 0,
            $"Recetas.Dominio debe ser puro. Referencias prohibidas: {string.Join(", ", prohibidas)}. " +
            "Si necesitas una capacidad externa, defínela como puerto e impleméntala en Infraestructura.");
    }

    [Fact]
    public void Dominio_NoDependeDeNingunProyectoDelaSolucion()
    {
        var prohibidas = ReferenciasDe(Dominio)
            .Where(nombre => nombre.StartsWith("Recetas.", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            prohibidas.Length == 0,
            $"Recetas.Dominio no puede depender de otras capas. Encontrado: {string.Join(", ", prohibidas)}.");
    }

    [Fact]
    public void Aplicacion_NoDependeDeInfraestructura()
    {
        var prohibidas = ReferenciasDe(Aplicacion)
            .Where(nombre => EsInfraestructura(nombre) || nombre == "Recetas.Infraestructura")
            .ToArray();

        Assert.True(
            prohibidas.Length == 0,
            $"Recetas.Aplicacion solo puede depender del dominio. Referencias prohibidas: {string.Join(", ", prohibidas)}.");
    }

    [Fact]
    public void Aplicacion_NoDependeDeLaApi()
    {
        Assert.DoesNotContain("Recetas.Api", ReferenciasDe(Aplicacion));
    }

    [Fact]
    public void Infraestructura_NoDependeDeAplicacionNiDeLaApi()
    {
        var referencias = ReferenciasDe(Infraestructura);

        Assert.DoesNotContain("Recetas.Aplicacion", referencias);
        Assert.DoesNotContain("Recetas.Api", referencias);
    }

    [Fact]
    public void Contratos_NoDependeDeNingunaOtraCapa()
    {
        var prohibidas = ReferenciasDe(Contratos)
            .Where(nombre => nombre.StartsWith("Recetas.", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            prohibidas.Length == 0,
            "Recetas.Contratos es el contrato público hacia los clientes: si arrastrase el dominio, " +
            $"cualquier cambio interno se filtraría a la API pública. Encontrado: {string.Join(", ", prohibidas)}.");
    }

    private static string[] ReferenciasDe(Assembly ensamblado) =>
        ensamblado.GetReferencedAssemblies()
            .Select(referencia => referencia.Name ?? string.Empty)
            .ToArray();

    private static bool EsInfraestructura(string nombreDeEnsamblado) =>
        PrefijosDeInfraestructura.Any(prefijo =>
            nombreDeEnsamblado.StartsWith(prefijo, StringComparison.Ordinal));
}
