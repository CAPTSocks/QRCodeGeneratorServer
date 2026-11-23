
using System;
using System.Windows;
using System.Windows.Controls;

namespace QRCodeGenerator
{
    public partial class RaceSetup : UserControl
    {
        public RaceSetup()
        {
            InitializeComponent();

            Loaded += RaceSetup_Loaded;
            Unloaded += RaceSetup_Unloaded;
        }

        private MainWindow? GetMainWindow()
        {
            return Application.Current.MainWindow as MainWindow;
        }

        // Subscribe while this page is displayed.
        private void RaceSetup_Loaded(
            object sender, RoutedEventArgs e)
        {
            ClientStore.ClientsChanged -= ClientStore_ClientsChanged;
            ClientStore.ClientsChanged += ClientStore_ClientsChanged;

            RefreshConnectedClients();

            if (GetMainWindow()?.IsServerRunning == true)
            {
                ServerStatusText.Text = "Server Running";
                ServerStatusText.Foreground =
                    System.Windows.Media.Brushes.LightGreen;

                StartServerButton.IsEnabled = false;
                SetupRaceButton.IsEnabled = true;
            }
        }

        private void RaceSetup_Unloaded(
            object sender, RoutedEventArgs e)
        {
            ClientStore.ClientsChanged -= ClientStore_ClientsChanged;
        }

        // Start the WebSocket server and display its QR code.
        private void StartServerButton_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = GetMainWindow();

                if (mainWindow == null)
                    return;

                var connection = mainWindow.StartServer();

                QrImage.Source = connection.QrCode;

                ServerAddressText.Text = connection.Url;

                ServerStatusText.Text = "Server Running";
                ServerStatusText.Foreground =
                    System.Windows.Media.Brushes.LightGreen;

                StartServerButton.IsEnabled = false;
                SetupRaceButton.IsEnabled = true;

                RefreshConnectedClients();
            }
            catch (Exception ex)
            {
                ServerStatusText.Text = "Server Error";
                ServerStatusText.Foreground =
                    System.Windows.Media.Brushes.IndianRed;

                MessageBox.Show(
                    $"Failed to start server:\n{ex.Message}",
                    "Server Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Update whenever mobile clients connect or disconnect.
        private void ClientStore_ClientsChanged(
            object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(
                RefreshConnectedClients));
        }


        private void RefreshConnectedClients()
        {
            ConnectedClientsBox.Items.Clear();

            foreach (var entry in ClientStore.ClientInfos)
            {
                string clientId = entry.Key;
                ClientInfo info = entry.Value;

                bool isConnected =
                    ClientStore.Clients.ContainsKey(clientId);

                string status = isConnected
                    ? "Connected"
                    : "Disconnected";

                string name =
                    $"{info.FirstName} {info.LastName}".Trim();

                if (string.IsNullOrEmpty(name))
                    name = "Unregistered driver";

                ConnectedClientsBox.Items.Add(
                    $"{name} - {status}");
            }

            int connectedCount = ClientStore.Clients.Count;
            int registeredCount = ClientStore.ClientInfos.Count;

            ConnectedClientsTitle.Text =
                $"Connected Racers ({connectedCount}/{registeredCount})";
        }


        // Validate race settings, send race information,
        // and navigate to RaceStatus.
        private void SetupRaceButton_Click(
            object sender, RoutedEventArgs e)
        {
            var mainWindow = GetMainWindow();

            if (mainWindow?.IsServerRunning != true)
            {
                MessageBox.Show(
                    "Please start the server first.",
                    "Server Offline",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string raceName = RaceNameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(raceName))
            {
                MessageBox.Show(
                    "Please enter a race name.",
                    "Invalid Race Name",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DurationBox.Text, out int raceLength)
                || raceLength <= 0)
            {
                MessageBox.Show(
                    "Please enter a valid positive race length.",
                    "Invalid Race Length",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool isMinuteRace =
                (DurationUnitBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() == "Minutes";

            string raceHeat =
                (HeatBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() ?? "Heat 1";

            // Build the race information.
            RaceStartInfo newRace = new RaceStartInfo
            {
                RaceName = raceName,
                RaceHeat = raceHeat,
                RaceDate = DateTime.Now,
                RaceLength = raceLength,
                IsMinuteRace = isMinuteRace,
                RaceType = PracticeCheckbox.IsChecked == true
                    ? "Practice"
                    : "Race",
                canPauseRace = AllowPausingCheckBox.IsChecked == true,
                showFuelBar = IncludeFuelCheckBox.IsChecked == true
            };

            // Create the authoritative race session before sending
            // race information to the connected phones.
            RaceSessionStore.Current = new RaceSession(
                newRace,
                ClientStore.ClientInfos.Keys
            );

            // Send the race information to all connected phones.
            foreach (var clientID in ClientStore.Clients.Keys)
            {
                ClientStore.TrySendJsonToClient(
                    clientID, newRace);
            }

            // Open the existing race status screen.
            mainWindow.NavigateToRaceStatus(
                raceLength,
                newRace,
                isMinuteRace);
        }
    }
}
