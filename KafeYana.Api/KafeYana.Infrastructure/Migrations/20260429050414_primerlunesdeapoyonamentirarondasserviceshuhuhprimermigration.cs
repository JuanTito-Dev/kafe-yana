using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class primerlunesdeapoyonamentirarondasserviceshuhuhprimermigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dni = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Correo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Correonormalizado = table.Column<string>(type: "text", nullable: true),
                    Fecha_nacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Puntos = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
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
                name: "Venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false, computedColumnSql: "'VTA-' || CAST(\"Id\" AS VARCHAR)", stored: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Cliente = table.Column<string>(type: "text", nullable: false),
                    Cajero = table.Column<string>(type: "text", nullable: false),
                    Productos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Pago = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venta", x => x.Id);
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
                name: "Producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: ""),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Categoria_Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Producto_Categorias_Categoria_Id",
                        column: x => x.Categoria_Id,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_Pedido_Clientes_Id_Cliente",
                        column: x => x.Id_Cliente,
                        principalTable: "Clientes",
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
                name: "Detalle_venta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id_venta = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Comprado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo_barra = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Unidad_medida = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                        name: "FK_Comprado_Producto_Id_Producto",
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
                    Producible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Stock_actual = table.Column<int>(type: "integer", nullable: false),
                    Id_Producto = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elaborado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Elaborado_Producto_Id_Producto",
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
                        name: "FK_Mesa_Pedido_Id_Pedido",
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
                        name: "FK_Ronda_Pedido_Id_Pedido",
                        column: x => x.Id_Pedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_Receta_Elaborado_Id_Elaborado",
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
                        name: "FK_Variacion_Elaborado_Id_Elaborado",
                        column: x => x.Id_Elaborado,
                        principalTable: "Elaborado",
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
                    table.PrimaryKey("PK_Detalle_promocion", x => new { x.Id_Producto, x.Id_Promocion });
                    table.ForeignKey(
                        name: "FK_Detalle_promocion_Producto_Id_Producto",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Detalle_promocion_Promocion_Id_Promocion",
                        column: x => x.Id_Promocion,
                        principalTable: "Promocion",
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
                    Id_Producto = table.Column<int>(type: "integer", nullable: false),
                    Nombre_Producto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0.00m)
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
                        name: "IX_Detalle_Ronda_ProductoId",
                        column: x => x.Id_Producto,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_Detalle_Insumo_Id_insumo",
                        column: x => x.Id_insumo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Detalle_Receta_Id_receta",
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
                        name: "FK_Opcion_Variacion_Id_variacion",
                        column: x => x.Id_variacion,
                        principalTable: "Variacion",
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
                        name: "FK_Ajuste_Insumo_Id_Insumo",
                        column: x => x.Id_Insumo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ajuste_Insumo_Id_InsumoNuevo",
                        column: x => x.Id_InsumoNuevo,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ajuste_Opcion_Id_Opcion",
                        column: x => x.Id_Opcion,
                        principalTable: "Opcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Detalle_Ronda_Opcion",
                columns: table => new
                {
                    Id_Detalle_Ronda = table.Column<int>(type: "integer", nullable: false),
                    Id_Opcion = table.Column<int>(type: "integer", nullable: false),
                    TipoOpcion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "normal"),
                    ValorAnterior = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CostoExtra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detalle_Ronda_Opcion", x => new { x.Id_Detalle_Ronda, x.Id_Opcion });
                    table.ForeignKey(
                        name: "FK_Detalle_Ronda_Opcion_Detalle_Ronda_Id_Detalle_Ronda",
                        column: x => x.Id_Detalle_Ronda,
                        principalTable: "Detalle_Ronda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Detalle_Ronda_Opcion_Opcion_Id_Opcion",
                        column: x => x.Id_Opcion,
                        principalTable: "Opcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "ix_categorias_nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Celular",
                table: "Clientes",
                column: "Celular",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correo",
                table: "Clientes",
                column: "Correo",
                unique: true,
                filter: "\"Correo\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Correonormalizado",
                table: "Clientes",
                column: "Correonormalizado",
                unique: true,
                filter: "\"Correonormalizado\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Dni",
                table: "Clientes",
                column: "Dni",
                unique: true,
                filter: "\"Dni\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comprado_Codigo_barra",
                table: "Comprado",
                column: "Codigo_barra");

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
                name: "IX_Detalle_Id_insumo",
                table: "Detalle",
                column: "Id_insumo");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_promocion_Id_Promocion",
                table: "Detalle_promocion",
                column: "Id_Promocion");

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Id_Producto",
                table: "Detalle_Ronda",
                column: "Id_Producto");

            migrationBuilder.CreateIndex(
                name: "ix_detalle_ronda_ronda_producto_unique",
                table: "Detalle_Ronda",
                columns: new[] { "Id_Ronda", "Id_Producto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_Ronda_Opcion_Id_Opcion",
                table: "Detalle_Ronda_Opcion",
                column: "Id_Opcion");

            migrationBuilder.CreateIndex(
                name: "ix_detalle_ronda_opcion_unique",
                table: "Detalle_Ronda_Opcion",
                columns: new[] { "Id_Detalle_Ronda", "Id_Opcion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Detalle_venta_Id_venta",
                table: "Detalle_venta",
                column: "Id_venta");

            migrationBuilder.CreateIndex(
                name: "IX_Elaborado_Id_Producto",
                table: "Elaborado",
                column: "Id_Producto",
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
                name: "IX_Insumo_Nombre",
                table: "Insumo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesa_Id_Pedido",
                table: "Mesa",
                column: "Id_Pedido",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesa_Nombre",
                table: "Mesa",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_Id",
                table: "Opcion",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_Id_variacion",
                table: "Opcion",
                column: "Id_variacion");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Id",
                table: "Pedido",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_Id_Cliente",
                table: "Pedido",
                column: "Id_Cliente",
                unique: true);

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
                name: "IX_Producto_Tipo",
                table: "Producto",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Promocion_Producto_Id",
                table: "Promocion",
                column: "Producto_Id",
                unique: true);

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
                name: "IX_Receta_Id_Elaborado",
                table: "Receta",
                column: "Id_Elaborado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receta_Nombre",
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
                name: "EmailIndex",
                table: "usuario",
                column: "NormalizedEmail",
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
                name: "IX_Variacion_Id_Elaborado",
                table: "Variacion",
                column: "Id_Elaborado");

            migrationBuilder.CreateIndex(
                name: "IX_Variacion_Nombre",
                table: "Variacion",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Variacion_Requerido",
                table: "Variacion",
                column: "Requerido");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "Comprado");

            migrationBuilder.DropTable(
                name: "Detalle");

            migrationBuilder.DropTable(
                name: "Detalle_promocion");

            migrationBuilder.DropTable(
                name: "Detalle_Ronda_Opcion");

            migrationBuilder.DropTable(
                name: "Detalle_venta");

            migrationBuilder.DropTable(
                name: "Mesa");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "refreshtoken");

            migrationBuilder.DropTable(
                name: "stock_ajuste");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Insumo");

            migrationBuilder.DropTable(
                name: "Receta");

            migrationBuilder.DropTable(
                name: "Promocion");

            migrationBuilder.DropTable(
                name: "Detalle_Ronda");

            migrationBuilder.DropTable(
                name: "Opcion");

            migrationBuilder.DropTable(
                name: "Venta");

            migrationBuilder.DropTable(
                name: "usuario");

            migrationBuilder.DropTable(
                name: "Ronda");

            migrationBuilder.DropTable(
                name: "Variacion");

            migrationBuilder.DropTable(
                name: "Pedido");

            migrationBuilder.DropTable(
                name: "Elaborado");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "Categorias");
        }
    }
}
