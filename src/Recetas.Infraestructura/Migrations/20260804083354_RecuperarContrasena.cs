using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recetas.Infraestructura.Migrations
{
    /// <inheritdoc />
    public partial class RecuperarContrasena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_de_cambio_de_contrasena",
                table: "usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "solicitudes_de_contrasena",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_del_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fecha_de_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_de_caducidad = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_de_consumo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitudes_de_contrasena", x => x.Id);
                    table.ForeignKey(
                        name: "FK_solicitudes_de_contrasena_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_contrasena_hash_del_token",
                table: "solicitudes_de_contrasena",
                column: "hash_del_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_contrasena_usuario",
                table: "solicitudes_de_contrasena",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solicitudes_de_contrasena");

            migrationBuilder.DropColumn(
                name: "fecha_de_cambio_de_contrasena",
                table: "usuarios");
        }
    }
}
