using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ventarondadepedidosjjprarversifunca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste");

            migrationBuilder.CreateTable(
                name: "Ronda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Ronda_Descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ronda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ronda_Pedido_Id_Pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_ronda",
                columns: table => new
                {
                    Id_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_ronda", x => new { x.Id_Ronda, x.Id_Producto });
                    table.ForeignKey(
                        name: "FK_Detalle_ronda_Ronda_Id_Ronda",
                        column: x => x.Id_Ronda,
                        principalTable: "Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Producto asociado a una venta",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_ronda_Id_Producto",
                table: "Detalle_ronda",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "IX_Ronda_Id_Pedido",
                table: "Ronda",
                column: "Id_Pedido");

            migrationBuilder.AddForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste",
                column: "Id_InsumoNuevo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste");

            migrationBuilder.DropTable(
                name: "Detalle_ronda");

            migrationBuilder.DropTable(
                name: "Ronda");

            migrationBuilder.AddForeignKey(
                name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                table: "Ajuste",
                column: "Id_InsumoNuevo",
                principalTable: "Insumo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
