using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace XMLSigner.Library
{
    class NetworkPort
    {
        private static HashSet<int> ports = new HashSet<int>();

        static NetworkPort()
        {
            GetAllUsedPorts();
        }

        internal static HashSet<int> GetAllUsedPorts()
        {
            ports.Clear();
            ports.TrimExcess();
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                ports.Add(tcpi.LocalEndPoint.Port);
            }
            Log.Print(LogLevel._Low, "Already Used Ports - " + string.Join(", ", ports));
            Log.Print(LogLevel._Low, "Used Port Count - " + ports.Count);
            return ports;
        }

        internal static bool CheckIfPortAvailable(int port)
        {
            Log.Print(LogLevel._Low, "Port Checking - " + port);
            return !ports.Contains(port);
        }
    }
}
