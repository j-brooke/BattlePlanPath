using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BattlePlanPath.Tests")]
namespace BattlePlanPath
{
    /// <summary>
    /// Special limited version of a Priority Queue for use in A* pathfinding.
    /// The item type (generic parameter T) for this class must descend from IndexedQueueItem.
    /// This exposes a field, QueueIndex, that the queue uses to track the item's location in
    /// its heap, to speed up the AdjustPriority operation.
    ///
    /// In the interest of speed, very few checks are performed on the legality of operations
    /// for this class.
    /// </summary>
    /// <remarks>
    /// Some warnings:
    /// * An item may only belong to one IndexedIntrinsicPriorityQueue at a given time.
    /// * If the item's priority value changes, you must call AdjustPriority with it before
    ///   performing any other queue operations, and before changing other items' priority properties.
    /// </remarks>
    internal class IndexedIntrinsicPriorityQueue<T>
        where T : IndexedQueueItem
    {
        public int Count => _count;

        /// <summary>
        /// Creates a new queue using the given function for prioritization.  compareFunc(a,b)
        /// should return true if a is higher priority than b.
        /// </summary>
        public IndexedIntrinsicPriorityQueue(int initialCapacity, Func<T,T,bool> compareFunc)
        {
            _heap = new T[initialCapacity];
            _count = 0;
            _compareFunc = compareFunc;
        }

        /// <summary>
        /// Creates a new queue using the given function for prioritization.  compareFunc(a,b)
        /// should return true if a is higher priority than b.
        /// </summary>
        public IndexedIntrinsicPriorityQueue(Func<T,T,bool> compareFunc)
            : this(_defaultInitialCapacity, compareFunc)
        { }

        /// <summary>
        /// Adds an item to the priority queue.  O(log(n))
        /// </summary>
        /// <remarks>
        /// WARNING: an item may only belong to a single IndexedIntrinsicPriorityQueue at a time.
        /// </remarks>
        public void Enqueue(T item)
        {
            if (_count==_heap.Length)
                Array.Resize(ref _heap, _heap.Length*2);

            var newIdx = _count;
            _heap[newIdx] = item;
            item.QueueIndex = newIdx;
            _count += 1;

            ShiftUp(newIdx);
        }

        /// <summary>
        /// Returns the next (highest priority) item in the queue and removes it.  O(log(n))
        /// </summary>
        public T Dequeue()
        {
            if (_count==0)
                throw new InvalidOperationException();

            var itemToReturn = _heap[0];
            itemToReturn.QueueIndex = -1;

            // Promote the last item in the array to fill the void of the item we're taking
            // away.
            _heap[0] = _heap[_count-1];
            _heap[0].QueueIndex = 0;
            _heap[_count-1] = default(T);
            _count -= 1;

            // Make sure the newly-promoted item settles down to its proper place in the tree.
            ShiftDown(0);

            return itemToReturn;
        }

        /// <summary>
        /// Adjusts the position of this item in the queue after its priority properties have changed.
        /// O(log(n))
        /// </summary>
        /// <remarks>
        /// WARNING: If the properties that go into your priority calculation change for an item in the queue,
        /// you MUST call AdjustPriority before performing any other queue operations, and before
        /// changing other items' priority-related properties.  Failure to do so will corrupt your queue.
        /// </remarks>
        public void AdjustPriority(T item)
        {
            var idx = item.QueueIndex;

            Debug.Assert(idx>=0 && idx<_count);
            Debug.Assert(item.Equals(_heap[idx]));

            if (idx>0 && IsHeapier(idx, IndexOfParent(idx)))
                ShiftUp(idx);
            else
                ShiftDown(idx);
        }

        private const int _defaultInitialCapacity = 256;
        private T[] _heap;
        private int _count;

        private readonly Func<T,T,bool> _compareFunc;

        private void SwapAt(int indexA, int indexB)
        {
            var temp = _heap[indexA];
            _heap[indexA] = _heap[indexB];
            _heap[indexB] = temp;

            _heap[indexA].QueueIndex = indexA;
            _heap[indexB].QueueIndex = indexB;
        }

        /// <summary>
        /// Walk the tree toward the root, repeatedly replacing the current item with its parent
        /// if the current item is higher priority.
        /// </summary>
        private void ShiftUp(int startIndex)
        {
            var idx = startIndex;
            while (idx > 0)
            {
                var parIdx = IndexOfParent(idx);
                if (IsHeapier(idx, parIdx))
                {
                    SwapAt(idx, parIdx);
                    idx = parIdx;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Walk the tree toward children, repeatedly replacing the current item with one of its
        /// children if the child has higher priority than this item.
        /// </summary>
        private void ShiftDown(int startIndex)
        {
            var idx = startIndex;
            while (true)
            {
                var leftIdx = IndexOfLeftChild(idx);
                var rightIdx = leftIdx+1;
                bool leftIsHeapier = leftIdx<_count && IsHeapier(leftIdx, idx);
                bool rightIsHeapier = rightIdx<_count && IsHeapier(rightIdx, idx);
                bool swapRight = rightIsHeapier && IsHeapier(rightIdx, leftIdx);
                bool swapLeft = leftIsHeapier;
                if (swapRight)
                {
                    SwapAt(rightIdx, idx);
                    idx = rightIdx;
                }
                else if (swapLeft)
                {
                    SwapAt(leftIdx, idx);
                    idx = leftIdx;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Abstraction of the comparison. Returns true if the value at indexA is higher priority
        /// than the value at indexB.  I'm generally avoiding saying "higher priority" in the code,
        /// though, since it suggests a max-heap of number types, which might not be the case.
        /// </summary>
        private bool IsHeapier(int indexA, int indexB)
        {
            return _compareFunc(_heap[indexA], _heap[indexB]);
        }

        // We're using an array as a binary heap, so you can always compute the indicies of parents and
        // children from a particular index.
        //
        // It looks like this, where the numbers below are array indices.
        //           0
        //      1          2
        //    3   4     5     6
        //   7 8 9 10 11 12 13 14
        private static int IndexOfParent(int currentIndex)
        {
            return (currentIndex-1) / 2;
        }

        private static int IndexOfLeftChild(int currentIndex)
        {
            return currentIndex*2 + 1;
        }

        private static int IndexOfRightChild(int currentIndex)
        {
            return currentIndex*2 + 2;
        }
    }
}