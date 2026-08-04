package com.angelgf.recetas.datos

import android.app.Activity
import android.util.Log
import com.google.android.gms.ads.MobileAds
import com.google.android.gms.ads.RequestConfiguration
import com.google.android.ump.ConsentDebugSettings
import com.google.android.ump.ConsentInformation
import com.google.android.ump.ConsentRequestParameters
import com.google.android.ump.UserMessagingPlatform
import java.util.concurrent.atomic.AtomicBoolean

/**
 * Arranque de la publicidad: consentimiento primero, anuncios después.
 *
 * El orden no es cosmético. Servir anuncios personalizados en el Espacio Económico
 * Europeo sin recabar consentimiento incumple la normativa, así que **primero** se
 * pregunta a la plataforma de mensajes de Google (UMP) y **solo** cuando dice que
 * se pueden pedir anuncios se inicializa el SDK.
 *
 * Si algo falla —sin red, servicio caído—, la aplicación sigue funcionando sin
 * anuncios. Un recetario que no se abre porque no ha podido cargar publicidad
 * sería exactamente lo que `mission.md` prohíbe.
 */
object Anuncios {

    private const val ETIQUETA = "Anuncios"

    /** El SDK solo se inicializa una vez por proceso. */
    private val inicializado = AtomicBoolean(false)

    @Volatile
    private var sePuedenPedir = false

    /** Si ya se puede pedir un anuncio. Los componibles lo consultan antes de pintar. */
    val listos: Boolean get() = sePuedenPedir

    /**
     * Pide el consentimiento si hace falta y arranca el SDK.
     *
     * @param alEstarListo se invoca cuando ya se pueden cargar anuncios; no se
     *   invoca si el usuario los rechaza o si algo falla.
     */
    fun preparar(actividad: Activity, esDepuracion: Boolean, alEstarListo: () -> Unit) {
        val informacion = UserMessagingPlatform.getConsentInformation(actividad)

        val parametros = ConsentRequestParameters.Builder()
            .setTagForUnderAgeOfConsent(false)
            .apply {
                if (esDepuracion) {
                    // En depuración se fuerza el comportamiento del Espacio
                    // Económico Europeo: si no, desde España se vería el
                    // formulario pero desde otro sitio no, y el camino que hay
                    // que probar es precisamente el que más restricciones tiene.
                    setConsentDebugSettings(
                        ConsentDebugSettings.Builder(actividad)
                            .setDebugGeography(ConsentDebugSettings.DebugGeography.DEBUG_GEOGRAPHY_EEA)
                            .build()
                    )
                }
            }
            .build()

        Log.i(ETIQUETA, "Pidiendo estado de consentimiento…")

        informacion.requestConsentInfoUpdate(
            actividad,
            parametros,
            {
                Log.i(ETIQUETA, "Consentimiento consultado. ¿Se pueden pedir anuncios? ${informacion.canRequestAds()}")

                // Muestra el formulario si procede. Si no procede, sigue de largo.
                UserMessagingPlatform.loadAndShowConsentFormIfRequired(actividad) { error ->
                    if (error != null) {
                        Log.i(ETIQUETA, "Formulario de consentimiento: ${error.message}")
                    }

                    continuarSiSePuede(actividad, informacion, esDepuracion, alEstarListo)
                }
            },
            { error ->
                Log.i(ETIQUETA, "No se pudo obtener el consentimiento: ${error.message}")

                // Que la consulta falle NO significa que haya que renunciar a los
                // anuncios. Un fallo de red, o que falte configurar el formulario
                // en la consola, es distinto de que el usuario haya dicho que no.
                //
                // UMP conserva el estado de la última vez y sigue sabiendo
                // responder: si dice que se pueden pedir anuncios, se piden. Si
                // dice que no, no se piden. La decisión sigue siendo suya, no
                // nuestra, y así un error de configuración no deja la aplicación
                // sin publicidad para siempre.
                continuarSiSePuede(actividad, informacion, esDepuracion, alEstarListo)
            }
        )
    }

    private fun continuarSiSePuede(
        actividad: Activity,
        informacion: ConsentInformation,
        esDepuracion: Boolean,
        alEstarListo: () -> Unit
    ) {
        if (!informacion.canRequestAds()) {
            Log.i(ETIQUETA, "El consentimiento no permite pedir anuncios.")
            return
        }

        if (inicializado.compareAndSet(false, true)) {
            if (esDepuracion) {
                // Segunda defensa, además de usar bloques de prueba: marca este
                // aparato como dispositivo de prueba, de modo que ni siquiera un
                // bloque real serviría un anuncio facturable desde aquí.
                //
                // El identificador del emulador es constante y público.
                MobileAds.setRequestConfiguration(
                    RequestConfiguration.Builder()
                        .setTestDeviceIds(listOf(DISPOSITIVO_DE_PRUEBA_EMULADOR))
                        .build()
                )
            }

            MobileAds.initialize(actividad) { estado ->
                Log.i(ETIQUETA, "SDK de anuncios inicializado: ${estado.adapterStatusMap.keys}")
                sePuedenPedir = true
                alEstarListo()
            }
        } else {
            sePuedenPedir = true
            alEstarListo()
        }
    }

    /** Identificador que el SDK asigna a cualquier emulador de Android. */
    private const val DISPOSITIVO_DE_PRUEBA_EMULADOR = "33BE2250B43518CCDA7DE426D04EE231"
}
