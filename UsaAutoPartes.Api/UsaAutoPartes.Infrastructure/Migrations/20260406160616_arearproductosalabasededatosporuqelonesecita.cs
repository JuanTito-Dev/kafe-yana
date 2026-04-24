using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class arearproductosalabasededatosporuqelonesecita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    CodigoAux = table.Column<string>(type: "text", nullable: false),
                    CodigoAux2 = table.Column<string>(type: "text", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    Marca = table.Column<string>(type: "text", nullable: false),
                    Compatibilidad = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false),
                    Unidad_Medida = table.Column<string>(type: "text", nullable: false),
                    Ubicacion = table.Column<string>(type: "text", nullable: false),
                    Stock_Actual = table.Column<int>(type: "integer", nullable: false),
                    Stock_Minimo = table.Column<int>(type: "integer", nullable: false),
                    Costo = table.Column<double>(type: "double precision", precision: 10, scale: 2, nullable: false),
                    Precio = table.Column<double>(type: "double precision", precision: 10, scale: 2, nullable: false),
                    ConversionABs = table.Column<double>(type: "double precision", nullable: false),
                    Proveedor = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Codigo",
                table: "Producto",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoAux",
                table: "Producto",
                column: "CodigoAux",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoAux2",
                table: "Producto",
                column: "CodigoAux2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Nombre",
                table: "Producto",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Producto");
        }
    }
}
