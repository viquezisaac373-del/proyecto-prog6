using System.Net;

namespace ProyectoHospital.Services
{
    public static class IpCliente
    {
        public static string? Obtener(HttpContext contexto)
        {
            IPAddress? ip = contexto.Connection.RemoteIpAddress;

            if (ip is null)
                return null;

            if (IPAddress.IsLoopback(ip))
                return IPAddress.Loopback.ToString();

            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            return ip.ToString();
        }
    }
}
