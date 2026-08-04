// Los complementos se declaran aquí sin aplicarlos, y cada módulo aplica los que
// necesita. Es lo que permite que la versión viva en un solo sitio.
plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.compose) apply false
    alias(libs.plugins.kotlin.serialization) apply false
}
