# 021 · Favoritos privados

**Estado:** hecha

## Qué hace

Marcar una receta para volver a encontrarla, y una pantalla que lista lo marcado.

**Privado de verdad:** lo que marcas solo lo ves tú. El autor de la receta no se
entera, no hay contador, y en ninguna respuesta de la API aparece cuántas veces
se ha marcado nada.

## Por qué

Buscar entre lo publicado devuelve recetas que no son tuyas y que **no puedes
guardar en tu recetario**: copiarlas sería apropiárselas, y publicar una copia
llenaría la parte pública de duplicados. Hoy la única forma de volver a una
receta ajena que te gustó es acordarte de cómo se llamaba.

Es el caso de uso que `mission.md` reconoce por su nombre al descartar las
valoraciones: *"volver a encontrar lo bueno que te encontraste"*, y dice que la
respuesta son **favoritos privados, que no cuentan nada a nadie**.

## Encaje con la misión

`mission.md` descarta las valoraciones de 0 a 5 estrellas, y el motivo importa
aquí: *"en cuanto cada receta lleva una nota al lado las recetas compiten entre
sí"*. Un favorito **visible o contable** sería exactamente eso con otro nombre —
un ranking, aunque el número no salga en pantalla, porque en cuanto el dato
existe alguien lo ordena.

Por eso la privacidad no es un ajuste de esta feature: **es la feature**. Un
favorito que el autor pudiera ver ya no sería esto.

## Criterios de aceptación

- [x] Puedo marcar como favorita **cualquier receta que pueda ver**: las mías y las publicadas por otros.
- [x] Marcar dos veces la misma receta **no la duplica** y responde igual.
- [x] Desmarcar algo que no estaba marcado **no es un error**.
- [x] No puedo marcar una receta **privada ajena**, ni saber si existe: responde `404`, como el resto de la API.
- [x] Tengo una pantalla con **mis favoritos**, y en la ficha se ve si la receta lo es.
- [x] **Nadie más ve mis favoritos.** Ninguna respuesta de la API dice cuántas veces se ha marcado una receta ni quién lo hizo, ni siquiera al autor.
- [x] Si una receta ajena **deja de estar publicada**, desaparece de mis favoritos mientras siga así. La marca no se pierde: si vuelve a publicarse, vuelve a aparecer.
- [x] Al **borrar una receta**, desaparece de los favoritos de todos.
- [x] Al **borrarse una cuenta**, se van sus favoritos.

**La web no se ha visto funcionar en un navegador.** El botón de la ficha y la
página de favoritos están comprobados con bUnit, que pinta el componente pero no
es un navegador de verdad.

## Decisiones

**Se puede marcar lo propio.** El backlog lo planteó solo para recetas ajenas, y
la regla más simple es *lo que puedas ver*. Prohibir lo propio obligaría a
explicar la excepción en la interfaz —el corazón aparece unas veces sí y otras
no— y no protege nada: en un recetario de doscientas recetas, señalar las cinco
que sí salen bien es el mismo caso de uso.

**El favorito no da acceso.** Marcar no es guardar una copia: si el autor
despublica, la receta deja de verse, y su favorito deja de aparecer en la lista.
Lo contrario convertiría los favoritos en una forma de conservar el acceso a algo
que su autor decidió dejar de compartir, y en una puerta trasera para leer al
menos el nombre de una receta retirada por moderación (020).

**La marca sobrevive a que la receta deje de ser visible.** No se borra la fila
al despublicar: el autor puede volver a publicarla, y perder la marca por un
cambio de visibilidad ajeno sería una pérdida de datos que el usuario no ha
pedido. Solo se borra cuando se borra la receta.

## Fuera de alcance

- **Carpetas, listas o etiquetas de favoritos.** Una sola lista. Organizar viene de la mano de las etiquetas libres del backlog, que es otra feature y afecta a todo el recetario, no solo a esto.
- **Notas privadas sobre una receta ajena** ("le puse menos sal"). Es texto de usuario sobre contenido ajeno, con su propia conversación sobre moderación y exportación. Otra feature.
- **Buscar dentro de los favoritos.** Con una lista corta no hace falta; si crece, se resuelve con un filtro en la búsqueda y no con una búsqueda aparte.
- **Favoritos en Android.** La 014 lleva la paridad de la app, y esta feature entra en la web y en la API. La API se diseña igual, sin nada a medida de Blazor.
- **Recomendaciones, "lo más marcado" o cualquier agregado.** Es justo lo que la misión descarta.
