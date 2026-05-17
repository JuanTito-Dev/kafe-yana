namespace KafeYana.Domain.TiposDeDatos
{
    public class TipoDisponibilidad
    {
        public const string Mesas            = "Mesas";
        public const string ParaLlevar       = "ParaLlevar";
        public const string MesasYParaLlevar = "MesasYParaLlevar";

        public static readonly string[] Todos = [Mesas, ParaLlevar, MesasYParaLlevar];
    }
}
