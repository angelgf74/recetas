# 018 · Tests de la web — Plan

_Cómo se implementa lo descrito en `spec.md`. Debe respetar la `constitution/`._

## Enfoque

Proyecto de test espejo, como el resto: `tests/Recetas.Web.Tests`, con **bUnit** sobre xUnit. Referencia a `Recetas.Web`, que es lo que mete la web en la cadena de compilación de `dotnet test`.

**No se prueban componentes contra la API de verdad.** `ClienteDeApi` envuelve un `HttpClient`, así que se le da uno con un manejador falso que devuelve respuestas preparadas. Nada de red, nada de Docker: estos tests tienen que correr en el filtro `Categoria!=Integracion`.

**Se comprueba qué puede hacer el usuario, no cómo está escrito el HTML.** Las aserciones buscan textos de botones y enlaces —"Editar", "Denunciar esta receta"—, no clases CSS ni estructura del marcado. Un test que se rompe al renombrar una clase se acaba actualizando sin leerlo.

## Implementación

1. **`tests/Recetas.Web.Tests/Recetas.Web.Tests.csproj`** — xUnit + `bunit`, con referencia a `Recetas.Web`. Añadir a `Recetas.slnx`.

2. **`Dobles/ManejadorDeRespuestas.cs`** — `HttpMessageHandler` que responde con lo que se le diga por ruta. Es la costura por la que entra todo lo demás.

3. **`Dobles/ContextoDeWeb.cs`** — base de los tests: registra `ClienteDeApi` con el manejador falso, `EstadoDeSesion` con un `IJSRuntime` de mentira, y lo que pidan los componentes.

4. **`Componentes/ConfirmacionTests.cs`** — que no invoca la acción hasta confirmar, y que cancelar no la invoca nunca.

5. **`Paginas/FichaDeRecetaTests.cs`** — el grueso: receta propia, ajena, ajena con permiso de retirada, y ajena ya denunciada.

6. **`tests/Recetas.Arquitectura.Tests`** — extender los **dos** enfoques con el proyecto nuevo, como exige `CLAUDE.md`.

7. **Comprobación final** — romper la web a propósito y verificar que `dotnet test` se pone en rojo. Sin eso, la mitad de la feature no está demostrada.

## Decisiones

- **bUnit y no pruebas con navegador** — lo que ha fallado dos veces es "qué se ofrece en pantalla según el estado", que bUnit responde en milisegundos y sin infraestructura. Un navegador de verdad prueba además el CSS y la navegación real, a cambio de minutos por ejecución y de tests que fallan solos.

- **Manejador HTTP falso en lugar de una interfaz nueva** — se podría extraer un `IClienteDeApi` y doblarlo, pero eso añade una abstracción a producción para comodidad del test. El `HttpClient` ya es esa costura, y así se prueba también la traducción de respuestas que hace `ClienteDeApi`.

- **Aserciones por texto visible** — es lo que ve el usuario y lo que describe el criterio de aceptación. Si mañana el botón de editar pasa a ser un icono, el test debe fallar y hacernos mirar si sigue habiendo forma de editar.

- **El proyecto se llama `Recetas.Web.Tests`** y va en `tests/`, como los otros cuatro. La convención ya existe; inventar otra para este solo sería ruido.

## Riesgos

- **bUnit desactualizado respecto a .NET 10.** Si el paquete no soporta la versión, hay que decidir entre esperar o fijar una anterior. Se comprueba antes de escribir tests: si no restaura, esta feature cambia de forma.

- **Tests frágiles por acoplarse al texto.** Buscar "Editar" es frágil si mañana pone "Modificar". Es el precio de probar lo que ve el usuario, y es el fallo bueno: obliga a mirar. Lo que no se acepta es acoplarse a clases CSS, que cambian sin que cambie el comportamiento.

- **Que el proyecto nuevo se olvide en los tests de arquitectura.** Es justo lo que `CLAUDE.md` avisa. Va en la lista de tareas, y uno de los dos enfoques (el que lee los `.csproj`) lo detectaría igualmente.
