using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Caja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalVentas = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalEfectivo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalTarjeta = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalQr = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalIngresos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalEgresos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    Abierta = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaApertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AbiertaPor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CerradaPor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CajaHistorial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Apertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalIngresos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalEgresos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalVentas = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalEfectivo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalTarjeta = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    TotalQr = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m),
                    Diferencia = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: false),
                    AbiertaPor = table.Column<string>(type: "text", nullable: false),
                    CerradaPor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaHistorial", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatActividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoCaeb = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TipoActividad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatActividades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatActividadesDocumentosSector",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoActividad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodigoDocumentoSector = table.Column<int>(type: "integer", nullable: false),
                    TipoDocumentoSector = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatActividadesDocumentosSector", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatDocumentosSector",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoClasificador = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatDocumentosSector", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    Estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Color = table.Column<string>(type: "char(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatEventosSignificativos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatEventosSignificativos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatLeyendas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoActividad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescripcionLeyenda = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatLeyendas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatMetodosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatMetodosPago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatMotivosAnulacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatMotivosAnulacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatPaisesOrigen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatPaisesOrigen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatTiposDocumentoIdentidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatTiposDocumentoIdentidad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatTiposEmision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatTiposEmision", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatUnidadesMedida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatUnidadesMedida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodigosSiat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoProducto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescripcionProducto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CodigoActividad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescripcionActividad = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosSiat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionQr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionQr", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cufd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoControl = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    FechaEmisionSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cufd", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cuis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventosSignificativosSiat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoMotivo = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaHoraInicioEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraFinEvento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CodigoAmbiente = table.Column<int>(type: "integer", nullable: false),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    CodigoSistema = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nit = table.Column<long>(type: "bigint", nullable: false),
                    Cufd = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CufdEvento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoControlEvento = table.Column<string>(type: "text", nullable: true),
                    Cuis = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoRecepcionEventoSignificativo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Transaccion = table.Column<bool>(type: "boolean", nullable: false),
                    CodigosRespuestaJson = table.Column<string>(type: "text", nullable: false),
                    Origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EstadoContingencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosSignificativosSiat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistorialReferido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreReferidor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NombreReferido = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PuntosReferidor = table.Column<int>(type: "integer", nullable: false),
                    PuntosReferido = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialReferido", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Insumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unidad_min_uso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Unidad_compra = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Factor_conversion = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Stock_actual = table.Column<int>(type: "integer", nullable: false),
                    Stock_min = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insumo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromocionTemporada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionTemporada", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "PuntosVentaSiat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UltimaSyncActividades = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncMotivoAnulacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncActividadesDocumentoSector = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncLeyendas = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncCodigosSiat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncEventosSignificativos = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncPaisOrigen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncTipoDocumentoIdentidad = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncTipoEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncMetodoPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimaSyncUnidadMedida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Cafc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuntosVentaSiat", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferidosConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PuntosReferidor = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PuntosReferido = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferidosConfig", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "stock_ajuste",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Ajuste = table.Column<int>(type: "integer", nullable: false),
                    StockAnterior = table.Column<int>(type: "integer", nullable: false),
                    StockNuevo = table.Column<int>(type: "integer", nullable: false),
                    Perdida = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: false),
                    Usuario = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_ajuste", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CajaMovimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Caja = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Nota = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaMovimientos", x => x.Id);
                    table.ForeignKey(
                        name: "fk_cajamovimiento_caja",
                        column: x => x.Id_Caja,
                        principalTable: "Caja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CajaHistorialMovimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_CajaHistorial = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaHistorialMovimientos", x => x.Id);
                    table.ForeignKey(
                        name: "fk_cajahistorialmovimiento_cajahistorial",
                        column: x => x.Id_CajaHistorial,
                        principalTable: "CajaHistorial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: ""),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UrlImagen = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CodigoSin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    Categoria_Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.Id);
                    table.ForeignKey(
                        name: "fk_productos_categoria",
                        column: x => x.Categoria_Id,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dni = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Correo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Correonormalizado = table.Column<string>(type: "text", nullable: true),
                    Fecha_nacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Puntos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NumeroCompras = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IdPaisOrigen = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.CheckConstraint("CK_Cliente_Puntos_NonNegative", "\"Puntos\" >= 0");
                    table.ForeignKey(
                        name: "FK_Clientes_CatPaisesOrigen_IdPaisOrigen",
                        column: x => x.IdPaisOrigen,
                        principalTable: "CatPaisesOrigen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NitEmisor = table.Column<long>(type: "bigint", nullable: false),
                    RazonSocialEmisor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Municipio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NumeroFactura = table.Column<long>(type: "bigint", nullable: true),
                    Facturado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Cuf = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Cufd = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodigoSucursal = table.Column<int>(type: "integer", nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoTipoDocumentoIdentidad = table.Column<int>(type: "integer", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoCliente = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CodigoMetodoPago = table.Column<int>(type: "integer", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoTotalSujetoIva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CodigoMoneda = table.Column<int>(type: "integer", nullable: false),
                    TipoCambio = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    MontoTotalMoneda = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Leyenda = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoDocumentoSector = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CodigoPuntoVenta = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NombreRazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Complemento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NumeroTarjeta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MontoGiftCard = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DescuentoAdicional = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CodigoExcepcion = table.Column<int>(type: "integer", nullable: true),
                    Cafc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TipoEmision = table.Column<int>(type: "integer", nullable: true),
                    EventoSignificativoSiatId = table.Column<int>(type: "integer", nullable: true),
                    EstadoSiat = table.Column<int>(type: "integer", nullable: true),
                    RevertidaAnulacion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CodigoRecepcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMensaje = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CodigoHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    XmlBase64 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Venta_EventosSignificativosSiat_EventoSignificativoSiatId",
                        column: x => x.EventoSignificativoSiatId,
                        principalTable: "EventosSignificativosSiat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsumoMovimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_insumo = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Costo_Unitario = table.Column<decimal>(type: "numeric(14,7)", precision: 14, scale: 7, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(14,7)", precision: 14, scale: 7, nullable: false),
                    Stock_resultante = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsumoMovimiento", x => x.Id);
                    table.ForeignKey(
                        name: "fx_insumo_movimiento",
                        column: x => x.Id_insumo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nombre_Proveedor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Recibido = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Id_Proveedor = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenCompra", x => x.Id);
                    table.ForeignKey(
                        name: "fk_ordencompra_proveedor",
                        column: x => x.Id_Proveedor,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refreshtoken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiraEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refreshtoken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refreshtoken_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comprado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo_barra = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Unidad_medida = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodigoUnidadMedida = table.Column<int>(type: "integer", nullable: false),
                    Marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Costo_compra = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Stock_actual = table.Column<int>(type: "integer", nullable: false),
                    Stock_minimo = table.Column<int>(type: "integer", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprado", x => x.Id);
                    table.ForeignKey(
                        name: "fx_producto_comprado",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Elaborado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Unidad_medida = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodigoUnidadMedida = table.Column<int>(type: "integer", nullable: false),
                    Producible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Stock_actual = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Ubicacion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elaborado", x => x.Id);
                    table.ForeignKey(
                        name: "fx_producto_elaborado",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Movimiento_Producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Costo_Unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Stock_resultante = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimiento_Producto", x => x.Id);
                    table.ForeignKey(
                        name: "fx_producto_movimientos",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductoCanjeable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    NombreProducto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    Disponible = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoCanjeable", x => x.Id);
                    table.ForeignKey(
                        name: "fk_productocanjeable_producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Promocion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Producto_Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promocion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Promocion_Producto_Producto_Id",
                        column: x => x.Producto_Id,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPromocionTemporada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_PromocionTemporada = table.Column<int>(type: "integer", nullable: false),
                    CodigoReclamo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPromocionTemporada", x => x.Id);
                    table.ForeignKey(
                        name: "fk_historialpromociontemporada_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historialpromociontemporada_promocion",
                        column: x => x.Id_PromocionTemporada,
                        principalTable: "PromocionTemporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    CodigoVenta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PuntosBase = table.Column<int>(type: "integer", nullable: false),
                    PuntosFinales = table.Column<int>(type: "integer", nullable: false),
                    Desglose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPuntos", x => x.Id);
                    table.ForeignKey(
                        name: "fk_historialpuntos_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0.00m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedido", x => x.Id);
                    table.ForeignKey(
                        name: "fx_pedido_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                    DescuentoAdicional = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MontoTotalDevuelto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoDescuentoCreditoDebito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoEfectivoCreditoDebito = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CodigoMotivoAjuste = table.Column<int>(type: "integer", nullable: false),
                    TipoEmision = table.Column<int>(type: "integer", nullable: true),
                    EstadoSiat = table.Column<int>(type: "integer", nullable: true),
                    RevertidaAnulacion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaAnulacionSiat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CodigoRecepcion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorMensaje = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CodigoHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    XmlBase64 = table.Column<string>(type: "text", nullable: true),
                    EventoSignificativoSiatId = table.Column<int>(type: "integer", nullable: true),
                    IdVenta = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaAjuste", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaAjuste_EventoSignificativoSiat_EventoSignificativoSiatId",
                        column: x => x.EventoSignificativoSiatId,
                        principalTable: "EventosSignificativosSiat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotaAjuste_Venta",
                        column: x => x.IdVenta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VentaPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdVenta = table.Column<int>(type: "integer", nullable: false),
                    CodigoMetodoPago = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VentaPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VentaPagos_Venta_IdVenta",
                        column: x => x.IdVenta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenItemInsumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Orden = table.Column<int>(type: "integer", nullable: false),
                    Id_Insumo = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenItemInsumo", x => x.Id);
                    table.ForeignKey(
                        name: "fk_ordeniteminsumo_insumo",
                        column: x => x.Id_Insumo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordeniteminsumo_orden",
                        column: x => x.Id_Orden,
                        principalTable: "OrdenCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenItemProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Orden = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenItemProducto", x => x.Id);
                    table.ForeignKey(
                        name: "fk_ordenitemproducto_orden",
                        column: x => x.Id_Orden,
                        principalTable: "OrdenCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ordenitemproducto_producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Receta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Porciones = table.Column<int>(type: "integer", nullable: false),
                    Nota = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, defaultValue: ""),
                    Id_Elaborado = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receta", x => x.Id);
                    table.ForeignKey(
                        name: "fx_elaborado_receta",
                        column: x => x.Id_Elaborado,
                        principalTable: "Elaborado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Variacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Requerido = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Id_Elaborado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variacion", x => x.Id);
                    table.ForeignKey(
                        name: "fx_variacion_elaborado",
                        column: x => x.Id_Elaborado,
                        principalTable: "Elaborado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HitoCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroCompras = table.Column<int>(type: "integer", nullable: false),
                    Id_ProductoCanjeable = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icono = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HitoCompra", x => x.Id);
                    table.ForeignKey(
                        name: "fk_hitocompra_productocanjeable",
                        column: x => x.Id_ProductoCanjeable,
                        principalTable: "ProductoCanjeable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "PromocionTemporada_ProductoCanjeable",
                columns: table => new
                {
                    Id_PromocionTemporada = table.Column<int>(type: "integer", nullable: false),
                    Id_ProductoCanjeable = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promociontemporada_productocanjeable", x => new { x.Id_PromocionTemporada, x.Id_ProductoCanjeable });
                    table.ForeignKey(
                        name: "fk_promtemp_productocanjeable",
                        column: x => x.Id_ProductoCanjeable,
                        principalTable: "ProductoCanjeable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_promtemp_promociontemporada",
                        column: x => x.Id_PromocionTemporada,
                        principalTable: "PromocionTemporada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_promocion",
                columns: table => new
                {
                    Id_Promocion = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Opcional = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producto_promocion", x => new { x.Id_Producto, x.Id_Promocion });
                    table.ForeignKey(
                        name: "fx",
                        column: x => x.Id_Promocion,
                        principalTable: "Promocion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fx_detallecombo_producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: true),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesa", x => x.Id);
                    table.ForeignKey(
                        name: "fx_pedido_mesa",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Parallevar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parallevar", x => x.Id);
                    table.ForeignKey(
                        name: "fx_pedido_parallevar",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Ronda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Ronda_Descripcion = table.Column<string>(type: "text", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ronda", x => x.Id);
                    table.ForeignKey(
                        name: "fx_pedido_ronda",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CodigoMetodoPago = table.Column<int>(type: "integer", nullable: false),
                    EsPagoFinal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Cajero = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Facturada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Id_Venta = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubVenta", x => x.Id);
                    table.ForeignKey(
                        name: "fk_subventa_pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subventa_venta",
                        column: x => x.Id_Venta,
                        principalTable: "Venta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                    NumeroLineaOriginal = table.Column<int>(type: "integer", nullable: false),
                    NroItem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
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

            migrationBuilder.CreateTable(
                name: "Detalle",
                columns: table => new
                {
                    Id_insumo = table.Column<int>(type: "integer", nullable: false),
                    Id_receta = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Merma = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle", x => new { x.Id_receta, x.Id_insumo });
                    table.ForeignKey(
                        name: "fx_detalle_insumo",
                        column: x => x.Id_insumo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fx_detalle_receta",
                        column: x => x.Id_receta,
                        principalTable: "Receta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Opcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    AjustePrecio = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TipoOpcion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "normal"),
                    ValorAnterior = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Id_variacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opcion", x => x.Id);
                    table.ForeignKey(
                        name: "fx_opcion_variacion",
                        column: x => x.Id_variacion,
                        principalTable: "Variacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "HistorialPromocionPermanente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_PromocionPermanente = table.Column<int>(type: "integer", nullable: false),
                    CodigoVenta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TipoRecompensa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorRecompensa = table.Column<int>(type: "integer", nullable: false),
                    TipoCondicion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorCondicion = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPromocionPermanente", x => x.Id);
                    table.ForeignKey(
                        name: "fk_historialpromocionpermanente_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historialpromocionpermanente_promocion",
                        column: x => x.Id_PromocionPermanente,
                        principalTable: "PromocionPermanente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromocionPermanenteProgreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Cliente = table.Column<int>(type: "integer", nullable: false),
                    Id_PromocionPermanente = table.Column<int>(type: "integer", nullable: false),
                    ContadorCompras = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ReclamoMontoMinimoPendiente = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionPermanenteProgreso", x => x.Id);
                    table.ForeignKey(
                        name: "fk_promocionpermanenteprogreso_cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_promocionpermanenteprogreso_promocion",
                        column: x => x.Id_PromocionPermanente,
                        principalTable: "PromocionPermanente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_Ronda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Nombre_Producto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0.00m),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: ""),
                    CodigoSin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    CodigoUnidadMedida = table.Column<int>(type: "integer", nullable: false, defaultValue: 57),
                    CantidadDescontada = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Ronda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Detalle_Ronda_Ronda_Id_Ronda",
                        column: x => x.Id_Ronda,
                        principalTable: "Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_detalle_ronda_producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubVentaDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_SubVenta = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Nombre_Producto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: ""),
                    CodigoSin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    CodigoUnidadMedida = table.Column<int>(type: "integer", nullable: false, defaultValue: 57),
                    OrigenRondaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubVentaDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "fk_subventadetalle_subventa",
                        column: x => x.Id_SubVenta,
                        principalTable: "SubVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ajuste",
                columns: table => new
                {
                    Id_Opcion = table.Column<int>(type: "integer", nullable: false),
                    Id_Insumo = table.Column<int>(type: "integer", nullable: false),
                    Id_InsumoNuevo = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TipoAjuste = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ajuste", x => new { x.Id_Insumo, x.Id_Opcion });
                    table.ForeignKey(
                        name: "fx_ajuste_insumoNevo",
                        column: x => x.Id_InsumoNuevo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fx_ajuste_insumobase",
                        column: x => x.Id_Insumo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fx_ajuste_opcion",
                        column: x => x.Id_Opcion,
                        principalTable: "Opcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_Ronda_Combo_Item",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Detalle_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Ronda_Combo_Item", x => x.Id);
                    table.ForeignKey(
                        name: "fk_detallerondacomboitem_detalleronda",
                        column: x => x.Id_Detalle_Ronda,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_detallerondacomboitem_producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_Ronda_Opcion",
                columns: table => new
                {
                    Id_Detalle_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Id_Opcion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Ronda_Opcion", x => new { x.Id_Detalle_Ronda, x.Id_Opcion });
                    table.ForeignKey(
                        name: "fk_detallerondaopcion_detalleronda",
                        column: x => x.Id_Detalle_Ronda,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_detallerondaopcion_opcion",
                        column: x => x.Id_Opcion,
                        principalTable: "Opcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoInventarioComprometido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Pedido = table.Column<int>(type: "integer", nullable: false),
                    Id_Detalle_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoInventarioComprometido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoInventarioComprometido_Detalle_Ronda_Id_Detalle_Ronda",
                        column: x => x.Id_Detalle_Ronda,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoInventarioComprometidoLinea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_Comprometido = table.Column<int>(type: "integer", nullable: false),
                    TipoEntidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: true),
                    Id_Insumo = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoInventarioComprometidoLinea", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoInventarioComprometidoLinea_PedidoInventarioCompromet~",
                        column: x => x.Id_Comprometido,
                        principalTable: "PedidoInventarioComprometido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceleradorPuntos_Tipo_Unique",
                table: "AceleradorPuntos",
                column: "Tipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ajuste_Id_Insumo",
                table: "Ajuste",
                column: "Id_Insumo");

            migrationBuilder.CreateIndex(
                name: "IX_Ajuste_Id_InsumoNuevo",
                table: "Ajuste",
                column: "Id_InsumoNuevo");

            migrationBuilder.CreateIndex(
                name: "IX_Ajuste_Id_Opcion",
                table: "Ajuste",
                column: "Id_Opcion");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CajaHistorialMovimientos_Id_CajaHistorial",
                table: "CajaHistorialMovimientos",
                column: "Id_CajaHistorial");

            migrationBuilder.CreateIndex(
                name: "IX_CajaMovimientos_Id_Caja",
                table: "CajaMovimientos",
                column: "Id_Caja");

            migrationBuilder.CreateIndex(
                name: "IX_CatActividades_CodigoCaeb",
                table: "CatActividades",
                column: "CodigoCaeb",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatActividadesDocumentosSector_CodigoActividad_CodigoDocumentoSector",
                table: "CatActividadesDocumentosSector",
                columns: new[] { "CodigoActividad", "CodigoDocumentoSector" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatDocumentosSector_CodigoClasificador",
                table: "CatDocumentosSector",
                column: "CodigoClasificador",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categorias_nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatEventosSignificativos_Codigo",
                table: "CatEventosSignificativos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatLeyendas_CodigoActividad_DescripcionLeyenda",
                table: "CatLeyendas",
                columns: new[] { "CodigoActividad", "DescripcionLeyenda" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatMetodosPago_Codigo",
                table: "CatMetodosPago",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatMotivosAnulacion_Codigo",
                table: "CatMotivosAnulacion",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatPaisesOrigen_Codigo",
                table: "CatPaisesOrigen",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatTiposDocumentoIdentidad_Codigo",
                table: "CatTiposDocumentoIdentidad",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatTiposEmision_Codigo",
                table: "CatTiposEmision",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatUnidadesMedida_Codigo",
                table: "CatUnidadesMedida",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Codigo",
                table: "Clientes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correonormalizado",
                table: "Clientes",
                column: "Correonormalizado",
                unique: true,
                filter: "\"Correonormalizado\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_IdPaisOrigen",
                table: "Clientes",
                column: "IdPaisOrigen");

            migrationBuilder.CreateIndex(
                name: "Unique_celular_cliente",
                table: "Clientes",
                column: "Celular",
                unique: true,
                filter: "\"Celular\" IS NOT NULL AND \"Celular\" <> ''");

            migrationBuilder.CreateIndex(
                name: "Unique_correo_cliente",
                table: "Clientes",
                column: "Correo",
                unique: true,
                filter: "\"Correo\" <> ''");

            migrationBuilder.CreateIndex(
                name: "Unique_Dni_cliente",
                table: "Clientes",
                column: "Dni",
                unique: true,
                filter: "\"Dni\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "unique_nombre_cliente",
                table: "Clientes",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodigosSiat_CodigoActividad",
                table: "CodigosSiat",
                column: "CodigoActividad");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosSiat_CodigoProducto",
                table: "CodigosSiat",
                column: "CodigoProducto");

            migrationBuilder.CreateIndex(
                name: "codigo_producto_comprado",
                table: "Comprado",
                column: "Codigo_barra",
                unique: true,
                filter: "\"Codigo_barra\" IS NOT NULL AND \"Codigo_barra\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Comprado_Disponible",
                table: "Comprado",
                column: "Disponible");

            migrationBuilder.CreateIndex(
                name: "IX_Comprado_Id_Producto",
                table: "Comprado",
                column: "Id_Producto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cufd_SucursalPuntoVenta",
                table: "Cufd",
                columns: new[] { "CodigoSucursal", "CodigoPuntoVenta" });

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Id_insumo",
                table: "Detalle",
                column: "Id_insumo");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Pago_Id_venta",
                table: "Detalle_Pago",
                column: "Id_venta");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_promocion_Id_Promocion",
                table: "Detalle_promocion",
                column: "Id_Promocion");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Id_Producto",
                table: "Detalle_Ronda",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Id_Ronda",
                table: "Detalle_Ronda",
                column: "Id_Ronda");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Combo_Item_Id_Detalle_Ronda",
                table: "Detalle_Ronda_Combo_Item",
                column: "Id_Detalle_Ronda");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Combo_Item_Id_Producto",
                table: "Detalle_Ronda_Combo_Item",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion");

            migrationBuilder.CreateIndex(
                name: "IX_Elaborado_Id_Producto",
                table: "Elaborado",
                column: "Id_Producto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosSignificativosSiat_CodigoRecepcion",
                table: "EventosSignificativosSiat",
                column: "CodigoRecepcionEventoSignificativo",
                unique: true,
                filter: "\"CodigoRecepcionEventoSignificativo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventosSignificativosSiat_Estado_FechaRegistro",
                table: "EventosSignificativosSiat",
                columns: new[] { "EstadoContingencia", "FechaRegistro" });

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

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Cliente",
                table: "HistorialPromocionPermanente",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Id_PromocionPermanente",
                table: "HistorialPromocionPermanente",
                column: "Id_PromocionPermanente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionPermanente_Venta_TipoRecompensa",
                table: "HistorialPromocionPermanente",
                columns: new[] { "CodigoVenta", "TipoRecompensa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionTemporada_Cliente",
                table: "HistorialPromocionTemporada",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionTemporada_Cliente_Promocion",
                table: "HistorialPromocionTemporada",
                columns: new[] { "Id_Cliente", "Id_PromocionTemporada" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPromocionTemporada_Id_PromocionTemporada",
                table: "HistorialPromocionTemporada",
                column: "Id_PromocionTemporada");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPuntos_Cliente",
                table: "HistorialPuntos",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPuntos_CodigoVenta",
                table: "HistorialPuntos",
                column: "CodigoVenta");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialReferido_Fecha",
                table: "HistorialReferido",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_HitoCompra_Activo",
                table: "HitoCompra",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_HitoCompra_Id_ProductoCanjeable",
                table: "HitoCompra",
                column: "Id_ProductoCanjeable");

            migrationBuilder.CreateIndex(
                name: "IX_HitoCompra_NumeroCompras_Unique",
                table: "HitoCompra",
                column: "NumeroCompras",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_Categoria",
                table: "Insumo",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_Id",
                table: "Insumo",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "nombre_insumo",
                table: "Insumo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsumoMovimiento_Id_insumo",
                table: "InsumoMovimiento",
                column: "Id_insumo");

            migrationBuilder.CreateIndex(
                name: "IX_Mesa_Id_Pedido",
                table: "Mesa",
                column: "Id_Pedido",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Unique_mesa_nombre",
                table: "Mesa",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movimiento_Producto_Id_Producto",
                table: "Movimiento_Producto",
                column: "Id_Producto");

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
                name: "IX_NotaAjuste_EventoSignificativoSiatId",
                table: "NotaAjuste",
                column: "EventoSignificativoSiatId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_Id",
                table: "Opcion",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_Id_variacion",
                table: "Opcion",
                column: "Id_variacion");

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_Nombre",
                table: "Opcion",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompra_Id_Proveedor",
                table: "OrdenCompra",
                column: "Id_Proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItemInsumo_Id_Insumo",
                table: "OrdenItemInsumo",
                column: "Id_Insumo");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItemInsumo_Id_Orden",
                table: "OrdenItemInsumo",
                column: "Id_Orden");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItemProducto_Id_Orden",
                table: "OrdenItemProducto",
                column: "Id_Orden");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItemProducto_Id_Producto",
                table: "OrdenItemProducto",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "IX_Parallevar_Id_Pedido",
                table: "Parallevar",
                column: "Id_Pedido",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Id",
                table: "Pedido",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Id_Cliente",
                table: "Pedido",
                column: "Id_Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoInventarioComprometido_Id_Detalle_Ronda",
                table: "PedidoInventarioComprometido",
                column: "Id_Detalle_Ronda",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoInventarioComprometidoLinea_Id_Comprometido",
                table: "PedidoInventarioComprometidoLinea",
                column: "Id_Comprometido");

            migrationBuilder.CreateIndex(
                name: "id_nombre_producto",
                table: "Producto",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Categoria_Id",
                table: "Producto",
                column: "Categoria_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Codigo",
                table: "Producto",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Producto_CodigoSin",
                table: "Producto",
                column: "CodigoSin");

            migrationBuilder.CreateIndex(
                name: "IX_Producto_Tipo",
                table: "Producto",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoCanjeable_Activo",
                table: "ProductoCanjeable",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoCanjeable_Disponible",
                table: "ProductoCanjeable",
                column: "Disponible");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoCanjeable_Producto_Unique",
                table: "ProductoCanjeable",
                column: "Id_Producto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promocion_Producto_Id",
                table: "Promocion",
                column: "Producto_Id",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanenteProgreso_Cliente_Promo",
                table: "PromocionPermanenteProgreso",
                columns: new[] { "Id_Cliente", "Id_PromocionPermanente" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromocionPermanenteProgreso_Id_PromocionPermanente",
                table: "PromocionPermanenteProgreso",
                column: "Id_PromocionPermanente");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_Activo",
                table: "PromocionTemporada",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_FechaFin",
                table: "PromocionTemporada",
                column: "FechaFin");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_FechaInicio",
                table: "PromocionTemporada",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_Nombre_Unique",
                table: "PromocionTemporada",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromocionTemporada_ProductoCanjeable_Id_ProductoCanjeable",
                table: "PromocionTemporada_ProductoCanjeable",
                column: "Id_ProductoCanjeable");

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

            migrationBuilder.CreateIndex(
                name: "IX_PuntosVentaSiat_Sucursal_PuntoVenta",
                table: "PuntosVentaSiat",
                columns: new[] { "CodigoSucursal", "CodigoPuntoVenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receta_id_elaborado",
                table: "Receta",
                column: "Id_Elaborado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "receta_nombre",
                table: "Receta",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refreshtoken_Id",
                table: "refreshtoken",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_refreshtoken_Token",
                table: "refreshtoken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refreshtoken_UserId",
                table: "refreshtoken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ronda_Id_Pedido",
                table: "Ronda",
                column: "Id_Pedido");

            migrationBuilder.CreateIndex(
                name: "IX_SubVenta_Facturada",
                table: "SubVenta",
                column: "Facturada");

            migrationBuilder.CreateIndex(
                name: "IX_SubVenta_Id_Pedido",
                table: "SubVenta",
                column: "Id_Pedido");

            migrationBuilder.CreateIndex(
                name: "IX_SubVenta_Id_Venta",
                table: "SubVenta",
                column: "Id_Venta");

            migrationBuilder.CreateIndex(
                name: "IX_SubVentaDetalle_Id_SubVenta",
                table: "SubVentaDetalle",
                column: "Id_SubVenta");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "usuario",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fx_nombre_usuario_unico",
                table: "usuario",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_Id",
                table: "usuario",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_PhoneNumber",
                table: "usuario",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "usuario",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fx_varicion_nombre",
                table: "Variacion",
                columns: new[] { "Nombre", "Id_Elaborado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variacion_Id_Elaborado",
                table: "Variacion",
                column: "Id_Elaborado");

            migrationBuilder.CreateIndex(
                name: "IX_Variacion_Requerido",
                table: "Variacion",
                column: "Requerido");

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
                name: "IX_Venta_EstadoSiat_EventoSignificativoSiatId",
                table: "Venta",
                columns: new[] { "EstadoSiat", "EventoSignificativoSiatId" });

            migrationBuilder.CreateIndex(
                name: "IX_Venta_EventoSignificativoSiatId",
                table: "Venta",
                column: "EventoSignificativoSiatId");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_Facturado",
                table: "Venta",
                column: "Facturado");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_FechaEmision",
                table: "Venta",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_Venta_NumeroFactura_Cafc",
                table: "Venta",
                column: "NumeroFactura",
                unique: true,
                filter: "\"NumeroFactura\" IS NOT NULL AND \"Cafc\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VentaPagos_IdVenta",
                table: "VentaPagos",
                column: "IdVenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceleradorPuntos");

            migrationBuilder.DropTable(
                name: "Ajuste");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CajaHistorialMovimientos");

            migrationBuilder.DropTable(
                name: "CajaMovimientos");

            migrationBuilder.DropTable(
                name: "CatActividades");

            migrationBuilder.DropTable(
                name: "CatActividadesDocumentosSector");

            migrationBuilder.DropTable(
                name: "CatDocumentosSector");

            migrationBuilder.DropTable(
                name: "CatEventosSignificativos");

            migrationBuilder.DropTable(
                name: "CatLeyendas");

            migrationBuilder.DropTable(
                name: "CatMetodosPago");

            migrationBuilder.DropTable(
                name: "CatMotivosAnulacion");

            migrationBuilder.DropTable(
                name: "CatTiposDocumentoIdentidad");

            migrationBuilder.DropTable(
                name: "CatTiposEmision");

            migrationBuilder.DropTable(
                name: "CatUnidadesMedida");

            migrationBuilder.DropTable(
                name: "CodigosSiat");

            migrationBuilder.DropTable(
                name: "Comprado");

            migrationBuilder.DropTable(
                name: "ConfiguracionQr");

            migrationBuilder.DropTable(
                name: "Cufd");

            migrationBuilder.DropTable(
                name: "Cuis");

            migrationBuilder.DropTable(
                name: "Detalle");

            migrationBuilder.DropTable(
                name: "Detalle_Pago");

            migrationBuilder.DropTable(
                name: "Detalle_promocion");

            migrationBuilder.DropTable(
                name: "Detalle_Ronda_Combo_Item");

            migrationBuilder.DropTable(
                name: "Detalle_Ronda_Opcion");

            migrationBuilder.DropTable(
                name: "HistorialHitoCompra");

            migrationBuilder.DropTable(
                name: "HistorialPromocionPermanente");

            migrationBuilder.DropTable(
                name: "HistorialPromocionTemporada");

            migrationBuilder.DropTable(
                name: "HistorialPuntos");

            migrationBuilder.DropTable(
                name: "HistorialReferido");

            migrationBuilder.DropTable(
                name: "InsumoMovimiento");

            migrationBuilder.DropTable(
                name: "Mesa");

            migrationBuilder.DropTable(
                name: "Movimiento_Producto");

            migrationBuilder.DropTable(
                name: "NotaAjusteDetalle");

            migrationBuilder.DropTable(
                name: "OrdenItemInsumo");

            migrationBuilder.DropTable(
                name: "OrdenItemProducto");

            migrationBuilder.DropTable(
                name: "Parallevar");

            migrationBuilder.DropTable(
                name: "PedidoInventarioComprometidoLinea");

            migrationBuilder.DropTable(
                name: "PromocionPermanenteProgreso");

            migrationBuilder.DropTable(
                name: "PromocionTemporada_ProductoCanjeable");

            migrationBuilder.DropTable(
                name: "PuntosVentaSiat");

            migrationBuilder.DropTable(
                name: "ReferidosConfig");

            migrationBuilder.DropTable(
                name: "refreshtoken");

            migrationBuilder.DropTable(
                name: "ReglaBasePuntos");

            migrationBuilder.DropTable(
                name: "stock_ajuste");

            migrationBuilder.DropTable(
                name: "SubVentaDetalle");

            migrationBuilder.DropTable(
                name: "VentaPagos");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CajaHistorial");

            migrationBuilder.DropTable(
                name: "Caja");

            migrationBuilder.DropTable(
                name: "Receta");

            migrationBuilder.DropTable(
                name: "Promocion");

            migrationBuilder.DropTable(
                name: "Opcion");

            migrationBuilder.DropTable(
                name: "HitoCompra");

            migrationBuilder.DropTable(
                name: "NotaAjuste");

            migrationBuilder.DropTable(
                name: "Insumo");

            migrationBuilder.DropTable(
                name: "OrdenCompra");

            migrationBuilder.DropTable(
                name: "PedidoInventarioComprometido");

            migrationBuilder.DropTable(
                name: "PromocionPermanente");

            migrationBuilder.DropTable(
                name: "PromocionTemporada");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "SubVenta");

            migrationBuilder.DropTable(
                name: "Variacion");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Detalle_Ronda");

            migrationBuilder.DropTable(
                name: "ProductoCanjeable");

            migrationBuilder.DropTable(
                name: "Venta");

            migrationBuilder.DropTable(
                name: "Elaborado");

            migrationBuilder.DropTable(
                name: "Ronda");

            migrationBuilder.DropTable(
                name: "EventosSignificativosSiat");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "Pedido");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "CatPaisesOrigen");
        }
    }
}
