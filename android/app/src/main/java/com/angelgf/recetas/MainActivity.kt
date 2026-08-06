package com.angelgf.recetas

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewmodel.compose.viewModel
import com.angelgf.recetas.datos.ClienteDeApi
import com.angelgf.recetas.datos.SesionLocal
import com.angelgf.recetas.ui.AppViewModel
import com.angelgf.recetas.ui.EstadoDeLaApp
import com.angelgf.recetas.ui.Pantalla
import com.angelgf.recetas.ui.PantallaDeAjustes
import com.angelgf.recetas.ui.PantallaDeBusqueda
import com.angelgf.recetas.ui.PantallaDeCorreo
import com.angelgf.recetas.ui.PantallaDeElegirContrasena
import com.angelgf.recetas.ui.PantallaDeFicha
import com.angelgf.recetas.ui.PantallaDeFormulario
import com.angelgf.recetas.ui.PantallaDeRecetario
import com.angelgf.recetas.ui.PantallaDeSesion
import com.angelgf.recetas.ui.PrepararAnuncios
import com.angelgf.recetas.ui.TemaDeRecetas

class MainActivity : ComponentActivity() {

    /** Enlace con el que se abrió la aplicación, si vino de uno. */
    private var enlaceDeEntrada by mutableStateOf<Uri?>(null)

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        enlaceDeEntrada = intent?.data

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
                    val modelo: AppViewModel = viewModel(factory = FabricaDeAppViewModel(api))
                    Aplicacion(modelo, enlaceDeEntrada) { enlaceDeEntrada = null }
                }
            }
        }
    }

    /** La aplicación ya estaba abierta y llega otro enlace. */
    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        enlaceDeEntrada = intent.data
    }
}

@Composable
private fun Aplicacion(modelo: AppViewModel, enlace: Uri?, alConsumirEnlace: () -> Unit) {
    val estado: EstadoDeLaApp by modelo.estado.collectAsState()
    val contexto = LocalContext.current

    var anunciosListos by rememberSaveable { mutableStateOf(false) }
    PrepararAnuncios(esDepuracion = BuildConfig.DEBUG) { anunciosListos = true }

    // El enlace del correo trae el token del alta o del restablecimiento.
    LaunchedEffect(enlace) {
        val direccion = enlace ?: return@LaunchedEffect
        val token = direccion.getQueryParameter("token")

        if (!token.isNullOrBlank()) {
            modelo.abrirEnlaceDeCorreo(token, esAlta = direccion.path?.contains("registro") == true)
        }

        alConsumirEnlace()
    }

    // Selector de archivos del sistema: no exige permiso de almacenamiento, y es
    // el camino que funciona igual en todos los fabricantes.
    var recetaDeLaFoto by remember { mutableStateOf<String?>(null) }

    val elegirFoto = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
        val id = recetaDeLaFoto
        if (uri != null && id != null) {
            // Se lee el flujo directamente, sin copiar antes a un archivo temporal.
            // El tope de tamaño lo pone el servidor.
            val bytes = contexto.contentResolver.openInputStream(uri)?.use { it.readBytes() }
            if (bytes != null) modelo.subirFoto(id, bytes)
        }
        recetaDeLaFoto = null
    }

    Scaffold { relleno ->
        Surface(modifier = Modifier.padding(relleno)) {
            when (val pantalla = estado.pantalla) {
                Pantalla.Sesion -> PantallaDeSesion(estado, modelo)

                Pantalla.Alta -> PantallaDeCorreo(
                    estado, modelo,
                    titulo = "Crear cuenta",
                    explicacion = "Indica tu correo y te enviaremos un enlace para elegir tu contraseña.",
                    textoDelBoton = "Enviarme el enlace"
                ) { modelo.solicitarAlta(it) }

                Pantalla.RecuperarContrasena -> PantallaDeCorreo(
                    estado, modelo,
                    titulo = "Recuperar contraseña",
                    explicacion = "Indica tu correo y te enviaremos un enlace para elegir una contraseña nueva.",
                    textoDelBoton = "Enviarme el enlace"
                ) { modelo.solicitarContrasena(it) }

                is Pantalla.ElegirContrasena ->
                    PantallaDeElegirContrasena(estado, modelo, pantalla.token, pantalla.esAlta)

                Pantalla.Recetario -> PantallaDeRecetario(estado, modelo, anunciosListos)
                Pantalla.Busqueda -> PantallaDeBusqueda(estado, modelo, anunciosListos)
                Pantalla.Ajustes -> PantallaDeAjustes(modelo)

                is Pantalla.Formulario -> PantallaDeFormulario(estado, modelo, pantalla.recetaId)

                // La ficha NO recibe el parámetro de anuncios: es la pantalla que
                // se lee cocinando y ahí no va publicidad. Ver mission.md.
                is Pantalla.Ficha -> PantallaDeFicha(estado, modelo) { id ->
                    recetaDeLaFoto = id
                    elegirFoto.launch("image/*")
                }
            }
        }
    }
}

private class FabricaDeAppViewModel(private val api: ClienteDeApi) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T = AppViewModel(api) as T
}
