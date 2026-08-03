using Microsoft.Extensions.DependencyInjection;
using Recetas.Aplicacion.Recetas;
using Recetas.Aplicacion.Registro;
using Recetas.Aplicacion.Salud;
using Recetas.Aplicacion.Sesiones;

namespace Recetas.Aplicacion;

/// <summary>
/// Registro de los casos de uso.
/// </summary>
public static class InyeccionDeDependencias
{
    public static IServiceCollection AnadirAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<ConsultarSalud>();
        servicios.AddScoped<SolicitarRegistro>();
        servicios.AddScoped<CompletarRegistro>();
        servicios.AddScoped<IniciarSesion>();
        servicios.AddScoped<ResolverIngredientes>();
        servicios.AddScoped<GestionDeRecetas>();
        servicios.AddScoped<GestionDeFotos>();

        return servicios;
    }
}
