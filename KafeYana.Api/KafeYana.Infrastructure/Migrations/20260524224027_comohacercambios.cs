using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class comohacercambios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialPromocionPermanente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_PromocionPermanente = table.Column<int>(type: "integer", nullable: false),
                    CodigoVenta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TipoRecompensa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorRecompensa = table.Column<int>(type: "integer", nullable: false),
                    TipoCondicion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorCondicion = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPromocionPermanente", x => x.Id);
                    table.ForeignKey(
                        name: "fk_historialpromocionpermanente_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historialpromocionpermanente_promocion",
                        column: x => x.Id_PromocionPermanente,
                        principalTable: "PromocionPermanente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromocionPermanenteProgreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_PromocionPermanente = table.Column<int>(type: "integer", nullable: false),
                    ContadorCompras = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionPermanenteProgreso", x => x.Id);
                    table.ForeignKey(
                        name: "fk_promocionpermanenteprogreso_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_promocionpermanenteprogreso_promocion",
                        column: x => x.Id_PromocionPermanente,
                        principalTable: "PromocionPermanente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Cliente",
                table: "HistorialPromocionPermanente",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_CodigoVenta_Unique",
                table: "HistorialPromocionPermanente",
                column: "CodigoVenta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Id_PromocionPermanente",
                table: "HistorialPromocionPermanente",
                column: "Id_PromocionPermanente");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanenteProgreso_Cliente_Promo",
                table: "PromocionPermanenteProgreso",
                columns: new[] { "Id_Cliente", "Id_PromocionPermanente" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanenteProgreso_Id_PromocionPermanente",
                table: "PromocionPermanenteProgreso",
                column: "Id_PromocionPermanente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialPromocionPermanente");

            migrationBuilder.DropTable(
                name: "PromocionPermanenteProgreso");
        }
    }
}
