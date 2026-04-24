using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class arrregloclientecfechadenacimientoparatodossyagregarpuntos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Dni",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "LimiteCredito",
                table: "Clientes");

            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha_nacimiento",
                table: "Clientes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Puntos",
                table: "Clientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes",
                column: "Correo",
                unique: true,
                filter: "\"Correo\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correonormalizado",
                table: "Clientes",
                column: "Correonormalizado",
                unique: true,
                filter: "\"Correonormalizado\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Dni",
                table: "Clientes",
                column: "Dni",
                unique: true,
                filter: "\"Dni\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Correonormalizado",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Dni",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Fecha_nacimiento",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Puntos",
                table: "Clientes");

            migrationBuilder.AddColumn<double>(
                name: "LimiteCredito",
                table: "Clientes",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Dni",
                table: "Clientes",
                column: "Dni",
                unique: true);
        }
    }
}
