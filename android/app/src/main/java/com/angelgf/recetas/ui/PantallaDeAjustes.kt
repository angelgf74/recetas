package com.angelgf.recetas.ui

import android.app.Activity
import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.angelgf.recetas.datos.Anuncios

private const val URL_DE_LA_POLITICA = "https://recetas.angelgf.com.es/privacidad.html"

/**
 * Ajustes de la aplicación.
 *
 * Existe sobre todo por una razón concreta: **Google exige que el usuario pueda
 * revocar el consentimiento publicitario desde dentro de la aplicación**, y sin
 * ese enlace no deja publicar en Play. Un consentimiento que no se puede retirar
 * tampoco valdría bajo el RGPD, que da el mismo peso a darlo y a quitarlo.
 *
 * De paso recoge el enlace a la política de privacidad y el cierre de sesión, que
 * antes vivía en la barra superior.
 */
@Composable
fun PantallaDeAjustes(estado: EstadoDeLaApp, modelo: AppViewModel) {
    val contexto = LocalContext.current
    val actividad = contexto as? Activity

    // Cuántas recetas y fotos hay dentro: la baja lo dice antes de pedir
    // confirmación, y con números concretos, no con un "esto es irreversible".
    LaunchedEffect(Unit) { modelo.cargarResumenDeLaCuenta() }

    // Solo se ofrece si UMP dice que hace falta: fuera del Espacio Económico
    // Europeo no hay nada que revocar, y un botón que abre un formulario vacío
    // confunde más que ayuda.
    var hayOpciones by remember {
        mutableStateOf(actividad != null && Anuncios.hayQueOfrecerOpcionesDePrivacidad(actividad))
    }

    Column(Modifier.fillMaxSize()) {
        Barra(titulo = "Ajustes", modelo = modelo, atras = { modelo.irAlRecetario() })

        Column(Modifier.padding(16.dp)) {
            Text("Privacidad", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(8.dp))

            if (hayOpciones && actividad != null) {
                Text(
                    "Puedes cambiar o retirar el consentimiento que diste para la publicidad.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(Modifier.height(8.dp))

                OutlinedButton(
                    onClick = {
                        Anuncios.mostrarOpcionesDePrivacidad(actividad) {
                            hayOpciones = Anuncios.hayQueOfrecerOpcionesDePrivacidad(actividad)
                        }
                    },
                    modifier = Modifier.fillMaxWidth()
                ) { Text("Opciones de privacidad de los anuncios") }

                Spacer(Modifier.height(12.dp))
            }

            OutlinedButton(
                onClick = {
                    contexto.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(URL_DE_LA_POLITICA)))
                },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Política de privacidad") }

            Spacer(Modifier.height(24.dp))
            HorizontalDivider()
            Spacer(Modifier.height(16.dp))

            Text("Cuenta", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(8.dp))

            estado.identidad?.let {
                Text(
                    it.correo,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(Modifier.height(8.dp))
            }

            TextButton(onClick = { modelo.cerrarSesion() }) { Text("Cerrar sesión") }

            Spacer(Modifier.height(24.dp))
            HorizontalDivider()
            Spacer(Modifier.height(16.dp))

            BorrarMiCuenta(estado, modelo)
        }
    }
}

/**
 * Borrar la cuenta.
 *
 * Va al final y detras de un separador, lejos de "Cerrar sesion": son la accion
 * mas inocua y la mas destructiva del producto, y en una lista seguida se pulsan
 * por error. Ademas exige la contrasena, que es lo que de verdad las separa.
 *
 * **Google Play lo exige** para toda aplicacion que permita crear cuentas.
 */
@Composable
private fun BorrarMiCuenta(estado: EstadoDeLaApp, modelo: AppViewModel) {
    var abierto by rememberSaveable { mutableStateOf(false) }
    var contrasena by rememberSaveable { mutableStateOf("") }

    Text(
        "Borrar mi cuenta",
        style = MaterialTheme.typography.titleMedium,
        fontWeight = FontWeight.Bold,
        color = MaterialTheme.colorScheme.error
    )

    Spacer(Modifier.height(8.dp))

    val cuanto = estado.identidad?.let {
        "Se borrarán ${recuento(it.recetas, "receta", "recetas")} y " +
            "${recuento(it.fotos, "foto", "fotos")}. "
    } ?: ""

    Text(
        cuanto + "Las recetas que hayas compartido dejarán de verlas los demás. " +
            "No se puede deshacer y no hay periodo de gracia.",
        style = MaterialTheme.typography.bodySmall,
        color = MaterialTheme.colorScheme.onSurfaceVariant
    )

    Spacer(Modifier.height(12.dp))

    if (!abierto) {
        OutlinedButton(
            onClick = { abierto = true },
            modifier = Modifier.fillMaxWidth()
        ) { Text("Borrar mi cuenta") }

        return
    }

    OutlinedTextField(
        value = contrasena,
        onValueChange = { contrasena = it },
        label = { Text("Tu contraseña") },
        singleLine = true,
        visualTransformation = PasswordVisualTransformation(),
        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
        modifier = Modifier.fillMaxWidth()
    )

    Spacer(Modifier.height(8.dp))

    Row(verticalAlignment = Alignment.CenterVertically) {
        ConfirmarAccion(
            texto = "Borrar mi cuenta",
            pregunta = "¿Seguro? Se borrarán tu cuenta, tus recetas y tus fotos.",
            alConfirmar = { modelo.borrarMiCuenta(contrasena) }
        )

        Spacer(Modifier.width(8.dp))

        TextButton(onClick = {
            abierto = false
            contrasena = ""
        }) { Text("Cancelar") }
    }
}

private fun recuento(cuantas: Int, singular: String, plural: String): String =
    if (cuantas == 1) "1 $singular" else "$cuantas $plural"
