// Sin `kotlin-android`: desde AGP 9 el soporte de Kotlin va integrado en el
// complemento de Android, y aplicarlo aparte es un error.
plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.kotlin.serialization)
}

android {
    namespace = "com.angelgf.recetas"
    compileSdk = 37

    defaultConfig {
        applicationId = "com.angelgf.recetas"

        // 26 (Android 8) cubre prácticamente todo el parque y es lo que exige
        // EncryptedSharedPreferences para el almacén de claves.
        minSdk = 26
        targetSdk = 37
        versionCode = 1
        versionName = "0.1"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        debug {
            // La API de desarrollo corre en el equipo del programador, y se llega a
            // ella con `adb reverse tcp:5199 tcp:5199` (ver android/README.md).
            //
            // Se usa localhost y NO 10.0.2.2 —la dirección con la que el emulador
            // ve al anfitrión— porque 10.0.2.2 entra por la red y lo para el
            // cortafuegos de Windows, que abrir exige permisos de administrador.
            // `adb reverse` va por el canal de depuración, así que no toca nada, y
            // además funciona igual con un teléfono real conectado por cable.
            buildConfigField("String", "BASE_DE_LA_API", "\"http://localhost:5199/\"")
        }

        release {
            buildConfigField("String", "BASE_DE_LA_API", "\"https://recetas-api.angelgf.com.es/\"")

            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures {
        compose = true

        // Necesario para BASE_DE_LA_API: sin esto no se genera BuildConfig.
        buildConfig = true
    }

    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.lifecycle.viewmodel.compose)
    implementation(libs.androidx.activity.compose)

    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.ui)
    implementation(libs.androidx.ui.graphics)
    implementation(libs.androidx.ui.tooling.preview)
    implementation(libs.androidx.material3)

    implementation(libs.ktor.client.core)
    implementation(libs.ktor.client.okhttp)
    implementation(libs.ktor.client.content.negotiation)
    implementation(libs.ktor.serialization.kotlinx.json)
    implementation(libs.kotlinx.serialization.json)

    testImplementation(libs.junit)
    testImplementation(libs.ktor.client.mock)
    testImplementation(libs.kotlinx.coroutines.test)
}
