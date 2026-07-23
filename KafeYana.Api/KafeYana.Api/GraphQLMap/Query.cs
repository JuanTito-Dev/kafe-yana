namespace KafeYana.Api.GraphQLMap
{
    /// <summary>
    /// Root Query type para HotChocolate. Se llama <c>Query</c> (sin prefijo) para
    /// que la convención de nombres de HotChocolate v15 infiera el tipo GraphQL
    /// como <c>"Query"</c>, que es lo que esperan todas las extensiones
    /// <c>[ExtendObjectType("Query")]</c> (VentaQuery, CajaQuery, UsuarioQuery,
    /// ReporteCajaQuery, ReporteProductoQuery, etc.).
    ///
    /// HotChocolate v15 exige que el root Query type defina al menos un campo antes
    /// de aceptar las extensiones. <c>Ping</c> cumple ese rol — expone "pong"
    /// como health-check ligero.
    /// </summary>
    public class Query
    {
        public string Ping() => "pong";
    }
}
