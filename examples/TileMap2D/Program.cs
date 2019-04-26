using System;
using System.Linq;
using BattlePlanPath;

namespace TileMap2D
{
    /// <summary>
    /// Example program showing how to use BattlePlanPath in a simple 2D tile map application.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length>0 && args[0].ToLower().StartsWith("bench"))
                Benchmark();
            else
                Demo();
        }

        private static void Demo()
        {
            // Create a tile map.  In this case we're doing it from hard-coded strings, but it
            // would be trivial to get it from a file instead.
            var map = new Map(_demoMapStrings);

            // Create an instace of an IPathGraph.  This is a class that the caller provides
            // that answers which tiles are adjacent to which, and what the cost is to move from
            // one to the next.
            var tileGraph = new TileGraph(map);

            // Create a PathSolver instance.  The first type parameter here is Point2D.  It's whatever
            // we're using to identify nodes (tile in this case).  The second one - int here - is a dummy
            // type since we don't care about callerData/passthrough values in this example.
            var solver = new PathSolver<Point2D,int>(tileGraph);

            var allImportantPoints = _demoGoalPts.Concat(_demoStartPts);

            // Pre-build the PathSolver's data structures.  We're telling it to build a graph of all of the points
            // reachable from the ones we give it here.
            solver.BuildAdjacencyGraph(allImportantPoints);

            // Create a path and draw the map for each point in _demoStartPts.
            foreach (var startPt in _demoStartPts)
            {
                // Find the shortest path from startPt to any of points listed in _interestingGoals.  Of course,
                // you could just give it one ending point.  The third parameter, 0 here, is unused in this example.
                var pathResult = solver.FindPath(startPt, _demoGoalPts, 0);

                // Write the map out as a grid characters, with a couple extra bits:
                //   O is the starting point for our path.
                //   X is one of the ending points for our path.
                //   . is a tile we moved through on the path.
                var startPtsArray = new Point2D[] { startPt };
                var mapStrings = map.RowStrings(pathResult.Path, startPtsArray, _demoGoalPts);

                Console.WriteLine();
                foreach (var mapRow in mapStrings)
                    Console.WriteLine(mapRow);

                // Write performance info.
                Console.WriteLine(pathResult.PerformanceSummary());
            }
        }

        /// <summary>
        /// Search for a whole lot of paths using a large map.
        /// </summary>
        private static void Benchmark()
        {
            // Build the necessary pieces (as above in Demo()).
            var map = new Map(_benchmarkMapStrings);
            var tileGraph = new TileGraph(map);
            var solver = new PathSolver<Point2D,int>(tileGraph);

            // Init the solver's graph.  (This step is included in solver.LifetimeSolutionTimeMS, by the way.)
            solver.BuildAdjacencyGraph(_benchmarkPts);

            // Solve a path for every combination of 1 starting point and 2 destination points.  (So if P is the number of
            // points, then we're solving P*(P-1)*(P-2)/2 paths.)
            for (var startIdx=0; startIdx<_benchmarkPts.Length; ++startIdx)
            {
                var startPt = _benchmarkPts[startIdx];
                for (var endIdx1=0; endIdx1<_benchmarkPts.Length; ++endIdx1)
                {
                    if (endIdx1==startIdx)
                        continue;

                    for (var endIdx2=endIdx1+1; endIdx2<_benchmarkPts.Length; ++endIdx2)
                    {
                        if (endIdx2==startIdx)
                            continue;

                        var endPts = new[] { _benchmarkPts[endIdx1], _benchmarkPts[endIdx2] };
                        var pathResult = solver.FindPath(startPt, endPts, 0);
                    }
                }
            }

            // Write out a summary of the PathSolver's lifetime statistics.
            double pctGraphUsed = 100.0 * solver.LifetimeNodesTouchedCount / (solver.GraphSize * solver.PathSolvedCount);
            double pctReprocessed = 100.0 * solver.LifetimeNodesReprocessedCount / (solver.GraphSize * solver.PathSolvedCount);

            var msg = string.Format("pathCount={0}; timeMS={1}; %nodesTouched={2:F2}; %nodesReprocessed={3:F2}; maxQueueSize={4}",
                solver.PathSolvedCount,
                solver.LifetimeSolutionTimeMS,
                pctGraphUsed,
                pctReprocessed,
                solver.LifetimeMaxQueueSize);

            Console.WriteLine(msg);
        }

        // Map as a grid of chars.  A space is open/traversable terrain.  Anything else is blocked.
        private static readonly string[] _demoMapStrings =
        {
            "              #         ####   ##     ########    ",
            "                        ####   ##     ########    ",
            "             ##          ########        #####    ",
            "           ####          ########        #########",
            "           ####         #########        ####     ",
            "           ##   ####    #########        ####     ",
            "           ##                 #######    ####     ",
            "#############           #     #######             ",
            "####                 ####     #######             ",
            "##                            #########           ",
            "                               #### ###           ",
            "                                   ######         ",
            "                                #  ######         ",
            "                      #### ##   #  ######         ",
            "########          ########      #    ##           ",
            " ######           #####              ##      ###  ",
            "#######        ########                   ## ###  ",
            "   ####        ######              ###    ##      ",
            "   ####        #####                              ",
            "               ###                     #          ",
            "     ##                   ###                   ##",
            "     ##    ######         ###                  ###",
            "#######       ###                              ###",
            "              ###                            #####",
            "              ######                         #####",
            "           #########                         ##   ",
            "                ####    ###########               ",
            "                ####  #############               ",
            "   ####                ############               ",
            "   ####               ##############             #",
            "   ####               ##############             #",
            "   ####                                          #",
            "         #                                       #",
            "      #####       #       #####                ###",
            "###      #     ## ####### #####                ###",
            "               ##                     ######      "
        };

        // Acceptable end points for our demo paths.
        private static readonly Point2D[] _demoGoalPts =
        {
            new Point2D(8, 5),
            new Point2D(37, 0),
            new Point2D(1, 31),
        };

        // Starting points for our demo paths.
        private static readonly Point2D[] _demoStartPts =
        {
            new Point2D(44, 32),
            new Point2D(20, 22),
            new Point2D(48, 0),
            new Point2D(27, 11),
        };

        // A larger map - one that we don't expect to actually print - for benchmarks.
        private static readonly string[] _benchmarkMapStrings =
        {
            "::::::::::::::::::::::::                            :::::::::::   ::::::::: :::::::::::::::::::::     ::::::::::::::::::::::            : ::::::::: :::::::::::::::: :::::::::::::::::::::::::::",
            "::::::::::::::::::::::::                            :::::::::::   ::::::::: :::::::::::::::::::::     ::::::::::::::::::::::            : ::::::::: :::::::::::::::: :::::::::::::::::::::::::::",
            "::::::::::::::::::::::::                            :::::::::::   ::::::::: :::::::::::::::::::::   ::::::::::::::::::::::::            : ::::::::: :::::::::::::::: :::::::::::::::::::::::::::",
            "::::::::::::::::::::::::                            :::::::::::   ::::::::: :::::::::::::::::::::   ::::::::::::::::::::::::            : :::::::::::::::::::::::::: :::::::::::::::::::::::::::",
            "::::::::::::::::::::::::    ::::::::                :::::::::::             :::::::::::::::::::::   ::::::::::::::::::::::::            :           :::::::::::::::: :::::::::::::::::::::::::::",
            "::::::::::::::::::::::::    ::::::::            :::::::::::::::             :::::::::::::::::::::   ::::::::::::::::::::::::            :           :::::::::::::::: :::::::::::::::::::::::::::",
            "::::::::::::::::::::::::    ::::::::        :::::::::::::::::::             :::::::::::::::::::::   :::::::::::::::::::::::::::         :           ::::::::::::::::       ::::::::::::   ::::::",
            "::::::::::::::::::::::::    ::::::::        :::::::::::::::::::                    :::::   :::::    :::::::::::    :::::::::::::::::::  :           ::::::::::::::::       ::::::::::::   ::::::",
            "::::::::::::::::::::::::    ::::::::        :::::::::::::::::::                    :::::   :::::    :::::::::::    :::::::::::::::::::  :           ::::::::::::::::       ::::::::::::   ::::::",
            "::::::::::::::::::::::::    ::::::::        ::::::::::::::                         :::::   :::::    :::::::::::    :::::::::::::::::::  :           ::::::::::::::::       ::::::::::::   ::::::",
            "::::::::::::::::::::::::         :::::::::::::::::::::::::                         :::::   :::::       :           :::::::::::::::::::::::::::::::        ::::::::::    ::::::::::::::::::::::::",
            "::::::::::::::::::::::::         :::::::::::::::::::::::::                         ::::::::::::::      :           :::::::::::::::::::::::::::::::        ::::::::::    ::: :::::    :::::   :::",
            "::::::::::::::::::::::::         ::::::::::::::::::::::::::::::                    ::::::::::::::      :           :::::::::::::::::::::::::::::::        ::::::::::::::::: :::::    :::::   :::",
            "::::::::::::::::::::::::     ::::::::::::::::::::::::::::::::::                    ::::::::::::::                      :::::::::::::::::::::::::::        ::::::::::    ::: :::::    :::::   :::",
            "::::::::::::::::::::::::     ::::::::::         :::::::::::::::                    ::::::::::::::       :              :::::::::::::::::::::::::::        ::::::::::        :::::    :::::   :::",
            "                             ::::::::::      ::::::::::::::::::                    ::::::::::::::       :              :::::::::::::::::::::::::::        ::::::::::        :::::       :::     ",
            "                             ::::::::::      ::::::::::::::::::                    ::::::::::::::       :               ::::::::::::: ::::::::::::                          :::::       :::  :::",
            "                             ::::::::::::::::::::::::::::::::::                    ::::::::::::::       :               ::::::::::::: ::::::::::::                                      :::     ",
            "                             ::::::::::::::::::::::::::::::::::                    ::::::::::::::       :               ::::::::::::: ::   ::::::                                       :::     ",
            ":::::::::::::::::::          ::::::::::      ::::::::::::::::::             :::::::::                   :               ::::::::::::: ::   ::::::                                       :::     ",
            ":::::::::::::::::::          ::::::::::      :::::::::::                    :::::::::      ::::::::::   :            :::::::::::::::: ::   ::::::                                       :::     ",
            ":::::::::::::::::::         ::::::::         :::::::::::                                   ::::::::::::::::          ::::::::::  ::          :                                          ::::::: ",
            ":::::::::::::::::::                          :::::::::::                                   ::::::::::::::::          ::::::::::  ::          :                                            ::::: ",
            ":::::::::::::::::::                          :::::::::::                                   ::::::::::::::::          ::::::::::  ::          :                                            ::::: ",
            ":::::::::::::::::::                          ::::::::::::                                  ::::::::::::::::          ::::::::::              :                     :                      ::::: ",
            "::::::::::::::::::::::::::                   ::::::::::::    ::::::::::                    :::::::::::::::::::       ::::::::::              :                                            ::::: ",
            "::::::::::::::::::::::::::                   ::::::::::::    ::::::::::                         ::::::::::::::       ::::::::::              :                                            ::::: ",
            "::::::::::::::::::::::::::         :::::::::::::: :::::::    ::::::::::                         ::::::::::::::       ::::::::::              :                                            ::::: ",
            "::::::::::::::::::::::::::         :::::::::::::: :::::::                                                            ::::::::::              :                                            ::::: ",
            "              ::::::::::::         :::::::::::::: :::::::                                          :::::::::         ::::::::::               ::::::::::                                  ::::: ",
            "              ::::::::::::         :::::::::::::::::::::::::::::                             ::::::::                                         ::::::::::                                  ::::: ",
            "::::::::::::::::::::::::::         :::::::::::::::::::::::::::::     :::::::                 ::::::::                                         ::::::::::::::::::  ::::::                        ",
            "::::::::::::::::::::::::::         :::::::::::::::::::::::::::::    ::::::::                 ::::::::                                         ::::::::::::::::::::::::::                        ",
            "::::::::::::::::::::::::::                       :::::::::::::::    ::::::::                :::::::::                                         :::::::::::::::::::::::                           ",
            ":::::::::::::::::::::::::::::::::::              :::::::::::::::    ::::::::          ::    :::::::::                                         :::::::::::::::::::::::                           ",
            ":::::::::::::::::::::   :::::::::::              :::::::::::        ::::::::          ::     ::::::::                                         :::::::::::::::::::::::                           ",
            "::::::::::::      :::                            :::::::::::        ::::::            ::                                                      :::::::::::::::::::::::                           ",
            "::::::::::::      :::                                               ::::::            ::                                                                :::::::::::::                           ",
            "::::::::::::                                                            ::::::        ::                                                            :::::::::::::::::                           ",
            "::::::::::::                                                                          ::                                                            :::::::::::::::::                           ",
            "::::::::::::                          ::::                                            ::                                                            :::::::::::::::::                  ::::::   ",
            "::::::::::::            :::::::::::   ::::                                :::         ::                                                            :   :::::::::::::                  ::::::   ",
            ":::::::::               :::::::::::   ::::  ::::::::                      :::                                                                       :                        ::::::::::::::::   ",
            ":::::::::               :::::::::::   ::::  ::::::::    :::               :::                                                                       :                        ::::::::::::::::   ",
            ":::::::::               ::::::::::::::::::::::::::::    :::               :::                             :::::::::::::                             :                        ::::::::::::::::   ",
            ":::::::::               ::::::::::::::::::::::::::::                      :::                             :::::::::::::                             :                        ::::::::::::       ",
            ":::::   :               ::::::::::::::::::::::::::::                      :::                             :::::::::::::   ::                        :                        ::::::::::::       ",
            ":::::   :    :::::::             ::::::::::::::::::                       :::                             :::::::::::::   ::                        :   :             :::::: :::::::::::::      ",
            "             :::::::     ::::::::::::::::::::::::::                       :::                             :::::::::::::   ::                            :             :::::: :::::::::::::      ",
            "             :::::::     ::::::::::::::::::::::                           :::                                             ::         :                  ::            :::::: :::::::::::::      ",
            "             :::::::     ::::::::::     :::::::                                                                    ::::::::::::      :                  ::::::::::::  :::::: :::::::::::::      ",
            "             :::::::     ::::::::::                                                                                ::::::::::::      :                  ::::::::::::  ::::::                    ",
            ":::::::::    :::::::   :::::::::                                                                                   ::::::::::::      :                  ::::::::::::  ::::::                    ",
            ":::::::::    :::::::   :::::::::                                   :::::::::                                       ::::::::::::      :                  ::::::::::::  ::::::                    ",
            "             :::::::::::::::::::                                   :::::::::                                       :::::::::::::     :                  ::::::::::::  ::::::                    ",
            "             :::::::   :::::::::                                   :::::::::                                       :::::::::::::     :                  ::::::::::::  :::::::::::               ",
            "             :::::::   :::::::::                   :::::::::::::   :::::::::                                       ::::::::::::      :                  ::::::::::::  :::::::::::               ",
            "                       :::::::::        :          :::::::::::::   :::::::::                                       ::::::::::::      :                  ::                                  ::::",
            "                                        :          :::::::::::::  :::::::::::::::                                  ::::::::::::     :                   ::                                      ",
            ":::::::::::::::::::::::::::             :          :::::::::::::  ::::::::                                                          :                               ::::::::::::                ",
            ":::::::::::::::::::::::::::             ::::       :::::::::::::  ::::::::                                                         :::::                            ::::::::::::                ",
            ":::::::::::::::::::::::::::             :          :::::::::::::                                                                   :::::                            ::::::::::::                ",
            ":::::::::::::::::::::::::::                        ::::::::::::: :::::::::                                                         :::::                            :::::::::::::            :::",
            ":::::::::::::::::::::::::::                        ::::::::::::: :::::::::          :::::::                               ::::::::::::::                            ::::::::   ::        :::::::"
        };

        private static readonly Point2D[] _benchmarkPts =
        {
            new Point2D(2,17),
            new Point2D(39,7),
            new Point2D(41,20),
            new Point2D(15,37),
            new Point2D(6,47),
            new Point2D(37,59),
            new Point2D(64,63),
            new Point2D(59,26),
            new Point2D(79,8),
            new Point2D(98,8),
            new Point2D(112,8),
            new Point2D(130,2),
            new Point2D(142,6),
            new Point2D(166,14),
            new Point2D(187,12),
            new Point2D(182,57),
            new Point2D(158,45),
            new Point2D(158,61),
            new Point2D(130,38),
            new Point2D(94,47),
            new Point2D(30,11),
            new Point2D(30,32),
            new Point2D(21,55),
            new Point2D(54,43),
            new Point2D(86,60),
            new Point2D(118,49),
            new Point2D(135,59),
            new Point2D(164,53),
            new Point2D(176,24),
            new Point2D(152,24),
            new Point2D(134,9),
            new Point2D(137,19),
            new Point2D(113,9),
            new Point2D(94,28),
            new Point2D(74,36),
            new Point2D(69,18),
            new Point2D(21,53),
            new Point2D(191,60),
            new Point2D(191,15),
            new Point2D(166,26),
            new Point2D(191,28),
            new Point2D(150,46),
            new Point2D(82,40),
            new Point2D(37,50),
            new Point2D(36,43),
            new Point2D(40,15),
            new Point2D(42,8),
            new Point2D(59,10),
            new Point2D(102,10),
            new Point2D(141,29)
        };
    }
}
