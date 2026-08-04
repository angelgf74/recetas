package com.angelgf.recetas.ui

import android.app.Activity
import android.view.WindowManager
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.ui.platform.LocalContext

/**
 * Impide que la pantalla se apague mientras el componente esté en pantalla.
 *
 * `mission.md` dice que la aplicación se usa "con las manos sucias", leyendo la
 * receta mientras se cocina. Que la pantalla se apague a los treinta segundos y
 * haya que desbloquear el teléfono con las manos llenas de harina rompe justo ese
 * caso de uso.
 *
 * El `DisposableEffect` es lo que garantiza que la marca se quita al salir: sin
 * él, la pantalla se quedaría encendida el resto de la sesión.
 */
@Composable
fun MantenerPantallaEncendida() {
    val contexto = LocalContext.current

    DisposableEffect(Unit) {
        val ventana = (contexto as? Activity)?.window

        ventana?.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)

        onDispose {
            ventana?.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        }
    }
}
