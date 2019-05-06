using System;
using System.Collections.Generic;
using System.Linq;
using BattlePlanPath;

namespace StarMap
{
    /// <summary>
    /// Example program showing how to use BattlePlanPath in a non-tile space.  In this example,
    /// we're interested in plotting a course from one star to another.  Each type of spaceship
    /// has a maxiumum distance it can travel in one jump.  Some ships can travel through wormholes,
    /// when available, as shortcuts.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Create the map we'll use for this program.  Since StarMap implements IPathGraph, it can answer
            // questions PathSolver asks about connections and distances between stars.
            var maxJumpDist = _shipTypes.Select( (ship) => ship.MaxJumpDistance ).Max();
            var starMap = CreateMap(maxJumpDist);

            // Create a PathSolver instance that we'll use.  The first type parameter, string, is what
            // we'll use to identify nodes in the graph.  The second, ShipCharacteristics, is the type of
            // caller data that we will pass to FindPath, and it will subsequently pass to Cost and EstimatedCost.
            // In this example, what type of spaceship we're plotting a path for.
            var solver = new PathSolver<string,ShipCharacteristics>(starMap);

            // Pre-build PathSolver's internal data.  We have to give it one or more seed points - nodes that
            // we know exist.  One is sufficient as long as all of the others are reachable from it.
            solver.BuildAdjacencyGraph("A");

            // Show the user the star map.
            foreach (var row in _asciiMap)
                Console.WriteLine(row);

            // Prompt for input about what paths to plot.
            string startStar;
            string endStar;

            Console.Write("Enter starting star: ");
            var startStarInput = Console.ReadLine();
            if (startStarInput.Length > 0)
                startStar = startStarInput.ToUpper().Substring(0, 1);
            else
                return;

            Console.Write("Enter destination star: ");
            var endStarInput = Console.ReadLine();
            if (endStarInput.Length > 0)
                endStar = endStarInput.ToUpper().Substring(0, 1);
            else
                return;

            // Find a path for each type of spaceship, and display it to the user.
            Console.WriteLine($"--{startStar} to {endStar}--");
            foreach (var ship in _shipTypes)
                WritePath(starMap, solver, startStar, endStar, ship);
        }

        /// <summary>
        /// ASCII grid defining the relative positions of stars in our world.  The stars
        /// are each named by a single letter.  Rich immersion going on here.
        /// </summary>
        private static string[] _asciiMap = new string[]
        {
            "A               E",
            "            D",
            "    B                       O",
            "      J     P",
            "  C                 I",
            "               H",
            " F                       N",
            "        G       L",
            "                   M",
            "",
            "   K",
            "                 S",
            "",
            "              R          T",
            "",
            "",
            "                         X U",
            "      Q      Y",
            "",
            "                  W        Z",
        };

        /// <summary>
        /// Types of spaceships we want to plot paths for.
        /// </summary>
        private static ShipCharacteristics[] _shipTypes = new[]
        {
            new ShipCharacteristics("freighter", 7, false),
            new ShipCharacteristics("battleship", 8, true),
            new ShipCharacteristics("courier", 10, false)
        };

        /// <summary>
        /// There's a vast cycle of wormholes between certain stars that allow
        /// for much faster travel, if the ship is equipped for it.  This phenomenon
        /// is only present between stars named after vowels, because science!
        /// </summary>
        private const string _wormholeCycle = "AEIOU";

        /// <summary>
        /// Creates a StarMap object based on hard-coded text above.
        /// </summary>
        private static StarMap CreateMap(double maxJumpDistance)
        {
            // Create a list of stars based on an array of strings.  Each letter represents one star.
            var stars = new List<Star>();
            for (int y=0; y<_asciiMap.Length; ++y)
            {
                var row = _asciiMap[y];
                for (int x=0; x<row.Length; ++x)
                {
                    var symbol = row[x];
                    if (symbol != ' ')
                        stars.Add(new Star(symbol.ToString(), x, y));
                }
            }

            // Some stars have one-way wormholes between them.  Travelling by wormhole is very fast,
            // but only available to some types of ships.
            var wormholes = new List<Wormhole>();
            if (_wormholeCycle.Length>=2)
            {
                for (int i=0; i<_wormholeCycle.Length-1; ++i)
                    wormholes.Add(new Wormhole(_wormholeCycle[i].ToString(), _wormholeCycle[i+1].ToString()));
                wormholes.Add(new Wormhole(_wormholeCycle[_wormholeCycle.Length-1].ToString(), _wormholeCycle[0].ToString()));
            }

            return new StarMap(stars, wormholes, maxJumpDistance);
        }

        /// <summary>
        /// Plot a path between the given stars for a particular ship type, and write that path out to the console.
        /// </summary>
        private static void WritePath(StarMap map,
            PathSolver<string,ShipCharacteristics> solver,
            string fromStar,
            string toStar,
            ShipCharacteristics ship)
        {
            // Find the best path between our stars.  The third parameter here, ship, gets passed through
            // to StarMap's Cost and EstimatedCost methods.
            var pathInfo = solver.FindPath(fromStar, toStar, ship);
            if (pathInfo.Path == null)
            {
                Console.WriteLine($"{ship.ShipClass}: no path found from {fromStar} to {toStar}");
                return;
            }

            // Write out a string indicating each step in the path and its cost.
            var buff = new System.Text.StringBuilder();
            buff.Append(ship.ShipClass).Append(": ").Append(fromStar);

            string currentStar = fromStar;
            int i = 0;
            while (i<pathInfo.Path.Count)
            {
                string nextStar = pathInfo.Path[i];

                var cost = map.Cost(currentStar, nextStar, ship);
                buff.AppendFormat(" -({0:N1})-> ", cost);
                buff.Append(nextStar);

                i += 1;
                currentStar = nextStar;
            }

            Console.WriteLine(buff.ToString());

            // Write out debug/performance stats.
            Console.WriteLine("  " + pathInfo.PerformanceSummary());
        }
    }
}
