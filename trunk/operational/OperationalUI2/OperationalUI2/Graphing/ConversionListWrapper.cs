using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Units;

namespace UrbanChallenge.OperationalUI.Graphing {
	class ConversionListWrapper : IList<double> {
		private UnitConversion conversion;
		private IList<double> list;

		public ConversionListWrapper(UnitConversion conversion, IList<double> list) {
			this.conversion = conversion;
			this.list = list;
		}

		public UnitConversion Conversion {
			get { return conversion; }
			set { conversion = value; }
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
				return conversion.Convert(list[index]);
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
			get { return list.Count; }
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
			foreach (double val in list) {
				yield return conversion.Convert(val);
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
