# kotlinx.serialization genera serializadores por reflexión sobre las clases
# anotadas. Sin estas reglas, R8 los borra en la compilación de publicación y la
# aplicación falla al leer la primera respuesta de la API: un fallo que NO se ve
# en depuración.
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.**

-keepclassmembers class com.angelgf.recetas.datos.** {
    *** Companion;
}
-keepclasseswithmembers class com.angelgf.recetas.datos.** {
    kotlinx.serialization.KSerializer serializer(...);
}

# Ktor usa OkHttp por debajo y este trae referencias opcionales a Conscrypt.
-dontwarn org.conscrypt.**
-dontwarn org.bouncycastle.**
-dontwarn org.openjsse.**
