using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteIdPaisOrigen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdPaisOrigen",
                table: "Clientes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_IdPaisOrigen",
                table: "Clientes",
                column: "IdPaisOrigen");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_CatPaisesOrigen_IdPaisOrigen",
                table: "Clientes",
                column: "IdPaisOrigen",
                principalTable: "CatPaisesOrigen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_CatPaisesOrigen_IdPaisOrigen",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_IdPaisOrigen",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "IdPaisOrigen",
                table: "Clientes");
        }
    }
}
