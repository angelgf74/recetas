# 021 · Favoritos privados — Plan

## Enfoque

Una tabla de marcas con clave compuesta `(usuario_id, receta_id)`, un caso de uso
que la mantiene, y **el filtro de visibilidad aplicado al leer, no al escribir**.

Esa última frase es todo el diseño. La marca no comprueba nada del futuro: dice
que en ese momento la receta se podía ver. Quien decide qué sale en la lista es
la consulta que la lee, contra la visibilidad de **ahora**. Así, si el autor
despublica, la receta desaparece de la lista sin que nadie tenga que ir a borrar
filas cuando cambia una visibilidad — y sin que un olvido en ese sitio convierta
los favoritos en acceso a contenido retirado.

## Entidad y persistencia

`Favorito` en el dominio: `UsuarioId`, `RecetaId`, `FechaDeMarca`. Sin `Id`
propio: la clave **es** el par, y una clave sustituta permitiría dos filas
idénticas con identificadores distintos, que es justo lo que no puede pasar.

Tabla `favoritos`, con clave primaria compuesta y claves foráneas en cascada a
`recetas` y a `usuarios`, igual que `denuncias`:

- Borrar la receta se lleva sus marcas. Un favorito sin receta no significa nada.
- Borrar el usuario se lleva las suyas. La 016 ya borra las recetas del usuario
  en la capa de aplicación, y esa cascada arrastra las marcas que otros pusieran
  sobre ellas.

El índice de la clave primaria sirve para "mis favoritos"; se añade uno por
`receta_id` para que la cascada de borrado de receta no haga recorrido completo.

## Puertos

`IRepositorioDeFavoritos` — `EstaMarcadaAsync`, `MarcarAsync`, `DesmarcarAsync`.

`IRepositorioDeRecetas.ListarFavoritasAsync(usuarioId)` — devuelve **recetas**, no
marcas, así que va donde están las demás consultas de recetas. Como
`BuscarAsync`, **el filtro de visibilidad va dentro de la consulta**: se unen
favoritos y recetas y se descarta ahí lo que ya no se puede ver. Filtrar después
en memoria dejaría abierto que una refactorización devolviera la lista antes de
aplicar la condición, y aquí eso significa enseñar recetas retiradas.

## Aplicación

`GestionDeFavoritos` con tres operaciones:

- `MarcarAsync(usuarioId, recetaId)` — busca la receta, exige `PuedeVerla`, y si
  ya estaba marcada no hace nada. **`PuedeVerla`, no `EsDe`**: se puede marcar lo
  propio y lo ajeno publicado. No existe otra pregunta de permisos en esta
  feature.
- `DesmarcarAsync` — desmarcar lo que no estaba marcado es correcto. Lo contrario
  obligaría a la interfaz a saber el estado antes de actuar, y dos pestañas
  abiertas producirían un error que no es tal.
- `ListarMisFavoritasAsync`.

Desmarcar **no comprueba la visibilidad**: quitar una marca sobre algo que ya no
puedes ver tiene que seguir siendo posible, o quedaría una fila que el usuario no
tiene forma de eliminar.

## API

| Verbo    | Ruta                        | Qué hace                      |
|----------|-----------------------------|-------------------------------|
| `PUT`    | `/recetas/{id}/favorito`    | Marca. Idempotente, `204`.    |
| `DELETE` | `/recetas/{id}/favorito`    | Desmarca. Idempotente, `204`. |
| `GET`    | `/recetas/favoritas`        | Mis favoritas visibles.       |

`PUT` y no `POST`, al revés que en la publicación: publicar es un **acto** que
ocurre una vez, y marcar es un **estado** que se fija. Que marcar dos veces
responda igual no es una concesión, es lo que significa `PUT`.

`/recetas/favoritas` y no `/yo/favoritos`: devuelve recetas, y va junto a
`/recetas` y `/recetas/busqueda`, que son las otras consultas de conjunto. El
restrictor `{id:guid}` impide que choque con la ficha, igual que con `/busqueda`.

Marcar una receta privada ajena responde `404` con el mismo texto que el resto,
por el motivo de siempre: un `403` confirmaría que existe.

## Contratos

`RespuestaDeReceta` gana `EsFavorita`. Lo dice el servidor por lo mismo que
`EsMia`: el cliente no puede deducirlo sin pedir además la lista entera.

**`ResumenDeReceta` no lo gana.** El corazón se pinta en la ficha, no en las
tarjetas del recetario ni en los resultados de búsqueda. Marcarlas obligaría a
una consulta más por listado para un dato que solo sirve de adorno donde no se
puede pulsar; en la lista de favoritos, además, todas lo son. Si al usarlo se
echa en falta, se añade entonces.

**Ninguna respuesta lleva un recuento.** No hay campo que decir; se anota aquí
porque el hueco es deliberado y el próximo que lea el contrato podría creer que
falta.

## Web

- Botón de favorito en la ficha, junto a las demás acciones. Etiqueta clara —
  "Guardar en favoritos" / "Quitar de favoritos"— y no solo un icono: un corazón
  sin texto se confunde con "me gusta", que es exactamente lo que esto no es.
- Página `/favoritos`, con las mismas tarjetas y miniaturas del recetario.
- Enlace en la navegación.
- `ClienteDeApi` gana `MarcarFavoritaAsync`, `DesmarcarFavoritaAsync` y
  `ListarFavoritasAsync`. Ninguna página construye peticiones a mano.

## Pasos

1. `Favorito` en el dominio y `IRepositorioDeFavoritos`.
2. `ListarFavoritasAsync` en `IRepositorioDeRecetas`.
3. `GestionDeFavoritos` y registro en la inyección de dependencias.
4. Configuración de EF, `DbSet`, migración.
5. Implementación de los repositorios.
6. Contrato (`EsFavorita`) y endpoints.
7. Web: cliente, botón en la ficha, página de favoritos, navegación.
8. Tests: aplicación, integración y bUnit.

## Archivos afectados

**Nuevos**

- `src/Recetas.Dominio/Favoritos/Favorito.cs`
- `src/Recetas.Dominio/Puertos/IRepositorioDeFavoritos.cs`
- `src/Recetas.Aplicacion/Favoritos/GestionDeFavoritos.cs`
- `src/Recetas.Infraestructura/Persistencia/RepositorioDeFavoritosEf.cs`
- `src/Recetas.Infraestructura/Persistencia/Configuraciones/ConfiguracionDeFavorito.cs`
- `src/Recetas.Web/Pages/Favoritos.razor` (+ `.css`)
- `tests/Recetas.Aplicacion.Tests/Favoritos/GestionDeFavoritosTests.cs`
- `tests/Recetas.Api.Tests/FavoritosTests.cs`

**Modificados**

- `IRepositorioDeRecetas` y `RepositorioDeRecetasEf` — `ListarFavoritasAsync`
- `RecetasDbContext`, `InyeccionDeDependencias` (aplicación e infraestructura)
- `RecetasEndpoints` — tres rutas y el campo del contrato
- `ContratosDeRecetas` — `EsFavorita`
- `ClienteDeApi`, `FichaDeReceta.razor`, `MainLayout.razor`
- Dobles de test: repositorio de favoritos en memoria

## Riesgos y decisiones

- **Que la lista filtre por visibilidad es un requisito de seguridad, no una
  comodidad.** Sin ese filtro, cualquiera que marcara una receta antes de que la
  retiraran seguiría viéndola. Va con test explícito: marcar, despublicar,
  comprobar que desaparece; volver a publicar, comprobar que vuelve.
- **La exportación de datos (019) no incluye los favoritos.** Un favorito ajeno
  es una referencia a contenido de otra persona: exportar solo identificadores no
  sirve de nada, y exportar los nombres metería contenido ajeno en el paquete de
  uno. Se decide aparte; queda anotado en el backlog.
- **Los favoritos no cambian la fecha de modificación de la receta.** Marcarla no
  la toca: no es un cambio de la receta, y moverla en el listado del autor le
  filtraría que alguien la ha marcado.
- **Clave compuesta en EF.** No aplica la trampa de `ValueGeneratedNever()` —no
  hay `Guid` generado por el dominio— pero sí conviene comprobar que marcar dos
  veces a la vez no revienta: el caso de uso mira antes, y la clave primaria es
  la red que cubre las dos peticiones simultáneas que esa mirada no ve.
