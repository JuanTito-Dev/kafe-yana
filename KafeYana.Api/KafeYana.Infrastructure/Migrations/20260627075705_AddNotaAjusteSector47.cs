using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaAjusteSector47 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatMotivosAjuste");

            migrationBuilder.DropColumn(
                name: "UltimaSyncMotivoAjuste",
                table: "PuntosVentaSiat");

            migrationBuilder.AddColumn<int>(
                name: "NroItem",
                table: "NotaAjusteDetalle",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoAdicional",
                table: "NotaAjuste",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NroItem",
                table: "NotaAjusteDetalle");

            migrationBuilder.DropColumn(
                name: "DescuentoAdicional",
                table: "NotaAjuste");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSyncMotivoAjuste",
                table: "PuntosVentaSiat",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatMotivosAjuste",
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
                    table.PrimaryKey("PK_CatMotivosAjuste", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatMotivosAjuste_Codigo",
                table: "CatMotivosAjuste",
                column: "Codigo",
                unique: true);
        }
    }
}
