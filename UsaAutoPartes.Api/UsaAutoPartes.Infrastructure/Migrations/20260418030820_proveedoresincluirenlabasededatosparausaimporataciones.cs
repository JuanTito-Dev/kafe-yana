using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UsaAutoPartes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class proveedoresincluirenlabasededatosparausaimporataciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Pais = table.Column<string>(type: "text", nullable: false),
                    Moneda = table.Column<string>(type: "text", nullable: false),
                    Terminos = table.Column<string>(type: "text", nullable: false),
                    Nombre_Contacto = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: false),
                    TiempoReposicion = table.Column<int>(type: "integer", nullable: false),
                    SitioWeb = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Nota = table.Column<string>(type: "text", nullable: false),
                    Importaciones = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedor", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_Estado",
                table: "Proveedor",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_Id",
                table: "Proveedor",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_Nombre",
                table: "Proveedor",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Proveedor");
        }
    }
}
