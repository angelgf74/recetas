using Recetas.Contratos.Registro;
using Recetas.Dominio.Usuarios;

namespace Recetas.Arquitectura.Tests;

/// <summary>
/// <c>Recetas.Contratos</c> no referencia al dominio a propósito: es el contrato
/// público y no debe arrastrar tipos internos. El precio es que la política de
/// contraseñas está escrita en dos sitios.
/// <para>
/// Estos tests son lo que impide que esos dos sitios se separen. Sin ellos, subir
/// el mínimo en el dominio dejaría a la web validando con el valor antiguo: el
/// usuario recibiría un error del servidor después de que su formulario diera el visto bueno.
/// </para>
/// </summary>
public class CoherenciaDeContratosTests
{
    [Fact]
    public void LaLongitudMinimaDeContrasena_CoincideEnDominioYContratos()
    {
        Assert.Equal(
            Contrasena.LongitudMinima,
            PeticionDeCompletarRegistro.LongitudMinimaDeContrasena);
    }

    [Fact]
    public void LaLongitudMaximaDeContrasena_CoincideEnDominioYContratos()
    {
        Assert.Equal(
            Contrasena.LongitudMaxima,
            PeticionDeCompletarRegistro.LongitudMaximaDeContrasena);
    }
}
