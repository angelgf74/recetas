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

# WorkManager, que llega arrastrado por play-services-ads, guarda su estado en
# una base de datos Room. Room no instancia esa base directamente: busca por
# nombre la clase generada con el sufijo `_Impl` y la carga por reflexión. R8 no
# ve ninguna referencia a ella, la borra, y la aplicación muere al arrancar con
#
#   Unable to get provider androidx.startup.InitializationProvider:
#     Failed to create an instance of androidx.work.impl.WorkDatabase
#
# El fallo es en el arranque y NO se ve en depuración, donde R8 no corre. Aquí no
# se usa Room para nada propio: esto solo protege lo que trae la publicidad.
-keep class * extends androidx.room.RoomDatabase { <init>(); }
-keep class androidx.room.RoomDatabase { *; }
-dontwarn androidx.room.paging.**
