using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BattlePlanPath;

namespace BattlePlanPath.Tests
{
    [TestClass]
    public class IndexedIntrinsicPriorityQueueTests
    {
        [DataTestMethod]
        [DataRow(new int[] {})]
        [DataRow(new int[] {1})]
        [DataRow(new int[] {1, 1, 2, 3, 5, 8, 13, 21})]
        [DataRow(new int[] {21, 13, 8, 5, 3, 2, 1, 1})]
        [DataRow(new int[] {10, 7, 2, 1, 4, 9, 3, 8, 5, 6})]
        public void Dequeue_ItemsInOrder(int[] inputVals)
        {
            var sortedInputVals = inputVals.OrderBy( val => val ).ToArray();
            var queue = new IndexedIntrinsicPriorityQueue<TestQueueItem>(TestQueueItem.IsLessThan);

            foreach (var val in inputVals)
                queue.Enqueue(new TestQueueItem() { Value = val });
            
            var dequeuedVals = new List<int>();
            while (queue.Count!=0)
                dequeuedVals.Add(queue.Dequeue().Value);

            CollectionAssert.AreEqual(sortedInputVals, dequeuedVals);
        }

        [TestMethod]
        public void AdjustPriority_ChangesOrder()
        {
            // Create a bunch of TestQueueItems and enqueue them.
            var items = Enumerable.Range(1, 14)
                .Select(val => new TestQueueItem() { Value = val })
                .ToList();

            var queue = new IndexedIntrinsicPriorityQueue<TestQueueItem>(TestQueueItem.IsLessThan);
            foreach (var item in items)
                queue.Enqueue(item);

            // Change some of the values, letting the queue know each time.
            items[5].Value = 22;
            queue.AdjustPriority(items[5]);

            items[12].Value = 3;
            queue.AdjustPriority(items[12]);

            // Make a list of the values from the items, sorted according to LINQ.
            var valsSortedExternally = items.Select(item => item.Value)
                .OrderBy(val => val)
                .ToList();

            // Make a list of the values from the items, sorted according to the queue.
            var valsSortedByQueue = new List<int>();
            while (queue.Count != 0)
                valsSortedByQueue.Add(queue.Dequeue().Value);

            CollectionAssert.AreEqual(valsSortedExternally, valsSortedByQueue);
        }

        [TestMethod]
        public void Dequeue_ThrowsIfEmpty()
        {
            var queue = new IndexedIntrinsicPriorityQueue<TestQueueItem>(TestQueueItem.IsLessThan);
            Assert.ThrowsException<InvalidOperationException>(() => queue.Dequeue());
        }

        [TestMethod]
        public void Enqueue_ResizeIfNeeded()
        {
            var items = Enumerable.Range(1, 14)
                .Select(val => new TestQueueItem() { Value = val })
                .ToList();

            // Create a queue with a small initial capacity, and then enqueue more items than that.
            var queue = new IndexedIntrinsicPriorityQueue<TestQueueItem>(4, TestQueueItem.IsLessThan);
            foreach (var item in items)
                queue.Enqueue(item);

            Assert.AreEqual(items.Count, queue.Count);
        }

        private class TestQueueItem : IndexedQueueItem
        {
            public int Value { get; set; }

            public static bool IsLessThan(TestQueueItem a, TestQueueItem b)
            {
                return a.Value < b.Value;
            }
        }
    }
}
