using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace QRCodeGenerator
{
    internal class MessengerService
    {

        public event EventHandler<ClientMessageEventArgs>? StopTheRace;
        public MessengerService()
        {
            // Initialize the messenger service
            ClientStore.MessageReceived += ParseMessage;
        }

        private void ParseMessage(object? sender, ClientMessageEventArgs e)
        {
            // Implement message parsing logic here
            // For example, you could parse JSON messages or command strings
            StopRaceMessage stopRaceMessage = new StopRaceMessage(); 
            stopRaceMessage = JsonSerializer.Deserialize<StopRaceMessage>(e.Message);
            if (stopRaceMessage != null && stopRaceMessage.type == "pauseRace")
            {
                // Handle the pause race message
                Debug.WriteLine("Received pause race message from client: " + e.ClientId);
                StopTheRace?.Invoke(this, e);
                // Implement the logic to pause the race here
            }
        }
    }

    public class StopRaceMessage
    { 
        public String type = "pauseRace";
    }
}
