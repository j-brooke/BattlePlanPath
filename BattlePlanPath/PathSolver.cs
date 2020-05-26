using System;
using System.Collections.Generic;
using System.Linq;

namespace BattlePlanPath
{
    /// <summary>
    /// Class for finding the shortest path between arbitrary nodes in a directed graph
    /// using the A* algorithm.
    /// </summary>
    /// <typeparam name="TNode">Type that identifies distinct nodes or locations.  (For example, Vector2D, string, etc.)</typeparam>
    /// <typeparam name="TPassthrough">Type of data passed through from PathSolver.FindPath to the IPathGraph.Cost and EstimatedCost.)</typeparam>
    /// <remarks>
    /// <para>This class is designed for calculating many paths using the same adjacency graph each time, from a single thread.
    /// If your terrain frequently changes or which nodes are considered adjacent to others varies from one case to another,
    /// this probably isn't optimal for you.</para>
    /// <para>On construction you give PathSolver a IPathGraph instance.  This is an object that knows how to describe
    /// your problem space to the PathSolver.  In particular, it knows: 1) Which nodes can travel to others in a single step
    /// (neighbors); 2) The actual cost of travelling from one node to a neighbor (in distance, time, whatever); and
    /// 3) An estimated cost for travelling from one node to a distant one (the A* heuristic).</para>
    /// <para>Before calling FindPath, you'll need to call BuildAdjacencyGraph.  This builds internal data structures
    /// that will be used on all future FindPath calls.</para>
    /// </remarks>
    public class PathSolver<TNode, TPassthrough>
    {
        public int PathSolvedCount => _seqNum;
        public int LifetimeSolutionTimeMS => (int)_lifetimeTimer.ElapsedMilliseconds;
        public int LifetimeNodesTouchedCount => _lifetimeNodesTouchedCount;
        public int LifetimeNodesReprocessedCount => _lifetimeReprocessedCount;
        public int LifetimeMaxQueueSize => _lifetimeMaxQueueSize;
        public int GraphSize => _infoGraph.Count;

        /// <summary>
        /// Create a solver object for the given IPathGraph.  The PathSolver is reusable - it assumes you'll
        /// potentially want to find multiple paths using the same worldGraph.
        /// </summary>
        /// <param name="worldGraph">Object that knows how to describe your world in pathfinding terms.</param>
        public PathSolver(IPathGraph<TNode, TPassthrough> worldGraph)
        {
            _worldGraph = worldGraph;
            _infoGraph = new Dictionary<TNode, NodeInfo>();
            _seqNum = 0;
            _lifetimeTimer = new System.Diagnostics.Stopwatch();
        }

        /// <summary>
        /// Prepares the PathSolver by building a graph of which nodes are connected directly to each other.
        /// Call this before calling FindPath, or any time the shape of your world changes.
        /// </summary>
        /// <remarks>
        /// The idea behind this implementation is that the adjacency graph - which nodes are connected to
        /// each other - won't change often.  The actual costs for moving from X to Y can vary as much as you
        /// want, but the question of whether you can move directly from X to Y is fixed.  (You can work around
        /// this restriction a little by returning PositiveInfinity from the cost function when you want extra
        /// restrictions.)
        /// </remarks>
        /// <param name="seedNodeIds">A single node ID that is part of the space you'll be searching
        /// for paths in.  All nodes that you might search for should be reachable from the seed given.  If
        /// that's not guaranteed to be the case, use the overload where you pass an enumeration of seed IDs
        /// instead.</param>
        public void BuildAdjacencyGraph(TNode seedNodeId)
        {
            this.BuildAdjacencyGraph(new TNode[] { seedNodeId });
        }

        /// <summary>
        /// Prepares the PathSolver by building a graph of which nodes are connected directly to each other.
        /// Call this before calling FindPath, or any time the shape of your world changes.
        /// </summary>
        /// <remarks>
        /// The idea behind this implementation is that the adjacency graph - which nodes are connected to
        /// each other - won't change often.  The actual costs for moving from X to Y can vary as much as you
        /// want, but the question of whether you can move directly from X to Y is fixed.  (You can work around
        /// this restriction a little by returning PositiveInfinity from the cost function when you want extra
        /// restrictions.)
        /// </remarks>
        /// <param name="seedNodeIds">One or more node IDs that are part of the space you'll be searching
        /// for paths in.  These don't have to all be connected to each other, but every node you later
        /// search for paths from must be connected to at least one.</param>
        public void BuildAdjacencyGraph(IEnumerable<TNode> seedNodeIds)
        {
            _lifetimeTimer.Start();
            _infoGraph.Clear();

            // Create a NodeInfo for every reachable NodeId from the given start ones.  These are reusable
            // containers for the data FindPath uses in its computations.  The whole point of this function
            // is so that we can create these once, rather than doing it on the fly for every path we solve.
            var queue = new Queue<TNode>(seedNodeIds);
            while (queue.Count>0)
            {
                var nodeId = queue.Dequeue();
                if (!_infoGraph.ContainsKey(nodeId))
                {
                    var info = new NodeInfo(nodeId);
                    _infoGraph.Add(info.NodeId, info);

                    foreach (var neighbor in _worldGraph.Neighbors(nodeId))
                        queue.Enqueue(neighbor);
                }
            }

            // Now go back and make a list of neighbor NodeInfos for each NodeInfo so we don't have to
            // go through a lookup table every time.
            foreach (var info in _infoGraph.Values)
            {
                info.Neighbors = _worldGraph.Neighbors(info.NodeId)
                    .Select( (nid) => _infoGraph[nid] )
                    .ToArray();
            }

            _lifetimeTimer.Stop();
        }

        /// <summary>
        /// Clears the internal data structure.  Doing this might make it easier for the garbage collector
        /// to reclaim memory.
        /// </summary>
        public void ClearAdjacencyGraph()
        {
            // Break apart the dense network of objects pointing at each other to make it easier for the
            // garbage collector.
            foreach (var info in _infoGraph.Values)
                info.Neighbors = null;

            _infoGraph.Clear();
        }

        /// <summary>
        /// Finds the shortest path from the given start point to the given end point.
        /// (If your IPathGraph.EstimatedCost can overestimate the cost, then this won't always
        /// strictly be the shortest path.)
        /// </summary>
        /// <param name="startNodeId">
        /// Your identifier for the starting node in the path.  (This could be a
        /// 2D point, a city name, or whatever else makes sense in your world-space.)
        /// </param>
        /// <param name="endNodeId">
        /// Your identifier for the node to find a path to.
        /// </param>
        /// <param name="callerData">
        /// (Optional) Whatever info you want to be given to your IPathGraph while solving this path.
        /// For example, you might want to give it the speed of the particular world object you're finding a path for,
        /// or perhaps that world object itself.
        /// </param>
        public PathResult<TNode> FindPath(TNode startNodeId, TNode endNodeId, TPassthrough callerData)
        {
            return this.FindPath(startNodeId, new[] { endNodeId }, callerData);
        }

        /// <summary>
        /// Finds the shortest path from the given start point to one of the given end points.
        /// (If your IPathGraph.EstimatedCost can overestimate the cost, then this won't always
        /// strictly be the shortest path.)
        /// </summary>
        /// <param name="startNodeId">
        /// Your identifier for the starting node in the path.  (This could be a
        /// 2D point, a city name, or whatever else makes sense in your world-space.)
        /// </param>
        /// <param name="endNodeIdList">
        /// Collection of acceptable destination nodes to find a path to.  If more than
        /// one is given, FindPath returns the shortest of the paths to any of them.
        /// </param>
        /// <param name="callerData">
        /// (Optional) Whatever info you want to be given to your IPathGraph while solving this path.
        /// For example, you might want to give it the speed of the particular world object you're finding a path for,
        /// or perhaps that world object itself.
        /// </param>
        public PathResult<TNode> FindPath(TNode startNodeId, IEnumerable<TNode> endNodeIdList, TPassthrough callerData)
        {
            // Initialize some performance data.
            var timerStartValue = _lifetimeTimer.ElapsedMilliseconds;
            _lifetimeTimer.Start();
            int maxQueueSize = 0;
            int nodesTouchedCount = 0;
            int nodesReprocessedCount = 0;

            // Choose a new sequence number.  We'll rely on this to know which NodeInfos
            // have been touched aleady in this pass, and which contain stale data from
            // previous calls.
            _seqNum += 1;

            // The openQueue contains all of the nodes that have been discovered but haven't
            // been fully processed yet.
            var openQueue = new IndexedIntrinsicPriorityQueue<NodeInfo>(PriorityFunction);

            // Make sure our end points exist in the pre-build graph, and mark them as end points.
            if (!endNodeIdList.Any())
                throw new PathfindingException("endNodeIdList is empty");

            foreach (var endNodeId in endNodeIdList)
            {
                NodeInfo endInfo;
                if (!_infoGraph.TryGetValue(endNodeId, out endInfo))
                    throw new PathfindingException("Ending node is not in the adjacency graph.");
                endInfo.IsDestinationForSeqNum = _seqNum;
            }

            // Init the starting node, put it in the openQueue, and go.
            NodeInfo startInfo;
            if (!_infoGraph.TryGetValue(startNodeId, out startInfo))
                throw new PathfindingException("Starting node is not in the adjacency graph.");
            startInfo.LastVisitedSeqNum = _seqNum;
            startInfo.BestCostFromStart = 0;
            startInfo.BestPreviousNode = null;
            startInfo.EstimatedRemainingCost = EstimateRemainingCostToAny(startNodeId, endNodeIdList, callerData);
            startInfo.IsOpen = true;
            openQueue.Enqueue(startInfo);
            nodesTouchedCount += 1;

            NodeInfo arrivalInfo = null;
            while (openQueue.Count>0)
            {
                // Pull the current item from the queue.
                maxQueueSize = Math.Max(maxQueueSize, openQueue.Count);
                var currentInfo = openQueue.Dequeue();
                currentInfo.IsOpen = false;

                // If we start pulling infinities from the queue we'll assume that there are no non-infitite paths.
                // (Technically, there could be nodes where BestCostFromStart is finite and EstimatedRemainingCost is
                // infinite, but that doesn't seem like a useful case to worry about.)  Exit early and treat it
                // as a no-path-found situation.
                if (currentInfo.BestCostFromStart==double.PositiveInfinity)
                    break;

                // If this node is one of our goals for this call, we have found a good path.
                if (currentInfo.IsDestinationForSeqNum==_seqNum)
                {
                    arrivalInfo = currentInfo;
                    break;
                }

                foreach (var neighborInfo in currentInfo.Neighbors)
                {
                    // Calculate the total cost from the start to the neighbor we'll looking at, if travelling
                    // through the current node.
                    double costToNeighbor = currentInfo.BestCostFromStart + _worldGraph.Cost(currentInfo.NodeId, neighborInfo.NodeId, callerData);

                    // If the neighbor node hasn't been touched on this FindPath call, re-initialize it and put it
                    // in openQueue for later consideration.
                    if (neighborInfo.LastVisitedSeqNum!=_seqNum)
                    {
                        neighborInfo.LastVisitedSeqNum = _seqNum;
                        neighborInfo.BestCostFromStart = costToNeighbor;
                        neighborInfo.BestPreviousNode = currentInfo;
                        neighborInfo.EstimatedRemainingCost = EstimateRemainingCostToAny(neighborInfo.NodeId, endNodeIdList, callerData);
                        neighborInfo.IsOpen = true;

                        openQueue.Enqueue(neighborInfo);
                        nodesTouchedCount += 1;
                    }
                    else
                    {
                        // We've already looked at the neighbor node, but it's possible that coming at it from the
                        // current node is more efficient.  If so, update it.
                        if (costToNeighbor < neighborInfo.BestCostFromStart)
                        {
                            neighborInfo.BestCostFromStart = costToNeighbor;
                            neighborInfo.BestPreviousNode = currentInfo;
                            if (neighborInfo.IsOpen)
                            {
                                // The node's priority value has changed, so the priority queue needs to know.
                                openQueue.AdjustPriority(neighborInfo);
                            }
                            else
                            {
                                // We already processed this node and took it out of the Open list before, but
                                // then we found a shorter path to it.  We'll need to redo it.  (This can happen
                                // if IPathGraph.EstimatedCost - the A* heuristic - sometimes overestimates the cost
                                // of the remaining path.)
                                neighborInfo.IsOpen = true;
                                openQueue.Enqueue(neighborInfo);
                                nodesReprocessedCount += 1;
                            }
                        }
                    }
                }
            }

            // If we reached a destination, put together a list of the NodeIds that make up the path we found.
            List<TNode> path = null;
            if (arrivalInfo != null)
            {
                path = new List<TNode>();
                NodeInfo iter = arrivalInfo;
                while (iter.BestPreviousNode != null)
                {
                    path.Add(iter.NodeId);
                    iter = iter.BestPreviousNode;
                }

                path.Reverse();
            }

            // Update performance data
            _lifetimeTimer.Stop();
            int elapsedTimeMS = (int)(_lifetimeTimer.ElapsedMilliseconds - timerStartValue);

            _lifetimeNodesTouchedCount += nodesTouchedCount;
            _lifetimeReprocessedCount += nodesReprocessedCount;
            _lifetimeMaxQueueSize = Math.Max(_lifetimeMaxQueueSize, maxQueueSize);

            return new PathResult<TNode>()
            {
                Path = path,
                StartingNode = startNodeId,
                PathCost = arrivalInfo?.BestCostFromStart ?? double.PositiveInfinity,
                SolutionTimeMS = elapsedTimeMS,
                NodesTouchedCount = nodesTouchedCount,
                NodesReprocessedCount = nodesReprocessedCount,
                NodesInGraphCount = _infoGraph.Count,
                MaxQueueSize = maxQueueSize,
            };
        }

        public string PerformanceSummary()
        {
            double pctGraphUsed = 0;
            double pctReprocessed = 0;

            if (this.GraphSize>0 && this.PathSolvedCount>0)
            {
                pctGraphUsed = 100.0 * this.LifetimeNodesTouchedCount / (this.GraphSize * this.PathSolvedCount);
                pctReprocessed = 100.0 * this.LifetimeNodesReprocessedCount / (this.GraphSize * this.PathSolvedCount);
            }

            var msg = string.Format("pathCount={0}; timeMS={1}; %nodesTouched={2:F2}; %nodesReprocessed={3:F2}; maxQueueSize={4}",
                this.PathSolvedCount,
                this.LifetimeSolutionTimeMS,
                pctGraphUsed,
                pctReprocessed,
                this.LifetimeMaxQueueSize);
            return msg;
        }

        private readonly System.Diagnostics.Stopwatch _lifetimeTimer;

        private readonly IPathGraph<TNode,TPassthrough> _worldGraph;
        private readonly Dictionary<TNode,NodeInfo> _infoGraph;

        // Each time we run FindPath we use a new _seqNum.  This helps us keep track of which NodeInfo
        // instances we've touched this call, and which ones have stale data from previous calls.
        private int _seqNum;
        private int _lifetimeNodesTouchedCount;
        private int _lifetimeReprocessedCount;
        private int _lifetimeMaxQueueSize;

        /// <summary>
        /// Returns the smallest of the estimated costs to the given end points.
        /// </summary>
        private double EstimateRemainingCostToAny(TNode startNodeId, IEnumerable<TNode> endNodeIdList, TPassthrough callerData)
        {
            return endNodeIdList
                .Select( (endNodeId) => _worldGraph.EstimatedCost(startNodeId, endNodeId, callerData) )
                .Min();
        }

        /// <summary>
        /// Comparison function for the priority queue.  a is higher priority than b if its estimated total path cost
        /// (known cost from start + estimate of remaining cost to destination) is less than b's.
        /// </summary>
        private static bool PriorityFunction(NodeInfo a, NodeInfo b)
        {
            return (a.BestCostFromStart + a.EstimatedRemainingCost) < (b.BestCostFromStart + b.EstimatedRemainingCost);
        }

        /// <summary>
        /// Private class holding info during path-finding.  We create a bunch of these when BuildAdjacencyGraph
        /// is called, and then reuse them on every call to FindPath.
        /// </summary>
        private class NodeInfo : IndexedQueueItem
        {
            /// <summary>Identifier for a node.  Could be a string, Vector2D, etc.</summary>
            public TNode NodeId { get; }

            /// <summary>Link to all NodeInfos reachable from here in one step.</summary>
            public NodeInfo[] Neighbors { get; set; }

            /// <summary>Best total cost of all path steps to get here that we've found so far.</summary>
            public double BestCostFromStart { get; set; }

            /// <summary>Heuristic guess of how far we are from the goal.  Used to prioritize next examined nodes.</summary>
            public double EstimatedRemainingCost { get; set; }

            /// <summary>The node that we came from that gave us our curren BestCostFromStart.</summary>
            public NodeInfo BestPreviousNode { get; set; }

            /// <summary>Is this NodeInfo in openQueue?  It's quicker if we track it here than ask the queue.</summary>
            public bool IsOpen { get; set; }

            /// <summary>Is this one of our final destination points for the current FindPath request?</summary>
            public int IsDestinationForSeqNum { get; set; }

            /// <summary>
            /// Sequence number of the last FindPath call in which we touched this NodeInfo.  If it's not equal
            /// to the current call's seqnum we need to reinit a bunch of stuff.  This is quicker than looping through
            /// all known NodeInfos at the start of a FindPath and initializing stuff, or constructing brand new
            /// NodeInfos and wiring up their Neighbors.
            /// </summary>
            public int LastVisitedSeqNum { get; set; }

            public NodeInfo(TNode nodeId)
            {
                this.NodeId = nodeId;
                this.QueueIndex = -1;
            }
        }
    }
}
