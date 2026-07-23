using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KafeYana.Api.GraphQLMap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace KafeYana.Api.Helpers
{
    /// <summary>
    /// Helper para paginación offset-style (<c>skip</c>/<c>take</c>) en queries
    /// GraphQL. Reemplaza el patrón duplicado que existía en cada resolver antes
    /// de esta migración (CountAsync → Skip/Take → ToListAsync → OffsetPage&lt;T&gt;).
    ///
    /// HotChocolate v15 removió <c>[UseOffsetPaging]</c>; este helper es nuestra
    /// implementación uniforme en backend + alinea con la URL
    /// <c>?skip=N&amp;take=M</c> que arma el frontend en <c>usePagination.ts</c>.
    /// </summary>
    public static class OffsetPaging
    {
        /// <summary>
        /// Tope máximo permitido para <c>take</c>. Evita que un cliente pida
        /// <c>take: 99999</c> y OOME el server. Si HotChocolate configuró un
        /// <c>MaxPageSize</c> global menor, ese gana.
        /// </summary>
        public const int MaxTake = 200;

        /// <summary>
        /// Materializa un slice de <paramref name="source"/> aplicando
        /// <c>skip</c>/<c>take</c> opcionales y devuelve un wrapper
        /// <see cref="OffsetPage{T}"/> con el conteo total sin paginar.
        ///
        /// Valores inválidos (negativos) se tratan como "no aplicar". Un
        /// <c>take</c> mayor a <see cref="MaxTake"/> se clampa acá mismo.
        /// </summary>
        public static async Task<OffsetPage<T>> ToOffsetPageAsync<T>(
            this IQueryable<T> source,
            int? skip,
            int? take,
            CancellationToken ct = default)
        {
            var total = await source.CountAsync(ct);

            IQueryable<T> q = source;
            if (skip.HasValue && skip.Value > 0)
            {
                q = q.Skip(skip.Value);
            }

            if (take.HasValue && take.Value > 0)
            {
                q = q.Take(Math.Min(take.Value, MaxTake));
            }

            var items = await q.ToListAsync(ct);
            return new OffsetPage<T> { Items = items, TotalCount = total };
        }

        /// <summary>
        /// Overload que acepta <see cref="IIncludableQueryable{T,R}"/> (resultado
        /// de un <c>.Include(...)</c>/<c>.ThenInclude(...)</c> sobre un EF queryable).
        /// C# no puede inferir <typeparamref name="T"/> directamente desde
        /// <c>IIncludableQueryable{T,R}</c> porque tiene dos type parameters, así
        /// que declaramos este overload explícito. Internamente hace Cast&lt;T&gt;
        /// y delega al overload principal sobre <see cref="IQueryable{T}"/>.
        /// </summary>
        public static Task<OffsetPage<T>> ToOffsetPageAsync<T, R>(
            this IIncludableQueryable<T, R> source,
            int? skip,
            int? take,
            CancellationToken ct = default)
            where T : class
            => source.Cast<T>().ToOffsetPageAsync(skip, take, ct);
    }
}
