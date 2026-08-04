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
- [x] Probar en el emulador. **Ver abajo: el anuncio no llega a salir, y el motivo está fuera del código.**
- [ ] Crear el mensaje de consentimiento del RGPD en la consola de AdMob. **Bloquea la feature.**
- [ ] Actualizar la política de privacidad. Se hará **cuando los anuncios se sirvan de verdad**, no ahora: hoy diría algo falso, porque ni la web lleva publicidad ni la aplicación está publicada.
- [ ] Mover la feature a "Hecho" en `../../constitution/roadmap.md`.

## Lo que falta, y por qué no es código

La integración está terminada y se comporta como debe, pero **no sale ningún
anuncio**, y el diagnóstico es exacto. Esto es lo que responde la plataforma de
consentimiento de Google al arrancar:

```
No se pudo obtener el consentimiento: Publisher misconfiguration: Failed to read
publisher's account configuration; no form(s) configured for the input app ID.
Verify that you have configured one or more forms for this application and try
again. Received app ID: `ca-app-pub-8600791204816041~1145017083`.
```

Traducido: **en la consola de AdMob no hay creado ningún mensaje de
consentimiento** para esta aplicación. Sin él, UMP no puede decir si se pueden
pedir anuncios, y el código —correctamente— no pide ninguno.

Que la aplicación siga funcionando con normalidad en ese caso **es un criterio de
aceptación cumplido**, no un fallo: un recetario que no se abre porque no ha
podido cargar publicidad sería justo lo que `mission.md` prohíbe.

### Cómo se arregla

En la consola de AdMob, **Privacidad y mensajes → Reglamentos europeos → Crear**.
El formulario pide decisiones que no son técnicas: qué proveedores de anuncios se
autorizan y qué se le dice exactamente al usuario para recabar su consentimiento
bajo el RGPD. Son decisiones del titular de la cuenta.

Una vez creado y publicado el mensaje, no hay que tocar el código: la próxima vez
que arranque la aplicación, UMP responderá y el banner aparecerá.
