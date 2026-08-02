using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recetas.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class CuentasDeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solicitudes_de_registro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    hash_del_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_de_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_de_caducidad = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_de_consumo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitudes_de_registro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    hash_de_contrasena = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    fecha_de_alta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_correo",
                table: "solicitudes_de_registro",
                column: "correo");

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_hash_del_token",
                table: "solicitudes_de_registro",
                column: "hash_del_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_correo",
                table: "usuarios",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solicitudes_de_registro");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
