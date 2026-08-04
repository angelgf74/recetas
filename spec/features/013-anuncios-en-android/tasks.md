# 013 · Anuncios en Android — Tareas

- [x] Dependencias `play-services-ads` y `user-messaging-platform`.
- [x] Identificador de aplicación de AdMob en el manifiesto.
- [x] Identificadores de bloque por tipo de compilación: prueba en depuración, reales en publicación.
- [x] `Anuncios.kt`: consentimiento con UMP, inicialización y dispositivos de prueba.
- [x] `Banner.kt`: componible con `AndroidView`, adaptable y que se destruye al salir.
- [x] Banner al pie del recetario.
- [x] Banner al pie de la búsqueda.
- [x] Comprobar que la ficha sigue sin anuncios.
- [x] `gradlew assembleDebug` y `gradlew test` en verde, sin avisos.
- [x] Probar en un dispositivo real: el banner de prueba sale al pie del recetario.
- [ ] Crear el mensaje de consentimiento del RGPD en la consola de AdMob. **Ya no bloquea los anuncios, pero es obligatorio antes de publicar con anuncios reales en Europa.**
- [ ] Actualizar la política de privacidad. Se hará **cuando los anuncios se sirvan de verdad**, no ahora: hoy diría algo falso, porque ni la web lleva publicidad ni la aplicación está publicada.
- [ ] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## El error que costó dos diagnósticos equivocados

Durante un rato no salía ningún anuncio, y se dio por hecho que la causa era esta
respuesta de la plataforma de consentimiento:

```
Publisher misconfiguration: Failed to read publisher's account configuration; no
form(s) configured for the input app ID.
```

Es verdad que falta ese formulario. **Pero no era eso lo que impedía los
anuncios.** Era el código: ante un error de `requestConsentInfoUpdate` se
abandonaba sin más, y nunca se llegaba a preguntar si de todos modos se podían
pedir anuncios.

La distinción importa: **fallar al consultar el consentimiento no es lo mismo que
el usuario diciendo que no.** Puede ser la red, o una configuración incompleta en
la consola. UMP conserva el estado de la última consulta y sigue sabiendo
responder, así que ahora se le pregunta con `canRequestAds()` también en el camino
de error. Si dice que sí, se piden anuncios; si dice que no, no se piden.

Lección para el futuro: cuando algo no aparece, sospechar del código propio antes
que de la configuración ajena, aunque el log señale a la configuración ajena.

## Lo que sigue faltando

Crear el mensaje de consentimiento en **AdMob → Privacidad y mensajes →
Reglamentos europeos → Crear**. Ya no bloquea los anuncios de prueba, pero es
**obligatorio antes de publicar** con anuncios reales para usuarios europeos.

Ese formulario pide decisiones que no son técnicas —qué proveedores de anuncios se
autorizan y qué se le dice al usuario— y son del titular de la cuenta.
