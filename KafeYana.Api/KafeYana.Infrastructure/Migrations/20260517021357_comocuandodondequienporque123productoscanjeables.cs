using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class comocuandodondequienporque123productoscanjeables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductoCanjeable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    NombreProducto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    Disponible = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoCanjeable", x => x.Id);
                    table.ForeignKey(
                        name: "fk_productocanjeable_producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoCanjeable_Activo",
                table: "ProductoCanjeable",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoCanjeable_Disponible",
                table: "ProductoCanjeable",
                column: "Disponible");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoCanjeable_Producto_Unique",
                table: "ProductoCanjeable",
                column: "Id_Producto",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductoCanjeable");
        }
    }
}
