using System;

namespace StarMap
{
    /// <summary>
    /// Class describing the capabilities of a particular type of spaceship (at least
    /// as they pertain to pathfinding.)
    /// </summary>
    internal class ShipCharacteristics
    {
        public string ShipClass { get; }
        public double MaxJumpDistance { get; }
        public bool WormholeCapable { get; }

        public ShipCharacteristics(string shipClass, double maxJump, bool wormholeCapable)
        {
            this.ShipClass = shipClass;
            this.MaxJumpDistance = maxJump;
            this.WormholeCapable = wormholeCapable;
        }
    }
}
