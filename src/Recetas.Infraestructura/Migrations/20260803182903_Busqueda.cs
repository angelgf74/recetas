using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recetas.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Busqueda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nombre_para_busqueda",
                table: "recetas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nombre_para_busqueda",
                table: "ingredientes",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            // Relleno de las filas que ya existían.
            //
            // Sin esto se quedarían con la cadena vacía que puso defaultValue y
            // dejarían de encontrarse, sin que nada fallara: el peor tipo de
            // error, porque no hay excepción ni aviso, solo búsquedas que no
            // devuelven lo que deberían.
            //
            // Se replica en SQL lo que hace TextoParaBusqueda: recortar, colapsar
            // espacios, minúsculas y quitar diacríticos. No se usa la extensión
            // `unaccent` porque instalarla exige privilegios que el despliegue no
            // tiene; `translate` cubre los diacríticos del español.
            const string aSinAcentos =
                "translate(regexp_replace(trim(lower({0})), '\\s+', ' ', 'g'), " +
                "'áàäâãéèëêíìïîóòöôõúùüûñç', 'aaaaaeeeeiiiiooooouuuunc')";

            migrationBuilder.Sql(
                $"UPDATE recetas SET nombre_para_busqueda = {string.Format(aSinAcentos, "nombre")};");

            migrationBuilder.Sql(
                $"UPDATE ingredientes SET nombre_para_busqueda = {string.Format(aSinAcentos, "nombre")};");

            migrationBuilder.CreateIndex(
                name: "ix_recetas_nombre_para_busqueda",
                table: "recetas",
                column: "nombre_para_busqueda");

            migrationBuilder.CreateIndex(
                name: "ix_ingredientes_nombre_para_busqueda",
                table: "ingredientes",
                column: "nombre_para_busqueda");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_recetas_nombre_para_busqueda",
                table: "recetas");

            migrationBuilder.DropIndex(
                name: "ix_ingredientes_nombre_para_busqueda",
                table: "ingredientes");

            migrationBuilder.DropColumn(
                name: "nombre_para_busqueda",
                table: "recetas");

            migrationBuilder.DropColumn(
                name: "nombre_para_busqueda",
                table: "ingredientes");
        }
    }
}
