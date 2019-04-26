using System;

namespace BattlePlanPath
{
    /// <summary>
    /// Base class for items to be places in an IndexedIntrinsicPriorityQueue.
    /// </summary>
    internal class IndexedQueueItem
    {
        /// <summary>
        /// Used by IndexedIntrinsicPriorityQueue to keep track of where this item is in the heap.
        /// </summary>
        internal int QueueIndex;
    }
}
