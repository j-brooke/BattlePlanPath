using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BattlePlanPath.Tests
{
    [TestClass]
    public class PathSolverTests
    {
        [TestInitialize]
        public void Setup()
        {
            _testMap = new TestMap(_testMapData);
        }

        [TestMethod]
        public void FindPath_ThrowsIfGraphNotBuilt()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            PathResult<Point> pathResult = null;
            Assert.ThrowsException<PathfindingException>(() => pathResult = solver.FindPath(_startPt, _reachableEndPt, PathTestOption.Normal));
        }

        [TestMethod]
        public void FindPath_ThrowsIfStartNodeNotInGraph()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(_startPt);
            PathResult<Point> pathResult = null;
            var ptOutsideOfGraph = new Point(-1, -1);
            Assert.ThrowsException<PathfindingException>(() => pathResult = solver.FindPath( ptOutsideOfGraph, _reachableEndPt, PathTestOption.Normal));
        }

        [TestMethod]
        public void FindPath_ThrowsIfDestNodeNotInGraph()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(_startPt);
            PathResult<Point> pathResult = null;
            var ptOutsideOfGraph = new Point(-1, -1);
            Assert.ThrowsException<PathfindingException>(() => pathResult = solver.FindPath(_startPt, ptOutsideOfGraph, PathTestOption.Normal));
        }

        [TestMethod]
        public void FindPath_ThrowsIfEmptyDestinationList()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(_startPt);
            PathResult<Point> pathResult = null;
            var destList = new List<Point>();
            Assert.ThrowsException<PathfindingException>(() => pathResult = solver.FindPath(_startPt, destList, PathTestOption.Normal));
        }

        [TestMethod]
        public void FindPath_ZeroLengthPathToSelf()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(new Point[] { _startPt, _reachableEndPt, _unreachableEndPt });
            var solution = solver.FindPath(_startPt, _startPt, PathTestOption.Normal);

            Assert.IsTrue(solution.PathCost == 0);
            Assert.IsNotNull(solution.Path);
            Assert.IsTrue(solution.Path.Count == 0);
            StringAssert.Contains(solution.PerformanceSummary(), "Zero-length path");
        }

        [TestMethod]
        public void FindPath_GoodPathToReachable()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(new Point[] { _startPt, _reachableEndPt, _unreachableEndPt });
            var solution = solver.FindPath(_startPt, _reachableEndPt, PathTestOption.Normal);

            Assert.IsTrue(solution.PathCost >= _testMap.EstimatedCost(_startPt, _reachableEndPt, PathTestOption.Normal));
            Assert.IsNotNull(solution.Path);
            Assert.IsTrue(solution.Path.Count > 0);
            Assert.IsTrue(solution.Path[solution.Path.Count - 1] == _reachableEndPt);
            Assert.IsFalse(solution.Path.Contains(_startPt));
            Assert.IsTrue(solution.NodesReprocessedCount == 0);
            StringAssert.Contains(solution.PerformanceSummary(), $"Path from {_startPt}");
        }

        [TestMethod]
        public void FindPath_NoPathToUnreachable()
        {
            // In this case, _startPt and _unreachableEndPt are disconnected in the adjacency graph.
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(new Point[] { _startPt, _reachableEndPt, _unreachableEndPt });
            var solution = solver.FindPath(_startPt, _unreachableEndPt, PathTestOption.Normal);

            Assert.IsTrue(solution.PathCost == double.PositiveInfinity);
            Assert.IsNull(solution.Path);
            StringAssert.Contains(solution.PerformanceSummary(), "Path not found");
        }

        [TestMethod]
        public void FindPath_GoodPathEvenWithOverestimate()
        {
            // If we give PathSolver a bad estimate, we should still get a path, but it won't always be optimal.
            // NodesReprocessedCount can only be > 0 in this case.
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(new Point[] { _startPt, _reachableEndPt, _unreachableEndPt });
            var solution = solver.FindPath(_startPt, _reachableEndPt, PathTestOption.OverEstimateRemainingDistance);

            Assert.IsNotNull(solution.Path);
            Assert.IsTrue(solution.Path.Count > 0);
            Assert.IsTrue(solution.Path[solution.Path.Count - 1] == _reachableEndPt);
            Assert.IsFalse(solution.Path.Contains(_startPt));
            Assert.IsTrue(solution.NodesReprocessedCount > 0);
            StringAssert.Contains(solution.PerformanceSummary(), $"Path from {_startPt}");
        }

        [TestMethod]
        public void FindPath_NoPathIfAllInfiniteCosts()
        {
            // In this case, the start and end points are connected in the adjacency graph, but all possible paths
            // have an infinite cost.  PathSolver treats that as unreachable.
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(new Point[] { _startPt, _reachableEndPt, _unreachableEndPt });
            var solution = solver.FindPath(_startPt, _reachableEndPt, PathTestOption.InfiniteCost);

            Assert.IsTrue(solution.PathCost == double.PositiveInfinity);
            Assert.IsNull(solution.Path);
            StringAssert.Contains(solution.PerformanceSummary(), "Path not found");
        }

        [TestMethod]
        public void ClearAdjacencyGraph_InvalidatesOldNodes()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);

            // Build a graph based on all three points.
            solver.BuildAdjacencyGraph(new Point[] { _startPt, _reachableEndPt, _unreachableEndPt });

            // Clear the graph, and then rebuild it with just one point.
            solver.ClearAdjacencyGraph();
            solver.BuildAdjacencyGraph(_startPt);

            // Try to find a path to _unreachableEndPt.  PathSolver should complain that it doesn't know about that point.
            PathResult<Point> pathResult = null;
            Assert.ThrowsException<PathfindingException>(() => pathResult = solver.FindPath(_startPt, _unreachableEndPt, PathTestOption.Normal));
        }

        [TestMethod]
        public void PerformanceSummary_HasInfo()
        {
            var solver = new PathSolver<Point, PathTestOption>(_testMap);
            solver.BuildAdjacencyGraph(new Point[] { _startPt, _reachableEndPt, _unreachableEndPt });
            var solution = solver.FindPath(_startPt, _reachableEndPt, PathTestOption.Normal);

            var summary = solver.PerformanceSummary();
            Assert.IsNotNull(summary);
            StringAssert.Contains(summary, "pathCount=1;");
        }

        private TestMap _testMap;

        private static readonly string[] _testMapData = new string[]
        {
            "XXXXXXXXXXXXXXXX",
            "X           X  X",
            "X XXXX      X  X",
            "X    X      XXXX",
            "X    X         X",
            "X XXXX   XXXXX X",
            "X        X     X",
            "XXXXXXXXXXXXXXXX",
        };
        private static readonly Point _startPt = new Point(4, 3);  // Inside the ] on the left
        private static readonly Point _reachableEndPt = new Point(10, 6);  // Inside alcove bottom right
        private static readonly Point _unreachableEndPt = new Point(14, 1);  // Inside closed cell upper right

        private enum PathTestOption
        {
            Normal,
            OverEstimateRemainingDistance,
            InfiniteCost,
        }

        /// <summary>
        /// Primitive IPathGraph allowing movement in 4 directions, using an array of strings to create the map.
        /// </summary>
        private class TestMap : IPathGraph<Point, PathTestOption>
        {
            public string[] MapData { get; }

            public TestMap(string[] mapData)
            {
                MapData = mapData;
            }

            public bool IsTileOpen(Point loc)
            {
                // A tile is open if the corresponding character is a space.
                if (MapData == null || loc.Y >= MapData.Length || loc.Y < 0 || loc.X < 0)
                    return false;
                var rowData = MapData[loc.Y];
                return rowData != null && loc.X < rowData.Length && rowData[loc.X] == ' ';
            }

            public double Cost(Point fromNode, Point toNode, PathTestOption callerData)
            {
                // If we're testing for infinite costs, treat any tile on the diagonal X==Y
                // as having an infinite cost to move to.
                if (callerData == PathTestOption.InfiniteCost && toNode.X == toNode.Y)
                    return double.PositiveInfinity;

                var xDist = Math.Abs(toNode.X - fromNode.X);
                var yDist = Math.Abs(toNode.Y - fromNode.Y);
                var isAdjacent = (xDist == 0 && yDist == 1) || (xDist == 1 && yDist == 0);
                return (isAdjacent) ? 1 : 0;
            }

            public double EstimatedCost(Point fromNode, Point toNode, PathTestOption callerData)
            {
                var eucDist = Math.Sqrt(Math.Pow(toNode.X - fromNode.X, 2.0) + Math.Pow(toNode.Y - fromNode.Y, 2.0));

                // In the normal case, we want to make sure that our estimate is always at least a little less than the actual path distance,
                // to mitigate floating point issues. If we give an overestimate, PathSolver will still find a valid path if there is one,
                // but it's not guaranteed to be the optimal path.
                var multiplier = (callerData == PathTestOption.OverEstimateRemainingDistance) ? 2.0 : 0.99;
                return multiplier * eucDist;
            }

            public IEnumerable<Point> Neighbors(Point fromNode)
            {
                if (IsTileOpen(fromNode))
                {
                    var up = new Point(fromNode.X, fromNode.Y - 1);
                    var down = new Point(fromNode.X, fromNode.Y + 1);
                    var left = new Point(fromNode.X - 1, fromNode.Y);
                    var right = new Point(fromNode.X + 1, fromNode.Y);

                    if (IsTileOpen(up))
                        yield return up;
                    if (IsTileOpen(down))
                        yield return down;
                    if (IsTileOpen(left))
                        yield return left;
                    if (IsTileOpen(right))
                        yield return right;
                }
            }
        }
    }
}
