using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteNumeroComprasAndHistorialHitoCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroCompras",
                table: "Clientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HistorialHitoCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_HitoCompra = table.Column<int>(type: "integer", nullable: false),
                    NumeroComprasAlReclamar = table.Column<int>(type: "integer", nullable: false),
                    CodigoReclamo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialHitoCompra", x => x.Id);
                    table.ForeignKey(
                        name: "fk_historialhitocompra_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historialhitocompra_hitocompra",
                        column: x => x.Id_HitoCompra,
                        principalTable: "HitoCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialHitoCompra_Cliente",
                table: "HistorialHitoCompra",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialHitoCompra_Cliente_Hito",
                table: "HistorialHitoCompra",
                columns: new[] { "Id_Cliente", "Id_HitoCompra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialHitoCompra_Id_HitoCompra",
                table: "HistorialHitoCompra",
                column: "Id_HitoCompra");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialHitoCompra");

            migrationBuilder.DropColumn(
                name: "NumeroCompras",
                table: "Clientes");
        }
    }
}
