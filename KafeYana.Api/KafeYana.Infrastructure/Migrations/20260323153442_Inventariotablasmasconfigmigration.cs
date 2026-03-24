using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafeYana.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inventariotablasmasconfigmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "Insumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unidad_min_uso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Unidad_compra = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Factor_conversion = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Stock_actual = table.Column<decimal>(type: "numeric", nullable: false),
                    Stock_min = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insumo", x => x.Id);
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
                    Categoria_Id = table.Column<int>(type: "integer", nullable: false),
                    CategoriaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Producto_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Producto_Categorias_Categoria_Id",
                        column: x => x.Categoria_Id,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "Receta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    Requirido = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    Cantidad = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TipoAjuste = table.Column<string>(type: "text", nullable: false)
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
                        name: "FK_Ajuste_Opcion_Id_Opcion",
                        column: x => x.Id_Opcion,
                        principalTable: "Opcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ajuste_Id_Opcion",
                table: "Ajuste",
                column: "Id_Opcion");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_nombre",
                table: "Categorias",
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
                name: "IX_Opcion_Id",
                table: "Opcion",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Opcion_Id_variacion",
                table: "Opcion",
                column: "Id_variacion");

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
                name: "IX_Producto_CategoriaId",
                table: "Producto",
                column: "CategoriaId");

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
                name: "IX_Receta_Id_Elaborado",
                table: "Receta",
                column: "Id_Elaborado",
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
                name: "IX_Variacion_Requirido",
                table: "Variacion",
                column: "Requirido");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ajuste");

            migrationBuilder.DropTable(
                name: "Comprado");

            migrationBuilder.DropTable(
                name: "Detalle");

            migrationBuilder.DropTable(
                name: "Detalle_promocion");

            migrationBuilder.DropTable(
                name: "Opcion");

            migrationBuilder.DropTable(
                name: "Insumo");

            migrationBuilder.DropTable(
                name: "Receta");

            migrationBuilder.DropTable(
                name: "Promocion");

            migrationBuilder.DropTable(
                name: "Variacion");

            migrationBuilder.DropTable(
                name: "Elaborado");

            migrationBuilder.DropTable(
                name: "Producto");

            migrationBuilder.DropTable(
                name: "Categorias");
        }
    }
}
