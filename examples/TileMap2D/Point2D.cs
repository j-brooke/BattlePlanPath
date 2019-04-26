using System;

namespace TileMap2D
{
    /// <summary>
    /// 2D point in integer space.  Immutable value type.
    /// </summary>
    internal struct Point2D : IEquatable<Point2D>
    {
        public short X { get; }
        public short Y { get; }

        public Point2D(short x, short y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point2D(int x, int y)
        {
            this.X = (short)x;
            this.Y = (short)y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherPoint = (Point2D)obj;
            return this == otherPoint;
        }

        public bool Equals(Point2D other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return (this.X.GetHashCode() * 7901) ^ (this.Y.GetHashCode());
        }

        public override string ToString()
        {
            return $"({this.X},{this.Y})";
        }

        public static bool operator==(Point2D pointA, Point2D pointB)
        {
            return pointA.X == pointB.X & pointA.Y == pointB.Y;
        }

        public static bool operator!=(Point2D pointA, Point2D pointB)
        {
            return !(pointA==pointB);
        }
    }
}
