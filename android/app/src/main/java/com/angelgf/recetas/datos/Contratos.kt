package com.angelgf.recetas.datos

import kotlinx.serialization.Serializable

/**
 * Espejo de `Recetas.Contratos`, escrito a mano.
 *
 * No hay forma de compartir tipos entre C# y Kotlin sin generar código, y generar
 * código exigiría un paso más en la construcción para ahorrar cuatro clases de
 * datos. El precio es que estos tipos y los de la API pueden separarse.
 *
 * Si se separan, **se arregla esto**, no la API: `tech-stack.md` dice que el
 * contrato es uno solo para todas las superficies y que no hay endpoints a medida
 * de un cliente.
 *
 * Todo campo que la API pueda no mandar lleva valor por omisión: así una versión
 * nueva del servidor que añada campos no rompe una aplicación ya instalada, que es
 * el problema que no tiene la web —siempre se descarga entera— y sí tiene Android.
 */

@Serializable
data class PeticionDeInicioDeSesion(val correo: String, val contrasena: String)

@Serializable
data class RespuestaDeInicioDeSesion(val token: String, val caduca: String? = null)

@Serializable
data class RespuestaDeIdentidad(val id: String, val correo: String)

@Serializable
data class ResumenDeReceta(
    val id: String,
    val nombre: String,
    val tipoDePlato: String,
    val visibilidad: String,
    val esMia: Boolean = true,
    val fotoDePortadaId: String? = null
)

@Serializable
data class LineaDeIngredienteRespuesta(
    val nombre: String,
    val cantidad: Double? = null,
    val unidad: String
)

@Serializable
data class FotoRespuesta(val id: String, val tipo: String, val tamanoEnBytes: Long = 0)

@Serializable
data class RespuestaDeReceta(
    val id: String,
    val nombre: String,
    val tipoDePlato: String,
    val elaboracion: String,
    val visibilidad: String,
    val ingredientes: List<LineaDeIngredienteRespuesta> = emptyList(),
    val fotos: List<FotoRespuesta> = emptyList(),
    val esMia: Boolean = true,
    val raciones: Int? = null,
    val racionesMostradas: Int? = null
)

@Serializable
data class RespuestaDeBusqueda(
    val resultados: List<ResumenDeReceta> = emptyList(),
    val hayMas: Boolean = false
)

@Serializable
data class RespuestaDeError(val mensaje: String? = null)
