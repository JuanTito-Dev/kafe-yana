using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatLeyendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSyncLeyendas",
                table: "PuntosVentaSiat",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatLeyendas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoActividad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescripcionLeyenda = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatLeyendas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatLeyendas_CodigoActividad_DescripcionLeyenda",
                table: "CatLeyendas",
                columns: new[] { "CodigoActividad", "DescripcionLeyenda" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatLeyendas");

            migrationBuilder.DropColumn(
                name: "UltimaSyncLeyendas",
                table: "PuntosVentaSiat");
        }
    }
}
