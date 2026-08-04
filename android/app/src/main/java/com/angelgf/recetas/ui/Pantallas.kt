package com.angelgf.recetas.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
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
import com.angelgf.recetas.BuildConfig
import com.angelgf.recetas.datos.RespuestaDeReceta
import com.angelgf.recetas.datos.ResumenDeReceta

// ------------------------------------------------------------ Inicio de sesión

@Composable
fun PantallaDeSesion(estado: EstadoDeLaApp, modelo: AppViewModel) {
    // rememberSaveable y no remember: lo escrito tiene que sobrevivir al giro de
    // pantalla, que es justo cuando más rabia da perderlo.
    var correo by rememberSaveable { mutableStateOf("") }
    var contrasena by rememberSaveable { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text("Recetas", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(24.dp))

        estado.aviso?.let { Aviso(it, esError = false) }
        estado.error?.let { Aviso(it, esError = true) }

        OutlinedTextField(
            value = correo,
            onValueChange = { correo = it },
            label = { Text("Correo electrónico") },
            singleLine = true,
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Email,
                imeAction = ImeAction.Next
            ),
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(Modifier.height(12.dp))

        OutlinedTextField(
            value = contrasena,
            onValueChange = { contrasena = it },
            label = { Text("Contraseña") },
            singleLine = true,
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Password,
                imeAction = ImeAction.Done
            ),
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(Modifier.height(20.dp))

        Button(
            onClick = { modelo.iniciarSesion(correo, contrasena) },
            enabled = !estado.cargando && correo.isNotBlank() && contrasena.isNotBlank(),
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(if (estado.cargando) "Entrando…" else "Entrar")
        }

        Spacer(Modifier.height(16.dp))

        // El alta y la recuperación de contraseña necesitan un enlace de correo
        // que aterriza en la web. Duplicar ese flujo aquí, antes de tener enlaces
        // profundos, sería trabajo tirado (ver spec de la 012).
        Text(
            "¿No tienes cuenta o has olvidado la contraseña? Entra desde recetas.angelgf.com.es",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

// ------------------------------------------------------------------ Recetario

@Composable
fun PantallaDeRecetario(estado: EstadoDeLaApp, modelo: AppViewModel, anunciosListos: Boolean) {
    Column(Modifier.fillMaxSize()) {
        Barra(titulo = "Mi recetario", modelo = modelo)

        estado.error?.let { Aviso(it, esError = true) }

        // weight(1f) para que la lista se quede con todo el alto sobrante y el
        // banner no la empuje fuera de la pantalla.
        Box(Modifier.weight(1f)) {
            when {
                estado.cargando && estado.recetas.isEmpty() -> Cargando()

                estado.recetas.isEmpty() -> Vacio(
                    "Todavía no has guardado ninguna receta.",
                    "Créalas desde la web; aquí las tendrás a mano mientras cocinas."
                )

                else -> ListaDeRecetas(estado.recetas, modelo)
            }
        }

        BannerDeAnuncios(BuildConfig.ANUNCIO_RECETARIO, anunciosListos)
    }
}

@Composable
private fun ListaDeRecetas(recetas: List<ResumenDeReceta>, modelo: AppViewModel) {
    LazyColumn(
        contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        items(recetas, key = { it.id }) { receta ->
            Card(
                shape = RoundedCornerShape(8.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable { modelo.abrirReceta(receta.id) }
            ) {
                Row(
                    modifier = Modifier.padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    val fotoId = receta.fotoDePortadaId

                    MiniaturaDeReceta(
                        cargar = { fotoId?.let { modelo.miniatura(receta.id, it) } },
                        clave = "${receta.id}:${fotoId ?: "sin-foto"}"
                    )

                    Column(Modifier.weight(1f)) {
                        Text(receta.nombre, fontWeight = FontWeight.SemiBold)
                        Text(
                            Etiquetas.deTipo(receta.tipoDePlato)
                                + if (receta.visibilidad == "Publica") " · Publicada" else "",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
        }
    }
}

// ---------------------------------------------------------------------- Ficha

@Composable
fun PantallaDeFicha(estado: EstadoDeLaApp, modelo: AppViewModel) {
    // Mientras esta pantalla esté abierta, la pantalla no se apaga: se lee
    // cocinando y con las manos ocupadas. Solo aquí; en toda la aplicación
    // gastaría batería sin motivo.
    MantenerPantallaEncendida()

    val receta = estado.receta

    Column(Modifier.fillMaxSize()) {
        Barra(titulo = receta?.nombre ?: "Receta", modelo = modelo, atras = { modelo.irAlRecetario() })

        estado.error?.let { Aviso(it, esError = true) }

        if (receta == null) {
            if (estado.cargando) Cargando()
            return@Column
        }

        Column(
            modifier = Modifier
                .verticalScroll(rememberScrollState())
                .padding(16.dp)
        ) {
            Text(
                Etiquetas.deTipo(receta.tipoDePlato)
                    + (receta.raciones?.let { " · $it ${if (it == 1) "ración" else "raciones"}" } ?: ""),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            receta.fotos.firstOrNull()?.let { foto ->
                Spacer(Modifier.height(12.dp))
                ImagenDeLaApi(
                    cargar = { modelo.foto(receta.id, foto.id) },
                    clave = "${receta.id}:${foto.id}",
                    modifier = Modifier.fillMaxWidth()
                )
            }

            Spacer(Modifier.height(20.dp))
            Text("Ingredientes", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(8.dp))

            receta.ingredientes.forEach { linea ->
                Row(Modifier.padding(vertical = 4.dp)) {
                    Text(
                        text = "${Etiquetas.deCantidad(linea.cantidad)} ${Etiquetas.deUnidad(linea.unidad, linea.cantidad)}".trim(),
                        modifier = Modifier.weight(0.4f),
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(linea.nombre, modifier = Modifier.weight(0.6f))
                }
            }

            Spacer(Modifier.height(20.dp))
            Text("Elaboración", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(8.dp))

            // Un párrafo por línea: la API guarda la elaboración como texto y
            // partir en pasos es decisión de presentación, igual que en la web.
            receta.elaboracion.split('\n')
                .map { it.trim() }
                .filter { it.isNotEmpty() }
                .forEach { paso ->
                    Text(paso, modifier = Modifier.padding(bottom = 12.dp))
                }

            Spacer(Modifier.height(24.dp))
        }
    }
}

// -------------------------------------------------------------------- Búsqueda

@Composable
fun PantallaDeBusqueda(estado: EstadoDeLaApp, modelo: AppViewModel, anunciosListos: Boolean) {
    var nombre by rememberSaveable { mutableStateOf("") }
    var ingredientes by rememberSaveable { mutableStateOf("") }

    Column(Modifier.fillMaxSize()) {
        Barra(titulo = "Buscar", modelo = modelo, atras = { modelo.irAlRecetario() })

        Column(Modifier.padding(16.dp)) {
            OutlinedTextField(
                value = nombre,
                onValueChange = { nombre = it },
                label = { Text("Nombre") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(Modifier.height(8.dp))

            OutlinedTextField(
                value = ingredientes,
                onValueChange = { ingredientes = it },
                label = { Text("Ingredientes, separados por comas") },
                singleLine = true,
                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Search),
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(Modifier.height(12.dp))

            Button(
                onClick = { modelo.buscar(nombre, ingredientes) },
                enabled = !estado.cargando,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(if (estado.cargando) "Buscando…" else "Buscar")
            }
        }

        estado.error?.let { Aviso(it, esError = true) }

        val resultados = estado.resultados

        Box(Modifier.weight(1f)) {
            when {
                resultados == null -> Unit
                resultados.isEmpty() -> Vacio(
                    "No hay ninguna receta que cumpla eso.",
                    "Prueba con menos criterios o con otro ingrediente."
                )
                else -> Column {
                    if (estado.hayMasResultados) {
                        Text(
                            "Hay más resultados: afina la búsqueda.",
                            style = MaterialTheme.typography.bodySmall,
                            modifier = Modifier.padding(horizontal = 16.dp)
                        )
                    }
                    ListaDeRecetas(resultados, modelo)
                }
            }
        }

        BannerDeAnuncios(BuildConfig.ANUNCIO_BUSQUEDA, anunciosListos)
    }
}

// ------------------------------------------------------------------- Comunes

@Composable
private fun Barra(titulo: String, modelo: AppViewModel, atras: (() -> Unit)? = null) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(MaterialTheme.colorScheme.surface)
            .padding(horizontal = 12.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        if (atras != null) {
            TextButton(onClick = atras) { Text("‹ Volver") }
        }

        Text(
            titulo,
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            maxLines = 1,
            modifier = Modifier
                .weight(1f)
                .padding(horizontal = 8.dp)
        )

        if (atras == null) {
            TextButton(onClick = { modelo.irABuscar() }) { Text("Buscar") }
            TextButton(onClick = { modelo.cerrarSesion() }) { Text("Salir") }
        }
    }
}

@Composable
private fun Aviso(mensaje: String, esError: Boolean) {
    val colorFondo = if (esError) {
        MaterialTheme.colorScheme.errorContainer
    } else {
        MaterialTheme.colorScheme.secondaryContainer
    }

    Text(
        text = mensaje,
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp)
            .background(colorFondo, RoundedCornerShape(8.dp))
            .padding(12.dp),
        style = MaterialTheme.typography.bodyMedium
    )
}

@Composable
private fun Cargando() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        CircularProgressIndicator()
    }
}

@Composable
private fun Vacio(titulo: String, ayuda: String) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(titulo, style = MaterialTheme.typography.titleMedium)
        Spacer(Modifier.height(8.dp))
        Text(
            ayuda,
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}
