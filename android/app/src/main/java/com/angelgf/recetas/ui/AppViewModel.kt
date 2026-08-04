package com.angelgf.recetas.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.angelgf.recetas.datos.ClienteDeApi
import com.angelgf.recetas.datos.Resultado
import com.angelgf.recetas.datos.RespuestaDeReceta
import com.angelgf.recetas.datos.ResumenDeReceta
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

/**
 * Dónde está el usuario.
 *
 * Navegación con estado y no con `navigation-compose`: son cuatro pantallas y una
 * dependencia menos que descargar. Si la aplicación crece hasta necesitar enlaces
 * profundos —que hará falta para el enlace del correo del alta—, entonces sí.
 */
sealed interface Pantalla {
    data object Sesion : Pantalla
    data object Recetario : Pantalla
    data object Busqueda : Pantalla
    data class Ficha(val recetaId: String) : Pantalla
}

data class EstadoDeLaApp(
    val pantalla: Pantalla = Pantalla.Sesion,
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null,
    val recetas: List<ResumenDeReceta> = emptyList(),
    val receta: RespuestaDeReceta? = null,
    val resultados: List<ResumenDeReceta>? = null,
    val hayMasResultados: Boolean = false
)

/**
 * Estado de toda la aplicación.
 *
 * Vive en un [ViewModel] y no en la actividad porque un `ViewModel` sobrevive al
 * giro de pantalla. Es el criterio de aceptación "sobrevive a girar sin perder lo
 * cargado", y el fallo clásico de una primera aplicación Android.
 */
class AppViewModel(private val api: ClienteDeApi) : ViewModel() {

    private val _estado = MutableStateFlow(
        EstadoDeLaApp(pantalla = if (api.haySesion()) Pantalla.Recetario else Pantalla.Sesion)
    )

    val estado: StateFlow<EstadoDeLaApp> = _estado.asStateFlow()

    init {
        if (api.haySesion()) cargarRecetario()
    }

    // ------------------------------------------------------------ Sesión

    fun iniciarSesion(correo: String, contrasena: String) {
        lanzar {
            when (val resultado = api.iniciarSesion(correo.trim(), contrasena)) {
                is Resultado.Correcto -> {
                    _estado.update { it.copy(pantalla = Pantalla.Recetario, error = null) }
                    cargarRecetario()
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    fun cerrarSesion() {
        api.cerrarSesion()
        _estado.value = EstadoDeLaApp(pantalla = Pantalla.Sesion)
    }

    private fun volverAlInicioDeSesion(aviso: String?) {
        _estado.value = EstadoDeLaApp(
            pantalla = Pantalla.Sesion,
            aviso = aviso ?: "Tu sesión ha caducado. Vuelve a entrar."
        )
    }

    // ----------------------------------------------------------- Navegar

    fun irAlRecetario() {
        _estado.update { it.copy(pantalla = Pantalla.Recetario, error = null, receta = null) }
        cargarRecetario()
    }

    fun irABuscar() {
        _estado.update { it.copy(pantalla = Pantalla.Busqueda, error = null, receta = null) }
    }

    fun abrirReceta(id: String) {
        // Se limpia la receta anterior antes de pedir la nueva: si no, se vería un
        // instante la anterior con el nombre de la que se acaba de tocar.
        _estado.update { it.copy(pantalla = Pantalla.Ficha(id), receta = null, error = null) }

        lanzar {
            when (val resultado = api.receta(id)) {
                is Resultado.Correcto -> _estado.update { it.copy(receta = resultado.valor) }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    // ------------------------------------------------------------ Datos

    fun cargarRecetario() {
        lanzar {
            when (val resultado = api.misRecetas()) {
                is Resultado.Correcto -> _estado.update { it.copy(recetas = resultado.valor, error = null) }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    fun buscar(nombre: String, ingredientes: String) {
        val lista = ingredientes.split(',').map { it.trim() }.filter { it.isNotEmpty() }

        lanzar {
            when (val resultado = api.buscar(nombre, lista)) {
                is Resultado.Correcto -> _estado.update {
                    it.copy(
                        resultados = resultado.valor.resultados,
                        hayMasResultados = resultado.valor.hayMas,
                        error = null
                    )
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    suspend fun miniatura(recetaId: String, fotoId: String): ByteArray? = api.miniatura(recetaId, fotoId)

    suspend fun foto(recetaId: String, fotoId: String): ByteArray? = api.foto(recetaId, fotoId)

    private fun lanzar(bloque: suspend () -> Unit) {
        viewModelScope.launch {
            _estado.update { it.copy(cargando = true) }
            try {
                bloque()
            } finally {
                _estado.update { it.copy(cargando = false) }
            }
        }
    }
}
