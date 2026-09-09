using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigosSiat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodigosSiat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoProducto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescripcionProducto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodigoActividad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescripcionActividad = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosSiat", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodigosSiat_CodigoActividad",
                table: "CodigosSiat",
                column: "CodigoActividad");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosSiat_CodigoProducto",
                table: "CodigosSiat",
                column: "CodigoProducto");

            migrationBuilder.CreateIndex(
                name: "UX_CodigosSiat_Producto_Actividad",
                table: "CodigosSiat",
                columns: new[] { "CodigoProducto", "CodigoActividad" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodigosSiat");
        }
    }
}
