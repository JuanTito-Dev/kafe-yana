using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaAjuste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotaAjuste",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NitEmisor = table.Column<long>(type: "bigint", nullable: false),
                    RazonSocialEmisor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Municipio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NumeroNotaCreditoDebito = table.Column<long>(type: "bigint", nullable: false),
                    Cuf = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cufd = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoTipoDocumentoIdentidad = table.Column<int>(type: "integer", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoCliente = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoDocumentoSector = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    Leyenda = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NombreRazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CodigoExcepcion = table.Column<int>(type: "integer", nullable: true),
                    NumeroFacturaOriginal = table.Column<long>(type: "bigint", nullable: false),
                    NumeroAutorizacionCuf = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaEmisionFactura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MontoTotalOriginal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoTotalDevuelto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoDescuentoCreditoDebito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoEfectivoCreditoDebito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CodigoMotivoAjuste = table.Column<int>(type: "integer", nullable: false),
                    TipoEmision = table.Column<int>(type: "integer", nullable: true),
                    EstadoSiat = table.Column<int>(type: "integer", nullable: true),
                    RevertidaAnulacion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CodigoRecepcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMensaje = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CodigoHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    XmlBase64 = table.Column<string>(type: "text", nullable: true),
                    IdVenta = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaAjuste", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaAjuste_Venta",
                        column: x => x.IdVenta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotaAjusteDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdNotaAjuste = table.Column<int>(type: "integer", nullable: false),
                    ActividadEconomica = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoProductoSin = table.Column<int>(type: "integer", nullable: false),
                    CodigoProducto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnidadMedida = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CodigoDetalleTransaccion = table.Column<int>(type: "integer", nullable: false),
                    IdDetallePagoOriginal = table.Column<int>(type: "integer", nullable: false),
                    NumeroLineaOriginal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaAjusteDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaAjusteDetalle_NotaAjuste",
                        column: x => x.IdNotaAjuste,
                        principalTable: "NotaAjuste",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotaAjuste_Cuf",
                table: "NotaAjuste",
                column: "Cuf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotaAjuste_EstadoSiat",
                table: "NotaAjuste",
                column: "EstadoSiat");

            migrationBuilder.CreateIndex(
                name: "IX_NotaAjuste_FechaEmision",
                table: "NotaAjuste",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_NotaAjuste_IdVenta",
                table: "NotaAjuste",
                column: "IdVenta");

            migrationBuilder.CreateIndex(
                name: "IX_NotaAjuste_NumeroNotaCreditoDebito",
                table: "NotaAjuste",
                column: "NumeroNotaCreditoDebito",
                unique: true,
                filter: "\"NumeroNotaCreditoDebito\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotaAjusteDetalle_IdDetallePagoOriginal",
                table: "NotaAjusteDetalle",
                column: "IdDetallePagoOriginal");

            migrationBuilder.CreateIndex(
                name: "IX_NotaAjusteDetalle_IdNotaAjuste",
                table: "NotaAjusteDetalle",
                column: "IdNotaAjuste");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotaAjusteDetalle");

            migrationBuilder.DropTable(
                name: "NotaAjuste");
        }
    }
}
