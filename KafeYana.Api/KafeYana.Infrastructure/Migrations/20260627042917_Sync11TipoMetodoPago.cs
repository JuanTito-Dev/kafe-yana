using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sync11TipoMetodoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSyncMetodoPago",
                table: "PuntosVentaSiat",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSyncTipoEmision",
                table: "PuntosVentaSiat",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatMetodosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatMetodosPago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatTiposEmision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatTiposEmision", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VentaPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdVenta = table.Column<int>(type: "integer", nullable: false),
                    CodigoMetodoPago = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaPagos_Venta_IdVenta",
                        column: x => x.IdVenta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatMetodosPago_Codigo",
                table: "CatMetodosPago",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatTiposEmision_Codigo",
                table: "CatTiposEmision",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VentaPagos_IdVenta",
                table: "VentaPagos",
                column: "IdVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatMetodosPago");

            migrationBuilder.DropTable(
                name: "CatTiposEmision");

            migrationBuilder.DropTable(
                name: "VentaPagos");

            migrationBuilder.DropColumn(
                name: "UltimaSyncMetodoPago",
                table: "PuntosVentaSiat");

            migrationBuilder.DropColumn(
                name: "UltimaSyncTipoEmision",
                table: "PuntosVentaSiat");
        }
    }
}
