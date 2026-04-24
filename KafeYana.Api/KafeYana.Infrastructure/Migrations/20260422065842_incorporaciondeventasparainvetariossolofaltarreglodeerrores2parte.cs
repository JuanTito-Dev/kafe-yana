using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class incorporaciondeventasparainvetariossolofaltarreglodeerrores2parte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id_Opcion",
                table: "Detalle_rondas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "variacionId",
                table: "Detalle_rondas",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Venta",
                type: "text",
                nullable: false,
                computedColumnSql: "'VTA-' || CAST(\"Id\" AS VARCHAR)",
                stored: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_rondas_variacionId",
                table: "Detalle_rondas",
                column: "variacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_rondas_Variacion_variacionId",
                table: "Detalle_rondas",
                column: "variacionId",
                principalTable: "Variacion",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_rondas_Variacion_variacionId",
                table: "Detalle_rondas");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_rondas_variacionId",
                table: "Detalle_rondas");

            migrationBuilder.DropColumn(
                name: "Id_Opcion",
                table: "Detalle_rondas");

            migrationBuilder.DropColumn(
                name: "variacionId",
                table: "Detalle_rondas");

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Venta",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComputedColumnSql: "'VTA-' || CAST(\"Id\" AS VARCHAR)");
        }
    }
}
