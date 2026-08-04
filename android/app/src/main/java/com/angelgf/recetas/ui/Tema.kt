package com.angelgf.recetas.ui

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

/**
 * Los mismos tokens que `tokens.css` en la web.
 *
 * Se copian en lugar de inventar una paleta nueva para que las dos superficies no
 * acaben pareciendo dos productos distintos. No se usa color dinámico de Material
 * You por lo mismo: el terracota es del producto, no del fondo de pantalla.
 */
private val Acento = Color(0xFFB4541F)
private val AcentoOscuro = Color(0xFF8C3F16)
private val Fondo = Color(0xFFFDFCFA)
private val Superficie = Color(0xFFFFFFFF)
private val Texto = Color(0xFF24201C)
private val Error = Color(0xFF9B2226)

private val Claro = lightColorScheme(
    primary = Acento,
    onPrimary = Color.White,
    secondary = AcentoOscuro,
    background = Fondo,
    onBackground = Texto,
    surface = Superficie,
    onSurface = Texto,
    error = Error
)

private val Oscuro = darkColorScheme(
    primary = Color(0xFFE08A55),
    onPrimary = Color(0xFF3A1A08),
    secondary = Acento,
    background = Color(0xFF17140F),
    onBackground = Color(0xFFF2EDE6),
    surface = Color(0xFF221E18),
    onSurface = Color(0xFFF2EDE6),
    error = Color(0xFFE79A9C)
)

@Composable
fun TemaDeRecetas(oscuro: Boolean = isSystemInDarkTheme(), contenido: @Composable () -> Unit) {
    MaterialTheme(colorScheme = if (oscuro) Oscuro else Claro, content = contenido)
}

/** Etiquetas del enumerado del dominio, que llega como texto. */
object Etiquetas {
    fun deTipo(tipo: String): String = when (tipo) {
        "Entrante" -> "Entrante"
        "PrimerPlato" -> "Primer plato"
        "PlatoPrincipal" -> "Plato principal"
        "Guarnicion" -> "Guarnición"
        "Postre" -> "Postre"
        "Bebida" -> "Bebida"
        "Salsa" -> "Salsa"
        else -> tipo
    }

    fun deUnidad(unidad: String, cantidad: Double?): String {
        val singular = cantidad == 1.0

        return when (unidad) {
            "AlGusto" -> "al gusto"
            "Unidad" -> if (singular) "unidad" else "unidades"
            "Gramo" -> "g"
            "Kilogramo" -> "kg"
            "Mililitro" -> "ml"
            "Litro" -> "l"
            "Cucharada" -> if (singular) "cucharada" else "cucharadas"
            "Cucharadita" -> if (singular) "cucharadita" else "cucharaditas"
            "Taza" -> if (singular) "taza" else "tazas"
            "Pizca" -> if (singular) "pizca" else "pizcas"
            "Diente" -> if (singular) "diente" else "dientes"
            "Rama" -> if (singular) "rama" else "ramas"
            "Hoja" -> if (singular) "hoja" else "hojas"
            else -> unidad
        }
    }

    /** Cantidad sin ceros de más: 1,5 en lugar de 1,500. */
    fun deCantidad(cantidad: Double?): String {
        if (cantidad == null) return ""

        return if (cantidad % 1.0 == 0.0) {
            cantidad.toLong().toString()
        } else {
            cantidad.toString().trimEnd('0').trimEnd('.').replace('.', ',')
        }
    }
}
