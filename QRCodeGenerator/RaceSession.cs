


using System;
using System.Collections.Generic;
using System.Linq;

namespace QRCodeGenerator
{
    public enum RaceSessionStatus
    {
        Waiting,
        Running,
        Paused,
        Finished
    }

    public class DriverRaceState
    {
        public string ClientId { get; }
        public List<LapStats> Laps { get; } = new();
        public int CurrentLap => Laps.Count;
        public double BestLapTimeSeconds =>
            Laps.Count == 0 ? 0 : Laps.Min(l => l.LapTimeSeconds);

        public DriverRaceState(string clientId)
        {
            ClientId = clientId;
        }
    }

    public class RaceSession
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, DriverRaceState> _drivers = new();

        // Elapsed duration accumulated at the last state change.
        private long _elapsedAtTransitionMs;
        private DateTime? _runningSinceUtc;

        public RaceStartInfo RaceInfo { get; }
        public RaceSessionStatus Status { get; private set; } = RaceSessionStatus.Waiting;
        public DateTime? EndTime { get; private set; }

        public long ElapsedRaceTimeMs
        {
            get
            {
                lock (_lock)
                    return GetElapsedMsLocked();
            }
        }

        public RaceSession(RaceStartInfo raceInfo, IEnumerable<string> participantIds)
        {
            RaceInfo = raceInfo;
            foreach (string id in participantIds)
            {
                _drivers.TryAdd(id, new DriverRaceState(id));
            }
        }

        public string[] GetParticipantIds()
        {
            lock (_lock)
                return _drivers.Keys.ToArray();
        }

        public int GetLapCount(string clientId)
        {
            lock (_lock)
                return _drivers.TryGetValue(clientId, out var driver)
                    ? driver.CurrentLap : 0;
        }

        public double GetBestLapSeconds(string clientId)
        {
            lock (_lock)
                return _drivers.TryGetValue(clientId, out var driver)
                    ? driver.BestLapTimeSeconds : 0;
        }

        public int GetHighestLap()
        {
            lock (_lock)
                return _drivers.Count == 0 ? 0 : _drivers.Values.Max(d => d.CurrentLap);
        }

        public bool RecordLap(string clientId, LapStats lap)
        {
            lock (_lock)
            {
                if (Status != RaceSessionStatus.Running ||
                    !_drivers.TryGetValue(clientId, out var driver) ||
                    lap.LapNumber != driver.CurrentLap + 1)
                    return false;

                driver.Laps.Add(lap);
                return true;
            }
        }

        public List<LapStats> GetLapHistory(string clientId)
        {
            lock (_lock)
                return _drivers.TryGetValue(clientId, out var driver)
                    ? driver.Laps.ToList() : new List<LapStats>();
        }

        private long GetElapsedMsLocked()
        {
            long elapsed = _elapsedAtTransitionMs;
            if (Status == RaceSessionStatus.Running && _runningSinceUtc.HasValue)
            {
                elapsed += Math.Max(0, (long)(DateTime.UtcNow - _runningSinceUtc.Value).TotalMilliseconds);
            }
            return elapsed;
        }

        public void SetStatus(RaceSessionStatus status, long elapsedTimeMs)
        {
            lock (_lock)
            {
                if (Status == RaceSessionStatus.Finished)
                    return;

                _elapsedAtTransitionMs = Math.Max(0, elapsedTimeMs);
                _runningSinceUtc = status == RaceSessionStatus.Running
                    ? DateTime.UtcNow : null;
                Status = status;
                if (status == RaceSessionStatus.Finished)
                    EndTime = DateTime.Now;
            }
        }

        // Returns a complete, consistent snapshot for a registered race participant.
        // The DTO is anonymous because it is serialized by ClientStore's camelCase serializer.
        public object? CreateSyncMessage(string clientId)
        {
            lock (_lock)
            {
                if (!_drivers.TryGetValue(clientId, out var driver))
                    return null;

                long elapsed = GetElapsedMsLocked();
                long remaining = RaceInfo.IsMinuteRace
                    ? Math.Max(0, (long)TimeSpan.FromMinutes(RaceInfo.RaceLength).TotalMilliseconds - elapsed)
                    : 0;

                int leaderLap = _drivers.Count == 0
                    ? 0 : _drivers.Values.Max(d => d.CurrentLap);

                return new
                {
                    Type = "raceSync",
                    RaceInfo,
                    RaceState = Status.ToString().ToLowerInvariant(),
                    ElapsedRaceTime = elapsed,
                    RemainingRaceTimeMs = remaining,
                    EndTime,
                    LapLeaderCurrentLap = leaderLap,
                    Laps = driver.Laps.ToArray()
                };
            }
        }
    }

    public static class RaceSessionStore
    {
        public static RaceSession? Current { get; set; }
    }
}

