using Microsoft.Extensions.DependencyInjection;
using Recetas.Aplicacion.Contrasenas;
using Recetas.Aplicacion.Cuentas;
using Recetas.Aplicacion.Favoritos;
using Recetas.Aplicacion.Moderacion;
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
        servicios.AddScoped<SolicitarRestablecerContrasena>();
        servicios.AddScoped<RestablecerContrasena>();
        servicios.AddScoped<CambiarContrasena>();
        servicios.AddScoped<IniciarSesion>();
        servicios.AddScoped<ResolverIngredientes>();
        servicios.AddScoped<ResolverEtiquetas>();
        servicios.AddScoped<GestionDeRecetas>();
        servicios.AddScoped<GestionDeFotos>();
        servicios.AddScoped<ImportarReceta>();
        servicios.AddScoped<GestionDeFavoritos>();
        servicios.AddScoped<GestionDeDenuncias>();
        servicios.AddScoped<RetirarPorModeracion>();
        servicios.AddScoped<BorrarCuenta>();
        servicios.AddScoped<ConsultarResumenDeLaCuenta>();
        servicios.AddScoped<ExportarMisDatos>();

        return servicios;
    }
}
