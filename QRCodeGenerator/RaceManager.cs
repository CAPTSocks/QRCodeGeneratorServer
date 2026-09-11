using System;

namespace QRCodeGenerator
{
    /// <summary>
    /// Manages race and lap statistics.
    /// </summary>
    public class RaceManager
    {
        private int _lapNumber = 0;
        private TimeSpan _bestLap = TimeSpan.MaxValue;
        private readonly Random _rnd = new Random();

        /// <summary>
        /// Gets the current lap number.
        /// </summary>
        public int LapNumber => _lapNumber;

        /// <summary>
        /// Gets the best lap time recorded.
        /// </summary>
        public TimeSpan BestLap => _bestLap == TimeSpan.MaxValue ? TimeSpan.Zero : _bestLap;

        /// <summary>
        /// Simulates a lap time between 60 and 90 seconds.
        /// </summary>
        public TimeSpan SimulateLapTime()
        {
            const double minSeconds = 60.0;
            const double maxSeconds = 90.0;
            double lapSeconds = minSeconds + _rnd.NextDouble() * (maxSeconds - minSeconds);
            return TimeSpan.FromSeconds(lapSeconds);
        }

        /// <summary>
        /// Records a new lap and updates best lap time if applicable.
        /// </summary>
        public LapStats RecordLap(int racePosition = 1)
        {
            _lapNumber++;
            var lapTime = SimulateLapTime();
            UpdateBestLap(lapTime);

            return new LapStats
            {
                LapNumber = _lapNumber,
                LapTimeSeconds = lapTime.TotalSeconds,
                BestLapTimeSeconds = BestLap.TotalSeconds,
                RacePosition = racePosition
            };
        }

        public RaceStartInfo GetRaceStartInfo()
        {
            return new RaceStartInfo
            {
                RaceID = 12345,
                RaceName = "Grand Prix",
                RaceDate = DateTime.Now,
                RaceHeat = "Heat 1"
            };
        }

        /// <summary>
        /// Resets the race state.
        /// </summary>
        public void Reset()
        {
            _lapNumber = 0;
            _bestLap = TimeSpan.MaxValue;
        }

        /// <summary>
        /// Updates the best lap time if the provided time is faster.
        /// </summary>
        private void UpdateBestLap(TimeSpan lapTime)
        {
            if (lapTime < _bestLap)
                _bestLap = lapTime;
        }
    }
}
