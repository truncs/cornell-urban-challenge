using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Units;

namespace UrbanChallenge.OperationalUI.Graphing {
	class TimeWindowQueue {
		/// <summary>
		/// Used to calculate the initialize buffer length based on the window size. Units are packets/sec.
		/// </summary>
		private const double nominal_rate = 20;
		/// <summary>
		/// Minimum buffer left
		/// </summary>
		private const int min_size = 256;

		/// <summary>
		/// Storage for timestamp values
		/// </summary>
		private double[] timestamps;
		/// <summary>
		/// Storage for actual values
		/// </summary>
		private double[] values;

		/// <summary>
		/// Head of queue index
		/// </summary>
		private int head;
		/// <summary>
		/// Tail of queue index
		/// </summary>
		private int tail;
		/// <summary>
		/// Number of elements in the queue
		/// </summary>
		private int count;

		/// <summary>
		/// Time window size
		/// </summary>
		private double windowSize;

		/// <summary>
		/// Max and min value in the queue
		/// </summary>
		private double maxValue, minValue;

		/// <summary>
		/// Flag indicating if a reset was performed because a timestamp went backwards
		/// </summary>
		private bool resetFlag;

		/// <summary>
		/// Last received timestamp
		/// </summary>
		private double lastTimestamp;

		public TimeWindowQueue(double windowSize) {
			// figure out the initial size we want to use
			int initialSize = (int)(windowSize*nominal_rate);

			if (initialSize < min_size) {
				initialSize = min_size;
			}
			else {
				// find next largest power of two buffer length
				int size = min_size;
				while (size < initialSize) {
					size <<= 1;
				}
				
				initialSize = size;
			}

			// allocate the space
			timestamps = new double[initialSize];
			values = new double[initialSize];

			// reset the min/max value, last timestamp
			this.minValue = double.MaxValue;
			this.maxValue = double.MinValue;

			this.lastTimestamp = double.NaN;

			// store the window size
			this.windowSize = windowSize;
		}

		/// <summary>
		/// Gets the number of values in the queue
		/// </summary>
		public int Count {
			get { return count; }
		}

		/// <summary>
		/// Gets a flag indicating if the queue was reset because of a decreasing timestamp since the last call to Clear().
		/// </summary>
		public bool WasReset {
			get { return resetFlag; }
		}

		/// <summary>
		/// Gets the maximum value of the queue. Returns double.MinValue if there are no elements in the queue.
		/// </summary>
		public double MaxValue {
			get {
				if (count == 0) throw new InvalidOperationException();
				return maxValue; 
			}
		}

		/// <summary>
		/// Gets the minimum value of the queue. Returns double.MaxValue if there are no elements in the queue.
		/// </summary>
		public double MinValue {
			get {
				if (count == 0) throw new InvalidOperationException();
				return minValue; 
			}
		}

		/// <summary>
		/// Gets the minimum timestamp of the entries in the queue. Throws an InvalidOperationException if there 
		/// are no elements in the queue.
		/// </summary>
		public double MinTimestamp {
			get {
				if (count > 0) {
					return timestamps[head];
				}
				else {
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Gets the maximum timestamp of the entries in the queue. Throws an InvalidOperationException if there 
		/// are no elements in the queue.
		/// </summary>
		public double MaxTimestamp {
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
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Gets or sets the window size to use while maintaining the queue.
		/// </summary>
		public double WindowSize {
			get { return windowSize; }
			set {
				if (value < windowSize && count > 0) {
					MaintainQueue(MaxTimestamp-value);
				}

				windowSize = value;
			}
		}

		/// <summary>
		/// Adds a value/timestamp pair to the queue. If the timestamp if less than the most recent timestamp added to the 
		/// queue, the queue will be cleared and the WasReset flag will be set to true.
		/// </summary>
		public void Add(double value, double timestamp) {
			if (!double.IsNaN(lastTimestamp) && timestamp < lastTimestamp) {
				// we need to perform a reset
				Clear();
				// set the reset flag
				resetFlag = true;

				// we will have enough space because the buffer size starts out non-zero and can only grow, and we just 
				// cleared the buffers
			}
			else {
				// we're adding this value on to the end of the queue
				// run queue maintenance
				MaintainQueue(timestamp-windowSize);

				// check if we have space to add the new value
				if (count >= timestamps.Length) {
					// we'll run out of space if we add, resize the queue
					ResizeQueue(count+1);
				}
			}

			// add to the tail
			timestamps[tail] = timestamp;
			values[tail] = value;

			// update tail index
			tail = (tail+1)%timestamps.Length;
			// increment count
			count++;

			// update min/max value
			if (value > maxValue) maxValue = value;
			if (value < minValue) minValue = value;

			// set the last timestamp we received to this timestamp
			lastTimestamp = timestamp;
		}

		public void AddRange(TimeWindowQueue queue) {
			// perform a preliminary check to see if the queue has data
			if (queue.count == 0) {
				return;
			}

			// if it has data, therefore all the indicators (min, max value, reset, etc) will be valid

			// check if the time window of the other queue makes us have to flush our entire queue
			// basically, if the other queue's max timestamp less the window time is greater than our max timestamp,
			// then we know all of our data will get invalidated
			// also check if a reset was performed on the queue or we have no data
			if (queue.WasReset || this.count == 0 || (queue.MaxTimestamp - windowSize > this.MaxTimestamp)) {
				// we need to flush our queue
				Clear();
			}
			else {
				// now we know that we want to keep around some our data
				// run maintain queue to clean out our old data based on the max timestamp of the new queue
				MaintainQueue(queue.MaxTimestamp - windowSize);
			}

			// check if we have enough space for the data
			if (count + queue.count > timestamps.Length) {
				// resize our queue
				ResizeQueue(count + queue.count);
			}

			// we verified that we have space to hold the remaining data, so we can just copy data after tail (wrapping
			// around if needed) and be fine
			if (tail + queue.count <= timestamps.Length) {
				// don't need to wrap around
				// copy the data directly
				queue.CopyTo(0, values, timestamps, tail, queue.count);
			}
			else {
				// we need to wrap
				int firstLength = timestamps.Length-tail;
				// copy tail->end
				queue.CopyTo(0, values, timestamps, tail, firstLength);
				queue.CopyTo(firstLength, values, timestamps, 0, queue.count-firstLength);
			}

			// update tail pointer
			tail = (tail+queue.count)%timestamps.Length;
			// update count
			count += queue.count;

			// update min and max from other queue
			if (queue.maxValue > maxValue) maxValue = queue.maxValue;
			if (queue.minValue < minValue) minValue = queue.minValue;

			// update the last timestamp from the other queue
			this.lastTimestamp = queue.lastTimestamp;
		}

		public void CopyTo(int startIndex, double[] valuesDest, double[] timestampsDest, int indexDest, int copyCount) {
			// check that count/startIndex are valid
			if (startIndex + copyCount > count) {
				throw new ArgumentOutOfRangeException();
			}

			// check that there is space on the destion
			if (indexDest + copyCount > timestampsDest.Length) {
				throw new ArgumentOutOfRangeException();
			}

			// figure out starting index into our array
			startIndex = (head+startIndex)%timestamps.Length;

			// check if we need to split the shits
			if (startIndex + copyCount <= timestamps.Length) {
				// don't need to split 
				Array.Copy(values, startIndex, valuesDest, indexDest, copyCount);
				Array.Copy(timestamps, startIndex, timestampsDest, indexDest, copyCount);
			}
			else {
				// need to split
				int firstLength = values.Length - startIndex;
				// do startIndex -> end, 0 -> copyCount-firstLength
				Array.Copy(values, startIndex, valuesDest, indexDest, firstLength); 
				Array.Copy(values, 0, valuesDest, indexDest+firstLength, copyCount-firstLength);

				// do the same for timestamps
				Array.Copy(timestamps, startIndex, timestampsDest, indexDest, firstLength);
				Array.Copy(timestamps, 0, timestampsDest, indexDest+firstLength, copyCount-firstLength);
			}
		}

		public void Clear() {
			// reset pointers/counters
			head = tail = count = 0;
			// mark that we haven't had a reset
			resetFlag = false;

			// set min/max value to the extremes
			minValue = double.MaxValue;
			maxValue = double.MinValue;
		}

		private void ResizeQueue(int minSize) {
			// double the size of the queue for now
			int newSize = timestamps.Length*2;

			while (newSize < minSize) {
				// double at each iteration
				newSize <<= 1;
			}

			// allocate the new storage
			double[] newValues = new double[newSize];
			double[] newTimestamps = new double[newSize];

			// copy in the new values
			if (count > 0) {
				if (head < tail) {
					// can do it in a single pass
					Array.Copy(values, head, newValues, 0, count);
					Array.Copy(timestamps, head, newTimestamps, 0, count);
				}
				else {
					// need two operations
					int firstLength = timestamps.Length - head;
					// 1) head to end of array
					// 2) start of array to tail
					Array.Copy(values, head, newValues, 0, firstLength);
					Array.Copy(values, 0, newValues, firstLength, tail);

					// do the same for the timestamps
					Array.Copy(timestamps, head, newTimestamps, 0, firstLength);
					Array.Copy(timestamps, 0, newTimestamps, firstLength, tail);
				}
			}

			// swap in the new storage
			values = newValues;
			timestamps = newTimestamps;
			// set the new head/tail points
			head = 0;
			tail = count;
		}

		private void MaintainQueue(double cutoffTime) {
			// flag indicated if we deleted one of the limits (i.e. max or min) from the items we've removed
			bool limitWasDeleted = false;

			// loop through while there are items and we're less than the cutoff time
			while (count > 0 && timestamps[head] < cutoffTime) {
				double val = values[head];
				// check if we're deleting the min or max value
				if (val <= minValue) limitWasDeleted = true;
				if (val >= maxValue) limitWasDeleted = true;

				// increment the head index
				head = (head+1)%timestamps.Length;
				// decrement the count
				count--;
			}

			// check if we have no elements left
			if (count == 0 || limitWasDeleted) {
				// reset min and max to extreme values
				minValue = double.MaxValue;
				maxValue = double.MinValue;

				// recalculate the min/max if there are values left
				if (count > 0) {
					if (head < tail) {
						// can go straight through
						for (int i = head; i < tail; i++) {
							double val = values[i];
							// track min/max
							if (val < minValue) minValue = val;
							if (val > maxValue) maxValue = val;
						}
					}
					else {
						// do it in two parts, head->end, 0->tail
						for (int i = head; i < values.Length; i++) {
							double val = values[i];
							// track min/max
							if (val < minValue) minValue = val;
							if (val > maxValue) maxValue = val;
						}
						for (int i = 0; i < tail; i++) {
							double val = values[i];
							// track min/max
							if (val < minValue) minValue = val;
							if (val > maxValue) maxValue = val;
						}
					}
				}
			}

		}

		public IList<double> TimestampList {
			get { return new TimestampQueueWrapper(this); }
		}

		public IList<double> ValueList {
			get { return new ValueQueueWrapper(this); }
		}

		#region List wrapper classes

		/// <summary>
		/// Read-only wrapper class exposing the IList generic interface for the timestamps. 
		/// Only supports indexer, Count, and GetEnumerator for now
		/// </summary>
		private class TimestampQueueWrapper : IList<double> {
			private TimeWindowQueue parent;

			public TimestampQueueWrapper(TimeWindowQueue parent) {
				this.parent = parent;
			}

			#region IList<double> Members

			public int IndexOf(double item) {
				throw new NotSupportedException();
			}

			public void Insert(int index, double item) {
				throw new NotSupportedException();
			}

			public void RemoveAt(int index) {
				throw new NotSupportedException();
			}

			public double this[int index] {
				get {
					if (index >= parent.count) {
						throw new ArgumentOutOfRangeException();
					}

					return parent.timestamps[(parent.head+index)%parent.timestamps.Length]; 
				}
				set {
					throw new NotSupportedException();
				}
			}

			#endregion

			#region ICollection<double> Members

			public void Add(double item) {
				throw new NotSupportedException();
			}

			public void Clear() {
				throw new NotSupportedException();
			}

			public bool Contains(double item) {
				throw new NotSupportedException();
			}

			public void CopyTo(double[] array, int arrayIndex) {
				throw new NotSupportedException();
			}

			public int Count {
				get { return parent.count; }
			}

			public bool IsReadOnly {
				get { return true; }
			}

			public bool Remove(double item) {
				throw new NotSupportedException();
			}

			#endregion

			#region IEnumerable<double> Members

			public IEnumerator<double> GetEnumerator() {
				return EnumeratorHelper().GetEnumerator();
			}

			private IEnumerable<double> EnumeratorHelper() {
				if (parent.head < parent.tail) {
					// iterate straight through
					for (int i = parent.head; i < parent.tail; i++) {
						yield return parent.timestamps[i];
					}
				}
				else {
					// head -> end, 0 -> tail
					for (int i = parent.head; i < parent.timestamps.Length; i++) {
						yield return parent.timestamps[i];
					}
					for (int i = 0; i < parent.tail; i++) {
						yield return parent.timestamps[i];
					}
				}
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
				return GetEnumerator();
			}

			#endregion
		}

		/// <summary>
		/// Read-only wrapper class exposing the IList generic interface for the timestamps. 
		/// Only supports indexer, Count, and GetEnumerator for now
		/// </summary>
		private class ValueQueueWrapper : IList<double> {
			private TimeWindowQueue parent;

			public ValueQueueWrapper(TimeWindowQueue parent) {
				this.parent = parent;
			}

			#region IList<double> Members

			public int IndexOf(double item) {
				throw new NotSupportedException();
			}

			public void Insert(int index, double item) {
				throw new NotSupportedException();
			}

			public void RemoveAt(int index) {
				throw new NotSupportedException();
			}

			public double this[int index] {
				get {
					if (index >= parent.count) {
						throw new ArgumentOutOfRangeException();
					}

					return parent.values[(parent.head+index)%parent.values.Length];
				}
				set {
					throw new NotSupportedException();
				}
			}

			#endregion

			#region ICollection<double> Members

			public void Add(double item) {
				throw new NotSupportedException();
			}

			public void Clear() {
				throw new NotSupportedException();
			}

			public bool Contains(double item) {
				throw new NotSupportedException();
			}

			public void CopyTo(double[] array, int arrayIndex) {
				throw new NotSupportedException();
			}

			public int Count {
				get { return parent.count; }
			}

			public bool IsReadOnly {
				get { return true; }
			}

			public bool Remove(double item) {
				throw new NotSupportedException();
			}

			#endregion

			#region IEnumerable<double> Members

			public IEnumerator<double> GetEnumerator() {
				return EnumeratorHelper().GetEnumerator();
			}

			private IEnumerable<double> EnumeratorHelper() {
				if (parent.head < parent.tail) {
					// iterate straight through
					for (int i = parent.head; i < parent.tail; i++) {
						yield return parent.values[i];
					}
				}
				else {
					// head -> end, 0 -> tail
					for (int i = parent.head; i < parent.timestamps.Length; i++) {
						yield return parent.values[i];
					}
					for (int i = 0; i < parent.tail; i++) {
						yield return parent.values[i];
					}
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
