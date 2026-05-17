using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tuyelquehacelascompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromocionPermanente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    TipoCondicion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorCondicion = table.Column<int>(type: "integer", nullable: false),
                    TipoRecompensa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorRecompensa = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Id_ProductoCanjeable = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionPermanente", x => x.Id);
                    table.ForeignKey(
                        name: "fk_promocionpermanente_productocanjeable",
                        column: x => x.Id_ProductoCanjeable,
                        principalTable: "ProductoCanjeable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanente_Activo",
                table: "PromocionPermanente",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanente_Id_ProductoCanjeable",
                table: "PromocionPermanente",
                column: "Id_ProductoCanjeable");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanente_Nombre_Unique",
                table: "PromocionPermanente",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanente_TipoCondicion",
                table: "PromocionPermanente",
                column: "TipoCondicion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromocionPermanente");
        }
    }
}
