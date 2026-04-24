using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDetalleRondaOpcionUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Opcion_Detalle_Ronda_Detalle_RondaId",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_Opcion_Detalle_RondaId",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.DropColumn(
                name: "Detalle_RondaId",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Opcion_Detalle_Ronda_Id_Detalle_Ronda",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Detalle_Ronda",
                principalTable: "Detalle_Ronda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Opcion_Detalle_Ronda_Id_Detalle_Ronda",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.DropIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.AddColumn<int>(
                name: "Detalle_RondaId",
                table: "Detalle_Ronda_Opcion",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Detalle_RondaId",
                table: "Detalle_Ronda_Opcion",
                column: "Detalle_RondaId");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Opcion_Detalle_Ronda_Detalle_RondaId",
                table: "Detalle_Ronda_Opcion",
                column: "Detalle_RondaId",
                principalTable: "Detalle_Ronda",
                principalColumn: "Id");
        }
    }
}
