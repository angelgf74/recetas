package com.angelgf.recetas

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewmodel.compose.viewModel
import com.angelgf.recetas.datos.ClienteDeApi
import com.angelgf.recetas.datos.SesionLocal
import com.angelgf.recetas.ui.AppViewModel
import com.angelgf.recetas.ui.EstadoDeLaApp
import com.angelgf.recetas.ui.Pantalla
import com.angelgf.recetas.ui.PantallaDeBusqueda
import com.angelgf.recetas.ui.PantallaDeFicha
import com.angelgf.recetas.ui.PantallaDeRecetario
import com.angelgf.recetas.ui.PantallaDeSesion
import com.angelgf.recetas.ui.PrepararAnuncios
import com.angelgf.recetas.ui.TemaDeRecetas

class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        // La dirección de la API la fija el tipo de compilación: depuración apunta
        // al equipo de desarrollo, publicación a producción. Ver build.gradle.kts.
        val api = ClienteDeApi(
            baseDeLaApi = BuildConfig.BASE_DE_LA_API,
            sesion = SesionLocal(applicationContext)
        )

        setContent {
            TemaDeRecetas {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    // La fábrica es lo que hace que el ViewModel sobreviva al giro
                    // de pantalla con el mismo cliente dentro.
                    val modelo: AppViewModel = viewModel(factory = FabricaDeAppViewModel(api))
                    Aplicacion(modelo)
                }
            }
        }
    }
}

@Composable
private fun Aplicacion(modelo: AppViewModel) {
    val estado: EstadoDeLaApp by modelo.estado.collectAsState()

    // Estado observable propio: `Anuncios` no lo es, así que sin esto Compose no
    // se enteraría de que el consentimiento ha terminado.
    var anunciosListos by rememberSaveable { mutableStateOf(false) }

    PrepararAnuncios(esDepuracion = BuildConfig.DEBUG) { anunciosListos = true }

    Scaffold { relleno ->
        Surface(modifier = Modifier.padding(relleno)) {
            when (estado.pantalla) {
                Pantalla.Sesion -> PantallaDeSesion(estado, modelo)
                Pantalla.Recetario -> PantallaDeRecetario(estado, modelo, anunciosListos)
                Pantalla.Busqueda -> PantallaDeBusqueda(estado, modelo, anunciosListos)

                // La ficha NO recibe el parámetro: es la pantalla que se lee
                // cocinando y ahí no va publicidad. Ver mission.md.
                is Pantalla.Ficha -> PantallaDeFicha(estado, modelo)
            }
        }
    }
}

private class FabricaDeAppViewModel(private val api: ClienteDeApi) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T = AppViewModel(api) as T
}
