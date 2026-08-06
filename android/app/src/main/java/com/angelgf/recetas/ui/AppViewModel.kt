package com.angelgf.recetas.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.angelgf.recetas.datos.ClienteDeApi
import com.angelgf.recetas.datos.PeticionDeReceta
import com.angelgf.recetas.datos.RespuestaDeImportacion
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
 * Navegación con estado y no con `navigation-compose`: siguen siendo pocas
 * pantallas y es una dependencia menos.
 */
sealed interface Pantalla {
    data object Sesion : Pantalla
    data object Alta : Pantalla
    data object RecuperarContrasena : Pantalla

    /** Paso 2 del alta o del restablecimiento, al que se llega por el enlace del correo. */
    data class ElegirContrasena(val token: String, val esAlta: Boolean) : Pantalla

    data object Recetario : Pantalla
    data object Busqueda : Pantalla
    data object Ajustes : Pantalla
    data class Ficha(val recetaId: String) : Pantalla

    /** Crear una receta, o editar una existente si trae identificador. */
    data class Formulario(val recetaId: String? = null) : Pantalla
}

data class EstadoDeLaApp(
    val pantalla: Pantalla = Pantalla.Sesion,
    val cargando: Boolean = false,
    val error: String? = null,
    val aviso: String? = null,
    val recetas: List<ResumenDeReceta> = emptyList(),
    val receta: RespuestaDeReceta? = null,
    val resultados: List<ResumenDeReceta>? = null,
    val hayMasResultados: Boolean = false,

    /** Raciones que representan las cantidades en pantalla. Lo dice la respuesta. */
    val racionesMostradas: Int? = null,

    /** Borrador de una importación, para precargar el formulario. */
    val borrador: PeticionDeReceta? = null,
    val origenDelBorrador: String? = null,

    /** Receta que se está editando, ya cargada. */
    val recetaAEditar: RespuestaDeReceta? = null,

    /**
     * Recetas ya denunciadas en esta sesión, para no volver a ofrecerlo.
     *
     * Solo en memoria: el servidor ya impide la denuncia repetida, así que esto
     * es cortesía con el usuario, no una regla. Al reabrir la aplicación se
     * vuelve a ofrecer, y denunciar otra vez no hace daño.
     */
    val denunciadas: Set<String> = emptySet()
)

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
                    _estado.update { it.copy(pantalla = Pantalla.Recetario, error = null, aviso = null) }
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

    // ------------------------------------------------------------ Cuenta

    fun irAlAlta() = _estado.update { it.copy(pantalla = Pantalla.Alta, error = null, aviso = null) }

    fun irARecuperarContrasena() =
        _estado.update { it.copy(pantalla = Pantalla.RecuperarContrasena, error = null, aviso = null) }

    fun volverAlLogin() =
        _estado.update { it.copy(pantalla = Pantalla.Sesion, error = null, aviso = null) }

    fun solicitarAlta(correo: String) = pedirYAvisar { api.solicitarAlta(correo.trim()) }

    fun solicitarContrasena(correo: String) = pedirYAvisar { api.solicitarContrasena(correo.trim()) }

    /**
     * Llega desde el enlace del correo. El token viaja en la dirección; la
     * contraseña se manda después, en el cuerpo de la petición.
     */
    fun abrirEnlaceDeCorreo(token: String, esAlta: Boolean) {
        _estado.value = EstadoDeLaApp(
            pantalla = Pantalla.ElegirContrasena(token, esAlta)
        )
    }

    fun elegirContrasena(token: String, contrasena: String, esAlta: Boolean) {
        lanzar {
            val resultado =
                if (esAlta) api.completarAlta(token, contrasena)
                else api.restablecerContrasena(token, contrasena)

            when (resultado) {
                is Resultado.Correcto -> _estado.value = EstadoDeLaApp(
                    pantalla = Pantalla.Sesion,
                    aviso = resultado.valor.ifBlank { "Listo. Ya puedes iniciar sesión." }
                )
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    private fun pedirYAvisar(bloque: suspend () -> Resultado<String>) {
        lanzar {
            when (val resultado = bloque()) {
                is Resultado.Correcto -> _estado.update {
                    it.copy(aviso = resultado.valor, error = null)
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    // ----------------------------------------------------------- Navegar

    fun irAlRecetario() {
        _estado.update {
            it.copy(
                pantalla = Pantalla.Recetario,
                error = null,
                aviso = null,
                receta = null,
                borrador = null,
                origenDelBorrador = null,
                recetaAEditar = null
            )
        }
        cargarRecetario()
    }

    fun irABuscar() =
        _estado.update { it.copy(pantalla = Pantalla.Busqueda, error = null, receta = null) }

    fun irAAjustes() =
        _estado.update { it.copy(pantalla = Pantalla.Ajustes, error = null, aviso = null) }

    fun irACrearReceta() =
        _estado.update {
            it.copy(
                pantalla = Pantalla.Formulario(),
                error = null,
                aviso = null,
                borrador = null,
                origenDelBorrador = null,
                recetaAEditar = null
            )
        }

    fun irAEditarReceta(receta: RespuestaDeReceta) =
        _estado.update {
            it.copy(
                pantalla = Pantalla.Formulario(receta.id),
                error = null,
                aviso = null,
                recetaAEditar = receta,
                borrador = null
            )
        }

    fun abrirReceta(id: String, raciones: Int? = null) {
        _estado.update {
            it.copy(
                pantalla = Pantalla.Ficha(id),
                receta = if (raciones == null) null else it.receta,
                error = null,
                aviso = null
            )
        }

        lanzar {
            when (val resultado = api.receta(id, raciones)) {
                is Resultado.Correcto -> _estado.update {
                    it.copy(receta = resultado.valor, racionesMostradas = resultado.valor.racionesMostradas)
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    /**
     * Ajusta las cantidades a otro número de comensales.
     *
     * Se parte siempre de la receta guardada, nunca de lo ya escalado: encadenar
     * redondeos acumularía error (ver feature 010).
     */
    fun ajustarRaciones(raciones: Int) {
        val id = (_estado.value.pantalla as? Pantalla.Ficha)?.recetaId ?: return

        if (raciones in 1..100) abrirReceta(id, raciones)
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

    // -------------------------------------------------------- Escritura

    fun guardarReceta(peticion: PeticionDeReceta, recetaId: String?) {
        lanzar {
            val resultado =
                if (recetaId == null) {
                    when (val creada = api.crearReceta(peticion)) {
                        is Resultado.Correcto -> {
                            abrirReceta(creada.valor.id)
                            return@lanzar
                        }
                        is Resultado.Fallo -> Resultado.Fallo(creada.mensaje)
                        Resultado.SesionCaducada -> Resultado.SesionCaducada
                    }
                } else {
                    api.actualizarReceta(recetaId, peticion)
                }

            when (resultado) {
                is Resultado.Correcto -> abrirReceta(recetaId!!)
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    fun borrarReceta(id: String) {
        lanzar {
            when (val resultado = api.borrarReceta(id)) {
                is Resultado.Correcto -> irAlRecetario()
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    fun cambiarVisibilidad(id: String, publicar: Boolean) {
        lanzar {
            val resultado = if (publicar) api.publicar(id) else api.despublicar(id)

            when (resultado) {
                is Resultado.Correcto -> {
                    _estado.update {
                        it.copy(
                            aviso = if (publicar) "Receta compartida: ya pueden verla otros usuarios."
                            else "Receta retirada: vuelve a ser solo tuya."
                        )
                    }
                    recargarFicha(id)
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    fun denunciar(recetaId: String, motivo: String, comentario: String?) {
        lanzar {
            when (val resultado = api.denunciar(recetaId, motivo, comentario)) {
                is Resultado.Correcto -> _estado.update {
                    it.copy(
                        aviso = "Gracias. Hemos recibido tu aviso y lo revisaremos.",
                        denunciadas = it.denunciadas + recetaId
                    )
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    fun subirFoto(recetaId: String, contenido: ByteArray) {
        lanzar {
            when (val resultado = api.subirFoto(recetaId, contenido)) {
                is Resultado.Correcto -> {
                    _estado.update { it.copy(aviso = "Foto añadida.") }
                    recargarFicha(recetaId)
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    fun borrarFoto(recetaId: String, fotoId: String) {
        lanzar {
            when (val resultado = api.borrarFoto(recetaId, fotoId)) {
                is Resultado.Correcto -> recargarFicha(recetaId)
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    private suspend fun recargarFicha(id: String) {
        when (val resultado = api.receta(id)) {
            is Resultado.Correcto -> _estado.update {
                it.copy(receta = resultado.valor, racionesMostradas = resultado.valor.racionesMostradas)
            }
            is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
            Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
        }
    }

    // --------------------------------------------------------- Importar

    fun importar(direccion: String) {
        lanzar {
            when (val resultado = api.importar(direccion.trim())) {
                is Resultado.Correcto -> _estado.update {
                    it.copy(borrador = aBorrador(resultado.valor), origenDelBorrador = resultado.valor.origen, error = null)
                }
                is Resultado.Fallo -> _estado.update { it.copy(error = resultado.mensaje) }
                Resultado.SesionCaducada -> volverAlInicioDeSesion(null)
            }
        }
    }

    private fun aBorrador(importada: RespuestaDeImportacion) = PeticionDeReceta(
        nombre = importada.nombre,

        // El tipo de plato no se adivina: lo elige el usuario en el formulario.
        tipoDePlato = "PlatoPrincipal",
        elaboracion = importada.elaboracion,
        ingredientes = importada.ingredientes,
        raciones = importada.raciones
    )

    // ------------------------------------------------------------ Fotos

    suspend fun miniatura(recetaId: String, fotoId: String): ByteArray? = api.miniatura(recetaId, fotoId)

    suspend fun foto(recetaId: String, fotoId: String): ByteArray? = api.foto(recetaId, fotoId)

    fun limpiarAviso() = _estado.update { it.copy(aviso = null, error = null) }

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
