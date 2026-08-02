# Misión

## Qué construimos

Un recetario personal en la nube: cada usuario guarda sus recetas y decide cuáles comparte. Resuelve el problema de tener las recetas dispersas (libretas, capturas, enlaces) y no poder encontrar la que buscas cuando la necesitas.

Piezas principales:

1. **Cuentas de usuario** — alta **en dos pasos con verificación de correo**: el usuario indica su correo, recibe un enlace en ese buzón y solo al abrirlo elige contraseña y queda dada de alta la cuenta. Cada usuario tiene su propio recetario.
2. **Recetas** — crear, editar y consultar recetas, con fotos. Cada receta es **privada** (solo su autor) o **pública** (visible para cualquier usuario registrado). **Hace falta cuenta para consultar cualquier cosa**: no hay acceso anónimo.
3. **Búsqueda multicriterio** — encontrar recetas por ingredientes, nombre y tipo de plato, combinables entre sí.

## Para quién

- **Cocinero doméstico** — quiere sus recetas siempre a mano y encontrarlas rápido, tanto en el ordenador como en el móvil mientras cocina.
- **Quien comparte recetas** — publica algunas de sus recetas para que otros usuarios las consulten, sin exponer todo su recetario.

_Todos son usuarios registrados: no hay figura de visitante anónimo. El registro es la puerta de entrada al producto._

## Principios

- **Privado por defecto** — una receta nace privada; publicarla es un acto explícito del autor.
- **Encontrar antes que acumular** — el valor está en recuperar la receta correcta, no en almacenar muchas. La búsqueda es la función central, no un añadido.
- **Usable con las manos sucias** — se consulta mientras se cocina: pocas pantallas, texto legible, sin fricción.
- **El autor manda sobre sus datos** — sus recetas son suyas: puede editarlas, despublicarlas y borrarlas en cualquier momento.
- **Comunidad cerrada** — todo el contenido queda dentro del círculo de usuarios registrados. "Público" significa "visible para quien tenga cuenta", nunca "abierto a internet".
- **Correo verificado o no hay cuenta** — no existen cuentas con correo sin comprobar. Es lo que sostiene la comunidad cerrada: cada cuenta corresponde a un buzón real cuyo dueño confirmó el alta.

## Qué NO es

- **No es una red social** — no hay seguidores, ni muro, ni comentarios, ni valoraciones. Público significa "consultable", no "conversable".
- **No es un planificador de menús ni un generador de listas de la compra** — el alcance es guardar y encontrar recetas.
- **No es un catálogo editorial** — el contenido lo aportan los usuarios; no hay recetas curadas por el proyecto ni importación masiva de recetarios ajenos.
- **No es una tienda** — sin pagos, compras dentro de la app ni planes de suscripción. La única monetización prevista es **publicidad con AdMob en la app Android** (ver `tech-stack.md`); la web y la API no llevan publicidad.
- **La publicidad no manda sobre el producto** — no se diseñan pantallas, ni se fuerza la navegación, ni se recorta funcionalidad para generar impresiones. Si un anuncio estorba al consultar una receta mientras se cocina, el anuncio se quita.
