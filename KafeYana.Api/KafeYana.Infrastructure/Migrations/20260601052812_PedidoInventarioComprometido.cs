using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PedidoInventarioComprometido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PedidoInventarioComprometido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Id_Detalle_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoInventarioComprometido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoInventarioComprometido_Detalle_Ronda_Id_Detalle_Ronda",
                        column: x => x.Id_Detalle_Ronda,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoInventarioComprometidoLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Comprometido = table.Column<int>(type: "integer", nullable: false),
                    TipoEntidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: true),
                    Id_Insumo = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoInventarioComprometidoLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoInventarioComprometidoLinea_PedidoInventarioCompromet~",
                        column: x => x.Id_Comprometido,
                        principalTable: "PedidoInventarioComprometido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoInventarioComprometido_Id_Detalle_Ronda",
                table: "PedidoInventarioComprometido",
                column: "Id_Detalle_Ronda",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoInventarioComprometidoLinea_Id_Comprometido",
                table: "PedidoInventarioComprometidoLinea",
                column: "Id_Comprometido");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoInventarioComprometidoLinea");

            migrationBuilder.DropTable(
                name: "PedidoInventarioComprometido");
        }
    }
}
