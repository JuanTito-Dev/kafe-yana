using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoSignificativoSiat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosSignificativosSiat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoMotivo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaHoraInicioEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraFinEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoAmbiente = table.Column<int>(type: "integer", nullable: false),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    CodigoSistema = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nit = table.Column<long>(type: "bigint", nullable: false),
                    Cufd = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CufdEvento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cuis = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoRecepcionEventoSignificativo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Transaccion = table.Column<bool>(type: "boolean", nullable: false),
                    CodigosRespuestaJson = table.Column<string>(type: "text", nullable: false),
                    Origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EstadoContingencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosSignificativosSiat", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cufd_SucursalPuntoVenta",
                table: "Cufd",
                columns: new[] { "CodigoSucursal", "CodigoPuntoVenta" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosSignificativosSiat_CodigoRecepcion",
                table: "EventosSignificativosSiat",
                column: "CodigoRecepcionEventoSignificativo",
                unique: true,
                filter: "\"CodigoRecepcionEventoSignificativo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventosSignificativosSiat_Estado_FechaRegistro",
                table: "EventosSignificativosSiat",
                columns: new[] { "EstadoContingencia", "FechaRegistro" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosSignificativosSiat");

            migrationBuilder.DropIndex(
                name: "IX_Cufd_SucursalPuntoVenta",
                table: "Cufd");
        }
    }
}
