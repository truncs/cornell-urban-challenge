using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

namespace UrbanChallenge.Common.Utility {
	public class RollingQueue<T> : IList<T>, IList {
		private class ComparisonHelper : IComparer<T> {
			private Comparison<T> comparer;

			public ComparisonHelper(Comparison<T> comparer) {
				this.comparer = comparer;
			}

			#region IComparer<T> Members

			public int Compare(T x, T y) {
				return comparer(x, y);
			}

			#endregion
		}

		private T[] data;
		private int length;
		private int head = 0, tail = 0;

		public RollingQueue(int capacity) {
			capacity = capacity+1;
			data = new T[capacity];
			length = capacity;
		}

		public int Capacity {
			get { return length-1; }
			set {
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				SetCapacity(value); 
			}
		}

		private void SetCapacity(int newCapacity) {
			newCapacity = newCapacity + 1;
			T[] dataTemp = new T[newCapacity];

			int origCount = Count;
			int start = 0;
			if (newCapacity < origCount) {
				start = origCount - newCapacity;
			}

			for (int i = start; i < origCount; i++) {
				dataTemp[i-start] = this[i];
			}

			data = dataTemp;
			length = newCapacity;
			head = 0;
			tail = Math.Min(newCapacity, origCount);
		}

		public void AddRange(T[] range) {
			if (range == null)
				throw new ArgumentNullException("range");

			for (int i = 0; i < range.Length; i++) {
				Add(range[i]);
			}
		}

		public void AddRange(IEnumerable<T> range) {
			if (range == null)
				throw new ArgumentNullException("range");

			foreach (T val in range) {
				Add(val);
			}
		}

		public T[] ToArray() {
			int count = Count;
			T[] ret = new T[count];
			for (int i = 0; i < count; i++) {
				ret[i] = this[i];
			}

			return ret;
		}

		public void Sort() {
			Sort(Comparer<T>.Default);
		}

		public void Sort(Comparison<T> comparer) {
			Sort(new ComparisonHelper(comparer));
		}

		public void Sort(IComparer<T> comparer) {
			int count = Count;
			if (count == 0)
				return;

			if (tail < head) {
				// array is broken up, restructure
				Condense();
			}

			// call the array sory
			Array.Sort(data, head, count, comparer);
		}

		public bool VerifySort(Comparison<T> comparer) {
			// loop through the make sure that the order is proper
			for (int i = 0; i < Count-1; i++) {
				if (comparer(this[i], this[i+1]) > 0) {
					return false;
				}
			}

			return true;
		}

		private void Condense() {
			int count = Count;

			data = ToArray();
			head = 0;
			tail = count;
		}

		public delegate int FindComparison(T x);

		public T FindClosest(FindComparison comparer) {
			int lower = 0;
			int upper = Count-1;
			while (lower <= upper) {
				int compareResult;
				int mid = lower + ((upper - lower) >> 1);
				try {
					compareResult = comparer(this[mid]);
				}
				catch (Exception exception) {
					throw new InvalidOperationException("Compare failed", exception);
				}
				if (compareResult == 0) {
					return this[mid];
				}
				if (compareResult < 0) {
					lower = mid + 1;
				}
				else {
					upper = mid - 1;
				}
			} 

			Debug.Assert(comparer(this[lower-1]) >= 0);
			if (lower < Count) {
				Debug.Assert(comparer(this[lower]) <= 0);
			}

			return this[lower-1];
		}

		#region IList<T> Members

		public int IndexOf(T item) {
			return Array.IndexOf(data, item);
		}

		public void Insert(int index, T item) {
			throw new NotSupportedException();
		}

		public void RemoveAt(int index) {
			throw new NotSupportedException();
		}

		public T this[int index] {
			get {
				if (index < 0 || index > Count-1)
					throw new ArgumentOutOfRangeException();

				return data[(head+index)%length]; 
			}
			set {
				if (index < 0 || index > Count-1)
					throw new ArgumentOutOfRangeException();

				data[(head+index)%length] = value; 
			}
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item) {
			data[tail] = item;
			tail = (tail+1)%length;
			if (tail == head) {
				head = (head+1)%length;
			}
		}

		public void Clear() {
			head = tail = 0;
		}

		public bool Contains(T item) {
			return IndexOf(item) != -1;
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			throw new NotSupportedException();
		}

		public int Count {
			get {
				if (tail < head) {
					return (tail+length)-head;
				}
				else {
					return tail-head;
				}
			}
		}

		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}

		public bool Remove(T item) {
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator() {
			for (int i = 0; i < Count; i++) {
				yield return this[i];
			}
		}

		#endregion

		#region IList Members

		int IList.Add(object value) {
			Add((T)value);
			return -1;
		}

		void IList.Clear() {
			Clear();
		}

		bool IList.Contains(object value) {
			return Contains((T)value);
		}

		int IList.IndexOf(object value) {
			return IndexOf((T)value);
		}

		void IList.Insert(int index, object value) {
			throw new NotSupportedException();
		}

		bool IList.IsFixedSize {
			get { return true; }
		}

		bool IList.IsReadOnly {
			get { return false; }
		}

		void IList.Remove(object value) {
			throw new NotSupportedException();
		}

		void IList.RemoveAt(int index) {
			throw new NotSupportedException();
		}

		object IList.this[int index] {
			get {
				return this[index];
			}
			set {
				this[index] = (T)value;
			}
		}

		#endregion

		#region ICollection Members

		void ICollection.CopyTo(Array array, int index) {
			throw new NotSupportedException();
		}

		int ICollection.Count {
			get { return Count; }
		}

		bool ICollection.IsSynchronized {
			get { return false; }
		}

		object ICollection.SyncRoot {
			get { return this; }
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion
	}
}
