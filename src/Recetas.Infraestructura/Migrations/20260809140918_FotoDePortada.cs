using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recetas.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class FotoDePortada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "foto_de_portada_elegida_id",
                table: "recetas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_recetas_foto_de_portada_elegida_id",
                table: "recetas",
                column: "foto_de_portada_elegida_id");

            migrationBuilder.AddForeignKey(
                name: "FK_recetas_fotos_foto_de_portada_elegida_id",
                table: "recetas",
                column: "foto_de_portada_elegida_id",
                principalTable: "fotos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recetas_fotos_foto_de_portada_elegida_id",
                table: "recetas");

            migrationBuilder.DropIndex(
                name: "IX_recetas_foto_de_portada_elegida_id",
                table: "recetas");

            migrationBuilder.DropColumn(
                name: "foto_de_portada_elegida_id",
                table: "recetas");
        }
    }
}
