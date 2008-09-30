using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Path
{
    
	/// <summary>
	/// Represents a general path constructed by a series of path segments (which inherity from IPathSegment).
	/// </summary>
	[Serializable]
	public class Path : IPath
	{
		private List<IPathSegment> segments;
		private CoordinateMode coordMode = CoordinateMode.AbsoluteProjected;

		/// <summary>
		/// Constructs an empty path, i.e. one with no segments
		/// </summary>
		public Path()
		{
			segments = new List<IPathSegment>();
		}

		/// <summary>
		/// Constructs an empty path with the specified coordinate mode
		/// </summary>
		/// <param name="coordMode">Reference frame for the path points</param>
		public Path(CoordinateMode coordMode) {
			this.coordMode = coordMode;
			segments = new List<IPathSegment>();
		}

		/// <summary>
		/// Constructs a path initialized with the specified path segments
		/// </summary>
		/// <param name="initial"></param>
		public Path(IEnumerable<IPathSegment> initial)
		{
			segments = new List<IPathSegment>(initial);
		}

		/// <summary>
		/// Constructs a path initialized with the specified path segments and coordinate mode
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="coordMode"></param>
		public Path(IEnumerable<IPathSegment> initial, CoordinateMode coordMode)
			: this(initial) {
			this.coordMode = coordMode;
		}

		public void Transform(Matrix3 m) {
			foreach (IPathSegment seg in segments) {
				seg.Transform(m);
			}
		}

		#region IPath Members

		/// <summary>
		/// Reference frame for the path points/segments
		/// </summary>
		public CoordinateMode CoordinateMode {
			get { return coordMode; }
			set { coordMode = value; }
		}

		/// <summary>
		/// Creates a deep copy of the path.
		/// </summary>
		/// <returns></returns>
		public IPath Clone() {
			return new Path(CopyEnumerator(), coordMode);
		}

		/// <summary>
		/// Utility function for deep-copying of path.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IPathSegment> CopyEnumerator() {
			foreach (IPathSegment seg in segments) {
				yield return seg.Clone();
			}
		}

		/// <summary>
		/// Returns the first point on the path. This will be the starting point of the first segment.
		/// </summary>
		/// <remarks>
		/// If there are no path segments, this will return <c>PointOnPath.Empty</c>
		/// </remarks>
		public PointOnPath StartPoint
		{
			get {
				if (segments.Count == 0)
					return PointOnPath.Empty;
				else
					return segments[0].StartPoint;
			}
		}

		/// <summary>
		/// Returns the final point on the path. This will be the ending point of the last segment.
		/// </summary>
		/// <remarks>
		/// If there are no path segments, this will return <c>PointOnPath.Empty</c>.
		/// </remarks>
		public PointOnPath EndPoint
		{
			get {
				if (segments.Count == 0)
					return PointOnPath.Empty;
				else
					return segments[segments.Count - 1].EndPoint;
			}
		}

		/// <summary>
		/// Returns the closest point on the path in terms of Euclidean distance.
		/// </summary>
		/// <param name="pt">Point to test</param>
		/// <returns>
		/// Closest point on the path. If there are no path segments, will return <c>PointOnPath.Empty</c>.
		/// </returns>
		/// <remarks>
		/// This enumerates over all path segments and class <c>IPathSegment.ClosestPoint(pt)</c> on each. The
		/// segment and associated point that has the smallest distance to the target point will be returned.
		/// </remarks>
		public PointOnPath GetClosest(Coordinates pt)
		{
			double minDist = 1e100;
			PointOnPath minpop = PointOnPath.Empty;
			for (int i = 0; i < segments.Count; i++)
			{
				IPathSegment seg = segments[i];
				PointOnPath pop = seg.ClosestPoint(pt);
				//if (pop != seg.StartPoint && pop != seg.EndPoint) {
				double dist = pop.pt.DistanceTo(pt);
				if (dist < minDist)
				{
					minDist = dist;
					minpop = pop;
				}
				//}
			}

			return minpop;
		}

		/// <summary>
		/// Returns the closest segment and point that is on or past the segment specified by <c>prev</c>.
		/// </summary>
		/// <param name="pt">Target point</param>
		/// <param name="prev">PointOnPath to start search from</param>
		/// <returns>Closest segment and point past <c>prev</c></returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the segment specified by <c>prev</c> is not a member of the path's segments collection.
		/// </exception>
		/// <remarks>
		/// Starting with the segment specified by <c>prev</c>, will search forward until a segment's closest point
		/// is not it's end point, i.e. the target point is not past the end of the segment. Will return 
		/// <c>EndPoint</c> if none of the path segments satisify this condition, indicating that the point is past
		/// the end of the path.
		/// </remarks>
		public PointOnPath GetClosest(Coordinates pt, PointOnPath prev)
		{
			int i = segments.IndexOf(prev.segment);
			if (i == -1)
				throw new ArgumentOutOfRangeException();

			for (; i < segments.Count; i++)
			{
				IPathSegment seg = segments[i];
				PointOnPath pop = seg.ClosestPoint(pt);
				if (pop != seg.EndPoint)
				{
					return pop;
				}
			}

			return EndPoint;
		}

		/// <summary>
		/// Advances down the path by <c>dist</c> units.
		/// </summary>
		/// <param name="pt">Starting point on path</param>
		/// <param name="dist">
		/// Distance to advance. On exit, will be the distance that could not be satisfied or 0 if the request did not go past
		/// the end of the path.
		/// </param>
		/// <returns>
		/// New advanced <c>PointOnPath</c> or <c>EndPoint</c> if past the end of the path.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the segment specified by <c>pt</c> is not in the segments collection.
		/// </exception>
		/// <remarks>
		/// Advances the point starting with the segment specified in <c>pt</c> and moving forward along the path. At each segment, 
		/// the IPathSegment.Advance method is called, then moving to the next segment until dist is exhausted. 
		/// </remarks>
		/// <example>
		/// This shows an example advancing down the first thirty meters of a path and checking if the result is past the end.
		/// <code>
		/// PointOnPath start = path.StartPoint;
		/// double dist = 30;
		/// PointOnPath thirtyMeters = path.Advance(start, ref dist);
		/// if (dist > 0) {
		///		Debug.WriteLine("Past the end of the path!");
		/// }
		/// else {
		///		Debug.WriteLine("Not past the end!");
		/// }
		/// </code>
		/// </example>
		public PointOnPath AdvancePoint(PointOnPath pt, ref double dist)
		{
			int ind = segments.IndexOf(pt.segment);
			if (ind == -1)
				throw new ArgumentOutOfRangeException();

			if (dist > 0)
			{
				while (dist > 0)
				{
					IPathSegment seg = pt.segment;
					pt = seg.AdvancePoint(pt, ref dist);

					if (dist > 0)
					{
						// increment the segment index
						ind++;
						// check if we're at the end
						if (ind == segments.Count)
						{
							return pt;
						}
						else
						{
							// otherwise, move the PointOnPath to the beginning of the next segment
							pt = segments[ind].StartPoint;
						}
					}
				}
			}
			else
			{
				while (dist < 0)
				{
					IPathSegment seg = pt.segment;
					pt = seg.AdvancePoint(pt, ref dist);

					if (dist < 0)
					{
						// increment the segment index
						ind--;
						// check if we're at the end
						if (ind == -1)
						{
							return pt;
						}
						else
						{
							// otherwise, move the PointOnPath to the beginning of the next segment
							pt = segments[ind].EndPoint;
						}
					}
				}
			}


			return pt;
		}

		/// <summary>
		/// Total length of the curve. Calculated as the sum of all segment lengths.
		/// </summary>
		public double Length
		{
			get {
				double dist = 0;
				foreach (IPathSegment seg in segments)
				{
					dist += seg.Length;
				}
				return dist;
			}
		}

		/// <summary>
		/// Calculates the distance between two points, integrated along the path.
		/// </summary>
		/// <param name="ptStart">Starting point</param>
		/// <param name="ptEnd">Ending point</param>
		/// <returns>
		/// Distance between the two path points integrated along the path. If <c>startPt</c> is before 
		/// <c>endPt</c>, this will be positive. If <c>startPt</c> is after <c>endPt</c>, this will be negative.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the segment specified by either <c>startPt</c> or <c>endPt</c> does not exist in the segments collection.
		/// </exception>
		public double DistanceBetween(PointOnPath ptStart, PointOnPath ptEnd)
		{
			int startIndex = segments.IndexOf(ptStart.segment);
			int endIndex = segments.IndexOf(ptEnd.segment);

			if (startIndex == -1 || endIndex == -1)
				throw new ArgumentOutOfRangeException();

			if (startIndex == endIndex)
			{
				return ptEnd.dist - ptStart.dist;
			}
			else if (startIndex < endIndex)
			{
				double dist = 0;
				dist += ptStart.segment.DistanceToGo(ptStart);
				while (++startIndex < endIndex)
				{
					dist += segments[startIndex].Length;
				}
				dist += ptEnd.dist;
				return dist;
			}
			else
			{
				// startIndex > endIndex
				double dist = 0;
				dist -= ptStart.dist;
				while (--startIndex > endIndex)
				{
					dist -= segments[startIndex].Length;
				}
				dist -= ptEnd.segment.DistanceToGo(ptEnd);
				return dist;
			}
		}

		#endregion

		#region IList<IPathSegment> Members

		/// <summary>
		/// Returns the index of the specified segment.
		/// </summary>
		/// <param name="item">Segment to look for.</param>
		/// <returns>
		/// Zero-based index of segment if found, -1 if not found.
		/// </returns>
		public int IndexOf(IPathSegment item)
		{
			return segments.IndexOf(item);
		}

		/// <summary>
		/// Inserts the segment at <paramref name="index"/>.
		/// </summary>
		/// <param name="index">
		/// Zero-based index to insert at. The new segment is placed before the segment currently at index. 
		/// If <paramref name="index"/> equals <see cref="Count"/>, new segment is placed at the end of the collection.
		/// </param>
		/// <param name="item">New segment to insert.</param>
		/// <remarks>
		/// There is no checking done to ensure that the <see cref="IPathSegment.EndPoint"/> of the previous segment in the path matches the <see cref="IPathSegment.StartPoint"/> of the new segment.
		/// This checking must be done by the user.
		/// </remarks>
		public void Insert(int index, IPathSegment item)
		{
			segments.Insert(index, item);
		}

		/// <summary>
		/// Removes a segment at the specified index.
		/// </summary>
		/// <param name="index">Index of segment to remove.</param>
		public void RemoveAt(int index)
		{
			segments.RemoveAt(index);
		}

		/// <summary>
		/// Gets or sets the segment at the specified index. 
		/// </summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><c>index</c> is not a valid index in the list.</exception>
		public IPathSegment this[int index]
		{
			get { return segments[index]; }
			set { segments[index] = value; }
		}

		#endregion

		#region ICollection<IPathSegment> Members

		/// <summary>
		/// Add a path segment to the end of the path.
		/// </summary>
		/// <param name="item">New item to add</param>
		/// <remarks>
		/// There is no checking done to ensure that the EndPoint of the previous segment in the path matches the StartPoint of the new segment.
		/// This checking must be done by the user.
		/// </remarks>
		public void Add(IPathSegment item)
		{
			segments.Add(item);
		}

		/// <summary>
		/// Removes all segments from the path.
		/// </summary>
		public void Clear()
		{
			segments.Clear();
		}

		/// <summary>
		/// Checks if the path contains the specified segment.
		/// </summary>
		/// <param name="item">Segment to search for.</param>
		/// <returns>True if the item is found in the path, false otherwise.</returns>
		public bool Contains(IPathSegment item)
		{
			return segments.Contains(item);
		}

		/// <summary>
		/// Copies the segments of the path to an array, starting at a particular array index
		/// </summary>
		/// <param name="array">
		/// The one-dimensional array that is the destination of the elements copied from the path. The array must have zero-based indexing.
		/// </param>
		/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
		public void CopyTo(IPathSegment[] array, int arrayIndex)
		{
			segments.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Gets the number of segments in the path.
		/// </summary>
		/// <value>
		/// The number of segments in the path.
		/// </value>
		public int Count
		{
			get { return segments.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether the ICollection is read-only.
		/// </summary>
		/// <value>
		/// true if the ICollection is read-only; otherwise, false. 
		/// </value>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Removes the first occurrence of a specific segment from the path
		/// </summary>
		/// <param name="item">The object to remove from the path</param>
		/// <returns>
		/// true if item was successfully removed from the path; otherwise, false. 
		/// This method also returns false if item is not found in the path
		/// </returns>
		/// <remarks>
		/// Equality is determined using the Equals method of the path segment.
		/// </remarks>
		public bool Remove(IPathSegment item)
		{
			return segments.Remove(item);
		}

		#endregion

		#region IEnumerable<IPathSegment> Members

		/// <summary>
		/// Returns an enumerator that iterates through the segments collection.
		/// </summary>
		/// <returns>IEnumerator that can be used to iterate through the segments collection.</returns>
		public IEnumerator<IPathSegment> GetEnumerator()
		{
			return segments.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
