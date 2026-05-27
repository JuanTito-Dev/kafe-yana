using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class quemiras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id_PromocionPermanenteDescuento",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NombrePromocionDescuento",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PorcentajeDescuento",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroCompras",
                table: "Clientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HistorialHitoCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_HitoCompra = table.Column<int>(type: "integer", nullable: false),
                    NumeroComprasAlReclamar = table.Column<int>(type: "integer", nullable: false),
                    CodigoReclamo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialHitoCompra", x => x.Id);
                    table.ForeignKey(
                        name: "fk_historialhitocompra_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historialhitocompra_hitocompra",
                        column: x => x.Id_HitoCompra,
                        principalTable: "HitoCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "PromocionPermanenteProgreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_PromocionPermanente = table.Column<int>(type: "integer", nullable: false),
                    ContadorCompras = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ReclamoMontoMinimoPendiente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
                name: "IX_HistorialHitoCompra_Cliente",
                table: "HistorialHitoCompra",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialHitoCompra_Cliente_Hito",
                table: "HistorialHitoCompra",
                columns: new[] { "Id_Cliente", "Id_HitoCompra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialHitoCompra_Id_HitoCompra",
                table: "HistorialHitoCompra",
                column: "Id_HitoCompra");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Cliente",
                table: "HistorialPromocionPermanente",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Id_PromocionPermanente",
                table: "HistorialPromocionPermanente",
                column: "Id_PromocionPermanente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Venta_TipoRecompensa",
                table: "HistorialPromocionPermanente",
                columns: new[] { "CodigoVenta", "TipoRecompensa" },
                unique: true);

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
                name: "HistorialHitoCompra");

            migrationBuilder.DropTable(
                name: "HistorialPromocionPermanente");

            migrationBuilder.DropTable(
                name: "HistorialPromocionTemporada");

            migrationBuilder.DropTable(
                name: "PromocionPermanenteProgreso");

            migrationBuilder.DropColumn(
                name: "Id_PromocionPermanenteDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NombrePromocionDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PorcentajeDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NumeroCompras",
                table: "Clientes");
        }
    }
}
