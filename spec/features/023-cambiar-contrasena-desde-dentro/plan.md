# 023 · Cambiar contraseña desde dentro — Plan

## Enfoque

Casi todo existe. `Usuario.CambiarContrasena` ya es público y lo usa la 008;
`Contrasena.TryCrear` ya valida la política; `IHasheadorDeContrasenas` ya sabe
verificar y derivar. Lo que falta es el caso de uso que ata las tres piezas
partiendo de la contraseña actual en vez de un token de correo, el endpoint
autenticado, el aviso por correo y el formulario.

## Aplicación

`CambiarContrasena`, en `Aplicacion/Contrasenas/`, junto a los otros dos casos
de uso de contraseña:

```csharp
public enum ResultadoDeCambioDeContrasena
{
    Correcto,

    /// <summary>Usuario inexistente o contraseña actual incorrecta. Mismo 401 para las dos.</summary>
    CredencialesIncorrectas,

    ContrasenaNoValida
}

public sealed class CambiarContrasena(
    IRepositorioDeUsuarios usuarios,
    IHasheadorDeContrasenas hasheador,
    IEnviadorDeCorreo enviadorDeCorreo,
    IReloj reloj,
    ILogger<CambiarContrasena> registro)
{
    public async Task<ResultadoDeCambioDeContrasena> EjecutarAsync(
        Guid usuarioId, string contrasenaActual, string contrasenaNueva, CancellationToken cancelacion = default);
}
```

Orden dentro del método: buscar el usuario, verificar la contraseña actual
—como en `BorrarCuenta`—, validar la nueva con `Contrasena.TryCrear`, cambiarla,
guardar, avisar por correo. El aviso va en un `try/catch` que solo registra:
mismo patrón que `RetirarPorModeracion.AvisarAlAutorAsync` y
`BorrarCuenta.AvisarAsync`, para que un fallo de Brevo no deshaga un cambio ya
hecho.

## Dominio e infraestructura

- `IEnviadorDeCorreo.EnviarConfirmacionDeCambioDeContrasenaAsync(CorreoElectronico, CancellationToken)`.
- Texto en `MensajesDeCorreo`: qué ha pasado y a quién escribir si no ha sido
  el usuario. Sin enlace ni token: es un aviso, no una acción.
- Implementación en `EnviadorDeCorreoBrevo` y `EnviadorDeCorreoDeConsola`.

## API

`PUT /yo/contrasena`, en `SesionesEndpoints.cs` junto al resto de rutas de
`/yo`: es la misma familia de "operaciones sobre la cuenta de quien pregunta",
igual que `DELETE /yo` y `GET /yo/datos`.

```csharp
grupo.MapPut("/yo/contrasena", CambiarContrasenaAsync)
    .RequireAuthorization()
    .RequireRateLimiting(LimitesDePeticiones.CambioDeContrasena);
```

Nuevo límite en `LimitesDePeticiones`: comprobar una contraseña es la misma
superficie de ataque que el inicio de sesión, así que usa la misma forma
—10 por 5 minutos— y su propio cubo, configurable por separado para los tests.

Contrato `PeticionDeCambioDeContrasena` (`Recetas.Contratos.Contrasenas`):
`ContrasenaActual` y `ContrasenaNueva`, con las mismas anotaciones de longitud
que `PeticionDeRestablecerContrasena`.

Respuesta: `401` con el mismo texto que `BorrarCuenta` para credenciales
incorrectas — no distinguir "no existe" de "contraseña mala" porque quien
pregunta ya tiene un token válido. `400` si la nueva no cumple la política.
`200` con mensaje de éxito si todo va bien, siguiendo el mismo patrón que
`POST /contrasena/restablecer`.

## Web

En `MiCuenta.razor`, una sección nueva entre los datos de la cuenta y "Borrar
mi cuenta" —cambiar la contraseña no es una zona peligrosa, borrar sí—:

- Dos campos: contraseña actual y contraseña nueva.
- Un botón que llama a `Api.CambiarContrasenaAsync`.
- Al terminar, limpiar los campos y mostrar el aviso de éxito; no se navega a
  ningún sitio, la sesión sigue abierta.

`ClienteDeApi.CambiarContrasenaAsync(string actual, string nueva)`.

## Pasos

1. `IEnviadorDeCorreo` + `MensajesDeCorreo` + los dos enviadores.
2. `CambiarContrasena` en Aplicación, registrado en la inyección de dependencias.
3. `LimitesDePeticiones.CambioDeContrasena`.
4. Contrato y endpoint.
5. `ClienteDeApi` y sección en `MiCuenta.razor`.
6. Tests.

## Archivos afectados

**Nuevos**

- `src/Recetas.Aplicacion/Contrasenas/CambiarContrasena.cs`
- `tests/Recetas.Aplicacion.Tests/Contrasenas/CambiarContrasenaTests.cs`
- `tests/Recetas.Api.Tests/CambioDeContrasenaTests.cs`

**Modificados**

- `IEnviadorDeCorreo.cs`, `MensajesDeCorreo.cs`, `EnviadorDeCorreoBrevo.cs`, `EnviadorDeCorreoDeConsola.cs`
- `InyeccionDeDependencias.cs` (Aplicación)
- `LimitesDePeticiones.cs`
- `Recetas.Contratos/Contrasenas/` (nuevo archivo de petición o añadido al existente)
- `SesionesEndpoints.cs`
- `ClienteDeApi.cs`, `MiCuenta.razor`
- Dobles de test: `EnviadorDeCorreoEspia` (los dos)

## Riesgos y decisiones

- **El límite de peticiones necesita su propio cubo**, no compartir el de
  inicio de sesión: son endpoints distintos con distinta exposición (uno pide
  JWT, el otro no), y compartir cubo dejaría que agotar uno bloqueara el otro
  sin motivo.
- **No se revalida el JWT ni se pide "hace poco que iniciaste sesión".** Fuera
  de alcance, anotado en la spec. Un atacante con el JWT robado puede cambiar
  la contraseña sabiendo la actual —que también tendría, si robó la sesión con
  phishing— pero el aviso por correo es la única defensa de esta feature, y es
  la misma que ya ofrece la baja.
- **Test de que el aviso llega** incluso cuando falla el envío la operación se
  mantiene: mismo patrón que `Denunciar_SiElCorreoFalla_LaDenunciaSigueGuardada`
  y `Retirar_SiElCorreoFalla_LaRecetaSigueRetirada`.
