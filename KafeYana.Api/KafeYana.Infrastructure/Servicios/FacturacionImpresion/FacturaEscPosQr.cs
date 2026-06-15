using System.Text;

namespace KafeYana.Infrastructure.Servicios.FacturacionImpresion
{
    internal static class FacturaEscPosQr
    {
        public static void Escribir(MemoryStream ms, string url)
        {
            var data = Encoding.ASCII.GetBytes(url);
            var storeLen = data.Length + 3;
            var pL = (byte)(storeLen % 256);
            var pH = (byte)(storeLen / 256);

            ms.WriteByte(0x1D);
            ms.WriteByte(0x28);
            ms.WriteByte(0x6B);
            ms.WriteByte(pL);
            ms.WriteByte(pH);
            ms.WriteByte(0x31);
            ms.WriteByte(0x50);
            ms.WriteByte(0x30);
            ms.Write(data, 0, data.Length);

            ms.WriteByte(0x1D);
            ms.WriteByte(0x28);
            ms.WriteByte(0x6B);
            ms.WriteByte(0x03);
            ms.WriteByte(0x00);
            ms.WriteByte(0x31);
            ms.WriteByte(0x51);
            ms.WriteByte(0x30);
        }
    }
}
