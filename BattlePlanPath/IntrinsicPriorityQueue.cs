using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace BattlePlanPath
{
    /// <summary>
    /// Priority queue implemented as a min/max heap.  By "intrinsic", I mean that the priority value
    /// is the item type (T) itself, or can be derived from it or its properties, by the given comparison
    /// function.  (An alternative would be to store the priority value separately.)
    /// </summary>
    /// <remarks>
    /// A few cautions:
    /// * If the properties that go into an item's priority value change, you must call Remove or
    ///   AdjustPriority with it before performing any other queue operations, and before changing other
    ///   items' priority properties.
    /// * Duplicate items are allowed, but Remove and AdjustPriority make no guarantees about which instance
    ///   they operate on when duplicates exist.  This shouldn't be a problem for simple immutable types
    ///   like string or double, but it could be a problem for complex types.
    /// </remarks>
    public class IntrinsicPriorityQueue<T>
    {
        public int Count => _count;

        /// <summary>
        /// Creates a new queue using the given function for prioritization.  compareFunc(a,b)
        /// should return true if a is higher priority than b.
        /// </summary>
        public IntrinsicPriorityQueue(int initialCapacity, Func<T,T,bool> compareFunc)
        {
            _heap = new T[initialCapacity];
            _count = 0;
            _compareFunc = compareFunc;
        }

        /// <summary>
        /// Creates a new queue using the given function for prioritization.  compareFunc(a,b)
        /// should return true if a is higher priority than b.
        /// </summary>
        public IntrinsicPriorityQueue(Func<T,T,bool> compareFunc)
            : this(_defaultInitialCapacity, compareFunc)
        { }

        /// <summary>
        /// Creates a new queue using the given Comparison<T> for prioritization.  For instance,
        /// you could give it StringComparer.InvariantCultureIgnoreCase.Compare and false for
        /// highestValuesFirst to give you strings from a-z.
        /// </summary>
        public IntrinsicPriorityQueue(int initialCapacity, Comparison<T> compareFunc, bool highValuesFirst)
        {
            _heap = new T[initialCapacity];
            _count = 0;

            if (highValuesFirst)
            {
                _compareFunc = (T a, T b) => compareFunc(a, b) > 0;
            }
            else
            {
                _compareFunc = (T a, T b) => compareFunc(a, b) < 0;
            }
        }

        /// <summary>
        /// Creates a new queu using the given Comparison<T> for prioritization.  For instance,
        /// you could give it StringComparer.InvariantCultureIgnoreCase.Compare and false for
        /// highestValuesFirst to give you strings from a-z.
        /// </summary>
        public IntrinsicPriorityQueue(Comparison<T> compareFunc, bool highValuesFirst)
            : this(_defaultInitialCapacity, compareFunc, highValuesFirst)
        { }

        public void Clear()
        {
            Array.Clear(_heap, 0, _heap.Length);
            _count = 0;
        }

        /// <summary>
        /// Adds an item to the priority queue.  O(log(n))
        /// </summary>
        public void Enqueue(T item)
        {
            if (_count==_heap.Length)
                Array.Resize(ref _heap, _heap.Length*2);

            var newIdx = _count;
            _heap[newIdx] = item;
            _count += 1;

            ShiftUp(newIdx);
        }

        /// <summary>
        /// Looks at the next (highest priorty) item in the queue without removing it.  O(1)
        /// </summary>
        public T Peek()
        {
            if (_count==0)
                throw new InvalidOperationException();
            return _heap[0];
        }

        /// <summary>
        /// Returns the next (highest priority) item in the queue and removes it.  O(log(n))
        /// </summary>
        public T Dequeue()
        {
            if (_count==0)
                throw new InvalidOperationException();

            var itemToReturn = _heap[0];

            // Promote the last item in the array to fill the void of the item we're taking
            // away.
            _heap[0] = _heap[_count-1];
            _heap[_count-1] = default(T);
            _count -= 1;

            // Make sure the newly-promoted item settles down to its proper place in the tree.
            ShiftDown(0);

            return itemToReturn;
        }

        /// <summary>
        /// Tests whether the queue has any instances equal to testItem.  O(n)
        /// </summary>
        public bool Contains(T testItem)
        {
            // Loop through all entries.  This is O(n) of course.  We could make this more efficient
            // by keeping a Dictionary<T,int> that tracks a count of each value.  That would add
            // some overhead to Enqueue and Dequeue but wouldn't hurt their algorithmic efficiency.
            foreach (var item in _heap)
            {
                if (item.Equals(testItem))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Removes an arbitrary instance from the queue that is equal to item, if one exists.  O(n)
        /// </summary>
        public void Remove(T item)
        {
            for (int i=_count-1; i>=0; --i)
            {
                if (_heap[i].Equals(item))
                {
                    // Promote the last item in the array to fill the void of the item we're taking
                    // away.
                    _heap[i] = _heap[_count-1];
                    _heap[_count-1] = default(T);
                    _count -= 1;

                    // Make sure the newly-promoted item settles down to its proper place in the tree.
                    ShiftDown(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Adjusts the position of an arbitrary entry equal to item in the queue after its priority
        /// properties have changed. O(n)
        /// </summary>
        /// <remarks>
        /// WARNING: If the properties that go into your priority calculation change for an item in the queue,
        /// you MUST call AdjustPriority before performing any other queue operations, and before
        /// changing other items' priority-related properties.  Failure to do so will corrupt your queue.
        /// </remarks>
        public void AdjustPriority(T item)
        {
            for (int i=_count-1; i>=0; --i)
            {
                if (_heap[i].Equals(item))
                {
                    var parIdx = IndexOfParent(i);
                    if (i>0 && IsHeapier(i, parIdx))
                        ShiftUp(i);
                    else
                        ShiftDown(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Comparison function that prioritizes smaller values based on the type's CompareTo method.
        /// Provided as a convenience for callers to pass to the constructor.
        /// </summary>
        public static bool LessThan<U>(U a, U b) where U : T, IComparable<T>
        {
            return a.CompareTo(b) < 0;
        }

        /// <summary>
        /// Comparison function that prioritizes larger values based on the type's CompareTo method.
        /// Provided as a convenience for callers to pass to the constructor.
        /// </summary>
        public static bool GreaterThan<U>(U a, U b) where U : T, IComparable<T>
        {
            return a.CompareTo(b) > 0;
        }

        private const int _defaultInitialCapacity = 256;
        private T[] _heap;
        private int _count;

        private readonly Func<T,T,bool> _compareFunc;

        /// <summary>
        /// Swaps the elements at two given places in the array.
        /// </summary>
        private void SwapAt(int indexA, int indexB)
        {
            var temp = _heap[indexA];
            _heap[indexA] = _heap[indexB];
            _heap[indexB] = temp;
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