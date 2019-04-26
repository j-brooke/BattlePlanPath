using System;
using System.Collections.Generic;
using System.Linq;

namespace BattlePlanPath
{
    /// <summary>
    /// Bundle of information returned after searching for a path.
    /// </summary>
    public class PathResult<T>
    {
        /// <summary>
        /// The sequence of nodes to take to get to one of the destinations.  The destination
        /// node is included in the path, but the starting node isn't.  Null if no path
        /// could be found.
        /// </summary>
        public List<T> Path { get; set; }
        public T StartingNode { get; set; }

        /// <summary>
        /// Sum of all of the costs from one node to the next for this path, as given by IPathGraph.
        /// </summary>
        public double PathCost { get; set; }

        // -- Everything else is just performance data --

        /// <summary>
        /// Amount of time it took to find this path.
        /// </summary>
        public int SolutionTimeMS { get; set; }

        /// <summary>
        /// The number of unique nodes considered while finding this path.  Ideally this will be much lower
        /// than NodesInGraphCount, but that depends on the complexity of your graph and the quality of your
        /// heuristic.
        /// </summary>
        public int NodesTouchedCount { get; set; }

        /// <summary>
        /// The number of nodes that were previously "closed" but had to be re-opened.  This can only happen
        /// if your EstimatedCost heuristic can sometimes exceed the true cost.  (Watch out for floating point
        /// issues though.)  It's not necessarily bad if this happens - it's a speed vs. quality balance.
        /// </summary>
        public int NodesReprocessedCount { get; set; }

        /// <summary>
        /// Number of nodes in the entire known graph (as built by PathSolver.BuildAdjacencyGraph)
        /// </summary>
        public int NodesInGraphCount { get; set; }

        /// <summary>
        /// Largest number of nodes waiting to be processed at any time during the algorithm.
        /// </summary>
        public int MaxQueueSize { get; set; }

        /// <summary>
        /// Returns a string based on the performance fields, intended for logging.
        /// </summary>
        public string PerformanceSummary()
        {
            string pathIds;
            if (Path == null)
                pathIds = $"Path not found from {this.StartingNode}:";
            else if (Path.Count==0)
                pathIds = $"Zero-length path from {this.StartingNode}:";
            else
                pathIds = $"Path from {this.StartingNode} to {this.Path[this.Path.Count-1]}: cost={this.PathCost.ToString("F2")}; steps={this.Path.Count}";

            double pctGraphUsed = 100.0 * this.NodesTouchedCount / this.NodesInGraphCount;
            double pctReprocessed = 100.0 * this.NodesReprocessedCount / this.NodesTouchedCount;
            var msg = string.Format("{0} timeMS={1}; %nodesTouched={2:F2}; %nodesReprocessed={3:F2}; maxQueueSize={4}",
                pathIds,
                this.SolutionTimeMS,
                pctGraphUsed,
                pctReprocessed,
                this.MaxQueueSize);
            return msg;
        }
    }
}
