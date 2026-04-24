using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class proveedorfirstdemandaconcontactoydemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DNI = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    Celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_celular",
                table: "Proveedores",
                column: "Celular",
                unique: true,
                filter: "\"Celular\" != ''");

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_email",
                table: "Proveedores",
                column: "Email",
                unique: true,
                filter: "\"Email\" != ''");

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_razon_social",
                table: "Proveedores",
                column: "RazonSocial",
                unique: true,
                filter: "\"RazonSocial\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_proveedores_telefono",
                table: "Proveedores",
                column: "Telefono",
                unique: true,
                filter: "\"Telefono\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Proveedores");
        }
    }
}
