using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class nuwarrelolistadeproductospracodioauxuniqie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Producto_CodigoAux",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CodigoAux2",
                table: "Producto");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoAux",
                table: "Producto",
                column: "CodigoAux",
                unique: true,
                filter: "\"CodigoAux\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoAux2",
                table: "Producto",
                column: "CodigoAux2",
                unique: true,
                filter: "\"CodigoAux2\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Producto_CodigoAux",
                table: "Producto");

            migrationBuilder.DropIndex(
                name: "IX_Producto_CodigoAux2",
                table: "Producto");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoAux",
                table: "Producto",
                column: "CodigoAux",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoAux2",
                table: "Producto",
                column: "CodigoAux2",
                unique: true);
        }
    }
}
