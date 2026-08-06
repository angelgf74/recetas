package com.angelgf.recetas.ui

import android.app.Activity
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.viewinterop.AndroidView
import com.angelgf.recetas.datos.Anuncios
import com.google.android.gms.ads.AdRequest
import com.google.android.gms.ads.AdSize
import com.google.android.gms.ads.AdView

/**
 * Banner de AdMob al pie de una pantalla.
 *
 * **Dónde NO se pone: en la ficha de receta.** Es la pantalla que se lee
 * cocinando y con las manos ocupadas, y `mission.md` dice que un anuncio que
 * estorbe ahí se quita. Por lo mismo no hay intersticiales en ninguna parte.
 *
 * Si el consentimiento todavía no permite pedir anuncios, este componible **no
 * ocupa nada**: la lista llega hasta abajo. Reservar el hueco dejaría una franja
 * gris permanente en cuanto fallara la red o el usuario rechazara la publicidad.
 */
@Composable
fun BannerDeAnuncios(idDelBloque: String, habilitado: Boolean, modifier: Modifier = Modifier) {
    // `habilitado` llega como parámetro y no se consulta a `Anuncios` directamente:
    // ese objeto no es estado observable, así que Compose no se enteraría de que
    // el consentimiento ha terminado y el banner no aparecería hasta el siguiente
    // repintado por otro motivo.
    if (!habilitado) {
        return
    }

    val contexto = LocalContext.current
    val anchoEnDp = LocalConfiguration.current.screenWidthDp

    AndroidView(
        modifier = modifier.fillMaxWidth(),
        factory = { ctx ->
            AdView(ctx).apply {
                adUnitId = idDelBloque

                // Adaptable en línea, no AdSize.BANNER.
                //
                // El fijo son 320x50 en cualquier pantalla: en un móvil moderno se
                // ve pequeño y desaprovecha el ancho. El adaptable calcula la
                // altura a partir del ancho real del dispositivo.
                //
                // Se usa el adaptable EN LÍNEA y no el anclado porque el segundo
                // está obsoleto en el SDK.
                setAdSize(tamanoAdaptable(anchoEnDp))

                loadAd(AdRequest.Builder().build())
            }
        }
    )

    // Un AdView vivo después de salir de la pantalla sigue refrescando y pidiendo
    // anuncios: fuga de memoria e impresiones que nadie llega a ver.
    DisposableEffect(idDelBloque) {
        onDispose { }
    }
}

/**
 * Altura máxima del banner.
 *
 * Se acota a propósito: sin tope, un adaptable en línea puede pedir hasta el alto
 * de la pantalla, y en el pie de una lista eso taparía el contenido. Con este
 * valor ocupa una franja discreta.
 */
private const val ALTURA_MAXIMA_EN_DP = 60

private fun tamanoAdaptable(anchoEnDp: Int): AdSize =
    AdSize.getInlineAdaptiveBannerAdSize(anchoEnDp, ALTURA_MAXIMA_EN_DP)

/**
 * Arranca el consentimiento y el SDK una sola vez, cuando la interfaz ya está en
 * pantalla.
 *
 * Va aquí y no en el arranque de la aplicación para que abrir Recetas no dependa
 * de que el SDK de anuncios responda.
 */
@Composable
fun PrepararAnuncios(esDepuracion: Boolean, alEstarListo: () -> Unit) {
    val contexto = LocalContext.current

    DisposableEffect(Unit) {
        val actividad = contexto as? Activity

        if (actividad != null) {
            // Si el SDK ya arrancó, hay que avisar igualmente. `Anuncios.listos`
            // vive en el proceso y el estado del componible en la actividad, así
            // que recrear la actividad sin matar el proceso —rotar la pantalla,
            // salir con «atrás» y volver a entrar— los separa: el SDK sigue
            // listo, pero la pantalla cree que no y no pinta el banner nunca más.
            if (Anuncios.listos) {
                alEstarListo()
            } else {
                Anuncios.preparar(actividad, esDepuracion, alEstarListo)
            }
        }

        onDispose { }
    }
}
