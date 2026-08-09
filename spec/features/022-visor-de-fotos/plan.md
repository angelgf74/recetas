# 022 · Visor de fotos — Plan

## Enfoque

**Feature solo de web.** Ni un endpoint nuevo, ni un campo nuevo en el contrato:
todo lo que hace falta —foto completa, miniatura, borrado— ya existe desde la 009.
Lo que cambia es cuándo se pide cada cosa.

Un componente `VisorDeFotos` que recibe la lista de fotos de la receta y el
índice por el que empezar, y la ficha que pinta miniaturas en lugar de las fotos
enteras.

## Sin JavaScript

El teclado y el foco se hacen con Blazor: `@onkeydown` sobre el contenedor del
diálogo y `FocusAsync` sobre un `ElementReference`. No se añade ningún archivo
`.js` ni se toca la política de seguridad de contenido, que **no permite
`unsafe-inline`**: cualquier manejador escrito en el HTML sería bloqueado por
nginx en producción y funcionaría en desarrollo, que es la peor combinación.

El foco entra en el diálogo al abrirlo, y al cerrarlo vuelve a la tira de
miniaturas. No se implementa una trampa de foco completa —recorrer con el
tabulador puede salirse del diálogo—: hacerlo bien exige interceptar el
tabulador en ambos extremos, y con tres botones dentro no compensa. Escape
cierra, que es la salida que importa.

## Qué se descarga y cuándo

| Momento | Petición |
|---------|----------|
| Cargar la ficha | Una miniatura por foto |
| Abrir el visor | El archivo completo de **esa** foto |
| Pasar de foto | El archivo completo de la nueva |

El componente `FotoDeReceta` ya sabe pedir una u otra según el parámetro
`Miniatura`, así que el visor lo reutiliza sin tocarlo.

**No se precarga la siguiente**: sería pedir un archivo grande que quizá nadie
mire, que es lo que esta feature viene a evitar.

## Borrar desde el visor

La acción de borrar se va de la tira y entra en el visor, junto a la foto que se
está mirando. No es un capricho de colocación: en una tira de miniaturas de
4,5 rem, "borrar esta" es una operación irreversible sobre algo que no se ve
bien. Dentro del visor se borra lo que se está mirando.

Al confirmar: se cierra el visor y la ficha recarga, que es lo que ya hacía.

## Componente

```
VisorDeFotos
  RecetaId      Guid
  Fotos         IReadOnlyList<FotoRespuesta>
  Indice        int          (por cuál empezar)
  PuedeBorrar   bool         (EsMia, lo decide la ficha)
  AlCerrar      EventCallback
  AlBorrar      EventCallback<Guid>
```

El visor no llama a la API para borrar: avisa a la ficha, que es quien ya tiene
`BorrarFotoAsync` y sabe recargar. Un componente de presentación que además
escribe en el servidor tiene dos motivos para cambiar.

## Pasos

1. `VisorDeFotos.razor` — superposición, navegación, teclado, foco.
2. Estilos del visor en `app.css`.
3. `FichaDeReceta.razor` — tira de miniaturas, apertura del visor, borrado desde él.
4. Tests de bUnit.

## Archivos afectados

**Nuevos**

- `src/Recetas.Web/Componentes/VisorDeFotos.razor`
- `tests/Recetas.Web.Tests/Componentes/VisorDeFotosTests.cs`

**Modificados**

- `src/Recetas.Web/Pages/FichaDeReceta.razor`
- `src/Recetas.Web/wwwroot/css/app.css`
- `tests/Recetas.Web.Tests/Paginas/FichaDeRecetaTests.cs`

## Riesgos y decisiones

- **Los tests de bUnit no prueban un navegador.** Pintan el componente y disparan
  eventos, así que sirven para "¿hay botón?" y "¿cambia de foto?", pero no para
  saber si la superposición se ve bien o si Escape llega. Eso hay que mirarlo en
  pantalla, y se dirá en la spec si no se ha hecho.
- **Las miniaturas se generan la primera vez que se piden** (009). La ficha de
  una receta con fotos antiguas hará ese trabajo al abrirse por primera vez;
  es lo mismo que ya pasa en los listados.
- **`aspect-ratio` de los estados de carga.** La clase `.foto-miniatura` ya anula
  el 4/3 de los huecos de carga y error; el visor necesita lo contrario, así que
  sus estados van con clases propias y no reutilizan `.foto`.
- **Escape cierra el visor, no la ficha.** El manejador va en el contenedor del
  diálogo, que es quien tiene el foco. Si el foco no entrara, Escape no haría
  nada y el botón de cerrar seguiría estando: la salida no depende del teclado.
