using System;
using System.Text.Json;

namespace QRCodeGenerator
{
    /// <summary>
    /// DTO for lap and race statistics to be sent to clients.
    /// </summary>
    public class LapStats
    {
        public string raceData { get; set; } = "RaceData";
        public int LapNumber { get; set; }
        public double LapTimeSeconds { get; set; }
        public double BestLapTimeSeconds { get; set; }
        public int RacePosition { get; set; }
    }

    /// <summary>
    /// Generates JSON payloads from LapStats objects.
    /// </summary>
    public static class JsonGenerator
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
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
                raceData = "RaceData",
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
        public static string GenerateJson(LapStats stats)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            return JsonSerializer.Serialize(stats, Options);
        }
    }
}
