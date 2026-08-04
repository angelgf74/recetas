# 013 · Anuncios en Android

**Estado:** código terminado; **bloqueada** por configuración pendiente en la
consola de AdMob (ver `tasks.md`).

## Qué hace

La aplicación Android muestra un banner de AdMob al pie del recetario y de la
búsqueda, y pide el consentimiento que exige la normativa europea antes de
cargarlos.

## Por qué

Es la única monetización prevista del producto, y `tech-stack.md` la sitúa
exclusivamente en Android: la web y la API no la conocen.

## Dónde va y dónde no

`mission.md` es explícito: *"La publicidad no manda sobre el producto — no se
diseñan pantallas, ni se fuerza la navegación, ni se recorta funcionalidad para
generar impresiones. Si un anuncio estorba al consultar una receta mientras se
cocina, el anuncio se quita."*

De ahí salen las dos reglas de esta feature:

- **Banner al pie del recetario y de la búsqueda.**
- **Nada en la ficha de receta.** Es la pantalla que se lee cocinando, con las
  manos ocupadas. Ni banner ni intersticial.

Tampoco hay intersticiales en ninguna parte: un anuncio a pantalla completa al
abrir una receta es exactamente "forzar la navegación".

## Criterios de aceptación

### Anuncios

- [ ] El recetario muestra un banner al pie.
- [ ] La búsqueda muestra un banner al pie.
- [ ] La ficha de receta **no muestra ningún anuncio**.
- [ ] El banner se adapta al ancho del dispositivo en lugar de usar un tamaño fijo.
- [ ] Si el anuncio no carga, **no queda un hueco vacío**: la lista ocupa todo.
- [ ] El identificador de aplicación de AdMob está en el manifiesto.

### Que no cueste la cuenta

- [ ] En compilaciones de **depuración** se usan los **bloques de prueba de Google**, nunca los reales.
- [ ] En **publicación** se usan los bloques reales.
- [ ] El emulador y los dispositivos de desarrollo se registran como dispositivos de prueba.

### Consentimiento

- [ ] Antes de cargar ningún anuncio se consulta el estado de consentimiento con la plataforma de mensajes de Google (UMP).
- [ ] Si procede mostrar el formulario, se muestra.
- [ ] Los anuncios solo se cargan cuando el estado permite pedirlos.
- [ ] Un fallo al obtener el consentimiento **no rompe la aplicación**: se sigue sin anuncios.

### Calidad

- [ ] `gradlew assembleDebug` y `gradlew test` en verde.
- [ ] Sin avisos nuevos.
- [ ] La aplicación arranca y se usa igual aunque no haya red para los anuncios.

## Fuera de alcance

- **Mediación** con otras redes publicitarias.
- **Anuncios en la web**, que `tech-stack.md` descarta.
- **Intersticiales, bonificados y anuncios de carga de aplicación.** Ver arriba.
- **Un ajuste dentro de la aplicación para volver a abrir el formulario de
  consentimiento.** Google lo pide para poder cambiar de opinión; hoy la
  aplicación no tiene pantalla de ajustes donde ponerlo.
