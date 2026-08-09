// Entrega al navegador un archivo que ya ha descargado la aplicación.
//
// Hace falta porque la descarga no puede ser un enlace normal: un `<a href>` lo
// pide el navegador por su cuenta, sin la cabecera de autorización, y meter el
// testigo de sesión en la dirección lo dejaría escrito en los registros del
// servidor y en el historial.
//
// Va en un archivo aparte y no en línea porque la política de seguridad de
// contenido no admite scripts embebidos.

window.descargarArchivo = async (nombre, referenciaAlFlujo) => {
    const flujo = await referenciaAlFlujo.stream();
    const contenido = await new Response(flujo).blob();

    // El objeto vive en memoria del navegador hasta que se revoca. Sin el
    // revoke, cada descarga dejaría el archivo entero retenido durante toda la
    // sesión.
    const direccion = URL.createObjectURL(contenido);

    const enlace = document.createElement('a');
    enlace.href = direccion;
    enlace.download = nombre ?? 'descarga';

    document.body.appendChild(enlace);
    enlace.click();
    document.body.removeChild(enlace);

    URL.revokeObjectURL(direccion);
};
