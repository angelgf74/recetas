using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Recetas.Contratos.Contrasenas;
using Recetas.Contratos.Registro;
using Recetas.Contratos.Sesiones;

namespace Recetas.Api.Tests;

/// <summary>
/// Fixture con el límite de registros bajado a dos, para comprobar el rechazo
/// sin tener que lanzar cientos de peticiones.
/// </summary>
public sealed class ApiConLimiteEstrechoFixture : ApiConPostgresFixture
{
    protected override int MaximoDeRegistrosPorVentana => 2;
}

[Trait("Categoria", "Integracion")]
public class LimiteDePeticionesTests(ApiConLimiteEstrechoFixture api)
    : IClassFixture<ApiConLimiteEstrechoFixture>
{
    [Fact]
    public async Task SuperarElLimite_Responde429YDejaDeEnviarCorreos()
    {
        var cliente = api.CreateClient();

        for (var intento = 0; intento < 2; intento++)
        {
            var permitida = await cliente.PostAsJsonAsync("/registro/solicitudes",
                new PeticionDeSolicitudDeRegistro { Correo = $"permitido-{intento}@ejemplo.com" });

            Assert.Equal(HttpStatusCode.OK, permitida.StatusCode);
        }

        var correosAntes = api.Correo.EnlacesDeAlta.Count;

        var rechazada = await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = "rechazado@ejemplo.com" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);

        // Lo importante no es el código, sino que la petición rechazada no
        // llegue a generar un envío: el límite existe para no bombardear buzones.
        Assert.Equal(correosAntes, api.Correo.EnlacesDeAlta.Count);
    }

}

/// <summary>
/// Fixture propio: el cubo del limitador se comparte por origen y ventana, así
/// que dos tests dentro de la misma clase se estorbarían entre sí.
/// </summary>
public sealed class ApiConLimiteEstrechoParaContrasenaFixture : ApiConPostgresFixture
{
    protected override int MaximoDeRegistrosPorVentana => 2;
}

[Trait("Categoria", "Integracion")]
public class LimiteCompartidoConElRestablecimientoTests(ApiConLimiteEstrechoParaContrasenaFixture api)
    : IClassFixture<ApiConLimiteEstrechoParaContrasenaFixture>
{
    /// <summary>
    /// El alta y el restablecimiento comparten cubo a propósito: los dos mandan
    /// correo a una dirección que no es la de quien llama. Con cubos separados,
    /// un mismo origen duplicaría los envíos alternando entre ambos, y esta
    /// petición habría respondido 200.
    /// </summary>
    [Fact]
    public async Task AgotarElLimiteConElAlta_TambienBloqueaElRestablecimiento()
    {
        var cliente = api.CreateClient();

        for (var intento = 0; intento < 2; intento++)
        {
            var permitida = await cliente.PostAsJsonAsync("/registro/solicitudes",
                new PeticionDeSolicitudDeRegistro { Correo = $"compartido-{intento}@ejemplo.com" });

            Assert.Equal(HttpStatusCode.OK, permitida.StatusCode);
        }

        var correosAntes = api.Correo.EnlacesDeContrasena.Count;

        var rechazada = await cliente.PostAsJsonAsync("/contrasena/solicitudes",
            new PeticionDeSolicitudDeContrasena { Correo = "compartido-0@ejemplo.com" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);
        Assert.Equal(correosAntes, api.Correo.EnlacesDeContrasena.Count);
    }
}

/// <summary>
/// Fixture propio: comprobar una contraseña actual es la misma superficie que
/// el inicio de sesión, con su propio cubo.
/// </summary>
public sealed class ApiConLimiteEstrechoParaCambioDeContrasenaFixture : ApiConPostgresFixture
{
    protected override int MaximoDeCambiosDeContrasenaPorVentana => 2;
}

[Trait("Categoria", "Integracion")]
public class LimiteDeCambioDeContrasenaTests(ApiConLimiteEstrechoParaCambioDeContrasenaFixture api)
    : IClassFixture<ApiConLimiteEstrechoParaCambioDeContrasenaFixture>
{
    private const string Contrasena = "una-contrasena-larga";

    [Fact]
    public async Task SuperarElLimite_Responde429()
    {
        var cliente = api.CreateClient();
        var correo = $"limite-contrasena-{Guid.NewGuid():N}@ejemplo.com";

        await cliente.PostAsJsonAsync("/registro/solicitudes",
            new PeticionDeSolicitudDeRegistro { Correo = correo });

        await cliente.PostAsJsonAsync("/registro/completar", new PeticionDeCompletarRegistro
        {
            Token = api.Correo.TokenEnviadoA(correo),
            Contrasena = Contrasena
        });

        var sesion = await cliente.PostAsJsonAsync("/sesiones",
            new PeticionDeInicioDeSesion { Correo = correo, Contrasena = Contrasena });
        var acceso = await sesion.Content.ReadFromJsonAsync<RespuestaDeInicioDeSesion>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso!.Token);

        for (var intento = 0; intento < 2; intento++)
        {
            var respuesta = await cliente.PutAsJsonAsync("/yo/contrasena", new PeticionDeCambioDeContrasena
            {
                // Deliberadamente incorrecta: lo que se mide es cuántos intentos se
                // dejan hacer, no si acierta. Con la correcta, el segundo intento
                // fallaría por ser ya la contraseña anterior.
                ContrasenaActual = "no es la correcta",
                ContrasenaNueva = "otra-contrasena-larga"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        }

        var rechazada = await cliente.PutAsJsonAsync("/yo/contrasena", new PeticionDeCambioDeContrasena
        {
            ContrasenaActual = "no es la correcta",
            ContrasenaNueva = "otra-contrasena-larga"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, rechazada.StatusCode);
    }
}
