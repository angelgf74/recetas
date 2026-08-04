# 013 · Anuncios en Android — Plan

## Enfoque

Tres piezas, y la segunda es la que puede costar dinero de verdad:

1. **El banner**, en un componible que se coloca donde toque.
2. **Identificadores de prueba en depuración**, reales solo al publicar.
3. **Consentimiento europeo**, antes de cargar nada.

## Por qué los identificadores de prueba no son opcional

Pulsar —o incluso servir— anuncios reales durante el desarrollo genera
impresiones y clics que AdMob considera **tráfico inválido**. La consecuencia
habitual no es un aviso: es la **suspensión de la cuenta de publicador**, que en
esta cuenta afecta a otras diez aplicaciones que ya generan ingresos.

Por eso los identificadores se eligen según el tipo de compilación, con el mismo
mecanismo que ya usa `BASE_DE_LA_API`, y además el emulador se registra
explícitamente como dispositivo de prueba. Son dos defensas para lo mismo, a
propósito.

Los identificadores de prueba de Google son públicos y están hechos para esto.

## Consentimiento

Servir anuncios personalizados en el Espacio Económico Europeo sin recabar
consentimiento incumple la normativa, y AdMob exige una plataforma de gestión de
consentimiento certificada. Se usa la de Google, **UMP**, que viene en su propia
biblioteca.

El orden importa: primero se pregunta a UMP, y solo cuando dice que se puede
pedir anuncios se inicializa el SDK y se cargan. Al revés, la primera petición
saldría sin consentimiento.

Si UMP falla —sin red, servicio caído—, la aplicación **sigue funcionando sin
anuncios**. Un recetario que no se abre porque no ha podido cargar publicidad
sería justo lo que `mission.md` prohíbe.

## Implementación

1. Dependencias `play-services-ads` y `user-messaging-platform` en el catálogo.
2. `AndroidManifest.xml`: metadato con el identificador de aplicación de AdMob.
3. `build.gradle.kts`: identificadores de bloque por tipo de compilación.
4. `datos/Anuncios.kt` — inicialización, consentimiento y registro del dispositivo de prueba.
5. `ui/Banner.kt` — el componible, con `AndroidView` sobre `AdView`.
6. Colocarlo en el recetario y en la búsqueda. **En la ficha no se toca nada.**

## Decisiones

- **Banner adaptable anclado**, no `AdSize.BANNER` fijo. El fijo son 320×50 en
  cualquier pantalla, que en un móvil moderno se ve pequeño y desaprovecha; el
  adaptable calcula la altura a partir del ancho real y rinde mejor.
- **El hueco no se reserva.** Si el anuncio no carga, el componible no ocupa nada
  y la lista llega hasta abajo. Reservar el hueco dejaría una franja gris
  permanente en cuanto fallara la red.
- **Un bloque por pantalla**, ya creados en la consola. Permite ver cuál rinde y
  retirar el que moleste sin tocar el otro.
- **`AdView` se destruye con el componible.** Un `AdView` vivo tras salir de la
  pantalla sigue refrescando y pidiendo anuncios: fuga de memoria e impresiones
  que nadie ve.
- **La inicialización va en la actividad, no en un `Application`.** No hace falta
  una clase `Application` propia solo para esto, y así el arranque de la
  aplicación no depende de que el SDK responda.

## Riesgos

- **Suspensión de la cuenta por tráfico inválido.** Ver arriba: mitigado con
  identificadores de prueba en depuración y registro del dispositivo.
- **El SDK de anuncios engorda el APK y el arranque.** Es el precio conocido de
  monetizar; se acota no metiendo mediación.
- **Anuncios inapropiados junto a recetas.** AdMob permite bloquear categorías
  desde la consola. No se toca aquí, pero conviene revisarlo tras las primeras
  impresiones reales.
