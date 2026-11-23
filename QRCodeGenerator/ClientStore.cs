
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;

namespace QRCodeGenerator
{
    public static class ClientStore
    {
        // Only active WebSocket connections.
        public static readonly ConcurrentDictionary<string, MobileBehavior>
            Clients = new();

        // Driver records survive WebSocket disconnections.
        public static readonly ConcurrentDictionary<string, ClientInfo>
            ClientInfos = new();

        // Maps private resume tokens to stable driver IDs.
        private static readonly Dictionary<string, string>
            ResumeTokens = new();

        private static readonly object SessionLock = new();

        public static event EventHandler? ClientsChanged;

        public static event EventHandler<ClientMessageEventArgs>?
            MessageReceived;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static void AddConnection(
            string clientId,
            MobileBehavior behavior)
        {
            lock (SessionLock)
            {
                Clients[clientId] = behavior;
            }

            OnClientsChanged();
        }

        public static ClientInfo RegisterClient(
            string temporaryId,
            MobileBehavior behavior,
            string firstName,
            string lastName,
            int pin,
            string? resumeToken)
        {
            ClientInfo info;

            lock (SessionLock)
            {
                string clientId = temporaryId;

                // Restore an existing identity if the token is valid.
                if (!string.IsNullOrWhiteSpace(resumeToken) &&
                    ResumeTokens.TryGetValue(
                        resumeToken, out string? existingId) &&
                    ClientInfos.ContainsKey(existingId))
                {
                    clientId = existingId;
                }

                // Remove the temporary connection mapping if
                // we're restoring an existing driver.
                if (clientId != temporaryId)
                {
                    Clients.TryRemove(temporaryId, out _);
                    ClientInfos.TryRemove(temporaryId, out _);
                }

                if (!ClientInfos.TryGetValue(clientId, out info!))
                {
                    info = new ClientInfo
                    {
                        ClientId = clientId,
                        ResumeToken = Guid.NewGuid().ToString("N")
                    };
                }

                // Update registration details.
                info.FirstName = firstName;
                info.LastName = lastName;
                info.pin = pin;

                // Ensure each driver has a resume token.
                if (string.IsNullOrEmpty(info.ResumeToken))
                {
                    info.ResumeToken =
                        Guid.NewGuid().ToString("N");
                }

                ClientInfos[clientId] = info;
                ResumeTokens[info.ResumeToken] = clientId;

                // Associates the existing identity with
                // this new WebSocket connection.
                Clients[clientId] = behavior;
            }

            OnClientsChanged();

            return info;
        }

        public static void RemoveConnection(
            string clientId,
            MobileBehavior behavior)
        {
            bool removed = false;

            lock (SessionLock)
            {
                // Only remove the connection if it still belongs
                // to the behavior that is disconnecting.
                if (Clients.TryGetValue(clientId, out var current) &&
                    ReferenceEquals(current, behavior))
                {
                    removed = Clients.TryRemove(clientId, out _);
                }

                // Do NOT remove ClientInfos here.
            }

            if (removed)
            {
                Trace.WriteLine(
                    $"Driver {clientId} disconnected. Session retained.");

                OnClientsChanged();
            }
        }

        public static bool IsCurrentConnection(
            string clientId,
            MobileBehavior behavior)
        {
            lock (SessionLock)
            {
                return Clients.TryGetValue(clientId, out var current)
                    && ReferenceEquals(current, behavior);
            }
        }

        public static bool TrySendToClient(
            string clientId,
            string message)
        {
            if (string.IsNullOrEmpty(clientId))
                return false;

            if (!Clients.TryGetValue(clientId, out var behavior))
                return false;

            bool sent = behavior.SendMessage(message);

            if (!sent)
            {
                Trace.WriteLine(
                    $"Failed to send message to {clientId}");
            }

            return sent;
        }

        public static void SendToClient(
            string clientId,
            string message)
        {
            if (!TrySendToClient(clientId, message))
            {
                throw new InvalidOperationException(
                    $"Failed to send message to client '{clientId}'");
            }
        }

        public static bool TrySendJsonToClient(
            string clientId,
            object payload)
        {
            try
            {
                string json = JsonSerializer.Serialize(
                    payload, JsonOptions);

                return TrySendToClient(clientId, json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    $"JSON send error for {clientId}: {ex.Message}");

                return false;
            }
        }

        public static void SendJsonToClient(
            string clientId,
            object payload)
        {
            if (!TrySendJsonToClient(clientId, payload))
            {
                throw new InvalidOperationException(
                    $"Failed to send JSON to client '{clientId}'");
            }
        }

        public static void OnMessageReceived(
            string clientId,
            string message)
        {
            MessageReceived?.Invoke(
                null,
                new ClientMessageEventArgs
                {
                    ClientId = clientId,
                    Message = message
                });
        }

        public static void OnClientsChanged()
        {
            ClientsChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public class ClientMessageEventArgs : EventArgs
    {
        public string ClientId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class ClientInfo
    {
        public string ClientId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int pin { get; set; }

        // Private credential used to restore a driver session.
        public string ResumeToken { get; set; } = string.Empty;

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(FirstName) &&
                string.IsNullOrWhiteSpace(LastName))
            {
                return ClientId;
            }

            return $"{FirstName} {LastName} {pin} ({ClientId})";
        }
    }
}
