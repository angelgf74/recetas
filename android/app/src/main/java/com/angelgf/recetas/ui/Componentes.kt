package com.angelgf.recetas.ui

import android.graphics.BitmapFactory
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

/**
 * Pinta una imagen que solo se sirve con autenticación.
 *
 * No se usa una biblioteca de carga de imágenes apuntada a la URL: el endpoint
 * exige la cabecera `Authorization` y una carga normal no la manda. Se piden los
 * bytes con el cliente autenticado y se decodifican aquí.
 */
@Composable
fun ImagenDeLaApi(
    cargar: suspend () -> ByteArray?,
    clave: String,
    modifier: Modifier = Modifier,
    esquinas: Dp = 8.dp
) {
    var bytes by remember(clave) { mutableStateOf<ByteArray?>(null) }
    var fallo by remember(clave) { mutableStateOf(false) }

    LaunchedEffect(clave) {
        val descargados = cargar()
        bytes = descargados
        fallo = descargados == null
    }

    val actuales = bytes
    val mapa = remember(actuales) {
        actuales?.let {
            runCatching { BitmapFactory.decodeByteArray(it, 0, it.size) }.getOrNull()
        }
    }

    Box(
        modifier = modifier.clip(RoundedCornerShape(esquinas)),
        contentAlignment = Alignment.Center
    ) {
        when {
            mapa != null -> Image(
                bitmap = mapa.asImageBitmap(),
                contentDescription = "Foto de la receta",
                contentScale = ContentScale.Crop,
                modifier = Modifier.fillMaxWidth()
            )

            // El hueco se pinta igual aunque la imagen falte: si no, las tarjetas
            // de al lado quedarían desalineadas.
            else -> Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(MaterialTheme.colorScheme.surfaceVariant),
                contentAlignment = Alignment.Center
            ) {
                if (fallo) {
                    Text(
                        text = "🍲",
                        style = MaterialTheme.typography.titleLarge,
                        modifier = Modifier.padding(4.dp)
                    )
                }
            }
        }
    }
}

/** Miniatura cuadrada de listado. */
@Composable
fun MiniaturaDeReceta(
    cargar: suspend () -> ByteArray?,
    clave: String,
    lado: Dp = 64.dp
) {
    ImagenDeLaApi(cargar = cargar, clave = clave, modifier = Modifier.size(lado))
}
