using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristdiadehoyclientesconfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                newName: "unique_nombre_cliente");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_Dni",
                table: "Clientes",
                newName: "Unique_Dni_cliente");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes",
                newName: "Unique_correo_cliente");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_Celular",
                table: "Clientes",
                newName: "Unique_celular_cliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "unique_nombre_cliente",
                table: "Clientes",
                newName: "IX_Clientes_Nombre");

            migrationBuilder.RenameIndex(
                name: "Unique_Dni_cliente",
                table: "Clientes",
                newName: "IX_Clientes_Dni");

            migrationBuilder.RenameIndex(
                name: "Unique_correo_cliente",
                table: "Clientes",
                newName: "IX_Clientes_Correo");

            migrationBuilder.RenameIndex(
                name: "Unique_celular_cliente",
                table: "Clientes",
                newName: "IX_Clientes_Celular");
        }
    }
}
