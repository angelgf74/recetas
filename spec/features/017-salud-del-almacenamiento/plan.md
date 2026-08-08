# 017 · Salud del almacenamiento — Plan

_Cómo se implementa lo descrito en `spec.md`. Debe respetar la `constitution/`._

## Enfoque

Un **puerto nuevo**, `IComprobadorDeAlmacenDeFotos`, hermano del que ya existe para la base de datos. El dominio pregunta "¿el almacenamiento está sano?" y no sabe que detrás hay un sistema de archivos, igual que `IAlmacenDeFotos` no lo sabe.

La alternativa —añadir un método al `IAlmacenDeFotos` actual— se descarta: ese puerto sirve para guardar y leer fotos, y sus implementaciones no tienen por qué saber de espacio libre. Un puerto por pregunta.

`EstadoDeSalud` deja de ser un enumerado de dos valores y pasa a ser un **resultado con detalle**, porque el criterio de aceptación pide decir qué pieza falla. El endpoint sigue traduciendo a `200` o `503` en un único punto.

## Implementación

1. **Dominio — `Puertos/IComprobadorDeAlmacenDeFotos.cs`.** Devuelve si el almacenamiento acepta escrituras y cuánto espacio queda.

2. **Dominio — `Salud/EstadoDeSalud.cs`.** Pasa de enumerado a registro: `BaseDeDatos`, `Almacenamiento` y un `EsCorrecto` derivado de ambos. Nada de guardar el "correcto" como un tercer campo que pueda contradecir a los otros dos.

3. **Aplicación — `ConsultarSalud`.** Pregunta a los dos puertos y compone el resultado. Las dos comprobaciones se lanzan **en paralelo**: son independientes y una sonda no debe tardar la suma de las dos.

4. **Infraestructura — `Fotos/ComprobadorDeAlmacenDeFotosEnDisco.cs`.** Comprueba en este orden: el directorio existe, se puede escribir —creando y borrando un archivo temporal— y `DriveInfo` dice que queda espacio por encima del umbral.

5. **Infraestructura — `OpcionesDeFotos`.** Un campo más: `MinimoDeEspacioLibreEnMb`, con valor por defecto.

6. **Contratos — `RespuestaDeSalud`.** Un campo más, `Almacenamiento`, junto al `BaseDeDatos` que ya está.

7. **API — `Program`.** El endpoint compone la respuesta desde el resultado y decide el código.

8. **Tests.** Dominio: que `EsCorrecto` solo lo sea con las dos piezas bien. Aplicación: cada combinación, y que una excepción del puerto se traduce a degradado. Infraestructura: directorio inexistente, sin permisos de escritura y umbral por encima del espacio real. Integración: `200` con todo sano y `503` con el directorio apuntando a un sitio imposible.

## Decisiones

- **Puerto nuevo en lugar de ampliar `IAlmacenDeFotos`** — guardar fotos y diagnosticar el disco son dos responsabilidades. Si mañana el almacén pasa a S3, el diagnóstico será otro (cuota de la cuenta, no espacio en disco) y conviene poder cambiarlo aparte.

- **Escribir de verdad para comprobar que se puede escribir** — mirar permisos con la API del sistema de archivos miente en cuanto hay ACL, un montaje de solo lectura o el disco lleno. La única comprobación fiable es intentarlo. El archivo se borra siempre, también si falla algo por el camino.

- **El archivo de prueba lleva un nombre reconocible** y va en el mismo directorio de fotos, no en el temporal del sistema: comprobar `/tmp` no dice nada del disco que importa.

- **Umbral configurable con valor por defecto** — el mínimo razonable depende del servidor, y dejarlo fijo en el código obligaría a desplegar para cambiarlo. Por defecto, suficiente para que quepan varias fotos al máximo tamaño permitido.

- **`EstadoDeSalud` como registro y no como enumerado con más valores** — un `Degradado` a secas no dice qué mirar, y añadir `DegradadoPorDisco`, `DegradadoPorBase` y `DegradadoPorAmbos` es una combinatoria que crece con cada dependencia nueva.

- **Las dos comprobaciones en paralelo** — son independientes. Encadenarlas haría que una sonda con la base lenta y el disco lento tardase la suma, y las sondas tienen tiempo de espera.

- **El endpoint sigue siendo anónimo.** Es la excepción que ya existía: un monitor externo no tiene sesión, y pedirle un JWT obligaría a guardar credenciales en el servicio de vigilancia. No revela nada del contenido: dice si el sistema opera, no qué hay dentro.

## Riesgos

- **Que la sonda escriba en cada llamada.** Un archivo de cero bytes cada cinco minutos no desgasta nada, pero conviene que sea de cero bytes y que se borre siempre. Si algún día molesta, la respuesta es cachear el resultado unos segundos, no dejar de comprobar.

- **`DriveInfo` sobre rutas raras.** En un montaje de red o un contenedor puede no informar bien. Si lanza, se trata como degradado y se registra: es más seguro avisar de más que dar por bueno un disco del que no se sabe nada.

- **Falso positivo al desplegar.** Durante el reinicio el directorio existe igual, así que no debería. Pero si el monitor avisa en cada despliegue, la gente deja de leer los avisos; conviene comprobarlo tras la primera puesta en producción.
