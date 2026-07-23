using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPuntoVentaSiat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PuntosVentaSiat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaSyncActividades = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuntosVentaSiat", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuntosVentaSiat_Sucursal_PuntoVenta",
                table: "PuntosVentaSiat",
                columns: new[] { "CodigoSucursal", "CodigoPuntoVenta" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuntosVentaSiat");
        }
    }
}
