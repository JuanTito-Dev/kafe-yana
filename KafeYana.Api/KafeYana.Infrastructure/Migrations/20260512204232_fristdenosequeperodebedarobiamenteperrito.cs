using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristdenosequeperodebedarobiamenteperrito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Producto_ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.AddColumn<int>(
                name: "Id_Producto",
                table: "Detalle_Ronda",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Nota",
                table: "Detalle_Ronda",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Id_Producto",
                table: "Detalle_Ronda",
                column: "Id_Producto");

            migrationBuilder.AddForeignKey(
                name: "fk_detalle_ronda_producto",
                table: "Detalle_Ronda",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_detalle_ronda_producto",
                table: "Detalle_Ronda");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_Id_Producto",
                table: "Detalle_Ronda");

            migrationBuilder.DropColumn(
                name: "Id_Producto",
                table: "Detalle_Ronda");

            migrationBuilder.DropColumn(
                name: "Nota",
                table: "Detalle_Ronda");

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "Detalle_Ronda",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_ProductoId",
                table: "Detalle_Ronda",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Producto_ProductoId",
                table: "Detalle_Ronda",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id");
        }
    }
}
