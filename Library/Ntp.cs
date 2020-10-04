using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XMLSigner.Library
{
    class Ntp
    {
        static string activeServer;

        static Ntp()
        {
            activeServer = GetFirstActiveNtpServerFromList();
            //activeServer = Properties.Resources.NtpServerUrl;
        }

        private static string GetFirstActiveNtpServerFromList()
        {
            HashSet<string> ntpServers = GetNtpServerList();
            foreach(string server in ntpServers)
            {
                if(IsNtpWorking(server))
                {
                    return server;
                }
            }
            return null;
        }

        private static bool IsNtpWorking(string server)
        {
            return TryGetNetworkTimeFromServer(server) != null ? true : false;
        }

        private static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }
            return pingable;
        }

        private static HashSet<string> GetNtpServerList()
        {
            string[] ntpServerArray = Properties.Resources.NtpServerList.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> ntpServerList = new HashSet<string>(ntpServerArray);
            return ntpServerList;
        }

        private static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private static DateTime? TryGetNetworkTimeFromServer(string activeServer)
        {
            try {
                byte[] ntpData = new byte[48];
                ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

                IPAddress[] addresses = Dns.GetHostEntry(activeServer).AddressList;
                IPEndPoint ipEndPoint = new IPEndPoint(addresses[0], 123);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                socket.Connect(ipEndPoint);
                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();

                ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
                ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

                ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                DateTime networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

                return networkDateTime;
            }
            catch(Exception ex)
            {
                Log.Print(LogLevel.Medium, ex.ToString());
                return null;
            }
        }

        private static DateTime GetNetworkTime()
        {
#if DEBUG
            if (!CheckForInternetConnection())
            {
                return DateTime.UtcNow;
            }
#endif
            //Should check time server by certificate, not added now
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B; //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)

            IPAddress[] addresses = Dns.GetHostEntry(activeServer).AddressList;
            IPEndPoint ipEndPoint = new IPEndPoint(addresses[0], 123);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            DateTime networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }

        internal static bool CheckIfLocalTimeIsOk(int allowedMaxMinuiteDiff = 2)
        {
            DateTime ntpTime = GetNetworkTime();
            DateTime localTime = DateTime.UtcNow;
            TimeSpan timeDiff = ntpTime - localTime;
            if (Math.Abs(timeDiff.TotalMinutes) <= allowedMaxMinuiteDiff)
                return true;
            else
                return false;
        }
    }
}
