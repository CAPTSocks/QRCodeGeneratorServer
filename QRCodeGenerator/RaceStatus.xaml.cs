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
        private readonly Stopwatch _raceStopwatch = new Stopwatch();
        private List<int> racerCurrentLaps = new List<int>();
        private DateTime _lastTimerUpdate;
        private TimeSpan _remainingDuration;
        private int totalLaps = 0;
        private int _highestLap = 0;
        private bool _isPaused = false;
        private bool _isFinished = false;
        private bool _hasStarted = false;
        private bool _isMinuteRace = true;

        private RaceManager raceManager;
        private MessengerService messenger;

        public RaceStatus(int durationMinutes, RaceStartInfo raceInfo, bool isMinuteRace)
        {
            InitializeComponent();

            _isMinuteRace = isMinuteRace;

            if (isMinuteRace)
            {
                _remainingDuration = TimeSpan.FromMinutes(durationMinutes);
            }
            else
            {
                totalLaps = durationMinutes;
                LapCountText.Text = $"Laps 0/{totalLaps}";
                LapCountText.Visibility = Visibility.Visible;
            }

            
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
            if (_isMinuteRace)
            {
                TimeSpan elapsed = now - _lastTimerUpdate;
                _remainingDuration -= elapsed;
                _lastTimerUpdate = now;
            }
            else
            {
                TimeSpan elapsed = now - _lastTimerUpdate;
                _remainingDuration += elapsed;
                _lastTimerUpdate = now;
            }

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
            if (!_isMinuteRace)
            {
                CheckHighestLap();
            }
            newLap.LapLeaderCurrentLap = _highestLap;

            LatestEventDetails.Text = "Sent lap " + newLap.LapNumber.ToString() + " to " + ClientStore.ClientInfos[clientIds[randomClient]].FirstName.ToString();
            
            var ClientIDToSend = clientIds[randomClient];
            ClientStore.TrySendJsonToClient(ClientIDToSend, newLap);

            //Send update to other clinets about the lap leader's current lap
            var lapLeaderUpdate = new LapLeaderUpdate
            {
                LapLeaderCurrentLap = _highestLap
            };
            foreach (var clientID in ClientStore.Clients.Keys)
            {
                if (clientID != ClientIDToSend)
                {
                    ClientStore.TrySendJsonToClient(clientID, lapLeaderUpdate);
                }
            }


        }

        private void CheckHighestLap()
        {
            _highestLap = 0;
            foreach (var lap in racerCurrentLaps)
            {
                if (lap > _highestLap)
                {
                    _highestLap = lap;
                }
            }
            LapCountText.Text = $"Laps {_highestLap}/{totalLaps}";
            if (_highestLap >= totalLaps)
            {
                _isFinished = true;
                _countdownTimer.Stop();
                _lapTimer.Stop();
                RaceStateText.Text = "Race finished";
                LatestEventTitle.Text = "Race completed!";
                LatestEventDetails.Text =
                    "The race has reached the total number of laps.";
                EndRace();
            }       
        }

        private void EndRace()
        {
            _raceStopwatch.Stop();

            long raceTimeMiliseconds = _raceStopwatch.ElapsedMilliseconds;
            RaceFinished raceFinMessage = new RaceFinished();
            raceFinMessage.ElapsedRaceTime = raceTimeMiliseconds;

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
                _raceStopwatch.Stop();

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
                _raceStopwatch.Start();
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

            _raceStopwatch.Start(); 

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