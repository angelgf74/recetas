# 023 · Cambiar contraseña desde dentro

**Estado:** hecha

## Qué hace

Con la sesión iniciada, cambiar la contraseña sabiendo la actual: sin pasar por
el correo. Vive en "Mi cuenta", junto a descargar los datos y borrar la cuenta.

Al terminar, se envía un correo avisando de que la contraseña ha cambiado.

## Por qué

La 008 resolvió "he olvidado mi contraseña". Esto resuelve el otro caso, más
común: **cambiarla porque sí** —una buena práctica periódica, sospecha de que
alguien más la conoce, o simplemente que se quiere una mejor— sin tener que
salir de la sesión, pedir un enlace y volver a entrar por el correo.

## Criterios de aceptación

- [x] `PUT /yo/contrasena` con la contraseña actual correcta y una nueva válida la cambia y responde `200`.
- [x] Después, se puede iniciar sesión con la contraseña nueva.
- [x] La contraseña anterior deja de servir para iniciar sesión.
- [x] Si la contraseña actual **no es correcta**, no cambia nada. Responde `401`, igual que al borrar la cuenta: quien pregunta ya tiene un token válido, así que la única lectura útil es "vuelve a identificarte".
- [x] Una contraseña nueva más corta que el mínimo responde `400` y no cambia nada.
- [x] Sin sesión, `401`.
- [x] Se envía un correo a la cuenta avisando de que su contraseña ha cambiado. Si el envío falla, **el cambio se mantiene**: el aviso es una cortesía, no una condición.
- [x] Superar el límite de peticiones responde `429`: es una superficie para probar contraseñas, igual que el inicio de sesión.
- [x] La pantalla de "Mi cuenta" ofrece cambiar la contraseña, con dos campos: la actual y la nueva.

**Visto en pantalla en un dispositivo real** el 9 de agosto de 2026: la
sección aparece bien colocada, con los dos campos, la ayuda de longitud
mínima y separada de "Borrar mi cuenta". **No se ha completado el formulario
con datos reales** contra la cuenta de producción, para no tocar una
contraseña en uso; el envío no tiene test de bUnit (no lo pedía `plan.md`).

## Decisiones

**Aviso por correo, aunque el backlog no lo pedía explícitamente.** Es el mismo
patrón que ya sigue el producto para cambios de cuenta sensibles: la baja avisa
(`EnviarConfirmacionDeBajaAsync`), la retirada por moderación avisa a su autor
(020). Quien recibe este correo sin haberlo pedido sabe que su sesión está
comprometida, justo cuando más lo necesita saber.

**`PUT`, no `POST`.** Se está fijando el valor de un recurso —la contraseña de
la cuenta—, no ejecutando un verbo que ocurre y se acaba. Mismo razonamiento
que llevó a `PUT /recetas/{id}/favorito` en la 021.

**Verificar la contraseña actual antes que validar la nueva.** Al revés que en
el restablecimiento (008), donde la contraseña se valida primero para no quemar
un enlace de un solo uso por una errata: aquí no hay ningún recurso que
consumir, así que se comprueba primero el permiso —la contraseña actual— y
luego los datos, como en `BorrarCuenta`.

## Fuera de alcance

- **Cerrar las sesiones abiertas al cambiar la contraseña.** Sigue siendo la
  limitación conocida de la 008: un JWT emitido antes sigue valiendo hasta que
  caduque. No se resuelve aquí por el mismo motivo que allí: cambia una
  decisión de arquitectura (tokens sin estado, de la 002), no es un descuido.
- **Exigir la contraseña de nuevo para acciones posteriores** ("reautenticación
  reciente"). El alcance de este producto no lo necesita.
- **Historial de contraseñas** ni impedir reutilizar una anterior.
- **Medidor de fortaleza** en la interfaz. La política ya es solo longitud
  (12 caracteres mínimo), y un medidor sugeriría reglas de composición que el
  proyecto decidió no exigir.
