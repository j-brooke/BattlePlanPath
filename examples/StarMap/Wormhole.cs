using System;

namespace StarMap
{
    /// <summary>
    /// Describes a one-way wormhole between two stars that can be traversed very quickly
    /// by a ship with the right equipment.
    /// </summary>
    public class Wormhole
    {
        public string EntryStar { get; }
        public string ExitStar { get; }

        public Wormhole(string entryStar, string exitStar)
        {
            this.EntryStar = entryStar;
            this.ExitStar = exitStar;
        }
    }
}
