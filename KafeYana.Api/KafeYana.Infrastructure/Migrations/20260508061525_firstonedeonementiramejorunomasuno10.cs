using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class firstonedeonementiramejorunomasuno10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "IX_Detalle_Ronda_ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.DropTable(
                name: "Detalle_Ronda_Opcion");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_Id_Producto",
                table: "Detalle_Ronda");

            migrationBuilder.DropIndex(
                name: "ix_detalle_ronda_ronda_producto_unique",
                table: "Detalle_Ronda");

            migrationBuilder.DropColumn(
                name: "Id_Producto",
                table: "Detalle_Ronda");

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "Detalle_Ronda",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Id_Ronda",
                table: "Detalle_Ronda",
                column: "Id_Ronda");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Producto_ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_Id_Ronda",
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

            migrationBuilder.CreateTable(
                name: "Detalle_Ronda_Opcion",
                columns: table => new
                {
                    Id_Detalle_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Id_Opcion = table.Column<int>(type: "integer", nullable: false),
                    CostoExtra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TipoOpcion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "normal"),
                    ValorAnterior = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Ronda_Opcion", x => new { x.Id_Detalle_Ronda, x.Id_Opcion });
                    table.ForeignKey(
                        name: "FK_Detalle_Ronda_Opcion_Detalle_Ronda_Id_Detalle_Ronda",
                        column: x => x.Id_Detalle_Ronda,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion",
                        column: x => x.Id_Opcion,
                        principalTable: "Opcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Id_Producto",
                table: "Detalle_Ronda",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "ix_detalle_ronda_ronda_producto_unique",
                table: "Detalle_Ronda",
                columns: new[] { "Id_Ronda", "Id_Producto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion");

            migrationBuilder.CreateIndex(
                name: "ix_detalle_ronda_opcion_unique",
                table: "Detalle_Ronda_Opcion",
                columns: new[] { "Id_Detalle_Ronda", "Id_Opcion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "IX_Detalle_Ronda_ProductoId",
                table: "Detalle_Ronda",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
