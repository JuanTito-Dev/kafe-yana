using System.Collections.Generic;

namespace KafeYana.Api.GraphQLMap
{
    /// <summary>
    /// Wrapper de paginación offset-style que devuelve un slice de items + total.
    /// HotChocolate v15 removió <c>[UseOffsetPaging]</c>, así que implementamos
    /// offset a mano en cada query (skip/take aplicados sobre el IQueryable antes
    /// de materializar). Esta shape es la que consume el frontend en
    /// <c>lib/queries/*.queries.ts</c>: <c>{ items, totalCount }</c>.
    /// </summary>
    public class OffsetPage<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
    }
}