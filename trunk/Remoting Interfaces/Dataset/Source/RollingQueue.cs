using System;
using System.Collections.Generic;
using System.Text;

namespace Dataset.Source {
	class RollingQueue<T> : ICollection<T> {
		private T[] data;
		private int head;
		private int tail;
		private int count;

		public RollingQueue(int capacity) {
			data = new T[capacity+1];
			head = tail = count = 0;
		}

		public void Enqueue(T item) {
			data[tail] = item;
			tail = (tail+1)%data.Length;

			if (tail == head) {
				// advance head
				head = (head+1)%data.Length;
			}
			else {
				count++;
			}
		}

		public T Dequeue() {
			if (count == 0)
				throw new InvalidOperationException();

			T val = data[head];
			head = (head+1)%data.Length;
			count--;

			return val;
		}

		public T Peek() {
			if (count == 0)
				throw new InvalidOperationException();

			return data[head];
		}

		#region ICollection<T> Members

		void ICollection<T>.Add(T item) {
			Enqueue(item);
		}

		public void Clear() {
			throw new NotImplementedException();
		}

		bool ICollection<T>.Contains(T item) {
			throw new NotImplementedException();
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			throw new NotImplementedException();
		}

		public int Count {
			get { return count; }
		}

		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}

		bool ICollection<T>.Remove(T item) {
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator() {
			return EnumHelper().GetEnumerator();
		}

		private IEnumerable<T> EnumHelper() {
			if (head < tail) {
				for (int i = head; i < tail; i++) {
					yield return data[i];
				}
			}
			else {
				for (int i = head; i < data.Length; i++) {
					yield return data[i];
				}

				for (int i = 0; i < tail; i++) {
					yield return data[i];
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
}
