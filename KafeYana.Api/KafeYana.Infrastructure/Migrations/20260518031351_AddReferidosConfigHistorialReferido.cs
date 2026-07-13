using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferidosConfigHistorialReferido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialReferido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreReferidor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NombreReferido = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PuntosReferidor = table.Column<int>(type: "integer", nullable: false),
                    PuntosReferido = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialReferido", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferidosConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PuntosReferidor = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PuntosReferido = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferidosConfig", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialReferido_Fecha",
                table: "HistorialReferido",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialReferido");

            migrationBuilder.DropTable(
                name: "ReferidosConfig");
        }
    }
}
