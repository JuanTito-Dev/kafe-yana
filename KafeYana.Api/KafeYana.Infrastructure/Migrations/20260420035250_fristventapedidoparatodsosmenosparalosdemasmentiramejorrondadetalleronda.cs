using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fristventapedidoparatodsosmenosparalosdemasmentiramejorrondadetalleronda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_ronda_Ronda_Id_Ronda",
                table: "Detalle_ronda");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Detalle_ronda",
                table: "Detalle_ronda");

            migrationBuilder.RenameTable(
                name: "Detalle_ronda",
                newName: "Detalle_rondas");

            migrationBuilder.RenameIndex(
                name: "IX_Detalle_ronda_Id_Producto",
                table: "Detalle_rondas",
                newName: "IX_Detalle_rondas_Id_Producto");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Detalle_rondas",
                table: "Detalle_rondas",
                columns: new[] { "Id_Ronda", "Id_Producto" });

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_rondas_Ronda_Id_Ronda",
                table: "Detalle_rondas",
                column: "Id_Ronda",
                principalTable: "Ronda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_rondas_Ronda_Id_Ronda",
                table: "Detalle_rondas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Detalle_rondas",
                table: "Detalle_rondas");

            migrationBuilder.RenameTable(
                name: "Detalle_rondas",
                newName: "Detalle_ronda");

            migrationBuilder.RenameIndex(
                name: "IX_Detalle_rondas_Id_Producto",
                table: "Detalle_ronda",
                newName: "IX_Detalle_ronda_Id_Producto");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Detalle_ronda",
                table: "Detalle_ronda",
                columns: new[] { "Id_Ronda", "Id_Producto" });

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_ronda_Ronda_Id_Ronda",
                table: "Detalle_ronda",
                column: "Id_Ronda",
                principalTable: "Ronda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
