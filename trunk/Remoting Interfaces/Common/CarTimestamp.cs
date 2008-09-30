using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common {
	[Serializable]
	public struct CarTimestamp : IComparable<CarTimestamp>, IComparable, IEquatable<CarTimestamp> {
		public static readonly CarTimestamp Invalid = new CarTimestamp(double.NaN);

		/// <summary>
		/// Actual timestamp value represented as elapsed seconds since the start of the car
		/// </summary>
		public readonly double ts;

		public CarTimestamp(double ts) {
			this.ts = ts;
		}

		public CarTimestamp(int seconds, int ticks) {
			this.ts = seconds + (ticks / 10000.0);
		}

		public override bool Equals(object obj) {
			if (obj is CarTimestamp) {
				return ((CarTimestamp)obj).ts == ts;
			}
			else {
				return false;
			}
		}

		public bool IsValid { get { return !double.IsNaN(ts); } }
		public bool IsInvalid { get { return double.IsNaN(ts); } }

		public override int GetHashCode() {
			return ts.GetHashCode();
		}

		public override string ToString() {
			return ts.ToString("F4");
		}

		#region IComparable<CarTimestamp> Members

		public int CompareTo(CarTimestamp other) {
			return ts.CompareTo(other.ts);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj) {
			if (obj is CarTimestamp) {
				return CompareTo((CarTimestamp)obj);
			}
			else {
				throw new ArgumentException("obj");
			}
		}

		#endregion

		#region IEquatable<CarTimestamp> Members

		public bool Equals(CarTimestamp other) {
			return ts == other.ts;
		}

		#endregion

		#region Operators

		public static bool operator ==(CarTimestamp ct0, CarTimestamp ct1) {
			return ct0.ts == ct1.ts;
		}

		public static bool operator !=(CarTimestamp ct0, CarTimestamp ct1) {
			return ct0.ts != ct1.ts;
		}

		public static bool operator <(CarTimestamp ct0, CarTimestamp ct1) {
			return ct0.ts < ct1.ts;
		}

		public static bool operator <=(CarTimestamp ct0, CarTimestamp ct1) {
			return ct0.ts <= ct1.ts;
		}

		public static bool operator >(CarTimestamp ct0, CarTimestamp ct1) {
			return ct0.ts > ct1.ts;
		}

		public static bool operator >=(CarTimestamp ct0, CarTimestamp ct1) {
			return ct0.ts >= ct1.ts;
		}

		public static implicit operator CarTimestamp(double t) {
			return new CarTimestamp(t);
		}

		public static explicit operator double(CarTimestamp t) {
			return t.ts;
		}

		#endregion
	}
}
