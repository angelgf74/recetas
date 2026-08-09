# 025 · Elegir la foto de portada — Plan

## Enfoque

`Receta.FotoDePortada` pasa de ser puramente derivada a tener una preferencia
opcional por delante: si el autor ha elegido una y esa foto sigue existiendo,
es esa; si no, sigue siendo la más antigua, exactamente como hasta ahora. No
cambia ningún consumidor de `FotoDePortada` —listados, búsqueda, favoritos—
porque todos leen esa propiedad, nunca la lista de fotos a pelo.

## Dominio

`Receta.FotoDePortadaElegidaId` — `Guid?`, `private set`. Nuevo método:

```csharp
public void ElegirFotoDePortada(Guid fotoId, DateTimeOffset ahora)
{
    if (!_fotos.Any(foto => foto.Id == fotoId))
    {
        throw new ArgumentException("Esa foto no es de esta receta.", nameof(fotoId));
    }

    FotoDePortadaElegidaId = fotoId;
    FechaDeModificacion = ahora;
}
```

`FotoDePortada` pasa a:

```csharp
public Foto? FotoDePortada =>
    (FotoDePortadaElegidaId is { } id ? _fotos.FirstOrDefault(f => f.Id == id) : null)
    ?? _fotos.OrderBy(f => f.FechaDeSubida).ThenBy(f => f.Id).FirstOrDefault();
```

`QuitarFoto` gana una línea: si la foto quitada era la elegida,
`FotoDePortadaElegidaId` vuelve a `null`. Sin esto quedaría apuntando a una
foto que ya no existe, y aunque `FotoDePortada` ya tolera eso con el `??`,
dejar la referencia colgada sería un dato incoherente sin necesidad.

## Infraestructura

Columna `foto_de_portada_elegida_id` en `recetas`, con **clave foránea a
`fotos.Id` y `ON DELETE SET NULL`**. No es estrictamente necesaria —el dominio
ya limpia la referencia en `QuitarFoto`, en la misma transacción que borra la
foto— pero es la red de seguridad si alguna vía futura borra una fila de
`fotos` sin pasar por el dominio. Se decide en la migración si EF Core forma
algún problema con la referencia cruzada entre `recetas` y `fotos` —cada una
referencia a la otra—; si lo diera, se cae a una columna sin restricción y el
dominio sigue siendo la única garantía.

## Aplicación

`GestionDeFotos.ElegirPortadaAsync(usuarioId, recetaId, fotoId)`:
busca la receta, exige `EsDe` (autoría, como subir y borrar, no `PuedeVerla`),
llama a `receta.ElegirFotoDePortada`, guarda. `ArgumentException` del dominio
se traduce a `ResultadoDeFoto.NoEncontrada`: la foto no es de esa receta, y
distinguirlo de "no existe" no aporta nada.

## API

`PUT /recetas/{id}/fotos/{fotoId}/portada` — autenticado, autoría. `204` si
va bien. Mismo razonamiento de verbo que en la 021: se fija un estado, no se
ejecuta un verbo que ocurre una vez.

`FotoRespuesta` gana `EsPortada` (`bool`), calculado comparando con
`receta.FotoDePortada?.Id`. Es lo único nuevo en el contrato: `ResumenDeReceta`
ya usa `FotoDePortadaId` derivado de `Receta.FotoDePortada`, así que listados,
búsqueda y favoritos recogen el cambio sin tocarlos.

## Web

El control vive en `VisorDeFotos`, junto al de borrar: "Hacer portada",
visible solo si es del autor y la foto que se está viendo no es ya la
portada. Mismo razonamiento que llevó el borrado al visor en la 022: es una
acción sobre una foto concreta, y se decide mirándola, no desde una tira de
miniaturas de 4,5 rem.

## Archivos afectados

- `Receta.cs`, `GestionDeFotos.cs`
- Migración, `ConfiguracionDeReceta.cs`
- `ContratosDeRecetas.cs` (`FotoRespuesta.EsPortada`), `RecetasEndpoints.cs` (nueva ruta, `ARespuesta`), `FotosEndpoints.cs` si la ruta vive ahí
- `ClienteDeApi.cs`, `VisorDeFotos.razor`, `FichaDeReceta.razor`
- Tests en dominio, aplicación e integración

## Riesgos

- **La referencia cruzada `recetas` ↔ `fotos`.** Si la migración o el modelo de
  EF se quejan, se documenta y se cae a la columna sin FK.
