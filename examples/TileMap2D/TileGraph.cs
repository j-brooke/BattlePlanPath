using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BattlePlanPath;

namespace TileMap2D
{
    /// <summary>
    /// This class is the glue between our application's 2D tile model to the pathfinder's mathy
    /// graph view of the world.  We're not using the caller data mechanism in this example,
    /// so it doesn't matter what we put for the type parameter TPassthrough.
    /// </summary>
    internal class TileGraph : IPathGraph<Point2D, int>
    {
        public TileGraph(Map map)
        {
            _map = map;
        }

        /// <summary>
        /// Returns all tiles adjacent to the given one, as long as they're in-bounds and unblocked.
        /// </summary>
        /// <param name="fromNode">Node whose neighbors should be returned.</param>
        public IEnumerable<Point2D> Neighbors(Point2D fromNode)
        {
            // In this example, a tile's neighbors, for navigation purposes, are all tiles
            // in any of the 8 basic directions, as long as they aren't blocked.  (But you could get
            // fancy with trapdoors and teleporters and such.)
            for (var x=fromNode.X-1; x<=fromNode.X+1; ++x)
            {
                for (var y=fromNode.Y-1; y<=fromNode.Y+1; ++y)
                {
                    // Don't include tiles outside of the map boundaries.
                    if (x<0 || x>=_map.Width || y<0 || y>=_map.Height)
                        continue;

                    // A tile isn't a neighbor of itself.
                    var adjacentPt = new Point2D(x, y);
                    if (adjacentPt == fromNode)
                        continue;

                    // A tile isn't a neighbor if it's blocked.
                    if (!_map.IsBlocked(adjacentPt))
                        yield return adjacentPt;
                 }
            }
        }

        /// <summary>
        /// Returns the cost of moving from one tile to one of its neighbors.
        /// </summary>
        /// <param name="fromNode">Node to move from</param>
        /// <param name="toNode">Node to move to</param>
        /// <param name="callerData">Not used in this example</param>
        public double Cost(Point2D fromNode, Point2D toNode, int callerData)
        {
            var distX = Math.Abs(toNode.X-fromNode.X);
            var distY = Math.Abs(toNode.Y-fromNode.Y);

            // This will only ever be called using a point and one of its Neighbors as determined above,
            // so we know that it will never be further than 1 unit in each direction.
            Debug.Assert(distX<=1 && distY<=1);
            Debug.Assert(distX!=0 || distY!=0);

            // If it's a diagonal, return Sqrt(2), otherwise return 1.  (You could take into consideration
            // all kinds of things, if you wanted to: uphill/downhill, mud vs asphalt.  And you could use
            // the callerData parameter for information about whatever agent we're calculating a path for.)
            if (distX + distY >= 2)
                return _sqrt2;
            else
                return 1.0;
        }

        /// <summary>
        /// Estimated total cost to move from one tile to a possibly distant one.  This is what A* calls the
        /// "heuristic".
        /// </summary>
        /// <param name="fromNode">Node to move from</param>
        /// <param name="toNode">Node to move to</param>
        /// <param name="callerData">Not used in this example</param>
        public double EstimatedCost(Point2D fromNode, Point2D toNode, int callerData)
        {
            var deltaX = toNode.X - fromNode.X;
            var deltaY = toNode.Y - fromNode.Y;

            // For simplicity, we'll return the Euclidean distance as our estimate.  (But it's not hard to come up
            // with a formula that more accurately represents the 8-direction movement we're using.)
            return Math.Sqrt(deltaX*deltaX + deltaY*deltaY);
        }

        private static readonly double _sqrt2 = Math.Sqrt(2);
        private readonly Map _map;
    }
}