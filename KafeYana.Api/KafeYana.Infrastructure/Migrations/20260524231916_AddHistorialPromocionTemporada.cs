using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistorialPromocionTemporada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialPromocionTemporada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_PromocionTemporada = table.Column<int>(type: "integer", nullable: false),
                    CodigoReclamo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPromocionTemporada", x => x.Id);
                    table.ForeignKey(
                        name: "fk_historialpromociontemporada_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historialpromociontemporada_promocion",
                        column: x => x.Id_PromocionTemporada,
                        principalTable: "PromocionTemporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionTemporada_Cliente",
                table: "HistorialPromocionTemporada",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionTemporada_Cliente_Promocion",
                table: "HistorialPromocionTemporada",
                columns: new[] { "Id_Cliente", "Id_PromocionTemporada" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionTemporada_Id_PromocionTemporada",
                table: "HistorialPromocionTemporada",
                column: "Id_PromocionTemporada");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialPromocionTemporada");
        }
    }
}
