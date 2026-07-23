using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCafcPuntoVentaSiat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cafc",
                table: "PuntosVentaSiat",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // El CAFC hardcodeado en appsettings.json (Siat:Cafc) solo está emitido por
            // el SIN para el PV=0 (histórico). Lo migramos a la fila correspondiente para
            // no romper el flujo de contingencia motivo 5/6/7 que ya funcionaba en ese PV.
            // El resto de PVs quedan con Cafc=NULL hasta que el SIN les emita uno propio.
            migrationBuilder.Sql(
                "UPDATE \"PuntosVentaSiat\" SET \"Cafc\" = '10135395A0C0C' " +
                "WHERE \"CodigoSucursal\" = 0 AND \"CodigoPuntoVenta\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cafc",
                table: "PuntosVentaSiat");
        }
    }
}
