using System;
using System.Collections.Generic;
using System.Linq;
using BattlePlanPath;

namespace StarMap
{
    /// <summary>
    /// Class that knows about which stars and wormholes exist and how difficult it is to get from one to another.
    /// The PathSolver relies on this to find the best path.  We're using strings to identify our nodes (stars),
    /// and the ShipCharacteristics class as supplemental info for each path we try to find.
    /// </summary>
    public class StarMap : IPathGraph<string, ShipCharacteristics>
    {
        /// <summary>
        /// Create the map based on lists created elsewhere.  The maxJump parameter is used to limit our notion
        /// of which which stars can be reached directly from other stars.
        /// </summary>
        public StarMap(IEnumerable<Star> stars, IEnumerable<Wormhole> wormholes, double maxJump)
        {
            _stars = stars.ToDictionary( (st) => st.Name );
            _wormholes = wormholes.ToList();
            _maxJumpDistance = maxJump;
        }

        /// <summary>
        /// Returns an enumeration of all of the other stars reachable in a single step from the given one.
        /// In this example's case, different ship types have different capabilities: some can jump to a star 10
        /// units away, others can't.  Here we're returning every star that any ship in our world can jump to.
        /// The Cost method will deal with whether a particular ship can or can't make a particular jump.
        /// </summary>
        public IEnumerable<string> Neighbors(string fromStarName)
        {
            // This implementation isn't horribly efficient, but it doesn't need to be.  IPathGraph.Neighbors is
            // only ever called by PathSolver.BuildAdjacencyGraph, which ideally only needs to be called once
            // regardless of how many paths you want to generate.

            var fromStar = _stars[fromStarName];

            // List all stars within _maxJumpDistance from this one.
            var jumpable = _stars.Values
                .Where( (toStar) => Distance(fromStar, toStar)<=_maxJumpDistance && fromStarName!=toStar.Name );

            // List all stars reachable by wormhole from here regardless of distance.  Wormholes are one-way.
            var wormable = _wormholes
                .Where( (wormhole) => wormhole.EntryStar==fromStarName )
                .Select( (wormhole) => _stars[wormhole.ExitStar] );

            // Combine the lists.
            return jumpable.Concat(wormable)
                .Select( (star) => star.Name )
                .Distinct();
        }


        /// <summary>
        /// Returns the cost of travelling from one star to an "adjacent" one for the given ship type.
        /// Adjacency is ruled by whatever Neighbors returns.  Some ships won't be capable of travelling to
        /// all adjacent stars; in that case, we'll return infinity.  In this example, "cost" means travel time,
        /// but it could just as easily be concerned with fuel usage, toll values, or whatever.
        /// </summary>
        public double Cost(string fromStarName, string toStarName, ShipCharacteristics shipData)
        {
            // All wormholes take the same amount of time to travel through, assuming the ship is able
            // to use them at all.
            var hasWormhole = _wormholes.Any( (wormhole) => wormhole.EntryStar==fromStarName && wormhole.ExitStar==toStarName );
            var wormCost = (hasWormhole && shipData.WormholeCapable)? _wormholeFixedCost : double.PositiveInfinity;

            // Each ship has a maximum distance it can travel in one step.  If this ship can't make it, we'll
            // return infinity.
            var directDist = Distance(_stars[fromStarName], _stars[toStarName]);
            var inRange = directDist<=shipData.MaxJumpDistance;

            // In this example, travel time is directly related to distance, but in a more complex game/simulation/whatever,
            // the cost might be the squareroot of the distance, or it might depend on the type of star or the speed of
            // the ship.
            var directCost = inRange? directDist : double.PositiveInfinity;

            return Math.Min(wormCost, directCost);
        }

        /// <summary>
        /// Returns the estimated total cost of travelling from one star to another, not necessarily adjacent one.
        /// This is what the A* algorithm calls the "heuristic".  It helps to restrict the number of nodes explored
        /// before finding the best path.
        /// </summary>
        public double EstimatedCost(string fromStarName, string toStarName, ShipCharacteristics shipData)
        {
            // If the ship is wormhole-capable, there's no simple way to estimate the total path cost.  The most efficient
            // path might take us backward a few steps to reach a wormhole, for a quick trip the rest of the way.
            // In these cases the algorithm will likely explore most of the graph.
            var wormCost = (shipData.WormholeCapable)? _wormholeFixedCost : double.PositiveInfinity;

            // For non-wormhole travel, the heuristic is easy - the Euclidean distance bewteen the two stars.
            var directCost = Distance(_stars[fromStarName], _stars[toStarName]);

            return Math.Min(wormCost, directCost);
        }

        private const double _wormholeFixedCost = 1.0;

        private Dictionary<string, Star> _stars;
        private List<Wormhole> _wormholes;
        private double _maxJumpDistance;

        private static double Distance(Star star1, Star star2)
        {
            var dx = star1.LocationX - star2.LocationX;
            var dy = star1.LocationY - star2.LocationY;
            return Math.Sqrt( dx*dx + dy*dy );
        }
    }
}
