using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Path
{
	/// <summary>
	/// Represents a location, segment, and distance along that segment on path. 
	/// </summary>
	[Serializable]
	public struct PointOnPath : IEquatable<PointOnPath>
	{
		/// <summary>
		/// Default PointOnPath instance.
		/// </summary>
		/// <remarks>Returned by some functions to indicate an unknown point.</remarks>
		public static readonly PointOnPath Empty = new PointOnPath();

		/// <summary>
		/// The segment on the path.
		/// </summary>
		public IPathSegment segment;
		/// <summary>
		/// The distance along the segment.
		/// </summary>
		public double dist;
		/// <summary>
		/// The 2-D coordinates of associated with the segment and distance along that segment.
		/// </summary>
		public Coordinates pt;

		/// <summary>
		/// Constructs the structure with the specified Parameters
		/// </summary>
		public PointOnPath(IPathSegment segment, double dist, Coordinates pt)
		{
			if (segment == null)
				throw new ArgumentNullException();

			this.segment = segment;
			this.dist = dist;
			this.pt = pt;
		}

		#region IEquatable<PointOnPath> Members

		/// <summary>
		/// Determines whether the object specified is equal to the current PointOnPath.
		/// </summary>
		/// <param name="obj">The object to compare with the current PointOnPath.</param>
		/// <returns>true if the specified object is equal to the current PointOnPath, false otherwise.</returns>
		public override bool Equals(object obj)
		{
			if (obj is PointOnPath)
				return Equals((PointOnPath)obj);
			else
				return false;
		}

		/// <summary>
		/// Determines whether the PointOnPath specified is equal to the current PointOnPath.
		/// </summary>
		/// <param name="other">The PointOnPath instance to compare with the current PointOnPath.</param>
		/// <returns>true if the specified object is equal to the current PointOnPath, false otherwise.</returns>
		public bool Equals(PointOnPath other)
		{
			if ((segment == null) != (other.segment == null))
				return false;

			if (segment != null && !segment.Equals(other.segment))
				return false;

			if (!pt.ApproxEquals(other.pt, 0.001))
				return false;

			return true;
		}

		/// <summary>
		/// Gets the hash value for the current PointOnPath. Calculated from the segment's hash code, the distance, and 
		/// <c>pt's</c> hash code.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return ((segment != null) ? segment.GetHashCode() : 0) ^ dist.GetHashCode() ^ pt.GetHashCode();
		}

		#endregion

		/// <summary>
		/// Returns a string that represents the current PointOnPath.
		/// </summary>
		/// <returns>A string that represents the current PointOnPath.</returns>
		public override string ToString()
		{
			return ((segment != null) ? segment.ToString() : "<null>") + ":" + pt.ToString() + "," + dist.ToString("F2");
		}

		/// <summary>
		/// Equality operator between two PointOnPath instances.
		/// </summary>
		public static bool operator ==(PointOnPath l, PointOnPath r)
		{
			return l.Equals(r);
		}

		/// <summary>
		/// Inequality operator between two PointOnPath instances.
		/// </summary>
		public static bool operator !=(PointOnPath l, PointOnPath r)
		{
			return !l.Equals(r);
		}
	}
}
