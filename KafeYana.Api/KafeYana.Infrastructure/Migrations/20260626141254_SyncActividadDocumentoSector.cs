using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncActividadDocumentoSector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSyncActividadesDocumentoSector",
                table: "PuntosVentaSiat",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatActividadesDocumentosSector",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoActividad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodigoDocumentoSector = table.Column<int>(type: "integer", nullable: false),
                    TipoDocumentoSector = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatActividadesDocumentosSector", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatActividadesDocumentosSector_CodigoActividad_CodigoDocumentoSector",
                table: "CatActividadesDocumentosSector",
                columns: new[] { "CodigoActividad", "CodigoDocumentoSector" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatActividadesDocumentosSector");

            migrationBuilder.DropColumn(
                name: "UltimaSyncActividadesDocumentoSector",
                table: "PuntosVentaSiat");
        }
    }
}
