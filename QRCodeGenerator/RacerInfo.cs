using System;
using System.Collections.Generic;
using System.Text;

namespace QRCodeGenerator
{
    internal static class Racers
    {
        public static List<RacerData> GetRacers()
        {
            return new List<RacerData>
            {
                new RacerData { RacerFirstName = "Wyat", RacerLastName = "Hedge", RacerPin = 1234 },
                new RacerData { RacerFirstName = "Jim", RacerLastName = "Smith", RacerPin = 5555 },
                new RacerData { RacerFirstName = "Alice", RacerLastName = "Johnson", RacerPin = 9012 },
                new RacerData { RacerFirstName = "Bob", RacerLastName = "Brown", RacerPin = 3456 }
            };
        }

        public static RacerData GetRacerByPin(int pin)
        {
            var racers = GetRacers();
            return racers.Find(r => r.RacerPin == pin);
        }

        public static void RegisterRacer(RacerData newRacer)
        {
            var racers = GetRacers();
            racers.Add(newRacer);
        }

        public static void RemoveRacer(int pin)
        {
            var racers = GetRacers();
            var racerToRemove = racers.Find(r => r.RacerPin == pin);
            if (racerToRemove != null)
            {
                racers.Remove(racerToRemove);
            }
        }
    }

    internal class RacerData
    {
        public string RacerFirstName { get; set; } = "John";
        public string RacerLastName { get; set; } = "Doe";
        public int RacerPin { get; set; } = 0000;
    }
}
