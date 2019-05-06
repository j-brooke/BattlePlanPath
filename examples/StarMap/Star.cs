using System;

namespace StarMap
{
    /// <summary>
    /// Class describing a star.  In this example, spaceships can travel from one star to another
    /// but they can't stop or turn inbetween.
    /// </summary>
    internal class Star
    {
        public string Name { get; }
        public int LocationX { get; }
        public int LocationY { get; }

        public Star(string name, int locX, int locY)
        {
            this.Name = name;
            this.LocationX = locX;
            this.LocationY = locY;
        }
    }
}
