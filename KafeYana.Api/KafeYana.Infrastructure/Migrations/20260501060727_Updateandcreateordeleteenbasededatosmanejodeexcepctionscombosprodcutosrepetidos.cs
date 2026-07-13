using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updateandcreateordeleteenbasededatosmanejodeexcepctionscombosprodcutosrepetidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Detalle_promocion",
                table: "Detalle_promocion");

            migrationBuilder.AddPrimaryKey(
                name: "pk_producto_promocion",
                table: "Detalle_promocion",
                columns: new[] { "Id_Producto", "Id_Promocion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_producto_promocion",
                table: "Detalle_promocion");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Detalle_promocion",
                table: "Detalle_promocion",
                columns: new[] { "Id_Producto", "Id_Promocion" });
        }
    }
}
