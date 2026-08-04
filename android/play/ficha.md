# Ficha de Google Play — Recetas

Textos y recursos para la ficha de la tienda. Los límites de caracteres son los
que impone Play; entre paréntesis, lo que ocupa cada texto.

> **Antes de publicar, leer la sección "Lo que falta" del final.** Hay cosas que
> no son texto ni imágenes y sin las cuales la ficha no se puede enviar.

---

## Datos de la aplicación

| Campo | Valor |
| --- | --- |
| Nombre del paquete | `com.angelgf.recetas` |
| Categoría | Comida y bebida |
| Tipo | Aplicación (no juego) |
| Precio | Gratuita |
| Contiene anuncios | **Sí** — declarar que sí, porque AdMob está previsto |
| Compras en la aplicación | No |
| Política de privacidad | `https://recetas.angelgf.com.es/privacidad.html` |
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

(72)

---

## Descripción completa (máx. 4000)

```
Recetas es un recetario personal en la nube. Guarda tus recetas, encuéntralas
cuando las necesitas y tenlas a mano mientras cocinas.

Nació de un problema concreto: las recetas acaban repartidas entre libretas,
capturas de pantalla y enlaces guardados, y justo cuando quieres una no aparece.

QUÉ HACE ESTA APLICACIÓN

• Consulta tu recetario completo, con la foto de cada receta.
• Abre una receta y léela cómoda mientras cocinas: la pantalla no se apaga sola,
  así no tienes que desbloquear el móvil con las manos llenas de harina.
• Busca por nombre o por ingredientes. Combina criterios para afinar: puedes
  buscar qué hacer con lo que te queda en la nevera.
• Tu sesión se recuerda: abres y estás dentro.

TUS RECETAS SON TUYAS

Toda receta nace privada y solo la ves tú. Publicarla es una decisión explícita
y reversible, y solo la hace visible para otras personas registradas en Recetas,
nunca para internet abierto ni para buscadores.

Cuando publicas una receta, los demás ven su contenido, nunca tu correo.

PENSADO PARA LA COCINA

Pocas pantallas, texto legible y sin florituras. Lo importante es encontrar la
receta correcta y poder leerla con las manos ocupadas.

PRIVACIDAD DE VERDAD

• Solo se pide un correo y una contraseña. Nada más.
• Sin analítica, sin cookies de seguimiento y sin perfilado.
• Las fotos se limpian de metadatos al subirlas: las coordenadas GPS que tu móvil
  incrusta en cada foto —normalmente las de tu casa— se borran antes de guardar
  nada.
• Los datos están en un servidor en España.

IMPORTANTE: ESTA VERSIÓN SOLO LEE

Esta primera versión de la aplicación sirve para CONSULTAR tu recetario. Para
crear recetas, editarlas, subir fotos o compartirlas, entra desde el navegador en
recetas.angelgf.com.es, que funciona igual de bien en el móvil.

Se dice aquí y no en la letra pequeña para que nadie se la descargue esperando
otra cosa. Crear y editar desde la aplicación llegará más adelante.

HACE FALTA CUENTA

Recetas es una comunidad cerrada: no hay acceso anónimo a ningún contenido. La
cuenta se crea gratis desde recetas.angelgf.com.es y requiere verificar tu correo.
```

(≈1 900)

---

## Novedades de la versión (máx. 500)

```
Primera versión.

Consulta tu recetario, abre cualquier receta y búscala por nombre o por
ingredientes. La pantalla no se apaga mientras lees una receta.

Para crear y editar recetas, de momento entra desde recetas.angelgf.com.es
```

(≈240)

---

## Recursos gráficos

| Recurso | Archivo | Tamaño | Estado |
| --- | --- | --- | --- |
| Icono | `graficos/icono-512.png` | 512×512 | Listo |
| Gráfico destacado | `graficos/destacado-1024x500.png` | 1024×500 | Listo |
| Captura 1 · Recetario | `capturas/1-recetario.png` | 1080×2424 | **Con datos de prueba** |
| Captura 2 · Ficha | `capturas/2-ficha.png` | 1080×2424 | **Con datos de prueba** |
| Captura 3 · Búsqueda | `capturas/3-busqueda.png` | 1080×2424 | **Con datos de prueba** |
| Captura 4 · Inicio de sesión | `capturas/4-sesion.png` | 1080×2424 | Aceptable |

El icono y el gráfico destacado se generan con el programa que hay en
`herramientas/graficos-de-play/`, a partir de la misma figura que el favicon de la
web, para que la tienda, el lanzador y el sitio no parezcan tres productos
distintos.

### Las capturas hay que rehacerlas

Son capturas reales del emulador, pero del **entorno de desarrollo**: salen
recetas llamadas "Tortitas para escalar" o "Croquetas de jamon" y, en lugar de
fotos de comida, unos óvalos amarillos que son la imagen de prueba con la que se
verificaron las fotos.

Publicar eso da una impresión pésima y además Play pide que las capturas
representen la aplicación real. Hay que:

1. Cargar en una cuenta cuatro o cinco recetas de verdad, con fotos de comida.
2. Repetir las capturas con esa cuenta.

El procedimiento está en `android/README.md`; las capturas se sacan con
`adb shell screencap`.

---

## Lo que falta antes de poder publicar

Ninguno de estos puntos es texto ni imagen, y sin ellos Play no deja enviar la
ficha:

- [ ] **Rehacer las capturas** con recetas y fotos reales (ver arriba).
- [ ] **Cuestionario de clasificación de contenido.** Lo rellena una persona; para
      una aplicación así, la respuesta a todo suele ser "no".
- [ ] **Sección de seguridad de los datos.** Hay que declarar que se recogen
      correo electrónico y contenido del usuario (recetas y fotos), que van
      cifrados en tránsito y que el borrado se solicita por correo. La política de
      privacidad publicada da todas las respuestas.
- [ ] **Declarar que la aplicación contiene anuncios**, aunque todavía no los
      muestre, porque AdMob está previsto. Si se declara que no y luego se añaden,
      es una infracción.
- [ ] **Firma de la aplicación**: generar el almacén de claves de publicación y
      compilar un AAB firmado. Ese almacén **no va al repositorio**.
- [ ] **Cuenta de desarrollador de Google Play** con la cuota pagada.
- [ ] Decidir si se publica con la aplicación tal como está —solo lectura— o se
      espera a que pueda crear y editar. La descripción actual es honesta al
      respecto, pero es una decisión de producto.
