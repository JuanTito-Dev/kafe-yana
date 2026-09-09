using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClienteCodigoFacturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Clientes"
                SET "Codigo" = UPPER(LEFT(TRIM("Nombre"), 1)) || '-' || LPAD("Id"::text, 4, '0')
                WHERE "Codigo" IS NULL OR "Codigo" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Codigo",
                table: "Clientes",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cliente_Codigo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Clientes");
        }
    }
}
