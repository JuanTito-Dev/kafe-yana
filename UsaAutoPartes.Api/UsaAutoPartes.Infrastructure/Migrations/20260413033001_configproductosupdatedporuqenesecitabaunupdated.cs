using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class configproductosupdatedporuqenesecitabaunupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Compatibilidad",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Proveedor",
                table: "Producto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Producto",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Producto",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Compatibilidad",
                table: "Producto",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Proveedor",
                table: "Producto",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
