using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tuyelquehacelascompras123334333 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HitoCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroCompras = table.Column<int>(type: "integer", nullable: false),
                    Id_ProductoCanjeable = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icono = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HitoCompra", x => x.Id);
                    table.ForeignKey(
                        name: "fk_hitocompra_productocanjeable",
                        column: x => x.Id_ProductoCanjeable,
                        principalTable: "ProductoCanjeable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromocionTemporada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionTemporada", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromocionTemporada_ProductoCanjeable",
                columns: table => new
                {
                    Id_PromocionTemporada = table.Column<int>(type: "integer", nullable: false),
                    Id_ProductoCanjeable = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promociontemporada_productocanjeable", x => new { x.Id_PromocionTemporada, x.Id_ProductoCanjeable });
                    table.ForeignKey(
                        name: "fk_promtemp_productocanjeable",
                        column: x => x.Id_ProductoCanjeable,
                        principalTable: "ProductoCanjeable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promtemp_promociontemporada",
                        column: x => x.Id_PromocionTemporada,
                        principalTable: "PromocionTemporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HitoCompra_Activo",
                table: "HitoCompra",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_HitoCompra_Id_ProductoCanjeable",
                table: "HitoCompra",
                column: "Id_ProductoCanjeable");

            migrationBuilder.CreateIndex(
                name: "IX_HitoCompra_NumeroCompras_Unique",
                table: "HitoCompra",
                column: "NumeroCompras",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_Activo",
                table: "PromocionTemporada",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_FechaFin",
                table: "PromocionTemporada",
                column: "FechaFin");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_FechaInicio",
                table: "PromocionTemporada",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_Nombre_Unique",
                table: "PromocionTemporada",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_ProductoCanjeable_Id_ProductoCanjeable",
                table: "PromocionTemporada_ProductoCanjeable",
                column: "Id_ProductoCanjeable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HitoCompra");

            migrationBuilder.DropTable(
                name: "PromocionTemporada_ProductoCanjeable");

            migrationBuilder.DropTable(
                name: "PromocionTemporada");
        }
    }
}
