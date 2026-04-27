using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class codigoreconoceaproductoenunpeidodedjalgdasdinsumoconrecetayerroresconfinalfeliz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.AddForeignKey(
                name: "detalle_ronda_id_opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion",
                principalTable: "Opcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "detalle_ronda_id_opcion",
                table: "Detalle_Ronda_Opcion");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion",
                principalTable: "Opcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
