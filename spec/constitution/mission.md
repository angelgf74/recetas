# Misión

## Qué construimos

Un recetario personal en la nube: cada usuario guarda sus recetas y decide cuáles comparte. Resuelve el problema de tener las recetas dispersas (libretas, capturas, enlaces) y no poder encontrar la que buscas cuando la necesitas.

Piezas principales:

1. **Cuentas de usuario** — alta **en dos pasos con verificación de correo**: el usuario indica su correo, recibe un enlace en ese buzón y solo al abrirlo elige contraseña y queda dada de alta la cuenta. Cada usuario tiene su propio recetario.
2. **Recetas** — crear, editar y consultar recetas, con fotos. Cada receta es **privada** (solo su autor) o **pública** (visible para cualquier usuario registrado). **Hace falta cuenta para consultar cualquier cosa**: no hay acceso anónimo.
3. **Búsqueda multicriterio** — encontrar recetas por ingredientes, nombre y tipo de plato, combinables entre sí.

Alrededor de esas tres, dos capacidades que salieron del uso real y encajan en el mismo problema —tener la receta correcta a mano— sin ampliarlo:

- **Escalar cantidades** a otro número de comensales. Lo que aporta valor no es multiplicar, sino **redondear a cantidades que se puedan medir en una cocina**.
- **Importar** desde una página web para no teclear una receta que ya existe. **De una en una y sin guardar nada por su cuenta**: devuelve un borrador que el usuario revisa. Es lo que la mantiene del lado de "guardar tus recetas" y no del de "acumular las de otros".

## Para quién

- **Cocinero doméstico** — quiere sus recetas siempre a mano y encontrarlas rápido, tanto en el ordenador como en el móvil mientras cocina.
- **Quien comparte recetas** — publica algunas de sus recetas para que otros usuarios las consulten, sin exponer todo su recetario.

_Todos son usuarios registrados: no hay figura de visitante anónimo. El registro es la puerta de entrada al producto._

## Principios

- **Privado por defecto** — una receta nace privada; publicarla es un acto explícito del autor.
- **Encontrar antes que acumular** — el valor está en recuperar la receta correcta, no en almacenar muchas. La búsqueda es la función central, no un añadido.
- **Usable con las manos sucias** — se consulta mientras se cocina: pocas pantallas, texto legible, sin fricción.
- **El autor manda sobre sus datos** — sus recetas son suyas: puede editarlas, despublicarlas y borrarlas en cualquier momento. Y puede **irse del todo**: borrar su cuenta desde la propia aplicación se lleva sus recetas y sus fotos, sin pedir permiso, sin periodo de gracia y sin tener que escribirle a nadie.
- **Se recoge lo mínimo** — un correo y una contraseña, y nada más. Sin analítica de uso, sin cookies de seguimiento y sin campos "por si acaso". Un dato que no se pide es un dato que no hay que proteger, ni declarar, ni borrar cuando alguien se va. Por eso también **las fotos se limpian de metadatos al subirlas**: la cámara escribe dentro las coordenadas de donde se tomaron, que en una receta son casi siempre las de la casa de quien cocina.
- **Compartir tiene consecuencias, y alguien responde de ellas** — si una receta pública puede verla cualquier usuario, tiene que poder denunciarse y tiene que poder retirarse. No es un trámite para una tienda de aplicaciones: es lo que hace responsable la decisión de dejar publicar. **Retirar devuelve la receta a privada, nunca la borra**: su autor no pierde su trabajo por una denuncia que puede ser infundada.
- **Comunidad cerrada** — todo el contenido queda dentro del círculo de usuarios registrados. "Público" significa "visible para quien tenga cuenta", nunca "abierto a internet".
- **Correo verificado o no hay cuenta** — no existen cuentas con correo sin comprobar. Es lo que sostiene la comunidad cerrada: cada cuenta corresponde a un buzón real cuyo dueño confirmó el alta.

## Qué NO es

- **No es una red social** — no hay seguidores, ni muro, ni comentarios, ni valoraciones. Público significa "consultable", no "conversable".

  **Las valoraciones cuentan aunque sean solo un número.** Se propusieron estrellas de 0 a 5 y se descartaron: aunque no lleven texto y no haya nada que moderar, en cuanto cada receta lleva una nota al lado las recetas compiten entre sí, y eso convierte el recetario en algo comparable. Si alguna vez hace falta cubrir el caso real —volver a encontrar lo bueno que te encontraste—, la respuesta son **favoritos privados**, que no cuentan nada a nadie.
- **No es un planificador de menús ni un generador de listas de la compra** — el alcance es guardar y encontrar recetas.
- **No es un catálogo editorial** — el contenido lo aportan los usuarios; no hay recetas curadas por el proyecto ni importación masiva de recetarios ajenos.
- **No es una tienda** — sin pagos, compras dentro de la app ni planes de suscripción. La única monetización es **publicidad con AdMob en la app Android** (ver `tech-stack.md`); la web y la API no llevan publicidad.
- **La publicidad no manda sobre el producto** — no se diseñan pantallas, ni se fuerza la navegación, ni se recorta funcionalidad para generar impresiones. Si un anuncio estorba al consultar una receta mientras se cocina, el anuncio se quita. Por eso los banners van al pie del recetario y de la búsqueda, y **no hay ninguno en la ficha de una receta**, que es la pantalla que se lee con las manos ocupadas. Tampoco hay anuncios a pantalla completa en ninguna parte.
- **Rechazar la publicidad no cuesta funcionalidad** — quien no consiente ve la aplicación exactamente igual. Un "acepta o no funciona" convertiría el consentimiento en un peaje, que es justo lo que no es.
