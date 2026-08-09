using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recetas.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Etiquetas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "etiquetas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nombre_para_busqueda = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etiquetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "etiquetas_de_receta",
                columns: table => new
                {
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etiqueta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etiquetas_de_receta", x => new { x.receta_id, x.etiqueta_id });
                    table.ForeignKey(
                        name: "FK_etiquetas_de_receta_etiquetas_etiqueta_id",
                        column: x => x.etiqueta_id,
                        principalTable: "etiquetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_etiquetas_de_receta_recetas_receta_id",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_etiquetas_nombre",
                table: "etiquetas",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_etiquetas_nombre_para_busqueda",
                table: "etiquetas",
                column: "nombre_para_busqueda");

            migrationBuilder.CreateIndex(
                name: "IX_etiquetas_de_receta_etiqueta_id",
                table: "etiquetas_de_receta",
                column: "etiqueta_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etiquetas_de_receta");

            migrationBuilder.DropTable(
                name: "etiquetas");
        }
    }
}
