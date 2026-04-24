using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class proveedorfirstdemandaconcontactoydemasgteutarcondetallerondaopciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_rondas_Ronda_Id_Ronda",
                table: "Detalle_rondas");

            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_rondas_Variacion_variacionId",
                table: "Detalle_rondas");

            migrationBuilder.DropForeignKey(
                name: "Producto asociado a una venta",
                table: "Detalle_rondas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Detalle_rondas",
                table: "Detalle_rondas");

            migrationBuilder.DropColumn(
                name: "Id_Opcion",
                table: "Detalle_rondas");

            migrationBuilder.RenameTable(
                name: "Detalle_rondas",
                newName: "Detalle_Ronda");

            migrationBuilder.RenameColumn(
                name: "variacionId",
                table: "Detalle_Ronda",
                newName: "ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_Detalle_rondas_variacionId",
                table: "Detalle_Ronda",
                newName: "IX_Detalle_Ronda_ProductoId");

            migrationBuilder.RenameIndex(
                name: "IX_Detalle_rondas_Id_Producto",
                table: "Detalle_Ronda",
                newName: "IX_Detalle_Ronda_Id_Producto");

            migrationBuilder.AlterColumn<decimal>(
                name: "Precio",
                table: "Detalle_Ronda",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre_Producto",
                table: "Detalle_Ronda",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Detalle_Ronda",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Detalle_Ronda",
                table: "Detalle_Ronda",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Detalle_Ronda_Opcion",
                columns: table => new
                {
                    Id_Detalle_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Id_Opcion = table.Column<int>(type: "integer", nullable: false),
                    Detalle_RondaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Ronda_Opcion", x => new { x.Id_Detalle_Ronda, x.Id_Opcion });
                    table.ForeignKey(
                        name: "FK_Detalle_Ronda_Opcion_Detalle_Ronda_Detalle_RondaId",
                        column: x => x.Detalle_RondaId,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion",
                        column: x => x.Id_Opcion,
                        principalTable: "Opcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_detalle_ronda_ronda_producto_unique",
                table: "Detalle_Ronda",
                columns: new[] { "Id_Ronda", "Id_Producto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Detalle_RondaId",
                table: "Detalle_Ronda_Opcion",
                column: "Detalle_RondaId");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_detalle_ronda_opcion_unique",
                table: "Detalle_Ronda_Opcion",
                columns: new[] { "Id_Detalle_Ronda", "Id_Opcion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Producto_Id_Producto",
                table: "Detalle_Ronda",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Producto_ProductoId",
                table: "Detalle_Ronda",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_Ronda_Ronda_Id_Ronda",
                table: "Detalle_Ronda",
                column: "Id_Ronda",
                principalTable: "Ronda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Producto_Id_Producto",
                table: "Detalle_Ronda");

            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Producto_ProductoId",
                table: "Detalle_Ronda");

            migrationBuilder.DropForeignKey(
                name: "FK_Detalle_Ronda_Ronda_Id_Ronda",
                table: "Detalle_Ronda");

            migrationBuilder.DropTable(
                name: "Detalle_Ronda_Opcion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Detalle_Ronda",
                table: "Detalle_Ronda");

            migrationBuilder.DropIndex(
                name: "ix_detalle_ronda_ronda_producto_unique",
                table: "Detalle_Ronda");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Detalle_Ronda");

            migrationBuilder.RenameTable(
                name: "Detalle_Ronda",
                newName: "Detalle_rondas");

            migrationBuilder.RenameColumn(
                name: "ProductoId",
                table: "Detalle_rondas",
                newName: "variacionId");

            migrationBuilder.RenameIndex(
                name: "IX_Detalle_Ronda_ProductoId",
                table: "Detalle_rondas",
                newName: "IX_Detalle_rondas_variacionId");

            migrationBuilder.RenameIndex(
                name: "IX_Detalle_Ronda_Id_Producto",
                table: "Detalle_rondas",
                newName: "IX_Detalle_rondas_Id_Producto");

            migrationBuilder.AlterColumn<decimal>(
                name: "Precio",
                table: "Detalle_rondas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldDefaultValue: 0.00m);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre_Producto",
                table: "Detalle_rondas",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "Id_Opcion",
                table: "Detalle_rondas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Detalle_rondas",
                table: "Detalle_rondas",
                columns: new[] { "Id_Ronda", "Id_Producto" });

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_rondas_Ronda_Id_Ronda",
                table: "Detalle_rondas",
                column: "Id_Ronda",
                principalTable: "Ronda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Detalle_rondas_Variacion_variacionId",
                table: "Detalle_rondas",
                column: "variacionId",
                principalTable: "Variacion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Producto asociado a una venta",
                table: "Detalle_rondas",
                column: "Id_Producto",
                principalTable: "Producto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
