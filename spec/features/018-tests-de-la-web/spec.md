# 018 · Tests de la web

**Estado:** implementado ✅

## Qué hace

La web entra en la suite de pruebas: un proyecto `Recetas.Web.Tests` con **bUnit** que renderiza componentes de Blazor y comprueba **qué se le ofrece al usuario** en cada situación.

De rebote —y no es lo menos importante— hace que `dotnet test` **compile la web**, cosa que antes no ocurría.

## Por qué

**Dos fallos reales de este proyecto, y ninguno lo detectó la suite.**

El primero, en la 015: se implementó la retirada por moderación con todos sus tests en verde **y sin ningún botón que la invocara**. Los tests comprobaban que el endpoint autorizaba bien, no que hubiera manera de llegar a él. El endpoint existía, la política de Google se cumplía sobre el papel, y en la aplicación no había nada que pulsar.

El segundo, en la 016: un `using` que faltaba en `ClienteDeApi` de la web llegó al commit y solo apareció al desplegar, porque **`dotnet test` no compila `Recetas.Web`**. Comprobado el 8 de agosto de 2026 rompiendo la web a propósito: 540 pruebas en verde y código de salida `0`.

La causa de lo segundo es simple: ningún proyecto de test referencia la web, así que nadie la construye. Un proyecto de tests la referencia por definición.

## Criterios de aceptación

- [x] Existe `tests/Recetas.Web.Tests` con bUnit, referenciando `Recetas.Web`.
- [x] **`dotnet test` falla si la web no compila.** Comprobado rompiéndola a propósito y viendo la suite en rojo.
- [x] La ficha de una receta **propia** ofrece editar, foto, compartir y borrar; **no** ofrece denunciar.
- [x] La ficha de una receta **ajena** ofrece denunciar; **no** ofrece editar, foto, compartir ni borrar.
- [x] La ficha de una receta ajena ofrece **retirar** solo cuando la respuesta dice que quien mira puede hacerlo. **Es el test que habría cazado el fallo de la 015.**
- [x] Una receta ajena **ya denunciada en esta sesión** deja de ofrecer denunciar.
- [x] El componente de confirmación **no ejecuta la acción hasta que se confirma**.
- [x] Los tests **no llegan a la red**: el cliente de API se alimenta con respuestas preparadas.
- [x] Los tests de arquitectura reconocen el proyecto nuevo, en sus **dos** enfoques.

## Fuera de alcance

- **Cobertura de toda la web.** Se prueban los componentes donde **una condición decide qué acciones se ofrecen**, que es donde ha fallado. Un formulario que solo pinta campos no gana nada con un test.
- **Pruebas de extremo a extremo con navegador** (Playwright y similares). Otra herramienta, otra velocidad y otro coste de mantenimiento; con bUnit se cubre lo que ha fallado de verdad.
- **Instantáneas de HTML.** Fijan el marcado y se rompen al cambiar una clase CSS, con lo que acaban actualizándose sin mirar. Se comprueba lo que el usuario puede hacer, no cómo está escrito.
- **Tests de la aplicación Android.** El mismo razonamiento aplica y merece su propia entrada en el backlog.
