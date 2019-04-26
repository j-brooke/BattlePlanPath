using System;
using System.Collections.Generic;

namespace BattlePlanPath
{
    /// <summary>
    /// Interface for answering questions about some concrete space in terms the pathfinding algorithm
    /// understands.  Use this to convert a tile map into a weighted directed graph, for instance.
    /// </summary>
    /// <typeparam name="TNode">Type that identifies distinct nodes or locations.  (For example, Vector2D, string, etc.)</typeparam>
    /// <typeparam name="TPassthrough">Type of data passed through from PathSolver.FindPath to the IPathGraph.Cost and EstimatedCost.)</typeparam>
    public interface IPathGraph<TNode, TPassthrough>
    {
        /// <summary>
        /// Returns an enumeration of nodes that are reachable in one step from the given one.
        /// </summary>
        /// <param name="fromNode">Node whose neighbors should be returned.</param>
        IEnumerable<TNode> Neighbors(TNode fromNode);

        /// <summary>
        /// Returns the actual cost of moving from one node to one of its neihbors.  This could be
        /// a distance, time, monetary value, or whatever.  The cost must be non-negative.  You may assume that
        /// toNode will always be a neighbor of fromNode: that is, reachable in one step.
        /// </summary>
        /// <param name="fromNode">Node to move from</param>
        /// <param name="toNode">Node to move to</param>
        /// <param name="callerData">Value that was given to PathSolver.FindPath for use by this IPathGraph</param>
        double Cost(TNode fromNode, TNode toNode, TPassthrough callerData);

        /// <summary>
        /// Estimated cost for the entire path from one node to any other one (not necessarily a neighbor).
        /// This is what the A* algorithm often calls the "heuristic".
        /// </summary>
        /// <param name="fromNode">Node to move from</param>
        /// <param name="toNode">Node to move to</param>
        /// <param name="callerData">Value that was given to PathSolver.FindPath for use by this IPathGraph</param>
        double EstimatedCost(TNode fromNode, TNode toNode, TPassthrough callerData);
    }
}
