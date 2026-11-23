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
            const double minSeconds = 4.0;
            const double maxSeconds = 10.0;
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
                BehindLeaderSeconds = _rnd.NextDouble() * -1,
                RacePosition = racePosition
            };
        }

        public LapStats CreateLapForDriver(
            int lapNumber,
            double previousBestLapSeconds,
            int racePosition = 1)
        {
            double lapTime = SimulateLapTime().TotalSeconds;

            double bestLap =
                previousBestLapSeconds <= 0
                    ? lapTime
                    : Math.Min(previousBestLapSeconds, lapTime);

            // Simulate a gap from the race leader.
            // The leader is always 0 seconds behind.
            double behindLeader = racePosition == 1
                ? 0
                : Math.Round(_rnd.NextDouble() * 5.0, 3);

            return new LapStats
            {
                LapNumber = lapNumber,
                LapTimeSeconds = lapTime,
                BestLapTimeSeconds = bestLap,
                BehindLeaderSeconds = behindLeader,
                RacePosition = racePosition
            };
        }



        public RaceStartInfo GetRaceStartInfo()
        {
            return new RaceStartInfo
            {
                RaceID = Guid.NewGuid().ToString(),
                RaceName = "Grand Prix",
                RaceDate = DateTime.Now,
                RaceHeat = "Heat 1",
                RaceType = "Race"
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
