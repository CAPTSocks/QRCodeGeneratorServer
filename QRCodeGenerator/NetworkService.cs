using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace QRCodeGenerator
{
    /// <summary>
    /// Provides network utility operations.
    /// </summary>
    public class NetworkService
    {
        /// <summary>
        /// Gets the local IPv4 address of the machine.
        /// </summary>
        /// <returns>IPv4 address or null if not found.</returns>
        public string? GetLocalIPv4()
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                    continue;

                try
                {
                    var properties = adapter.GetIPProperties();

                    foreach (var address in properties.UnicastAddresses)
                    {
                        if (address.Address.AddressFamily !=
                            AddressFamily.InterNetwork)
                            continue;

                        string ip = address.Address.ToString();

                        if (IPAddress.IsLoopback(address.Address))
                            continue;

                        if (ip.StartsWith("169.254."))
                            continue;

                        return ip;
                    }
                }
                catch (NetworkInformationException)
                {
                    // Skip adapters whose network properties cannot be read.
                    continue;
                }
                catch (SocketException)
                {
                    // Skip adapters with unsupported network configurations.
                    continue;
                }
            }

            return null;
        }
    }
}
