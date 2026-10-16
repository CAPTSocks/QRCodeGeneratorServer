using System;
using System.Linq;
using System.Windows;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace QRCodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NetworkService _networkService;
        private readonly ServerManager _serverManager;
        private readonly RaceManager _raceManager;
        private readonly QRCodeService _qrCodeService;
        private readonly MessageDisplayHandler _messageDisplayHandler;
        private List<RaceStartInfo> _raceStartInfos = new List<RaceStartInfo>();

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;

            // Initialize services
            _networkService = new NetworkService();
            _serverManager = new ServerManager(5000);
            _raceManager = new RaceManager();
            _qrCodeService = new QRCodeService();
            _messageDisplayHandler = new MessageDisplayHandler(MessagesListBox);

            RaceStartInfo raceInfo1 = _raceManager.GetRaceStartInfo();

            raceInfo1.RaceName = "Test Race 1";
            raceInfo1.RaceHeat = "Heat A";

            RaceStartInfo raceInfo2 = _raceManager.GetRaceStartInfo();
            raceInfo2.RaceName = "Test Race 2";
            raceInfo2.RaceHeat = "Heat B";
            raceInfo2.canPauseRace = true;
            raceInfo2.showFuelBar = true;

            RaceStartInfo raceInfo3 = _raceManager.GetRaceStartInfo();
            raceInfo3.RaceName = "Test Race 3";
            raceInfo3.RaceHeat = "Heat C";

            RaceStartInfo raceInfo4 = _raceManager.GetRaceStartInfo();
            raceInfo4.RaceName = "Test Practice 1";
            raceInfo4.RaceHeat = "Practice Heat";
            raceInfo4.RaceType = "Practice";

            _raceStartInfos.Add(raceInfo1);
            _raceStartInfos.Add(raceInfo2);
            _raceStartInfos.Add(raceInfo3);
            _raceStartInfos.Add(raceInfo4);

            foreach (var raceInfo in _raceStartInfos)
            {
                RaceSelector.Items.Add(raceInfo.RaceName);
            }

            // Subscribe to events
            _serverManager.StatusChanged += ServerManager_StatusChanged;
            ClientStore.MessageReceived += ClientStore_MessageReceived;
            ClientStore.ClientsChanged += ClientStore_ClientsChanged;

            // MessengerService messengerService = new MessengerService();
            // messengerService.StopTheRace += StopRaceTest;
            StartRaceButton.IsEnabled = false;
        }

        public void ReturnToMainWindow()
        {
            // Hide the race setup/status page.
            RacePageHost.Visibility = Visibility.Collapsed;
            RacePageHost.Content = null;

            // Show the main interface again.
            MainLayout.Visibility = Visibility.Visible;
        }

        public void NavigateToRaceStatus(int durationMinutes, RaceStartInfo raceInfo)
        {
            MainLayout.Visibility = Visibility.Collapsed;

            RacePageHost.Content =
                new RaceStatus(durationMinutes, raceInfo);

            RacePageHost.Visibility = Visibility.Visible;
        }

        private void OpenRaceSimulatorButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the main interface.
            MainLayout.Visibility = Visibility.Collapsed;

            // Display the race setup page.
            RacePageHost.Content = new RaceSimulator();
            RacePageHost.Visibility = Visibility.Visible;
        }



        private void ClientStore_ClientsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectedClientsBox.Items.Clear();
                foreach (var info in ClientStore.ClientInfos.Values)
                {
                    ConnectedClientsBox.Items.Add(info.ToString());
                }

                foreach (var info in ClientStore.ClientInfos.Values)
                {
                    var racer = Racers.GetRacerByPin(info.pin); // Ensure racer is registered
                    if (racer == null)
                    {
                        InfoText.Text += $"\nRacer not found for client: {info}";
                    }
                    else
                    {
                        InfoText.Text += $"\nRacer found in list: {info} - {racer.RacerFirstName} {racer.RacerLastName}";
                    }

                }
            });
        }

        private void ServerManager_StatusChanged(object? sender, ServerStatusEventArgs e)
        {
            InfoText.Text += $"\n{e.Message}";
        }

        private void ClientStore_MessageReceived(object? sender, ClientMessageEventArgs e)
        {
            _messageDisplayHandler.DisplayMessage(e);
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_serverManager.IsListening)
                    _serverManager.StartServer();

                string? ip = _networkService.GetLocalIPv4();
                if (string.IsNullOrEmpty(ip))
                {
                    InfoText.Text = "Could not determine local IPv4 address.";
                    return;
                }

                InfoText.Text = $"Local IP: {ip}\nStarting WebSocket server...";

                string payload = $"ws://{ip}:5000/mobile";
                QrImage.Source = _qrCodeService.GenerateQRCode(payload);

                InfoText.Text = $"Local IP: {ip}\nScan the QR code to connect to ws://{ip}:5000/";
            }
            catch (Exception ex)
            {
                InfoText.Text = $"Error: {ex.Message}";
            }
        }

        private void SendRaceButton_Click(object sender, RoutedEventArgs e)
        {
            RaceStartInfo raceInfo = _raceStartInfos[RaceSelector.SelectedIndex];
            foreach (var clientID in ClientStore.Clients.Keys)
                ClientStore.TrySendJsonToClient(clientID, raceInfo);
        }

        private void SendTestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stats = _raceManager.RecordLap();

                var firstClientId = ClientStore.Clients.Skip(0).First().Key;
                if (firstClientId == null)
                {
                    InfoText.Text += "\nNo connected clients to send lap.";
                    return;
                }

                bool sent = ClientStore.TrySendJsonToClient(firstClientId, stats);
                InfoText.Text += sent
                    ? $"\nSent lap {stats.LapNumber} to {firstClientId}"
                    : $"\nFailed to send lap to {firstClientId}";
            }
            catch (Exception ex)
            {
                InfoText.Text += $"\nError sending lap: {ex.Message}";
            }
        }

        private void SendLapButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stats = _raceManager.RecordLap();

                var firstClientId = ClientStore.Clients.Skip(1).First().Key;
                if (firstClientId == null)
                {
                    InfoText.Text += "\nNo connected clients to send lap.";
                    return;
                }

                bool sent = ClientStore.TrySendJsonToClient(firstClientId, stats);
                InfoText.Text += sent
                    ? $"\nSent lap {stats.LapNumber} to {firstClientId}"
                    : $"\nFailed to send lap to {firstClientId}";
            }
            catch (Exception ex)
            {
                InfoText.Text += $"\nError sending lap: {ex.Message}";
            }
        }

        private void SendFuelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fuelData = new FuelData { FuelLevel = Random.Shared.Next(0, 101) }; // Example fuel level
                var firstClientId = ClientStore.Clients.Skip(0).First().Key;
                if (firstClientId == null)
                {
                    InfoText.Text += "\nNo connected clients to send fuel data.";
                    return;
                }
                bool sent = ClientStore.TrySendJsonToClient(firstClientId, fuelData);
                InfoText.Text += sent
                    ? $"\nSent fuel data to {firstClientId}"
                    : $"\nFailed to send fuel data to {firstClientId}";
            }
            catch (Exception ex)
            {
                InfoText.Text += $"\nError sending fuel data: {ex.Message}";
            }
        }

        private void ClearMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            _messageDisplayHandler.ClearMessages();
        }

        private void RaceSelector_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RaceStartInfo? selectedRace = _raceStartInfos.ElementAtOrDefault(RaceSelector.SelectedIndex);
            RaceNameTextBox.Text = selectedRace?.RaceName ?? string.Empty;
            HeatTextBox.Text = selectedRace?.RaceHeat ?? string.Empty;
        }

        private void StopRaceTest(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                StartRaceButton.IsEnabled = true;
            });
        }

        private void StartRaceButton_Click(object sender, RoutedEventArgs e)
        {
            var raceStartMessage = new { Type = "unpauseRace" };
            foreach (var clientID in ClientStore.Clients.Keys)
                ClientStore.TrySendJsonToClient(clientID, raceStartMessage);
            StartRaceButton.IsEnabled = false;
        }

        private void StartTimerButton_Click(object sender, RoutedEventArgs e)
        {
            var raceTimeStart = new RaceTimeStart { StartTime = DateTime.Now };
            Debug.WriteLine("Race Start Time: " + raceTimeStart.StartTime);
            foreach (var clientID in ClientStore.Clients.Keys)
                ClientStore.TrySendJsonToClient(clientID, raceTimeStart);
        }

        private void SendNotificationButton_Click(object sender, RoutedEventArgs e)
        {
            var notification = new RaceNotification();
            foreach (var clientID in ClientStore.Clients.Keys)
                ClientStore.TrySendJsonToClient(clientID, notification);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
            => _serverManager.StopServer();


    }
}
