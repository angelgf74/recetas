using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recetas.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Recetas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ingredientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recetas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    autor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo_de_plato = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    elaboracion = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    visibilidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_de_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_de_modificacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ingredientes_de_receta",
                columns: table => new
                {
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingrediente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    unidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredientes_de_receta", x => new { x.receta_id, x.ingrediente_id });
                    table.ForeignKey(
                        name: "FK_ingredientes_de_receta_ingredientes_ingrediente_id",
                        column: x => x.ingrediente_id,
                        principalTable: "ingredientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ingredientes_de_receta_recetas_receta_id",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ingredientes_nombre",
                table: "ingredientes",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ingredientes_de_receta_ingrediente_id",
                table: "ingredientes_de_receta",
                column: "ingrediente_id");

            migrationBuilder.CreateIndex(
                name: "ix_recetas_autor",
                table: "recetas",
                column: "autor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingredientes_de_receta");

            migrationBuilder.DropTable(
                name: "ingredientes");

            migrationBuilder.DropTable(
                name: "recetas");
        }
    }
}
