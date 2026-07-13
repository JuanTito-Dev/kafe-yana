using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class comocuandodondequienporque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id_Cliente",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AceleradorPuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TipoAplicacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UmbralMonto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceleradorPuntos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    CodigoVenta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PuntosBase = table.Column<int>(type: "integer", nullable: false),
                    PuntosFinales = table.Column<int>(type: "integer", nullable: false),
                    Desglose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPuntos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPuntos_Clientes_Id_Cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReglaBasePuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cantidad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglaBasePuntos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceleradorPuntos_Tipo_Unique",
                table: "AceleradorPuntos",
                column: "Tipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPuntos_Cliente",
                table: "HistorialPuntos",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPuntos_CodigoVenta",
                table: "HistorialPuntos",
                column: "CodigoVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceleradorPuntos");

            migrationBuilder.DropTable(
                name: "HistorialPuntos");

            migrationBuilder.DropTable(
                name: "ReglaBasePuntos");

            migrationBuilder.DropColumn(
                name: "Id_Cliente",
                table: "Venta");
        }
    }
}
