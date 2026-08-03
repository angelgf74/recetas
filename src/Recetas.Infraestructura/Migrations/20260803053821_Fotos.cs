using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recetas.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class Fotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tamano_en_bytes = table.Column<long>(type: "bigint", nullable: false),
                    fecha_de_subida = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fotos_recetas_receta_id",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fotos_receta",
                table: "fotos",
                column: "receta_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fotos");
        }
    }
}
