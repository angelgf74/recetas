package com.angelgf.recetas.datos

import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.headersOf
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Tests de las dos cosas que el cliente hace y ninguna pantalla debe repetir: la
 * cabecera de autorización y el trato del 401.
 */
class ClienteDeApiTests {

    private val jsonHeaders = headersOf(HttpHeaders.ContentType, "application/json")

    private fun cliente(
        sesion: AlmacenDeSesion,
        responder: (io.ktor.client.request.HttpRequestData) -> io.ktor.client.engine.mock.MockRequestHandleScope.() -> Unit = { {} },
        motor: MockEngine
    ) = ClienteDeApi("https://api.test/", sesion, motor)

    // ------------------------------------------------------------- Cabecera

    @Test
    fun `pone la cabecera de autorizacion cuando hay sesion`() = runTest {
        var cabecera: String? = null

        val motor = MockEngine { peticion ->
            cabecera = peticion.headers[HttpHeaders.Authorization]
            respond("[]", HttpStatusCode.OK, jsonHeaders)
        }

        val api = ClienteDeApi("https://api.test/", SesionEnMemoria("un-token"), motor)
        api.misRecetas()

        assertEquals("Bearer un-token", cabecera)
    }

    @Test
    fun `no pone cabecera cuando no hay sesion`() = runTest {
        var cabecera: String? = "sin tocar"

        val motor = MockEngine { peticion ->
            cabecera = peticion.headers[HttpHeaders.Authorization]
            respond("[]", HttpStatusCode.OK, jsonHeaders)
        }

        val api = ClienteDeApi("https://api.test/", SesionEnMemoria(null), motor)
        api.misRecetas()

        assertNull(cabecera)
    }

    // ------------------------------------------------------------------ 401

    @Test
    fun `un 401 devuelve sesion caducada y borra el token`() = runTest {
        val sesion = SesionEnMemoria("un-token")
        val motor = MockEngine { respond("", HttpStatusCode.Unauthorized) }

        val api = ClienteDeApi("https://api.test/", sesion, motor)
        val resultado = api.misRecetas()

        assertTrue(resultado is Resultado.SesionCaducada)

        // Se borra aquí mismo: dejar una sesión muerta en el dispositivo solo
        // sirve para volver a fallar en la siguiente pantalla.
        assertNull(sesion.leer())
    }

    /**
     * El inicio de sesión es el único sitio donde un 401 no es "sesión caducada":
     * es "esas credenciales no valen". Confundirlos daría un mensaje absurdo a
     * quien acaba de teclear mal la contraseña.
     */
    @Test
    fun `un 401 al iniciar sesion habla de credenciales`() = runTest {
        val motor = MockEngine { respond("", HttpStatusCode.Unauthorized) }
        val api = ClienteDeApi("https://api.test/", SesionEnMemoria(null), motor)

        val resultado = api.iniciarSesion("alguien@ejemplo.com", "mal")

        assertTrue(resultado is Resultado.Fallo)
        assertEquals("Correo o contraseña incorrectos.", (resultado as Resultado.Fallo).mensaje)
    }

    // --------------------------------------------------------------- Sesión

    @Test
    fun `iniciar sesion guarda el token`() = runTest {
        val sesion = SesionEnMemoria(null)
        val motor = MockEngine {
            respond("""{"token":"nuevo-token"}""", HttpStatusCode.OK, jsonHeaders)
        }

        val api = ClienteDeApi("https://api.test/", sesion, motor)
        api.iniciarSesion("alguien@ejemplo.com", "una-contrasena-larga")

        assertEquals("nuevo-token", sesion.leer())
        assertTrue(api.haySesion())
    }

    @Test
    fun `cerrar sesion borra el token`() {
        val sesion = SesionEnMemoria("un-token")
        val api = ClienteDeApi("https://api.test/", sesion, MockEngine { respond("") })

        api.cerrarSesion()

        assertNull(sesion.leer())
        assertFalse(api.haySesion())
    }

    // --------------------------------------------------------------- Errores

    @Test
    fun `se prefiere el mensaje que manda la API`() = runTest {
        val motor = MockEngine {
            respond(
                """{"mensaje":"No se ha encontrado esa receta."}""",
                HttpStatusCode.NotFound,
                jsonHeaders
            )
        }

        val api = ClienteDeApi("https://api.test/", SesionEnMemoria("t"), motor)
        val resultado = api.receta("cualquiera")

        assertEquals("No se ha encontrado esa receta.", (resultado as Resultado.Fallo).mensaje)
    }

    @Test
    fun `sin mensaje del servidor se cae a un texto por codigo`() = runTest {
        val motor = MockEngine { respond("", HttpStatusCode.TooManyRequests) }

        val api = ClienteDeApi("https://api.test/", SesionEnMemoria("t"), motor)
        val resultado = api.receta("cualquiera")

        assertEquals("Demasiados intentos. Espera unos minutos.", (resultado as Resultado.Fallo).mensaje)
    }

    /** Sin red no puede quedarse la pantalla en blanco. */
    @Test
    fun `un fallo de red se traduce a un mensaje entendible`() = runTest {
        val motor = MockEngine { throw java.io.IOException("sin red") }

        val api = ClienteDeApi("https://api.test/", SesionEnMemoria("t"), motor)
        val resultado = api.misRecetas()

        assertTrue(resultado is Resultado.Fallo)
        assertTrue((resultado as Resultado.Fallo).mensaje.contains("conexión"))
    }

    // -------------------------------------------------------------- Contrato

    /**
     * La API puede añadir campos en una versión nueva, y una aplicación ya
     * instalada tiene que seguir funcionando. Es el problema que no tiene la web,
     * que se descarga entera en cada visita.
     */
    @Test
    fun `un campo desconocido en la respuesta no rompe nada`() = runTest {
        val motor = MockEngine {
            respond(
                """[{"id":"1","nombre":"Tortilla","tipoDePlato":"PlatoPrincipal",
                    "visibilidad":"Privada","esMia":true,"campoDelFuturo":42}]""",
                HttpStatusCode.OK,
                jsonHeaders
            )
        }

        val api = ClienteDeApi("https://api.test/", SesionEnMemoria("t"), motor)
        val resultado = api.misRecetas()

        assertTrue(resultado is Resultado.Correcto)
        assertEquals("Tortilla", (resultado as Resultado.Correcto).valor.single().nombre)
    }

    @Test
    fun `la busqueda repite el parametro de ingrediente`() = runTest {
        var consulta = ""

        val motor = MockEngine { peticion ->
            consulta = peticion.url.encodedQuery
            respond("""{"resultados":[],"hayMas":false}""", HttpStatusCode.OK, jsonHeaders)
        }

        val api = ClienteDeApi("https://api.test/", SesionEnMemoria("t"), motor)
        api.buscar("tortilla", listOf("patata", "huevo"))

        // Repetido, no separado por comas: es como lo espera la API (feature 006).
        assertTrue(consulta, consulta.contains("ingrediente=patata"))
        assertTrue(consulta, consulta.contains("ingrediente=huevo"))
    }
}
