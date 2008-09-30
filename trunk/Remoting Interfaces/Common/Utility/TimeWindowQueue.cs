using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Utility {
	public class TimeWindowQueue<T> : ICollection<KeyValuePair<double,T>> {
		private double[] timestamps;
		private T[] values;

		private int head, tail;
		private int count;

		private double windowSize;

		public TimeWindowQueue(double windowSize)
			: this(windowSize, 32) { }

		public TimeWindowQueue(double windowSize, int capacity) {
			this.timestamps = new double[capacity];
			this.values = new T[capacity];

			this.windowSize = windowSize;
		}

		#region Queue methods

		public double LastTimestamp {
			get {
				if (count > 0) {
					if (tail == 0) {
						return timestamps[timestamps.Length-1];
					}
					else {
						return timestamps[tail-1];
					}
				}
				else {
					throw new InvalidOperationException("Queue must have at least one value");
				}
			}
		}

		public double WindowSize {
			get { return windowSize; }
			set {
				if (value < windowSize && count > 0) {
					MaintainQueue(LastTimestamp-value);
				}

				windowSize = value;
			}
		}

		public void Maintain(double currentTimestamp) {
			MaintainQueue(currentTimestamp - windowSize);
		}

		public void Add(T value, double timestamp) {
			// clear out old stuff
			MaintainQueue(timestamp-windowSize);

			if (count == timestamps.Length) {
				ResizeQueue(count+1);
			}

			timestamps[tail] = timestamp;
			values[tail] = value;

			tail = (tail+1)%timestamps.Length;
			count++;
		}

		private void ResizeQueue(int minSize) {
			// add 1.5x the queue size
			int newSize = (3*timestamps.Length)/2;
			if (newSize < minSize) {
				newSize = minSize;
			}

			double[] newTimestamps = new double[newSize];
			T[] newValues = new T[newSize];

			// copy the data into the new stuff
			if (head < tail) {
				Array.Copy(timestamps, head, newTimestamps, 0, count);
				Array.Copy(values, head, newValues, 0, count);
			}
			else {
				// copy in two parts head->end, 0->tail
				Array.Copy(timestamps, head, newTimestamps, 0, timestamps.Length-head);
				Array.Copy(timestamps, 0, newTimestamps, timestamps.Length-head, tail);

				Array.Copy(values, head, newValues, 0, values.Length-head);
				Array.Copy(values, 0, newValues, values.Length-head, tail);
			}

			timestamps = newTimestamps;
			values = newValues;
			head = 0;
			tail = count;
		}

		private void MaintainQueue(double cutoffTime) {
			while (count > 0 && timestamps[head] < cutoffTime) {
				head = (head+1)%timestamps.Length;
				count--;
			}
		}

		public KeyValuePair<double, T> this[int index] {
			get {
				if (index >= count)
					throw new ArgumentOutOfRangeException("index");

				index = (head+index)%timestamps.Length;
				return new KeyValuePair<double, T>(timestamps[index], values[index]);
			}
		}

		#endregion

		#region ICollection<KeyValuePair<double,T>> Members

		void ICollection<KeyValuePair<double,T>>.Add(KeyValuePair<double, T> item) {
			Add(item.Value, item.Key);
		}

		public void Clear() {
			head = tail = count = 0;
		}

		bool ICollection<KeyValuePair<double,T>>.Contains(KeyValuePair<double, T> item) {
			throw new NotSupportedException();
		}

		public void CopyTo(KeyValuePair<double, T>[] array, int arrayIndex) {
			// check if the array has sufficient length
			if (array.Length + arrayIndex < count) {
				throw new ArgumentException("Array does not have sufficient capacity");
			}

			foreach (KeyValuePair<double, T> val in this){
				array[arrayIndex++] = val;
			}
		}

		public int Count {
			get { return count; }
		}

		bool ICollection<KeyValuePair<double,T>>.IsReadOnly {
			get { return false; }
		}

		bool ICollection<KeyValuePair<double,T>>.Remove(KeyValuePair<double, T> item) {
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<KeyValuePair<double,T>> Members

		public IEnumerator<KeyValuePair<double, T>> GetEnumerator() {
			return EnumeratorHelper().GetEnumerator();
		}

		private IEnumerable<KeyValuePair<double, T>> EnumeratorHelper() {
			if (head < tail) {
				for (int i = head; i < tail; i++) {
					yield return new KeyValuePair<double, T>(timestamps[i], values[i]);
				}
			}
			else {
				for (int i = head; i < timestamps.Length; i++) {
					yield return new KeyValuePair<double, T>(timestamps[i], values[i]);
				}

				for (int i = 0; i < tail; i++) {
					yield return new KeyValuePair<double, T>(timestamps[i], values[i]);
				}
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region Value collection

		private class ValueCollection : ICollection<T> {
			private TimeWindowQueue<T> parent;

			public ValueCollection(TimeWindowQueue<T> parent) {
				this.parent = parent;
			}

			#region ICollection<T> Members

			void ICollection<T>.Add(T item) {
				throw new NotSupportedException();
			}

			void ICollection<T>.Clear() {
				throw new NotSupportedException();
			}

			public bool Contains(T item) {
				for (int i = 0; i < parent.count; i++) {
					if (object.Equals(parent[i].Value, item)) {
						return true;
					}
				}

				return false;
			}

			public void CopyTo(T[] array, int arrayIndex) {
				if (array.Length + arrayIndex < parent.count) {
					throw new ArgumentException("Array does not have sufficient capacity");
				}

				for (int i = 0; i < parent.count; i++) {
					array[arrayIndex+i] = parent[i].Value;
				}
			}

			public int Count {
				get { return parent.count; }
			}

			bool ICollection<T>.IsReadOnly {
				get { return true; }
			}

			bool ICollection<T>.Remove(T item) {
				throw new NotSupportedException();
			}

			#endregion

			#region IEnumerable<T> Members

			public IEnumerator<T> GetEnumerator() {
				return EnumeratorHelper().GetEnumerator();
			}

			private IEnumerable<T> EnumeratorHelper() {
				foreach (KeyValuePair<double, T> item in parent) {
					yield return item.Value;
				}
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
				return GetEnumerator();
			}

			#endregion
		}

		#endregion

		#region Timestamp Collection

		private class TimestampCollection : ICollection<double> {
			private TimeWindowQueue<T> parent;

			public TimestampCollection(TimeWindowQueue<T> parent) {
				this.parent = parent;
			}

			#region ICollection<double> Members

			void ICollection<double>.Add(double item) {
				throw new NotSupportedException();
			}

			void ICollection<double>.Clear() {
				throw new NotSupportedException();
			}

			public bool Contains(double item) {
				for (int i = 0; i < parent.count; i++) {
					if (object.Equals(parent[i].Key, item)) {
						return true;
					}
				}

				return false;
			}

			public void CopyTo(double[] array, int arrayIndex) {
				if (array.Length + arrayIndex < parent.count) {
					throw new ArgumentException("Array does not have sufficient capacity");
				}

				for (int i = 0; i < parent.count; i++) {
					array[arrayIndex+i] = parent[i].Key;
				}
			}

			public int Count {
				get { return parent.count; }
			}

			bool ICollection<double>.IsReadOnly {
				get { return true; }
			}

			bool ICollection<double>.Remove(double item) {
				throw new NotSupportedException();
			}

			#endregion

			#region IEnumerable<double> Members

			public IEnumerator<double> GetEnumerator() {
				return EnumeratorHelper().GetEnumerator();
			}

			private IEnumerable<double> EnumeratorHelper() {
				foreach (KeyValuePair<double, T> item in parent) {
					yield return item.Key;
				}
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
				return GetEnumerator();
			}

			#endregion
		}

		#endregion
	}
}
