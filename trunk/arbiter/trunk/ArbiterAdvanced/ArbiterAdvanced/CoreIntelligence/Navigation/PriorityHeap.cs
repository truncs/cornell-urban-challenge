using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// PriorityQueue with a heap implementation
	/// </summary>
	public class PriorityHeap
	{
		/// <summary>
		/// Array contains the actual priority heap implementation
		/// </summary> 
		private IPriorityNode[] heap;

		/// <summary>
		/// Dictionary is for quick lookup of items by name
		/// </summary>
		private Dictionary<string, IPriorityNode> heapLookup;

    /// <summary>
    /// Keeps track of the number of items in the heap
    /// </summary>
    private int items = 0;

		/// <summary>
		/// Constructor
		/// </summary>
		public PriorityHeap()
		{
			// Initialize Fields
			heap = new IPriorityNode[CoreCommon.RoadNetwork.ArbiterWaypoints.Count];
			heapLookup = new Dictionary<string, IPriorityNode>(CoreCommon.RoadNetwork.ArbiterWaypoints.Count);
		}

		/// <summary>
		/// Prints the nodes With their cost for testing
		/// </summary>
		public void Print()
		{
			Console.WriteLine("Printing Priority Heap");
			Console.WriteLine("Number of items: " + items.ToString());

			foreach (IPriorityNode n in heap)
			{
				Console.WriteLine("Name: " + n.Name + ", " + "Value: " + n.Value);
			}
		}

		/// <summary>
		/// Pops off the node with the lowest cost
		/// </summary>
		/// <returns></returns>
		public IPriorityNode Pop()
		{
			IPriorityNode popped;

			// 1. Copy the entry at the root of the heap to a variable that is used to return a value
			if (items == 0)
			{
				return (null);
			}
      else if (items == 1)
      {
        // remove and return from heap
        popped = heap[0];
        heap[0] = null;
				items--;

        // remove from hashtable
        heapLookup.Remove(popped.Name);

        return popped;
      }
      else
      {
        // save the first node
        popped = heap[0];

        // remove from hashtable
        heapLookup.Remove(popped.Name);

        // 2. Copy the last entry in the deepest level to the root and delete this last node from heap 
        int last = items - 1;
        heap[0] = heap[last];
        heap[last] = null;
				items--;

        // 3. while out of place entry's cost is greater than one of its child's cost, swap with lowest cost child
        TrickleDown(0);

        // 4. Return node that was saved in step 1
        return popped;
      }
		}

		/// <summary>
		/// adds a node to the heap and reheapifies
		/// </summary>
		/// <param name="n">Node to add</param>
		public void Push(IPriorityNode n)
		{
			// adds node to the first available location
			heap[items] = n;

			// add to item count
			items++;

			// while the current node's cost is less than its parent's cost swap
			TrickleUp(items);

			// add node n to the hashTable
			heapLookup.Add(n.Name, n);		
		}

		/// <summary>
		/// check if the heap contains a certain node
		/// </summary>
		/// <param name="name">Name of node to check for</param>
		/// <returns>Value indicating if found or not</returns>
		public bool Contains(string name)
		{
			return this.heapLookup.ContainsKey(name);
		}

		/// <summary>
		/// Gets size of the heap
		/// </summary>
		/// <returns></returns>
		public int Count
		{
			get { return items; }
		}

		/// <summary>
		/// remove a node by its name
		/// </summary>
		/// <param name="name">name of node ot remove</param>
		/// <returns>True if node found and removed. False if node with name not found</returns>
		public bool Remove(string name)
		{
			// remove value from hashTable
			heapLookup.Remove(name);

			if (items == 0)
				return false;
			else if (items == 1)
			{
				if ((heap[0]).Name == name)
				{
					heap[0] = null;
					items--;
					return true;
				}
				else
					return false;
			}
			else
			{
				for (int i = 0; i < items; i++)
				{
					if ((heap[i]).Name == name)
					{
						heap[i] = heap[items - 1];
						heap[items - 1] = null;
						items--;
						TrickleDown(i);
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// fins a IPriorityNode with name and returns it
		/// </summary>
		/// <param name="name">name of node to find</param>
		/// <returns>Node with name</returns>
		public IPriorityNode Find(string name)
		{
			return heapLookup[name];
		}

		/// <summary>
		/// swaps 2 elements in the heap by index
		/// </summary>
		/// <param name="i"></param>
		/// <param name="j"></param> 
		private void Swap(int i, int j)
		{
			IPriorityNode temp = heap[i];
			heap[i] = heap[j];
			heap[j] = temp;
		}

		/// <summary>
		/// heapify down
		/// </summary>
		/// <param name="index"></param>
		private void TrickleDown(int index)
		{
			index = index + 1;
			int child1 = 2 * index;
			int child2 = (2 * index) + 1;

			// make sure that the heap contains index for child1 and child1 < index OR
			// make sure that the heap contains index for child2 and child2 < index
			while (((items >= child1) && ((heap[index - 1]).Value > (heap[child1 - 1]).Value))
				|| ((items >= child2) && ((heap[index - 1]).Value > (heap[child2 - 1]).Value)))
			{
				// if size < child2 then child2 cannot be compared to so auto-swap with child1
				if (items < child2)
				{
					Swap(index - 1, child1 - 1);
					index = child1;
					child1 = index * 2;
					child2 = (index * 2) + 1;
				}
				// if child1's cost less than child2's cost the nswap index with child1
				else if ((heap[child1 - 1]).Value < (heap[child2 - 1]).Value)
				{
					Swap(index - 1, child1 - 1);
					index = child1;
					child1 = index * 2;
					child2 = (index * 2) + 1;
				}
				// otherwise swap with child2
				else
				{
					Swap(index - 1, child2 - 1);
					index = child2;
					child1 = index * 2;
					child2 = (index * 2) + 1;
				}
			}
		}

		/// <summary>
		/// heapify up
		/// </summary>
		/// <param name="index"></param>
		private void TrickleUp(int index)
		{
			if (index == 1)
				return;

			// while the current node's cost is less than its parent's cost
			while ((heap[index - 1]).Value < (heap[((int)(index / 2)) - 1]).Value)
			{
				// swap current with its parent
				Swap(index - 1, ((int)(index / 2)) - 1);

				// move the index to current node
				index = index / 2;

				if (index == 0 || index == 1)
					return;
			}
		}
	}
}
