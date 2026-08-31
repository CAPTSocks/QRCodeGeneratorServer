using System;
using System.Net;
using WebSocketSharp.Server;

namespace QRCodeGenerator
{
    /// <summary>
    /// Manages the WebSocket server lifecycle.
    /// </summary>
    public class ServerManager
    {
        private WebSocketServer? _server;
        private readonly int _port;

        public event EventHandler<ServerStatusEventArgs>? StatusChanged;

        public ServerManager(int port = 5000)
        {
            _port = port;
        }

        /// <summary>
        /// Gets whether the server is currently listening.
        /// </summary>
        public bool IsListening => _server?.IsListening ?? false;

        /// <summary>
        /// Starts the WebSocket server on the specified port.
        /// </summary>
        public void StartServer()
        {
            if (IsListening)
                return;

            try
            {
                _server = new WebSocketServer(IPAddress.Any, _port);
                _server.AddWebSocketService<MobileBehavior>("/mobile");
                _server.Start();

                OnStatusChanged($"Server started at {_server.Address}");
                Console.WriteLine($"Server started at {_server.Address}");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Failed to start server: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops the WebSocket server.
        /// </summary>
        public void StopServer()
        {
            try
            {
                _server?.Stop();
                OnStatusChanged("WebSocket server stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping server: {ex.Message}");
            }
        }

        protected virtual void OnStatusChanged(string message)
        {
            StatusChanged?.Invoke(this, new ServerStatusEventArgs { Message = message });
        }
    }

    /// <summary>
    /// Event args for server status changes.
    /// </summary>
    public class ServerStatusEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
    }
}
