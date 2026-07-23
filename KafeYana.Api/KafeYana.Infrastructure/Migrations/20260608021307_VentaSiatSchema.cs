using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VentaSiatSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detalle_venta");

            migrationBuilder.DropIndex(
                name: "Codigo-repetido",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_Cajero",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_Cliente",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_Estado",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Cajero",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Cliente",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Fecha",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "MontoDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PagoEfectivo",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PagoQr",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PagoTarjeta",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Productos",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "PorcentajeDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NombrePromocionDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Id_PromocionPermanenteDescuento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Id_Cliente",
                table: "Venta");

            migrationBuilder.AddColumn<int>(
                name: "CodigoPuntoVenta",
                table: "Venta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoEmision",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoRecepcion",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoSiat",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodigoExcepcion",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cafc",
                table: "Venta",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoCliente",
                table: "Venta",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CodigoDocumentoSector",
                table: "Venta",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "CodigoHash",
                table: "Venta",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodigoMetodoPago",
                table: "Venta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CodigoMoneda",
                table: "Venta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CodigoSucursal",
                table: "Venta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CodigoTipoDocumentoIdentidad",
                table: "Venta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Complemento",
                table: "Venta",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cuf",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cufd",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoAdicional",
                table: "Venta",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Venta",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMensaje",
                table: "Venta",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEmision",
                table: "Venta",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Leyenda",
                table: "Venta",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoGiftCard",
                table: "Venta",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTotal",
                table: "Venta",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTotalMoneda",
                table: "Venta",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTotalSujetoIva",
                table: "Venta",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "NitEmisor",
                table: "Venta",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "NombreRazonSocial",
                table: "Venta",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "Venta",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "NumeroFactura",
                table: "Venta",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "NumeroTarjeta",
                table: "Venta",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocialEmisor",
                table: "Venta",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Venta",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipoCambio",
                table: "Venta",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Usuario",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "XmlBase64",
                table: "Venta",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Detalle_Pago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_venta = table.Column<int>(type: "integer", nullable: false),
                    ActividadEconomica = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoProductoSin = table.Column<int>(type: "integer", nullable: false),
                    CodigoProducto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnidadMedida = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoDescuento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    NumeroSerie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NumeroImei = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Pago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallePago_Venta",
                        column: x => x.Id_venta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Ventas legacy reciben defaults vacíos/cero; asignar valores únicos antes del índice.
            migrationBuilder.Sql(
                """
                UPDATE "Venta"
                SET "Cuf" = 'LEGACY-CUF-' || "Id"::text
                WHERE "Cuf" = '' OR "Cuf" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Venta"
                SET "NumeroFactura" = "Id"
                WHERE "NumeroFactura" = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Cuf",
                table: "Venta",
                column: "Cuf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EstadoSiat",
                table: "Venta",
                column: "EstadoSiat");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_FechaEmision",
                table: "Venta",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta",
                column: "NumeroFactura",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Pago_Id_venta",
                table: "Detalle_Pago",
                column: "Id_venta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detalle_Pago");

            migrationBuilder.DropIndex(
                name: "IX_Venta_Cuf",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_EstadoSiat",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_FechaEmision",
                table: "Venta");

            migrationBuilder.DropIndex(
                name: "IX_Venta_NumeroFactura",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Cafc",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoCliente",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoDocumentoSector",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoHash",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoMetodoPago",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoMoneda",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoSucursal",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoTipoDocumentoIdentidad",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Complemento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Cuf",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Cufd",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "DescuentoAdicional",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "ErrorMensaje",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "FechaEmision",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Leyenda",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "MontoGiftCard",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "MontoTotal",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "MontoTotalMoneda",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "MontoTotalSujetoIva",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NitEmisor",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NombreRazonSocial",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NumeroFactura",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "NumeroTarjeta",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "RazonSocialEmisor",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "TipoCambio",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "Usuario",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "XmlBase64",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "TipoEmision",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "EstadoSiat",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoRecepcion",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoPuntoVenta",
                table: "Venta");

            migrationBuilder.DropColumn(
                name: "CodigoExcepcion",
                table: "Venta");

            migrationBuilder.AddColumn<string>(
                name: "Cajero",
                table: "Venta",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cliente",
                table: "Venta",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Venta",
                type: "text",
                nullable: false,
                defaultValue: "VTA-LEGACY");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Venta",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha",
                table: "Venta",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<int>(
                name: "Id_Cliente",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id_PromocionPermanenteDescuento",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoDescuento",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoEfectivo",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoQr",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagoTarjeta",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PorcentajeDescuento",
                table: "Venta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Productos",
                table: "Venta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NombrePromocionDescuento",
                table: "Venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Venta",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Detalle_venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_venta = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_venta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleVenta_Venta",
                        column: x => x.Id_venta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "Codigo-repetido",
                table: "Venta",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Cajero",
                table: "Venta",
                column: "Cajero");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Cliente",
                table: "Venta",
                column: "Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Estado",
                table: "Venta",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_venta_Id_venta",
                table: "Detalle_venta",
                column: "Id_venta");
        }
    }
}
