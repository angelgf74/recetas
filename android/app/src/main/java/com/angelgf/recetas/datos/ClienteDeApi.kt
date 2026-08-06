package com.angelgf.recetas.datos

import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.engine.HttpClientEngine
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.client.request.delete
import io.ktor.client.request.get
import io.ktor.client.request.header
import io.ktor.client.request.parameter
import io.ktor.client.request.post
import io.ktor.client.request.put
import io.ktor.client.request.setBody
import io.ktor.client.statement.HttpResponse
import io.ktor.client.statement.readRawBytes
import io.ktor.http.ContentType
import io.ktor.http.HttpStatusCode
import io.ktor.http.contentType
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json

/**
 * Resultado de una llamada. Se prefiere esto a lanzar excepciones porque cada
 * pantalla tiene que pintar el error, no tragárselo.
 */
sealed interface Resultado<out T> {
    data class Correcto<T>(val valor: T) : Resultado<T>
    data class Fallo(val mensaje: String) : Resultado<Nothing>

    /** El token ya no vale. Lo trata la aplicación entera igual: volver al login. */
    data object SesionCaducada : Resultado<Nothing>
}

/**
 * Cliente de la API.
 *
 * Dos cosas viven aquí y en ningún otro sitio, igual que en
 * `ManejadorDeAutenticacion` de la web y por el mismo motivo —que ninguna pantalla
 * pueda olvidarse—:
 *
 * - La cabecera `Authorization`.
 * - El `401`, que se convierte en [Resultado.SesionCaducada].
 */
class ClienteDeApi(
    private val baseDeLaApi: String,
    private val sesion: AlmacenDeSesion,
    motor: HttpClientEngine? = null
) {
    private val json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
    }

    private val http: HttpClient = crearCliente(motor)

    private fun crearCliente(motor: HttpClientEngine?): HttpClient {
        val configuracion: io.ktor.client.HttpClientConfig<*>.() -> Unit = {
            install(ContentNegotiation) { json(this@ClienteDeApi.json) }

            defaultRequest {
                url(baseDeLaApi)
                sesion.leer()?.let { header("Authorization", "Bearer $it") }
            }
        }

        return if (motor is HttpClientEngine) HttpClient(motor, configuracion) else HttpClient(configuracion)
    }

    // ------------------------------------------------------------ Sesión

    suspend fun iniciarSesion(correo: String, contrasena: String): Resultado<Unit> = envolver {
        val respuesta = http.post("sesiones") {
            contentType(ContentType.Application.Json)
            setBody(PeticionDeInicioDeSesion(correo, contrasena))
        }

        if (!respuesta.status.isSuccess()) {
            // El inicio de sesión es el único sitio donde un 401 NO significa
            // "sesión caducada": significa que las credenciales no valen.
            return@envolver Resultado.Fallo(
                if (respuesta.status == HttpStatusCode.Unauthorized) {
                    "Correo o contraseña incorrectos."
                } else {
                    mensajeDeError(respuesta)
                }
            )
        }

        sesion.guardar(respuesta.body<RespuestaDeInicioDeSesion>().token)
        Resultado.Correcto(Unit)
    }

    fun cerrarSesion() = sesion.borrar()

    fun haySesion(): Boolean = sesion.leer() != null

    suspend fun yo(): Resultado<RespuestaDeIdentidad> = leer("yo")

    // ------------------------------------------------------------ Recetas

    suspend fun misRecetas(): Resultado<List<ResumenDeReceta>> = leer("recetas")

    /**
     * @param raciones comensales a los que ajustar las cantidades. El cálculo y el
     *   redondeo los hace el servidor: son reglas de negocio (feature 010) y no se
     *   reimplementan aquí.
     */
    suspend fun receta(id: String, raciones: Int? = null): Resultado<RespuestaDeReceta> =
        envolver {
            val respuesta = http.get("recetas/$id") {
                raciones?.let { parameter("raciones", it) }
            }

            traducir(respuesta)
        }

    suspend fun crearReceta(peticion: PeticionDeReceta): Resultado<RespuestaDeReceta> =
        envolver {
            val respuesta = http.post("recetas") {
                contentType(ContentType.Application.Json)
                setBody(peticion)
            }

            traducir(respuesta)
        }

    suspend fun actualizarReceta(id: String, peticion: PeticionDeReceta): Resultado<Unit> =
        envolver {
            val respuesta = http.put("recetas/$id") {
                contentType(ContentType.Application.Json)
                setBody(peticion)
            }

            sinContenido(respuesta)
        }

    suspend fun borrarReceta(id: String): Resultado<Unit> =
        envolver { sinContenido(http.delete("recetas/$id")) }

    // ------------------------------------------------------- Publicación

    suspend fun publicar(id: String): Resultado<Unit> =
        envolver { sinContenido(http.post("recetas/$id/publicacion")) }

    suspend fun despublicar(id: String): Resultado<Unit> =
        envolver { sinContenido(http.delete("recetas/$id/publicacion")) }

    // -------------------------------------------------------- Denuncias

    suspend fun denunciar(recetaId: String, motivo: String, comentario: String?): Resultado<Unit> =
        envolver {
            val respuesta = http.post("recetas/$recetaId/denuncias") {
                contentType(ContentType.Application.Json)
                setBody(PeticionDeDenuncia(motivo, comentario?.takeIf { it.isNotBlank() }))
            }

            sinContenido(respuesta)
        }

    // ------------------------------------------------------------- Fotos

    suspend fun subirFoto(recetaId: String, contenido: ByteArray): Resultado<Unit> =
        envolver {
            val respuesta = http.post("recetas/$recetaId/fotos") {
                setBody(contenido)
            }

            traducirVacio(respuesta)
        }

    suspend fun borrarFoto(recetaId: String, fotoId: String): Resultado<Unit> =
        envolver { sinContenido(http.delete("recetas/$recetaId/fotos/$fotoId")) }

    // --------------------------------------------------------- Importar

    suspend fun importar(direccion: String): Resultado<RespuestaDeImportacion> =
        envolver {
            val respuesta = http.post("recetas/importaciones") {
                contentType(ContentType.Application.Json)
                setBody(PeticionDeImportacion(direccion))
            }

            traducir(respuesta)
        }

    // ------------------------------------------------------------ Cuenta

    suspend fun solicitarAlta(correo: String): Resultado<String> =
        mensajeDe("registro/solicitudes", PeticionDeSolicitudDeRegistro(correo))

    suspend fun completarAlta(token: String, contrasena: String): Resultado<String> =
        mensajeDe("registro/completar", PeticionDeCompletarRegistro(token, contrasena))

    suspend fun solicitarContrasena(correo: String): Resultado<String> =
        mensajeDe("contrasena/solicitudes", PeticionDeSolicitudDeContrasena(correo))

    suspend fun restablecerContrasena(token: String, contrasena: String): Resultado<String> =
        mensajeDe("contrasena/restablecer", PeticionDeRestablecerContrasena(token, contrasena))

    /** Endpoints que responden 200 con un mensaje pensado para enseñárselo al usuario. */
    private suspend inline fun <reified T> mensajeDe(ruta: String, cuerpo: T): Resultado<String> =
        envolver {
            val respuesta = http.post(ruta) {
                contentType(ContentType.Application.Json)
                setBody(cuerpo)
            }

            if (respuesta.status.isSuccess()) {
                Resultado.Correcto(respuesta.body<RespuestaDeError>().mensaje.orEmpty())
            } else {
                Resultado.Fallo(mensajeDeError(respuesta))
            }
        }

    suspend fun buscar(nombre: String, ingredientes: List<String>): Resultado<RespuestaDeBusqueda> =
        envolver {
            val respuesta = http.get("recetas/busqueda") {
                if (nombre.isNotBlank()) parameter("nombre", nombre)

                // El parámetro se repite, no se separa por comas: es como lo
                // espera la API desde la feature 006.
                ingredientes.filter { it.isNotBlank() }.forEach { parameter("ingrediente", it) }
            }

            traducir(respuesta)
        }

    /**
     * Descarga una imagen como bytes.
     *
     * No sirve una biblioteca de carga de imágenes apuntada a la URL: el endpoint
     * de fotos exige la cabecera de autorización, y una etiqueta de imagen normal
     * no la manda. Es el mismo problema que la web resuelve con URLs `data:`.
     */
    suspend fun miniatura(recetaId: String, fotoId: String): ByteArray? =
        imagen("recetas/$recetaId/fotos/$fotoId/miniatura")

    suspend fun foto(recetaId: String, fotoId: String): ByteArray? =
        imagen("recetas/$recetaId/fotos/$fotoId")

    private suspend fun imagen(ruta: String): ByteArray? =
        try {
            val respuesta = http.get(ruta)
            if (respuesta.status.isSuccess()) respuesta.readRawBytes() else null
        } catch (excepcion: Exception) {
            // Una foto que no carga deja un hueco; no debe tumbar la pantalla.
            null
        }

    // --------------------------------------------------------- Utilidades

    private suspend inline fun <reified T> leer(ruta: String): Resultado<T> =
        envolver { traducir(http.get(ruta)) }

    /** Para respuestas 204, que no traen cuerpo que interpretar. */
    private suspend fun sinContenido(respuesta: HttpResponse): Resultado<Unit> =
        traducirVacio(respuesta)

    private suspend fun traducirVacio(respuesta: HttpResponse): Resultado<Unit> =
        when {
            respuesta.status.isSuccess() -> Resultado.Correcto(Unit)
            respuesta.status == HttpStatusCode.Unauthorized -> {
                sesion.borrar()
                Resultado.SesionCaducada
            }
            else -> Resultado.Fallo(mensajeDeError(respuesta))
        }

    private suspend inline fun <reified T> traducir(respuesta: HttpResponse): Resultado<T> =
        when {
            respuesta.status.isSuccess() -> Resultado.Correcto(respuesta.body<T>())
            respuesta.status == HttpStatusCode.Unauthorized -> {
                // El token ya no vale: se borra aquí para que no quede una sesión
                // muerta en el dispositivo esperando a fallar otra vez.
                sesion.borrar()
                Resultado.SesionCaducada
            }
            else -> Resultado.Fallo(mensajeDeError(respuesta))
        }

    /**
     * Traduce el estado HTTP a algo que un usuario entienda.
     *
     * Se intenta primero el mensaje que manda la API, que está escrito para leerse;
     * si no viene, se cae a un texto por código.
     */
    suspend fun mensajeDeError(respuesta: HttpResponse): String {
        val delServidor = try {
            respuesta.body<RespuestaDeError>().mensaje
        } catch (excepcion: Exception) {
            null
        }

        if (!delServidor.isNullOrBlank()) return delServidor

        return when (respuesta.status) {
            HttpStatusCode.NotFound -> "No se ha encontrado."
            HttpStatusCode.TooManyRequests -> "Demasiados intentos. Espera unos minutos."
            HttpStatusCode.BadRequest -> "Revisa los datos introducidos."
            else -> "No se ha podido completar la operación. Inténtalo de nuevo."
        }
    }

    private suspend fun <T> envolver(bloque: suspend () -> Resultado<T>): Resultado<T> =
        try {
            bloque()
        } catch (excepcion: Exception) {
            // Sin red, DNS caído, servidor apagado. El usuario necesita saber que
            // el problema es la conexión y no sus datos.
            Resultado.Fallo("No se ha podido contactar con el servidor. Comprueba tu conexión.")
        }
}

private fun HttpStatusCode.isSuccess(): Boolean = value in 200..299
