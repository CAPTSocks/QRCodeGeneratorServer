using System;
using System.Text.Json;

namespace QRCodeGenerator
{

    public class  baseJsonStats
    {
        
    }
    /// <summary>
    /// DTO for lap and race statistics to be sent to clients.
    /// </summary>
    public class LapStats : baseJsonStats
    {
        public string Type { get; set; } = "lapData";
        public int LapNumber { get; set; }
        public double LapTimeSeconds { get; set; }
        public double BestLapTimeSeconds { get; set; }
        public double BehindLeaderSeconds { get; set; }
        public int RacePosition { get; set; }
    }

    public class RaceStartInfo : baseJsonStats
    {
        public string Type { get; set; } = "raceInfo";
        public string RaceID { get; set; } = "";
        public string RaceName { get; set; } = "DefaultRaceName";
        public DateTime RaceDate { get; set; } = DateTime.Now;
        public string RaceHeat { get; set; } = "DefaultHeat";
        public string RaceType { get; set; } = "Race";

    }

    public class FuelData : baseJsonStats
    {
        public string Type { get; set; } = "fuelData";
        public int FuelLevel { get; set; } = 0;
    }

    public class RaceTimeStart : baseJsonStats
    {
        public string Type { get; set; } = "startRaceTimer";
        public DateTime StartTime { get; set; } = DateTime.Now;
    }

    public class RaceNotification : baseJsonStats
    {
        public string Type { get; set; } = "notification";
        public string Title { get; set; } = "Race coming up.";
        public string Message { get; set; } = "Your race is coming up next!";
        public int notificationId { get; set; } = 0;
    }

    /// <summary>
    /// Generates JSON payloads from LapStats objects.
    /// </summary>
    public static class JsonGenerator
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Creates a LapStats instance and serializes it to JSON.
        /// </summary>
        public static string GenerateJson(int lapNumber, TimeSpan lapTime, TimeSpan bestLapTime, int racePosition)
        {
            var stats = new LapStats
            {
                Type = "lapData",
                LapNumber = lapNumber,
                LapTimeSeconds = lapTime.TotalSeconds,
                BestLapTimeSeconds = bestLapTime.TotalSeconds,
                RacePosition = racePosition
            };

            return JsonSerializer.Serialize(stats, Options);
        }

        /// <summary>
        /// Serializes an existing LapStats object to JSON.
        /// </summary>
        public static string GenerateJson(baseJsonStats stats)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            return JsonSerializer.Serialize(stats, Options);
        }
    }
}
