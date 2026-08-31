using System;
using System.Net;
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
            try
            {
                foreach (var networkInterface in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (networkInterface.AddressFamily == AddressFamily.InterNetwork)
                        return networkInterface.ToString();
                }
            }
            catch
            {
                // Ignore DNS resolution errors
            }
            return null;
        }
    }
}
