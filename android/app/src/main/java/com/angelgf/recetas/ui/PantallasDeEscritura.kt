package com.angelgf.recetas.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.angelgf.recetas.datos.LineaDeIngredientePeticion
import com.angelgf.recetas.datos.PeticionDeReceta

/** Los mismos valores que la lista cerrada del dominio. */
private val TiposDePlato = listOf(
    "Entrante" to "Entrante",
    "PrimerPlato" to "Primer plato",
    "PlatoPrincipal" to "Plato principal",
    "Guarnicion" to "Guarnición",
    "Postre" to "Postre",
    "Bebida" to "Bebida",
    "Salsa" to "Salsa"
)

private val Unidades = listOf(
    "Unidad" to "unidad(es)",
    "Gramo" to "g",
    "Kilogramo" to "kg",
    "Mililitro" to "ml",
    "Litro" to "l",
    "Cucharada" to "cucharada(s)",
    "Cucharadita" to "cucharadita(s)",
    "Taza" to "taza(s)",
    "Pizca" to "pizca(s)",
    "Diente" to "diente(s)",
    "Rama" to "rama(s)",
    "Hoja" to "hoja(s)",
    "AlGusto" to "al gusto"
)

private class LineaEditable(
    nombre: String = "",
    cantidad: String = "1",
    unidad: String = "Unidad"
) {
    var nombre by mutableStateOf(nombre)
    var cantidad by mutableStateOf(cantidad)
    var unidad by mutableStateOf(unidad)
}

/**
 * Crear o editar una receta. El mismo formulario para las dos cosas, como en la
 * web y por lo mismo: duplicarlo garantizaría que un arreglo se aplique solo a
 * una de las dos pantallas.
 */
@Composable
fun PantallaDeFormulario(
    estado: EstadoDeLaApp,
    modelo: AppViewModel,
    recetaId: String?
) {
    val existente = estado.recetaAEditar
    val borrador = estado.borrador

    var nombre by rememberSaveable(existente?.id, borrador) {
        mutableStateOf(existente?.nombre ?: borrador?.nombre ?: "")
    }
    var tipo by rememberSaveable(existente?.id, borrador) {
        mutableStateOf(existente?.tipoDePlato ?: borrador?.tipoDePlato ?: "PlatoPrincipal")
    }
    var elaboracion by rememberSaveable(existente?.id, borrador) {
        mutableStateOf(existente?.elaboracion ?: borrador?.elaboracion ?: "")
    }
    var raciones by rememberSaveable(existente?.id, borrador) {
        mutableStateOf((existente?.raciones ?: borrador?.raciones)?.toString() ?: "")
    }

    val ingredientes = remember(existente?.id, borrador) {
        val iniciales = existente?.ingredientes?.map {
            LineaEditable(it.nombre, deCantidad(it.cantidad), it.unidad)
        } ?: borrador?.ingredientes?.map {
            LineaEditable(it.nombre, deCantidad(it.cantidad), it.unidad)
        } ?: listOf(LineaEditable())

        mutableStateListOf<LineaEditable>().also { it.addAll(iniciales.ifEmpty { listOf(LineaEditable()) }) }
    }

    Column(Modifier.fillMaxSize()) {
        Barra(
            titulo = if (recetaId == null) "Nueva receta" else "Editar receta",
            modelo = modelo,
            atras = { modelo.irAlRecetario() }
        )

        Column(
            Modifier
                .verticalScroll(rememberScrollState())
                .padding(16.dp)
        ) {
            estado.error?.let { AvisoDeError(it) }

            // Importar solo tiene sentido al crear: sobre una receta existente
            // machacaría lo que el usuario ya tiene escrito.
            if (recetaId == null) {
                ImportarDesdeUrl(estado, modelo)
                Spacer(Modifier.height(16.dp))
                HorizontalDivider()
                Spacer(Modifier.height(16.dp))
            }

            OutlinedTextField(
                value = nombre,
                onValueChange = { nombre = it },
                label = { Text("Nombre") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(Modifier.height(12.dp))
            Desplegable("Tipo de plato", TiposDePlato, tipo) { tipo = it }

            Spacer(Modifier.height(12.dp))
            OutlinedTextField(
                value = raciones,
                onValueChange = { nuevo -> raciones = nuevo.filter { it.isDigit() }.take(3) },
                label = { Text("Raciones (opcional)") },
                singleLine = true,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(Modifier.height(20.dp))
            Text("Ingredientes", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(8.dp))

            ingredientes.forEachIndexed { indice, linea ->
                Row(
                    Modifier.fillMaxWidth().padding(vertical = 4.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    OutlinedTextField(
                        value = linea.nombre,
                        onValueChange = { linea.nombre = it },
                        label = { Text("Ingrediente") },
                        singleLine = true,
                        modifier = Modifier.weight(1f)
                    )

                    TextButton(
                        onClick = { if (ingredientes.size > 1) ingredientes.removeAt(indice) },
                        enabled = ingredientes.size > 1
                    ) { Text("×") }
                }

                Row(
                    Modifier.fillMaxWidth().padding(bottom = 8.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    OutlinedTextField(
                        value = linea.cantidad,
                        onValueChange = { linea.cantidad = it },
                        label = { Text("Cant.") },
                        singleLine = true,
                        enabled = linea.unidad != "AlGusto",
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        modifier = Modifier.width(110.dp)
                    )

                    Desplegable("Unidad", Unidades, linea.unidad, Modifier.weight(1f)) {
                        linea.unidad = it
                    }
                }
            }

            OutlinedButton(
                onClick = { ingredientes.add(LineaEditable()) },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Añadir ingrediente") }

            Spacer(Modifier.height(20.dp))
            OutlinedTextField(
                value = elaboracion,
                onValueChange = { elaboracion = it },
                label = { Text("Elaboración") },
                minLines = 6,
                modifier = Modifier.fillMaxWidth()
            )
            Text(
                "Un paso por línea.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            Spacer(Modifier.height(20.dp))

            Button(
                onClick = {
                    modelo.guardarReceta(
                        PeticionDeReceta(
                            nombre = nombre.trim(),
                            tipoDePlato = tipo,
                            elaboracion = elaboracion.trim(),
                            raciones = raciones.toIntOrNull(),
                            ingredientes = ingredientes
                                .filter { it.nombre.isNotBlank() }
                                .map {
                                    LineaDeIngredientePeticion(
                                        nombre = it.nombre.trim(),
                                        cantidad = if (it.unidad == "AlGusto") null
                                        else it.cantidad.replace(',', '.').toDoubleOrNull(),
                                        unidad = it.unidad
                                    )
                                }
                        ),
                        recetaId
                    )
                },
                enabled = !estado.cargando
                    && nombre.isNotBlank()
                    && elaboracion.isNotBlank()
                    && ingredientes.any { it.nombre.isNotBlank() },
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(if (estado.cargando) "Guardando…" else if (recetaId == null) "Crear receta" else "Guardar cambios")
            }

            Spacer(Modifier.height(32.dp))
        }
    }
}

@Composable
private fun ImportarDesdeUrl(estado: EstadoDeLaApp, modelo: AppViewModel) {
    var direccion by rememberSaveable { mutableStateOf("") }

    Text("¿La has visto en una web?", fontWeight = FontWeight.SemiBold)
    Spacer(Modifier.height(8.dp))

    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        OutlinedTextField(
            value = direccion,
            onValueChange = { direccion = it },
            label = { Text("https://…") },
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri, imeAction = ImeAction.Go),
            modifier = Modifier.weight(1f)
        )

        OutlinedButton(
            onClick = { modelo.importar(direccion) },
            enabled = !estado.cargando && direccion.isNotBlank()
        ) { Text(if (estado.cargando) "…" else "Importar") }
    }

    estado.origenDelBorrador?.let {
        Spacer(Modifier.height(8.dp))
        Text(
            "Importado de $it. Revísalo antes de guardar.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun Desplegable(
    etiqueta: String,
    opciones: List<Pair<String, String>>,
    seleccionada: String,
    modifier: Modifier = Modifier,
    alElegir: (String) -> Unit
) {
    var abierto by remember { mutableStateOf(false) }
    val texto = opciones.firstOrNull { it.first == seleccionada }?.second ?: seleccionada

    ExposedDropdownMenuBox(
        expanded = abierto,
        onExpandedChange = { abierto = it },
        modifier = modifier
    ) {
        OutlinedTextField(
            value = texto,
            onValueChange = {},
            readOnly = true,
            label = { Text(etiqueta) },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = abierto) },
            modifier = Modifier
                .menuAnchor(androidx.compose.material3.ExposedDropdownMenuAnchorType.PrimaryNotEditable)
                .fillMaxWidth()
        )

        ExposedDropdownMenu(expanded = abierto, onDismissRequest = { abierto = false }) {
            opciones.forEach { (valor, etiquetaOpcion) ->
                DropdownMenuItem(
                    text = { Text(etiquetaOpcion) },
                    onClick = {
                        alElegir(valor)
                        abierto = false
                    }
                )
            }
        }
    }
}

private fun deCantidad(cantidad: Double?): String = Etiquetas.deCantidad(cantidad)

// ------------------------------------------------------------------ Cuenta

/** Paso 1 del alta o del restablecimiento: solo el correo. */
@Composable
fun PantallaDeCorreo(
    estado: EstadoDeLaApp,
    modelo: AppViewModel,
    titulo: String,
    explicacion: String,
    textoDelBoton: String,
    alEnviar: (String) -> Unit
) {
    var correo by rememberSaveable { mutableStateOf("") }

    Column(
        Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text(titulo, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(12.dp))
        Text(
            explicacion,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(Modifier.height(20.dp))

        estado.aviso?.let { AvisoDeExito(it) }
        estado.error?.let { AvisoDeError(it) }

        OutlinedTextField(
            value = correo,
            onValueChange = { correo = it },
            label = { Text("Correo electrónico") },
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email, imeAction = ImeAction.Done),
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(Modifier.height(16.dp))

        Button(
            onClick = { alEnviar(correo) },
            enabled = !estado.cargando && correo.isNotBlank(),
            modifier = Modifier.fillMaxWidth()
        ) { Text(if (estado.cargando) "Enviando…" else textoDelBoton) }

        Spacer(Modifier.height(12.dp))
        TextButton(onClick = { modelo.volverAlLogin() }) { Text("Volver a iniciar sesión") }
    }
}

/**
 * Paso 2: elegir la contraseña. Se llega desde el enlace del correo, que trae el
 * token en la dirección; la contraseña se manda después, en el cuerpo.
 */
@Composable
fun PantallaDeElegirContrasena(
    estado: EstadoDeLaApp,
    modelo: AppViewModel,
    token: String,
    esAlta: Boolean
) {
    var contrasena by rememberSaveable { mutableStateOf("") }

    Column(
        Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            if (esAlta) "Elige tu contraseña" else "Elige una contraseña nueva",
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold
        )

        Spacer(Modifier.height(20.dp))
        estado.error?.let { AvisoDeError(it) }

        OutlinedTextField(
            value = contrasena,
            onValueChange = { contrasena = it },
            label = { Text("Contraseña") },
            singleLine = true,
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password, imeAction = ImeAction.Done),
            modifier = Modifier.fillMaxWidth()
        )

        Text(
            "Al menos 12 caracteres. Una frase que recuerdes protege más que una palabra corta con símbolos.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(top = 8.dp)
        )

        Spacer(Modifier.height(20.dp))

        Button(
            onClick = { modelo.elegirContrasena(token, contrasena, esAlta) },
            enabled = !estado.cargando && contrasena.length >= 12,
            modifier = Modifier.fillMaxWidth()
        ) { Text(if (estado.cargando) "Guardando…" else "Continuar") }
    }
}

/** Los mismos valores que la lista cerrada del dominio. */
private val MotivosDeDenuncia = listOf(
    "NoEsUnaReceta" to "No es una receta",
    "Ofensivo" to "Contenido ofensivo o de odio",
    "Sexual" to "Contenido sexual",
    "Violento" to "Contenido violento",
    "Spam" to "Spam o publicidad",
    "Derechos" to "Copia contenido de otra persona",
    "Otro" to "Otro motivo"
)

/**
 * Denunciar una receta ajena.
 *
 * Discreto a proposito: es una salida de emergencia, no una accion principal.
 * Empieza plegado tras un enlace, porque un boton llamativo junto a una receta
 * invita a pulsarlo por error.
 *
 * **Google Play lo exige** para publicar una aplicacion que permita compartir
 * contenido entre usuarios, y sin el no se puede publicar. Ver la feature 015.
 */
@Composable
fun DenunciarReceta(recetaId: String, modelo: AppViewModel, yaDenunciada: Boolean) {
    var abierto by rememberSaveable(recetaId) { mutableStateOf(false) }
    var motivo by rememberSaveable(recetaId) { mutableStateOf(MotivosDeDenuncia.first().first) }
    var comentario by rememberSaveable(recetaId) { mutableStateOf("") }

    Spacer(Modifier.height(12.dp))

    when {
        // Ya avisada: repetir la oferta transmitiria que la primera vez no valio.
        yaDenunciada -> Text(
            "Gracias. Hemos recibido tu aviso y lo revisaremos.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        !abierto -> TextButton(onClick = { abierto = true }) {
            Text("Denunciar esta receta")
        }

        else -> Column {
            Text(
                "Cuentanos que pasa y la revisaremos. El autor no sabra quien ha avisado.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            Spacer(Modifier.height(8.dp))

            Desplegable(
                etiqueta = "Motivo",
                opciones = MotivosDeDenuncia,
                seleccionada = motivo,
                alElegir = { motivo = it }
            )

            Spacer(Modifier.height(8.dp))

            OutlinedTextField(
                value = comentario,
                onValueChange = { if (it.length <= LongitudMaximaDelComentario) comentario = it },
                label = { Text("Explicacion (opcional)") },
                minLines = 2,
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(Modifier.height(8.dp))

            Row(verticalAlignment = Alignment.CenterVertically) {
                Button(onClick = {
                    modelo.denunciar(recetaId, motivo, comentario)
                    abierto = false
                }) { Text("Enviar denuncia") }

                Spacer(Modifier.width(8.dp))

                TextButton(onClick = { abierto = false }) { Text("Cancelar") }
            }
        }
    }
}

/** Debe coincidir con `Denuncia` en el dominio. */
private const val LongitudMaximaDelComentario = 1_000
