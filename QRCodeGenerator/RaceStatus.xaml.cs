using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QRCodeGenerator
{
    public partial class RaceStatus : UserControl
    {
        private readonly DateTime _raceStartTime;
        private readonly TimeSpan _raceDuration;
        private readonly DispatcherTimer _countdownTimer;
        private readonly DispatcherTimer _lapTimer;
        private List<int> racerCurrentLaps = new List<int>();
        private DateTime _lastTimerUpdate;
        private TimeSpan _remainingDuration;
        private bool _isPaused = false;
        private bool _isFinished = false;
        private bool _hasStarted = false;

        private RaceManager raceManager;
        private MessengerService messenger;

        public RaceStatus(int durationMinutes, RaceStartInfo raceInfo)
        {
            InitializeComponent();

            _remainingDuration = TimeSpan.FromMinutes(durationMinutes);
            _lastTimerUpdate = DateTime.Now;

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _countdownTimer.Tick += CountdownTimer_Tick;

            _lapTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            _lapTimer.Tick += LapTimer_Tick;

            Loaded += RaceStatus_Loaded;
            Unloaded += RaceStatus_Unloaded;

            foreach (var clientID in ClientStore.Clients)
                racerCurrentLaps.Add(0);

            raceManager = new RaceManager();
            messenger = new MessengerService();
            messenger.StopTheRace += PauseRace;

        }

        private void RaceStatus_Loaded(object sender, RoutedEventArgs e)
        {
            _lastTimerUpdate = DateTime.Now;

            // Display the initial duration without starting the countdown.
            TimeRemainingText.Text =
                $"{(int)_remainingDuration.TotalMinutes:00}:" +
                $"{_remainingDuration.Seconds:00}";

            RaceStateText.Text = "Ready to start";
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (_isPaused || _isFinished)
                return;

            DateTime now = DateTime.Now;

            // Subtract only the time elapsed since the last update.
            TimeSpan elapsed = now - _lastTimerUpdate;
            _remainingDuration -= elapsed;
            _lastTimerUpdate = now;

            if (_remainingDuration < TimeSpan.Zero)
                _remainingDuration = TimeSpan.Zero;

            TimeRemainingText.Text =
                $"{(int)_remainingDuration.TotalMinutes:00}:" +
                $"{_remainingDuration.Seconds:00}";

            if (_remainingDuration == TimeSpan.Zero)
            {
                _isFinished = true;

                _countdownTimer.Stop();
                _lapTimer.Stop();

                RaceStateText.Text = "Race finished";
                LatestEventTitle.Text = "Time is up!";
                LatestEventDetails.Text =
                    "The race countdown has reached zero.";
                EndRace();
            }
            else
            {
                RaceStateText.Text = "Race in progress";
            }
        }

        private void LapTimer_Tick(object? sender, EventArgs e)
        {
            SendRandomRacerLap();
        }

        private void SendRandomRacerLap()
        {
            // TODO: Add your logic to select a random connected racer
            // and send them a lap.
            var clientIds = ClientStore.Clients.Keys.ToArray();

            if (clientIds.Length == 0)
            {
                return;
            }

            Random rand = new Random();
            Debug.WriteLine("Function called");
            int randomClient = rand.Next(0, clientIds.Length);
            var newLap = CreateLap();
            racerCurrentLaps[randomClient]++;
            newLap.LapNumber = racerCurrentLaps[randomClient];

            LatestEventDetails.Text = "Sent lap " + newLap.LapNumber.ToString() + " to " + ClientStore.ClientInfos[clientIds[randomClient]].FirstName.ToString();
            var ClientIDToSend = clientIds[randomClient];
            ClientStore.TrySendJsonToClient(ClientIDToSend, newLap);


        }

        private void EndRace()
        {
            RaceFinished raceFinMessage = new RaceFinished();
            foreach (var clientID in ClientStore.Clients.Keys)
            {
                ClientStore.TrySendJsonToClient(clientID, raceFinMessage);
            }

        }

        private void PauseRace(object? sender, ClientMessageEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_isPaused || _isFinished)
                    return;

                // Account for time elapsed since the last countdown tick.
                DateTime now = DateTime.Now;
                _remainingDuration -= now - _lastTimerUpdate;

                if (_remainingDuration < TimeSpan.Zero)
                    _remainingDuration = TimeSpan.Zero;

                _lastTimerUpdate = now;
                _isPaused = true;

                _countdownTimer.Stop();
                _lapTimer.Stop();

                string pauserName = "Unknown racer";

                if (ClientStore.ClientInfos.TryGetValue(e.ClientId, out var clientInfo))
                {
                    pauserName = clientInfo.FirstName;
                }

                PauseRaceForOtherClients(e);
                UpdatePausedDisplay(pauserName);
            });
        }

        private void PauseRaceForOtherClients(ClientMessageEventArgs pauserInfo)
        {
            var raceStartMessage = new { Type = "pauseRace" };
            foreach (var clientID in ClientStore.Clients.Keys)
            {
                if (clientID != pauserInfo.ClientId)
                {
                    ClientStore.TrySendJsonToClient(clientID, raceStartMessage);
                }
            }
        }

        private void UpdatePausedDisplay(string pauserName)
        {
            TimeRemainingText.Text =
                $"{(int)_remainingDuration.TotalMinutes:00}:" +
                $"{_remainingDuration.Seconds:00}";

            RaceStateText.Text = "Race paused";
            LatestEventTitle.Text = "Race paused";
            LatestEventDetails.Text =
                "The race timer and lap simulation were paused by " + pauserName + ".";
            ResumeRaceBorder.Visibility = Visibility.Visible;
        }

        private void ResumeRace()
        {
            Dispatcher.Invoke(() =>
            {
                if (!_isPaused || _isFinished)
                    return;

                _isPaused = false;
                _lastTimerUpdate = DateTime.Now;

                _countdownTimer.Start();
                _lapTimer.Start();

                RaceStateText.Text = "Race in progress";
                LatestEventTitle.Text = "Race resumed";
                LatestEventDetails.Text =
                    "The race timer and lap simulation have resumed.";
                ResumeRaceBorder.Visibility = Visibility.Collapsed;
                SendResumeRaceToClients();
            });
        }

        private void SendResumeRaceToClients()
        {
            var raceStartMessage = new { Type = "unpauseRace" };
            foreach (var clientID in ClientStore.Clients.Keys)
            {
                    ClientStore.TrySendJsonToClient(clientID, raceStartMessage);
            }
        }

        private void ResumeRace_Click(object sender, RoutedEventArgs e)
        {
            ResumeRace(); 
        }

        private void StartRaceButton_Click(object sender, RoutedEventArgs e)
        {
            // Prevent starting the same race more than once.
            if (_hasStarted || _isPaused || _isFinished)
                return;

            _hasStarted = true;
            _lastTimerUpdate = DateTime.Now;

            // Start the countdown and simulated lap timers.
            _countdownTimer.Start();
            _lapTimer.Start();

            RaceStateText.Text = "Race in progress";
            LatestEventTitle.Text = "Race started";
            LatestEventDetails.Text =
                "The race countdown and lap simulation have started.";
            

            var raceTimeStart = new RaceTimeStart { StartTime = DateTime.Now };
            foreach (var clientID in ClientStore.Clients.Keys)
                ClientStore.TrySendJsonToClient(clientID, raceTimeStart);
            StartRaceBorder.Visibility = Visibility.Collapsed;
        }

        private LapStats CreateLap()
        {
            LapStats newLap = new LapStats();
            newLap = raceManager.RecordLap();
            return newLap;
        }

        private void RaceStatus_Unloaded(object sender, RoutedEventArgs e)
        {
            _countdownTimer.Stop();
            _lapTimer.Stop();
            messenger.StopTheRace -= PauseRace;
        }
    }
}