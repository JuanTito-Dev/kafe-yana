using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class comocuandodondequienporque123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistorialPuntos_Clientes_Id_Cliente",
                table: "HistorialPuntos");

            migrationBuilder.AddForeignKey(
                name: "fk_historialpuntos_cliente",
                table: "HistorialPuntos",
                column: "Id_Cliente",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_historialpuntos_cliente",
                table: "HistorialPuntos");

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialPuntos_Clientes_Id_Cliente",
                table: "HistorialPuntos",
                column: "Id_Cliente",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
