using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClienteCelularOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Unique_celular_cliente",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "Celular",
                table: "Clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "Unique_celular_cliente",
                table: "Clientes",
                column: "Celular",
                unique: true,
                filter: "\"Celular\" IS NOT NULL AND \"Celular\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Unique_celular_cliente",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "Celular",
                table: "Clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "Unique_celular_cliente",
                table: "Clientes",
                column: "Celular",
                unique: true);
        }
    }
}
