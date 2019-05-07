# BattlePlanPath
An open-source C# implementation of the A* (A-Star) pathfinding algorithm.

## Requirements

BattlePlanPath is built on .NET Standard 2.0 - it has no additional dependencies - so I should work with any reasonably modern .NET platform.


## Characteristics

Some info to help you decide if BattlePlanPath is right for you project.

* Single-threaded
* Geometry-agnostic - BattlePlanPath doesn't care if your world is 2D, 3D, or entirely non-spatial.
* Static geometry - BattlePlanPath is optimized for world spaces that don't change often.
* Multiple destinations - You can give BattlePlanPath multiple destinations at once.  It will return the shortest path to any of them.


## Usage

### IPathGraph

In order to use BattlePlanPath, you need to write a class that implements the IPathGraph interface.

    public interface IPathGraph<TNode, TPassthrough>
    {
        IEnumerable<TNode> Neighbors(TNode fromNode);
        double Cost(TNode fromNode, TNode toNode, TPassthrough callerData);
        double EstimatedCost(TNode fromNode, TNode toNode, TPassthrough callerData);
    }

The first type parameter, TNode, is whatever you use to identify locations in your world-space.  In examples/TileMap2D, we use the Point2D struct to identify tiles in our map.  In examples/StarMap, we use strings.  You could just as easily use more complex types, too, as long as they have well-behaved Equals and GetHashCode methods.

The second type parameter, TPassthrough, is whatever you want it to be; BattlePlanPath doesn't care what it is.  Every time you call PathSolver.FindPath, you give it an instance of TPassthrough.  That value then gets passed along to IPathGraph.Cost and IPathGraph.EstimatedCost.  The point is to feed your IPathGraph implementation extra contextual information about whatever you're finding a path for.  If you don't want to use it you don't have to - just give it an int or something.

The Neighbors method is used to determine which nodes are reachable in a single step from a particular node.  In examples/TileMap2D, it's all of the tiles adjacent to the given one, where the tile isn't blocked.

The Cost method should return how expensive it is to move from one node to a particular neighbor.  What "cost" means is up to you - it might be distance, time, money, etc.  It's whatever you're trying to minimize while searching for a path.

EstimatedCost should return a quick guess at the the total cost of the path between two nodes, which might not be neighbors.  This is the A* algorithm's "heuristic".  The more accurate the estimates, the more efficiently a path can be found.  Note that if this method sometimes returns over-estimates - values greater than the true cost of the subpath - then the path produced by PathSolver.FindPath isn't guaranteed to be the ideal one.


### PathSolver.BuildAdjacencyGraph

Before you ask BattlePlanPath to find you a path, you have to call BuildAdjacencyGraph:

    public void BuildAdjacencyGraph(IEnumerable<TNode> seedNodeIds)

This allows a PathSolver to set up internal data structures ahead of time, for more efficient processing during the FindPath calls.  The assumption is that you'll be calling FindPath many many times using the same geometry.

The parameter, seedNodeIds, is an enumeration of one or more nodes that are in the space you want to find paths in.  You only need to provide more than one if there are parts of your space that can't be reached from the first one.


### PathSolver.FindPath

All that's left is to ask your PathSolver to find a path for you:

    public PathResult<TNode> FindPath(TNode startNodeId, IEnumerable<TNode> endNodeIdList, TPassthrough callerData)

The endNodeIdList parameter is an enumeration of possible ending points for your path.  FindPath use the one with the shortest path available.  A PathfindingException is thrown if endNodeIdList is empty.

The callerData parameter isn't used by BattlePlanPath - it is simply passed through to your methods, IPathGraph.Cost and IPathGraph.EstimatedCost, to help them make their decisions.

If startNodeId or any of the elements of endNodeIdList can't be found in the PathSolver's internal adjacency graph a PathfindingException is thrown.  That could mean that you neglected to call BuildAdjacencyGraph, that one of the points is flat-out unreachable (inside a wall, for instance), or that one of the points is unreachable from all of the seedNodeIds you passed to BuildAdjacencyGraph.

If FindPath found a path, the Path property of the returned PathResult will be a list of TNodes.  The destination will be included, but not the start.  It will be an empty list if the start and end nodes are the same.

If it couldn't find a path, the Path property will be null.  Likewise, if all paths have infinite costs, Path will be null.


## Examples

The examples directory contains a couple simple programs to illustrate how BattlePlanPath is used.

* TileMap2D plots paths through a simple 2D grid.
* StarMap simulates interstellar travel - with wormholes!
