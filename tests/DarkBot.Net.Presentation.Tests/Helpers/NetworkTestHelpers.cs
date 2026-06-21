using System.Net;
using System.Net.Sockets;

namespace DarkBot.Net.Presentation.Tests.Helpers;

internal static class NetworkTestHelpers
{
    public static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
