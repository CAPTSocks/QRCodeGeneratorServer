
using System;
using System.Windows;
using System.Windows.Media;

namespace QRCodeGenerator
{
    public partial class MainWindow : Window
    {
        private readonly NetworkService _networkService;
        private readonly ServerManager _serverManager;
        private readonly QRCodeService _qrCodeService;

        private readonly RaceSetup _raceSetup;

        public MainWindow()
        {
            InitializeComponent();

            _networkService = new NetworkService();
            _serverManager = new ServerManager(5000);
            _qrCodeService = new QRCodeService();

            _raceSetup = new RaceSetup();

            // Display setup immediately on launch.
            RacePageHost.Content = _raceSetup;

            Closing += MainWindow_Closing;
        }

        // Starts the server and generates its connection QR code.
        public (string Url, ImageSource QrCode) StartServer()
        {
            string? ip = _networkService.GetLocalIPv4();

            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new InvalidOperationException(
                    "Could not determine local IPv4 address.");
            }

            if (!_serverManager.IsListening)
            {
                _serverManager.StartServer();
            }

            string url = $"ws://{ip}:5000/mobile";

            ImageSource qrCode =
                _qrCodeService.GenerateQRCode(url);

            return (url, qrCode);
        }

        public bool IsServerRunning =>
            _serverManager.IsListening;

        // Navigate from setup to the race status screen.
        public void NavigateToRaceStatus(
            int raceLength,
            RaceStartInfo raceInfo,
            bool isMinuteRace)
        {
            RacePageHost.Content = new RaceStatus(
                raceLength,
                raceInfo,
                isMinuteRace);
        }

        // Return to the race setup screen.
        public void ReturnToMainWindow()
        {
            RacePageHost.Content = _raceSetup;
        }

        private void MainWindow_Closing(
            object? sender,
            System.ComponentModel.CancelEventArgs e)
        {
            _serverManager.StopServer();
        }
    }
}
