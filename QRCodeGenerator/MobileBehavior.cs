using QRCodeGenerator;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Windows;

/// <summary>
/// WebSocket behavior for mobile clients connecting to the server.
/// </summary>
/// 
namespace QRCodeGenerator
{
    public class MobileBehavior : WebSocketBehavior
    {
        private string? _clientId;

        protected override void OnOpen()
        {
            _clientId = Guid.NewGuid().ToString();

            ClientStore.AddConnection(_clientId, this);

            Trace.WriteLine(
                $"WebSocket opened with temporary ID {_clientId}");

            // Keep compatibility with the existing client.
            SendMessage($"CLIENT_ID:{_clientId}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (string.IsNullOrEmpty(_clientId))
                return;

            // Ignore messages from connections that have
            // already been replaced by a newer connection.
            if (!ClientStore.IsCurrentConnection(_clientId, this))
                return;

            try
            {
                using var document = JsonDocument.Parse(e.Data);
                var root = document.RootElement;

                if (root.TryGetProperty("type", out var type) &&
                    type.GetString()?.Equals(
                        "registration",
                        StringComparison.OrdinalIgnoreCase) == true)
                {
                    HandleRegistration(root);
                    return;
                }
            }
            catch (JsonException ex)
            {
                Trace.WriteLine(
                    $"Invalid JSON from {_clientId}: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Message error from {_clientId}: {ex.Message}");
                return;
            }

            // Preserve existing non-registration message handling.
            ClientStore.OnMessageReceived(_clientId, e.Data);

            SendMessage($"Server acknowledged: {e.Data}");
        }

        private void HandleRegistration(JsonElement root)
        {
            if (string.IsNullOrEmpty(_clientId))
                return;

            string firstName = GetString(root, "firstName");
            string lastName = GetString(root, "lastName");
            string pinString = GetString(root, "pin");
            string resumeToken = GetString(root, "resumeToken");

            if (!int.TryParse(pinString, out int pin))
            {
                Trace.WriteLine(
                    $"Invalid registration PIN from {_clientId}");
                return;
            }

            string temporaryId = _clientId;

            ClientInfo info = ClientStore.RegisterClient(
                temporaryId,
                this,
                firstName,
                lastName,
                pin,
                resumeToken
            );

            // The connection now belongs to the persistent ID.
            _clientId = info.ClientId;

            bool resumed = !string.IsNullOrEmpty(resumeToken)
                && string.Equals(
                    resumeToken,
                    info.ResumeToken,
                    StringComparison.Ordinal);

            Trace.WriteLine(
                $"Driver registered: {info.ClientId}, resumed: {resumed}");

            // The phone will save this token for future connections.
            ClientStore.TrySendJsonToClient(
                info.ClientId,
                new
                {
                    Type = "sessionIdentity",
                    ClientId = info.ClientId,
                    ResumeToken = info.ResumeToken,
                    Resumed = resumed
                }
            );

            SendRaceSync(info.ClientId);

            // Full raceSync will be added in the next phase.
        }


        private void SendRaceSync(string clientId)
        {
            // RaceStatus runs on the WPF UI thread.
            // Queue synchronization on that same thread so
            // it is ordered with simulated lap updates.
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    // A newer WebSocket may have replaced this one
                    // while the synchronization was being queued.
                    if (!ClientStore.IsCurrentConnection(
                        clientId, this))
                    {
                        return;
                    }

                    RaceSession? session = RaceSessionStore.Current;

                    if (session == null)
                    {
                        Trace.WriteLine(
                            "No active race session to synchronize.");
                        return;
                    }

                    // Build a snapshot for this particular driver.
                    object? syncMessage =
                        session.CreateSyncMessage(clientId);

                    if (syncMessage == null)
                    {
                        Trace.WriteLine(
                            $"Driver {clientId} is not a participant " +
                            "in the current race.");
                        return;
                    }

                    bool sent = ClientStore.TrySendJsonToClient(
                        clientId,
                        syncMessage
                    );

                    Trace.WriteLine(
                        sent
                            ? $"Race synchronization sent to {clientId}"
                            : $"Race synchronization failed for {clientId}"
                    );
                })
            );
        }


        private static string GetString(
            JsonElement root,
            string propertyName)
        {
            if (!root.TryGetProperty(
                propertyName, out var value))
            {
                return string.Empty;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String =>
                    value.GetString() ?? string.Empty,

                JsonValueKind.Number =>
                    value.GetRawText(),

                _ => string.Empty
            };
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Trace.WriteLine(
                $"WebSocket closed for driver {_clientId}");

            if (!string.IsNullOrEmpty(_clientId))
            {
                ClientStore.RemoveConnection(_clientId, this);
            }
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Trace.WriteLine(
                $"WebSocket error for {_clientId}: {e.Message}");
        }

        public bool SendMessage(string message)
        {
            try
            {
                if (State != WebSocketState.Open)
                    return false;

                Send(message);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"Send error for {_clientId}: {ex.Message}");

                return false;
            }
        }
    }
}


///// <summary>
///// Global store for connected mobile clients. Allows server code to send messages to specific clients.
///// </summary>
//public static class ClientStore
//{
//    public static readonly ConcurrentDictionary<string, MobileBehavior> Clients 
//        = new ConcurrentDictionary<string, MobileBehavior>();
//    // Additional map to hold client info such as names
//    public static readonly ConcurrentDictionary<string, ClientInfo> ClientInfos = new ConcurrentDictionary<string, ClientInfo>();

//    /// <summary>
//    /// Event raised when the client list (names / connections) changes.
//    /// </summary>
//    public static event EventHandler? ClientsChanged;

//    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
//    {
//        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//        WriteIndented = false
//    };

//    /// <summary>
//    /// Event raised when a message is received from a client.
//    /// </summary>
//    public static event EventHandler<ClientMessageEventArgs>? MessageReceived;

//    /// <summary>
//    /// Tries to send a string message to a specific client.
//    /// </summary>
//    /// <returns>True if successful, false otherwise.</returns>
//    public static bool TrySendToClient(string clientId, string message)
//    {
//        if (string.IsNullOrEmpty(clientId))
//            return false;

//        if (Clients.TryGetValue(clientId, out var behavior))
//        {
//            try
//            {
//                behavior.SendMessage(message);
//                Trace.WriteLine($"Sent message to {clientId}");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Trace.WriteLine($"Error sending message to {clientId}: {ex.Message}");
//                return false;
//            }
//        }

//        Trace.WriteLine($"Client {clientId} not found");
//        return false;
//    }

//    /// <summary>
//    /// Sends a string message to a specific client or throws if it fails.
//    /// </summary>
//    public static void SendToClient(string clientId, string message)
//    {
//        if (!TrySendToClient(clientId, message))
//            throw new InvalidOperationException($"Failed to send message to client '{clientId}'");
//    }

//    /// <summary>
//    /// Tries to send a JSON-serialized object to a specific client.
//    /// </summary>
//    /// <returns>True if successful, false otherwise.</returns>
//    public static bool TrySendJsonToClient(string clientId, object payload)
//    {
//        if (string.IsNullOrEmpty(clientId))
//            return false;

//        if (Clients.TryGetValue(clientId, out var behavior))
//        {
//            try
//            {
//                string json = JsonSerializer.Serialize(payload, JsonOptions);
//                behavior.SendMessage(json);
//                Trace.WriteLine($"Sent JSON to {clientId}");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Trace.WriteLine($"Error sending JSON to {clientId}: {ex.Message}");
//                return false;
//            }
//        }

//        Trace.WriteLine($"Client {clientId} not found");
//        return false;
//    }

//    /// <summary>
//    /// Sends a JSON-serialized object to a specific client or throws if it fails.
//    /// </summary>
//    public static void SendJsonToClient(string clientId, object payload)
//    {
//        if (!TrySendJsonToClient(clientId, payload))
//            throw new InvalidOperationException($"Failed to send JSON to client '{clientId}'");
//    }

//    /// <summary>
//    /// Raises the MessageReceived event to notify subscribers of incoming messages.
//    /// </summary>
//    public static void OnMessageReceived(string clientId, string message)
//    {
//        MessageReceived?.Invoke(null, new ClientMessageEventArgs 
//        { 
//            ClientId = clientId, 
//            Message = message 
//        });
//    }

//    /// <summary>
//    /// Raises the ClientsChanged event to notify UI that the connected clients changed.
//    /// </summary>
//    public static void OnClientsChanged()
//    {
//        ClientsChanged?.Invoke(null, EventArgs.Empty);
//    }
//}

///// <summary>
///// Event args for client messages.
///// </summary>
//public class ClientMessageEventArgs : EventArgs
//{
//    public string ClientId { get; set; } = string.Empty;
//    public string Message { get; set; } = string.Empty;
//    public DateTime Timestamp { get; set; } = DateTime.Now;
//}

///// <summary>
///// Lightweight client info stored by ClientStore when clients register their name.
///// </summary>
//public class ClientInfo
//{
//    public string ClientId { get; set; } = string.Empty;
//    public string FirstName { get; set; } = string.Empty;
//    public string LastName { get; set; } = string.Empty;
//    public int pin { get; set; } = 0;

//    public override string ToString() => string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName)
//        ? ClientId
//        : $"{FirstName} {LastName} {pin} ({ClientId})";
//} 
