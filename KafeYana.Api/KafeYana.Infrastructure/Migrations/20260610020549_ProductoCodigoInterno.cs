using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductoCodigoInterno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Producto",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Producto"
                SET "Codigo" = LPAD("Id"::text, 5, '0')
                WHERE "Codigo" IS NULL OR "Codigo" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Producto",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Codigo",
                table: "Producto",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Producto_Codigo",
                table: "Producto");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Producto");
        }
    }
}
