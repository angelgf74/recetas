# Ficha de Google Play — Recetas

Textos y recursos para la ficha de la tienda. Los límites de caracteres son los
que impone Play; entre paréntesis, lo que ocupa cada texto.

> **Antes de publicar, leer la sección "Lo que falta" del final.** Hay cosas que
> no son texto ni imágenes y sin las cuales la ficha no se puede enviar.

> **Esta ficha describe la aplicación completa** (features 014, 015 y 016): crea,
> edita, comparte y borra. La versión anterior de este archivo decía que la
> aplicación "solo lee", que era cierto en la 012 y dejó de serlo en la 014.
> **Prometer de menos también engaña**, y además contradice lo que el revisor ve.

---

## Datos de la aplicación

| Campo | Valor |
| --- | --- |
| Nombre del paquete | `com.angelgf.recetas` |
| Categoría | Comida y bebida |
| Tipo | Aplicación (no juego) |
| Precio | Gratuita — **no se puede cambiar después** |
| Contiene anuncios | **Sí** |
| Compras en la aplicación | No |
| Política de privacidad | `https://recetas.angelgf.com.es/privacidad.html` |
| Eliminación de la cuenta | `https://recetas.angelgf.com.es/borrar-cuenta.html` |
| Correo de contacto | `angelgf@gmail.com` |
| Sitio web | `https://recetas.angelgf.com.es` |

---

## Título (máx. 30)

```
Recetas: tu recetario privado
```

(29)

---

## Descripción corta (máx. 80)

```
Tu recetario personal: privado por defecto y siempre a mano en la cocina.
```

(73)

---

## Descripción completa (máx. 4000)

```
Recetas es un recetario personal en la nube. Guarda tus recetas, encuéntralas
cuando las necesitas y tenlas a mano mientras cocinas.

Nació de un problema concreto: las recetas acaban repartidas entre libretas,
capturas de pantalla y enlaces guardados, y justo cuando quieres una no aparece.

QUÉ PUEDES HACER

• Guarda tus recetas con ingredientes, pasos y fotos.
• Búscalas por nombre o por ingredientes, combinando criterios: sirve para
  decidir qué hacer con lo que te queda en la nevera.
• Ajusta las cantidades a los comensales que seas. No es una multiplicación
  seca: redondea a cantidades que se puedan medir de verdad en una cocina.
• Importa una receta pegando la dirección de una página web. Se rellena el
  formulario y tú revisas antes de guardar.
• Lee la receta con la pantalla encendida: no se apaga sola mientras cocinas,
  así no tienes que desbloquear el móvil con las manos llenas de harina.

TUS RECETAS SON TUYAS

Toda receta nace privada y solo la ves tú. Compartirla es una decisión explícita
y reversible, y solo la hace visible para otras personas registradas en Recetas,
nunca para internet abierto ni para buscadores.

Cuando compartes una receta, los demás ven su contenido, nunca tu correo.

PENSADO PARA LA COCINA

Pocas pantallas, texto legible y sin florituras. Lo importante es encontrar la
receta correcta y poder leerla con las manos ocupadas.

PRIVACIDAD

• Para tener cuenta solo se pide un correo y una contraseña. Nada más.
• Sin analítica de uso ni cookies de seguimiento.
• Las fotos se limpian de metadatos al subirlas: las coordenadas GPS que tu
  móvil incrusta en cada foto —normalmente las de tu casa— se borran antes de
  guardar nada.
• Tus recetas y tus fotos están en un servidor en España.
• Puedes borrar tu cuenta entera desde Ajustes, en el momento y sin pedir
  permiso a nadie.

PUBLICIDAD

La aplicación muestra anuncios al pie del recetario y de la búsqueda. No hay
anuncios mientras lees una receta, que es la pantalla que se usa cocinando, ni
anuncios a pantalla completa en ninguna parte.

La primera vez te preguntamos si aceptas la publicidad personalizada. Puedes
decir que no, y cambiar de opinión cuando quieras desde Ajustes: la aplicación
hace exactamente lo mismo en ambos casos.

HACE FALTA CUENTA

Recetas es una comunidad cerrada: no hay acceso anónimo a ningún contenido. La
cuenta se crea gratis desde la propia aplicación y requiere verificar tu correo.

También puedes usar Recetas desde el navegador, con la misma cuenta, en
recetas.angelgf.com.es
```

(2 528)

---

## Novedades de la versión (máx. 500)

```
Primera versión pública.

Crea y edita recetas, añade fotos, compártelas y búscalas por nombre o por
ingredientes. Ajusta las cantidades a los comensales que seas, e importa una
receta pegando la dirección de una página web.

Puedes borrar tu cuenta desde Ajustes cuando quieras.
```

(279)

---

## Recursos gráficos

| Recurso | Archivo | Tamaño | Estado |
| --- | --- | --- | --- |
| Icono | `graficos/icono-512.png` | 512×512 | Listo |
| Gráfico destacado | `graficos/destacado-1024x500.png` | 1024×500 | Listo |
| Captura 1 · Recetario | `capturas/1-recetario.png` | 1220×2440 | Listo |
| Captura 2 · Ficha con foto | `capturas/2-ficha.png` | 1220×2440 | Listo |
| Captura 3 · Búsqueda | `capturas/3-busqueda.png` | 1220×2440 | Listo |
| Captura 4 · Nueva receta | `capturas/4-nueva-receta.png` | 1220×2440 | Listo |

El icono y el gráfico destacado se generan con el programa que hay en
`herramientas/graficos-de-play/`, a partir de la misma figura que el favicon de la
web, para que la tienda, el lanzador y el sitio no parezcan tres productos
distintos.

> **Las capturas no llevan el banner de anuncios.** Se hicieron antes de la 013 y
> en un teléfono con un bloqueador de DNS que impide cargarlos. Play no exige que
> aparezcan —los anuncios se declaran aparte—, pero si algún día se rehacen, hay
> que decidir si se enseñan: una captura con el anuncio de prueba de Google
> ("This is a 320x50 test ad") sería peor que ninguna.

### Cómo se hicieron, por si hay que rehacerlas

Capturadas en un teléfono real (Redmi Note 13 Pro, 1220×2712) con una cuenta que
tiene recetas y fotos de verdad:

```powershell
adb exec-out screencap -p > captura.png
```

**Y después recortadas a 1220×2440.** El motivo importa: Play admite una relación
de aspecto **máxima de 2:1**, y la pantalla nativa del teléfono es 2,22:1, así que
las capturas sin tocar se rechazan. El recorte quita 90 píxeles de arriba y el
resto por abajo, lo que de paso elimina la barra de estado y la de navegación.

Si se rehacen en otro dispositivo, comprobar siempre esa proporción antes de
subirlas.

---

## Lo que falta antes de poder publicar

Ninguno de estos puntos es texto ni imagen, y sin ellos Play no deja enviar la
ficha. Los pasos detallados están en `publicar.md`.

- [x] ~~Capturas con recetas y fotos reales.~~
- [x] ~~Firma de la aplicación y AAB.~~ Generado, firmado y probado en un
      teléfono real. El almacén de claves **no está en el repositorio**.
- [ ] **Cuenta de desarrollador de Google Play** con la cuota pagada.
- [ ] **Cuestionario de clasificación de contenido.** Ojo con una pregunta:
      *"¿La aplicación permite que los usuarios intercambien contenido?"* La
      respuesta es **sí** —las recetas compartidas las ven otros usuarios— y eso
      exige poder denunciar y actuar, que es lo que trajo la feature 015.
- [ ] **Sección de seguridad de los datos.** Se recogen correo electrónico y
      contenido del usuario (recetas y fotos), van cifrados en tránsito, se
      comparten con la red publicitaria los identificadores de AdMob, y la cuenta
      se puede borrar desde la aplicación. Las respuestas están en `publicar.md`.
- [ ] **Declarar que la aplicación contiene anuncios.** Ya los muestra.
- [ ] **URL de eliminación de la cuenta**: `https://recetas.angelgf.com.es/borrar-cuenta.html`
- [ ] **Países.** Empezar solo por **España**: el mensaje de consentimiento de
      AdMob está configurado para el Espacio Económico Europeo, la política habla
      de RGPD y la AEPD, y toda la aplicación está en español. Ampliar después es
      inmediato; quitar un país ya publicado, no.
