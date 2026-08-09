# 025 · Elegir la foto de portada

**Estado:** hecha

## Qué hace

El autor de una receta puede designar cuál de sus fotos es la portada —la que
se ve en el recetario, en la búsqueda y en los favoritos—, en vez de que sea
siempre la primera que subió.

## Por qué

Desde la 009, `Receta.FotoDePortada` es "la más antigua, o ninguna". Es una
respuesta razonable por defecto, pero no siempre la correcta: la primera foto
que alguien sube suele ser una prueba, no la mejor. Sin forma de cambiarla, la
única salida era borrar fotos y volver a subirlas en el orden que se quería,
lo que además cambiaba sus fechas de subida.

## Criterios de aceptación

- [x] El autor puede elegir cualquiera de las fotos de su receta como portada.
- [x] Elegirla vale para el recetario, la búsqueda y los favoritos: en todos los sitios donde ya se mostraba una portada (heredan de `FotoDePortada`, no se tocaron).
- [x] Sin elección explícita, sigue siendo la más antigua — el comportamiento de la 009 no cambia para quien no toca nada.
- [x] Si se borra la foto elegida como portada, la portada vuelve a ser la derivada (la más antigua de las que queden), no queda un hueco.
- [x] Elegir la misma foto que ya es portada no es un error.
- [x] Quien no es el autor no puede elegir portada, ni en su propia foto ajena visible.
- [x] La ficha dice cuál de las fotos es la portada actual (insignia en la miniatura, y el botón "Hacer portada" solo aparece si no lo es ya).

**No se ha visto en un navegador.** El botón "Hacer portada" del visor y la
insignia de la miniatura están escritos y compilan, sin test de bUnit ni
comprobación en pantalla.

## Fuera de alcance

- **Reordenar todas las fotos.** Solo se elige una portada, no un orden completo de la galería.
- **Portada distinta por contexto** (una para el recetario, otra para compartir en redes). Una sola portada, para todo.
