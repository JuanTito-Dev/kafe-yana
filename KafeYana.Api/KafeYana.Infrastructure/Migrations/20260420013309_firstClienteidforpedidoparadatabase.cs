using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class firstClienteidforpedidoparadatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id_Pedido",
                table: "Mesa",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedido_Clientes_Id_Cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mesa_Id_Pedido",
                table: "Mesa",
                column: "Id_Pedido",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Id",
                table: "Pedido",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Id_Cliente",
                table: "Pedido",
                column: "Id_Cliente",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Mesa_Pedido_Id_Pedido",
                table: "Mesa",
                column: "Id_Pedido",
                principalTable: "Pedido",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mesa_Pedido_Id_Pedido",
                table: "Mesa");

            migrationBuilder.DropTable(
                name: "Pedido");

            migrationBuilder.DropIndex(
                name: "IX_Mesa_Id_Pedido",
                table: "Mesa");

            migrationBuilder.DropColumn(
                name: "Id_Pedido",
                table: "Mesa");
        }
    }
}
