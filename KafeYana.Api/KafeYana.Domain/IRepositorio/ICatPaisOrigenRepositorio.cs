using KafeYana.Domain.Entities.Catalogos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafeYana.Application.IRepositorio
{
    /// <summary>
    /// Repositorio del catálogo paramétrico de países de origen del SIAT
    /// (<c>CatPaisOrigen</c>, sincronizado por <c>SincronizadorCatPaisOrigen</c>).
    ///
    /// Se usa:
    ///   - En <c>ClientePedidoHelper</c> para resolver el código SIN (1..211)
    ///     que llega en <c>DtoVentaPedido.CodigoPaisOrigen</c> al FK local
    ///     <c>IdPaisOrigen</c> al crear/vincular un cliente extranjero.
    ///   - En <c>CatalogosController.GetPaisesOrigen</c> para devolver la
    ///     lista al frontend (dropdown de país en el formulario de datos
    ///     fiscales cuando el cliente es CEX/PAS).
    ///
    /// NO expone CRUD: la tabla es solo-leer para el código de negocio; las
    /// escrituras las hace exclusivamente <c>SincronizadorCatPaisOrigen</c>
    /// (atomic replace contra el SIAT).
    /// </summary>
    public interface ICatPaisOrigenRepositorio
    {
        /// <summary>
        /// Devuelve el país cuyo código SIN coincide con <paramref name="codigo"/>
        /// (1..211 según catálogo vigente). Null si no existe — el caller decide
        /// si lanzar 400 al cliente o continuar con IdPaisOrigen=null.
        /// </summary>
        Task<CatPaisOrigen?> GetByCodigoAsync(int codigo);

        /// <summary>
        /// Lista completa de países ordenados por código SIN ascendente
        /// (1=AFGANISTÁN, 22=BOLIVIA, …). Para alimentar el dropdown del
        /// formulario de datos fiscales.
        /// </summary>
        Task<IReadOnlyList<CatPaisOrigen>> GetAllOrderedAsync();
    }
}