using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoSignificativoSiatToVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventoSignificativoSiatId",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EstadoSiat_EventoSignificativoSiatId",
                table: "Venta",
                columns: new[] { "EstadoSiat", "EventoSignificativoSiatId" });

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EventoSignificativoSiatId",
                table: "Venta",
                column: "EventoSignificativoSiatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Venta_EventosSignificativosSiat_EventoSignificativoSiatId",
                table: "Venta",
                column: "EventoSignificativoSiatId",
                principalTable: "EventosSignificativosSiat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venta_EventosSignificativosSiat_EventoSignificativoSiatId",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_EstadoSiat_EventoSignificativoSiatId",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_EventoSignificativoSiatId",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "EventoSignificativoSiatId",
                table: "Venta");
        }
    }
}
