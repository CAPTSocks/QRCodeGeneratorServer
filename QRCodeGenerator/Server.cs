using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

/// <summary>
/// WebSocket behavior for mobile clients connecting to the server.
/// </summary>
public class MobileBehavior : WebSocketBehavior
{
    private string? _clientId;

    protected override void OnOpen()
    {
        Trace.WriteLine("Client connected");
        StoreClient();
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        try
        {
            // Try parse as JSON and handle register messages specially
            using var jsonDoc = JsonDocument.Parse(e.Data);
            var root = jsonDoc.RootElement;
            if (root.TryGetProperty("type", out var t)
                && string.Equals(t.GetString(), "registration", StringComparison.OrdinalIgnoreCase))
            {
                var first = root.GetProperty("firstName").GetString() ?? string.Empty;
                var last = root.GetProperty("lastName").GetString() ?? string.Empty;
                var newPin = root.GetProperty("pin").GetString() ?? string.Empty;
                if (!string.IsNullOrEmpty(_clientId))
                {
                    ClientStore.ClientInfos[_clientId] = new ClientInfo { ClientId = _clientId, FirstName = first, LastName = last, pin = int.Parse(newPin) };
                    ClientStore.OnClientsChanged();
                }
                return;
            }
        }
        catch
        {
            // not JSON or missing fields - treat as plain chat
        }

        // Default: treat as chat/plain message
        Trace.WriteLine($"Received from {_clientId}: {e.Data}");
        ClientStore.OnMessageReceived(_clientId ?? "unknown", e.Data);
        Send($"Server acknowledged: {e.Data}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Trace.WriteLine($"Client disconnected: {_clientId}");
        RemoveClient();
    }

    /// <summary>
    /// Stores the client in the global ClientStore with a unique ID.
    /// </summary>
    private void StoreClient()
    {
        try
        {
            _clientId = Guid.NewGuid().ToString();
            ClientStore.Clients[_clientId] = this;
            // initialize client info entry (empty name until client registers)
            ClientStore.ClientInfos[_clientId] = new ClientInfo { ClientId = _clientId };
           // ClientStore.OnClientsChanged();

            string endpoint = Context?.UserEndPoint?.ToString() ?? "unknown";
            Trace.WriteLine($"Stored client {_clientId} from {endpoint}");

            // Send client ID back to the client
            try { Send($"CLIENT_ID:{_clientId}"); }
            catch { /* Ignore send errors */ }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error storing client: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes the client from the global ClientStore.
    /// </summary>
    private void RemoveClient()
    {
        if (!string.IsNullOrEmpty(_clientId))
        {
            try
            {
                ClientStore.Clients.TryRemove(_clientId, out _);
                ClientStore.ClientInfos.TryRemove(_clientId, out _);
                ClientStore.OnClientsChanged();
                Trace.WriteLine($"Removed client {_clientId}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error removing client: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sends a message to this client.
    /// </summary>
    public void SendMessage(string message)
    {
        try
        {
            Send(message);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error sending message to {_clientId}: {ex.Message}");
        }
    }
}

/// <summary>
/// Global store for connected mobile clients. Allows server code to send messages to specific clients.
/// </summary>
public static class ClientStore
{
    public static readonly ConcurrentDictionary<string, MobileBehavior> Clients 
        = new ConcurrentDictionary<string, MobileBehavior>();
    // Additional map to hold client info such as names
    public static readonly ConcurrentDictionary<string, ClientInfo> ClientInfos = new ConcurrentDictionary<string, ClientInfo>();

    /// <summary>
    /// Event raised when the client list (names / connections) changes.
    /// </summary>
    public static event EventHandler? ClientsChanged;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Event raised when a message is received from a client.
    /// </summary>
    public static event EventHandler<ClientMessageEventArgs>? MessageReceived;

    /// <summary>
    /// Tries to send a string message to a specific client.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool TrySendToClient(string clientId, string message)
    {
        if (string.IsNullOrEmpty(clientId))
            return false;

        if (Clients.TryGetValue(clientId, out var behavior))
        {
            try
            {
                behavior.SendMessage(message);
                Trace.WriteLine($"Sent message to {clientId}");
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error sending message to {clientId}: {ex.Message}");
                return false;
            }
        }

        Trace.WriteLine($"Client {clientId} not found");
        return false;
    }

    /// <summary>
    /// Sends a string message to a specific client or throws if it fails.
    /// </summary>
    public static void SendToClient(string clientId, string message)
    {
        if (!TrySendToClient(clientId, message))
            throw new InvalidOperationException($"Failed to send message to client '{clientId}'");
    }

    /// <summary>
    /// Tries to send a JSON-serialized object to a specific client.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool TrySendJsonToClient(string clientId, object payload)
    {
        if (string.IsNullOrEmpty(clientId))
            return false;

        if (Clients.TryGetValue(clientId, out var behavior))
        {
            try
            {
                string json = JsonSerializer.Serialize(payload, JsonOptions);
                behavior.SendMessage(json);
                Trace.WriteLine($"Sent JSON to {clientId}");
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error sending JSON to {clientId}: {ex.Message}");
                return false;
            }
        }

        Trace.WriteLine($"Client {clientId} not found");
        return false;
    }

    /// <summary>
    /// Sends a JSON-serialized object to a specific client or throws if it fails.
    /// </summary>
    public static void SendJsonToClient(string clientId, object payload)
    {
        if (!TrySendJsonToClient(clientId, payload))
            throw new InvalidOperationException($"Failed to send JSON to client '{clientId}'");
    }

    /// <summary>
    /// Raises the MessageReceived event to notify subscribers of incoming messages.
    /// </summary>
    public static void OnMessageReceived(string clientId, string message)
    {
        MessageReceived?.Invoke(null, new ClientMessageEventArgs 
        { 
            ClientId = clientId, 
            Message = message 
        });
    }

    /// <summary>
    /// Raises the ClientsChanged event to notify UI that the connected clients changed.
    /// </summary>
    public static void OnClientsChanged()
    {
        ClientsChanged?.Invoke(null, EventArgs.Empty);
    }
}

/// <summary>
/// Event args for client messages.
/// </summary>
public class ClientMessageEventArgs : EventArgs
{
    public string ClientId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Lightweight client info stored by ClientStore when clients register their name.
/// </summary>
public class ClientInfo
{
    public string ClientId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int pin { get; set; } = 0;

    public override string ToString() => string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName)
        ? ClientId
        : $"{FirstName} {LastName} {pin} ({ClientId})";
} 
