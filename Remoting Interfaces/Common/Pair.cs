using System;
using System.Text;

namespace UrbanChallenge.Common {

	/// <summary>
	/// Generic pair of comparable object.
	/// </summary>
	/// <typeparam name="LeftType">
	/// Type of the left object.
	/// </typeparam>
	/// <typeparam name="RightType">
	/// Type of the right object.
	/// </typeparam>
	[Serializable]
	public struct Pair<LeftType, RightType>
	:	IComparable<Pair<LeftType, RightType>>
		where LeftType : IComparable<LeftType>
		where RightType : IComparable<RightType> {

		/// <summary>Constructor.</summary>
		/// <param name="left">The left object.</param>
		/// <param name="right">The right object.</param>
		public Pair(LeftType left, RightType right) {
			this.Left = left;
			this.Right = right;
		}

		/// <summary>The left object.</summary>
		public LeftType Left;

		/// <summary>The right object.</summary>
		public RightType Right;

		/// <summary>
		/// Generic comparison.
		/// </summary>
		/// <param name="other">
		/// The other pair.
		/// </param>
		/// <returns>
		/// The result of comparing the left objects if that is != 0.
		/// If the left objects are equal, the result of comparing the
		/// right objects.
		/// </returns>
		public int CompareTo(Pair<LeftType, RightType> other) {
			int leftResult = this.Left.CompareTo(other.Left);
			return leftResult == 0
				? this.Right.CompareTo(other.Right)
				: leftResult;
		}

		/// <summary>
		/// Generic equality.
		/// </summary>
		/// <param name="obj">The other pair.</param>
		/// <returns>True if both left and right object are equal.</returns>
		public override bool Equals(object obj) {
			if (obj is Pair<LeftType, RightType>) {
				Pair<LeftType, RightType> other = (Pair<LeftType, RightType>)obj;
				return this.Left.Equals(other.Left) && this.Right.Equals(other.Right);
			} else
				return false;
		}

		/// <summary>
		/// Generic hash function.
		/// </summary>
		/// <returns>A hash of left and right object.</returns>
		public override int GetHashCode() {
			return (Left.GetHashCode() << 16) ^ (Right.GetHashCode());
		}

		/// <summary>Generic conversion to string.</summary>
		/// <returns>"(Left, Right)"</returns>
		public override string ToString() {
			StringBuilder builder = new StringBuilder("(");
			builder.Append(Left.ToString());
			builder.Append(", ");
			builder.Append(Right.ToString());
			builder.Append(")");
			return builder.ToString();
		}

	}

}
