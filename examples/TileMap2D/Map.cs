using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileMap2D
{
    /// <summary>
    /// Simple 2D grid of characters.  Anything other than space is treated as a blocked tile.
    /// </summary>
    internal class Map
    {
        public int Width => _tileGrid.GetLength(0);
        public int Height => _tileGrid.GetLength(1);

        /// <summary>
        /// Create a map out of a grid of characters.  Each string in the rows enumeration is one row in the grid.
        /// A space is open/traversable, and any other character is blocked.
        /// </summary>
        public Map(IEnumerable<string> rows)
        {
            var height = rows.Count();
            var width = rows.Select( (rowStr) => rowStr?.Length ?? 0 ).Max();

            // Create a 2d array of chars and init it with spaces.
            _tileGrid = new char[width, height];
            for (int r=0; r<height; ++r)
                for (int c=0; c<width; ++c)
                    _tileGrid[c,r] = ' ';

            // Copy the given chars into the array.
            int y = 0;
            foreach (var rowStr in rows)
            {
                if (rowStr==null)
                    continue;
                for (int x=0; x<rowStr.Length; ++x)
                    _tileGrid[x,y] = rowStr[x];
                y += 1;
            }
        }

        /// <summary>
        /// Returns the character at the specified location in the grid.  Throws an ArgumentException if
        /// the given point is outside of the map's boundaries.
        /// </summary>
        public char GetTile(Point2D location)
        {
            if (location.X<0 || location.Y<0 || location.X >= this.Width || location.Y >= this.Height)
                throw new ArgumentException("Point out of map bounds");
            return _tileGrid[location.X, location.Y];
        }

        /// <summary>
        /// Returns true if tile is blocked (has a value other than space).
        /// </summary>
        public bool IsBlocked(Point2D location)
        {
            char tileSymbol = this.GetTile(location);
            return tileSymbol!=' ';
        }

        /// <summary>
        /// Return the map as an enumeration of strings, optionally embelishing it with path symbols.
        /// </summary>
        public IEnumerable<string> RowStrings(IEnumerable<Point2D> breadcrumbs,
            IEnumerable<Point2D> startSpots,
            IEnumerable<Point2D> goalSpots)
        {
            // Horrible, horrible algorithmic efficiency, but I'm trying to keep things simple for
            // sake of this example.
            for (short y=0; y<this.Height; ++y)
            {
                var buff = new StringBuilder();
                for (short x=0; x<this.Width; ++x)
                {
                    var thisPoint = new Point2D(x, y);
                    if (goalSpots!=null && goalSpots.Contains(thisPoint))
                        buff.Append('X');
                    else if (startSpots!=null && startSpots.Contains(thisPoint))
                        buff.Append('O');
                    else if (breadcrumbs!=null && breadcrumbs.Contains(thisPoint))
                        buff.Append('.');
                    else
                        buff.Append(this.GetTile(thisPoint));
                }

                yield return buff.ToString();
            }
        }

        private char[,] _tileGrid;
    }
}
