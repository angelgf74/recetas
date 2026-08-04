package com.angelgf.recetas.datos

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

/**
 * Guarda el token de sesión en el dispositivo.
 *
 * La web lo tiene en `localStorage` porque el navegador no ofrece nada mejor.
 * Aquí sí: un `SharedPreferences` normal es un XML en claro dentro del
 * almacenamiento de la aplicación, legible en un teléfono con acceso de
 * superusuario o en una copia de seguridad.
 */
interface AlmacenDeSesion {
    fun leer(): String?
    fun guardar(token: String)
    fun borrar()
}

/**
 * Cifra el token con una clave del almacén de claves del sistema.
 *
 * No se usa `EncryptedSharedPreferences`: está obsoleto en `androidx.security` y
 * usarlo deja la aplicación llena de avisos y atada a una biblioteca sin futuro.
 * Cifrar a mano son treinta líneas y evita esa dependencia entera.
 *
 * La clave se genera dentro del almacén y **nunca sale de él**: aquí solo se pide
 * cifrar y descifrar. Aunque alguien copie el XML, sin el hardware del teléfono no
 * saca el token.
 */
class SesionLocal(contexto: Context) : AlmacenDeSesion {

    private val preferencias = contexto.getSharedPreferences(ARCHIVO, Context.MODE_PRIVATE)

    override fun leer(): String? {
        val guardado = preferencias.getString(CLAVE_TOKEN, null) ?: return null

        return try {
            descifrar(guardado)
        } catch (excepcion: Exception) {
            // La clave puede invalidarse: el usuario cambia el bloqueo de
            // pantalla, se restaura el teléfono desde una copia… Ahí el token es
            // irrecuperable, y lo correcto es olvidarlo y pedir sesión otra vez,
            // no arrastrar un valor que no se puede descifrar.
            borrar()
            null
        }
    }

    override fun guardar(token: String) {
        preferencias.edit().putString(CLAVE_TOKEN, cifrar(token)).apply()
    }

    override fun borrar() {
        // clear() y no remove(): si algún día se guarda algo más junto al token,
        // cerrar sesión tiene que llevárselo todo.
        preferencias.edit().clear().apply()
    }

    private fun cifrar(texto: String): String {
        val cifrador = Cipher.getInstance(TRANSFORMACION)
        cifrador.init(Cipher.ENCRYPT_MODE, clave())

        val secreto = cifrador.doFinal(texto.toByteArray(Charsets.UTF_8))

        // El vector de inicialización lo genera el cifrador y hace falta para
        // descifrar. No es secreto, pero sí irrepetible, así que se guarda delante
        // del texto cifrado en lugar de fijar uno constante.
        val juntos = cifrador.iv + secreto

        return Base64.encodeToString(juntos, Base64.NO_WRAP)
    }

    private fun descifrar(guardado: String): String {
        val juntos = Base64.decode(guardado, Base64.NO_WRAP)

        val vector = juntos.copyOfRange(0, LONGITUD_DEL_VECTOR)
        val secreto = juntos.copyOfRange(LONGITUD_DEL_VECTOR, juntos.size)

        val cifrador = Cipher.getInstance(TRANSFORMACION)
        cifrador.init(Cipher.DECRYPT_MODE, clave(), GCMParameterSpec(BITS_DE_ETIQUETA, vector))

        return String(cifrador.doFinal(secreto), Charsets.UTF_8)
    }

    /** Devuelve la clave del almacén, creándola la primera vez. */
    private fun clave(): SecretKey {
        val almacen = KeyStore.getInstance(ALMACEN).apply { load(null) }

        (almacen.getEntry(ALIAS, null) as? KeyStore.SecretKeyEntry)?.let { return it.secretKey }

        val generador = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ALMACEN)

        generador.init(
            KeyGenParameterSpec.Builder(
                ALIAS,
                KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
            )
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(256)

                // Sin exigir que el usuario se autentique: la aplicación tiene que
                // poder leer el token al arrancar, sin huella ni PIN. Lo que
                // protege esta clave es que no salga del dispositivo.
                .setUserAuthenticationRequired(false)
                .build()
        )

        return generador.generateKey()
    }

    private companion object {
        const val ARCHIVO = "sesion"
        const val CLAVE_TOKEN = "token"
        const val ALMACEN = "AndroidKeyStore"
        const val ALIAS = "recetas.sesion"
        const val TRANSFORMACION = "AES/GCM/NoPadding"

        /** GCM usa 12 bytes de vector y 128 bits de etiqueta de autenticación. */
        const val LONGITUD_DEL_VECTOR = 12
        const val BITS_DE_ETIQUETA = 128
    }
}

/** Almacén en memoria, para los tests y para las vistas previas de Compose. */
class SesionEnMemoria(private var token: String? = null) : AlmacenDeSesion {
    override fun leer(): String? = token
    override fun guardar(token: String) { this.token = token }
    override fun borrar() { token = null }
}
