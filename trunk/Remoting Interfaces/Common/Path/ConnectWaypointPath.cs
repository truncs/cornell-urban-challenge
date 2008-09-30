using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Path
{
	/// <summary>
	/// Represents a path over an IConnectWaypoints instance. 
	/// </summary>
	/// <remarks>
	/// This class is useful for tracking a path between and Exit/Entry pair in the RNDF network. It will use the user partitions
	/// of the <see cref="IConnectWaypoints" /> instance for generating the path segments.
	/// </remarks>
	[Serializable]
	public class ConnectWaypointPath : IPath
	{
		private IConnectWaypoints cw;

		/// <summary>
		/// Constructs the <see cref="ConnectWaypointPath"/> with the specified <see cref="IConnectWaypoints"/> instance.
		/// </summary>
		/// <param name="cw"><see cref="IConnectWaypoints"/> instance to use</param>
		public ConnectWaypointPath(IConnectWaypoints cw)
		{
			this.cw = cw;
		}

		/// <summary>
		/// Returns a <see cref="PartitionPathSegment"/> instance constructed from the given <see cref="UserPartition"/>.
		/// </summary>
		/// <param name="partition"><see cref="UserPartition"/> instance to construct the segment with</param>
		/// <returns><see cref="PartitionPathSegment"/> instance referencing <paramref name="partition"/>.</returns>
		private PartitionPathSegment GetPathSegment(UserPartition partition)
		{
			return new PartitionPathSegment(partition.InitialWaypoint.Position, partition.FinalWaypoint.Position, partition);
		}

		#region IPath Members

		public CoordinateMode CoordinateMode {
			get { return CoordinateMode.AbsoluteProjected; }
		}

		/// <summary>
		/// Returns the first point on the path. 
		/// </summary>
		/// <remarks>
		/// This will be the starting point of the first <see cref="UserPartition"/> in the <see cref="IConnectWaypoints"/> instance, 
		/// which should also be the same location as <see cref="IConnectWaypoints.InitialWaypoint"/>.
		/// <para>If there are no user segments, this will return <c>PointOnPath.Empty</c></para>
		/// </remarks>
		public PointOnPath StartPoint
		{
			get {
				if (cw.UserPartitions.Count == 0)
					return PointOnPath.Empty;

				PartitionPathSegment segment = GetPathSegment(cw.UserPartitions[0]);
				return segment.StartPoint;
			}
		}

		/// <summary>
		/// Returns the final point on the path. 
		/// </summary>
		/// <remarks>
		/// This will be the ending point of the last <see cref="UserPartition"/> in the <see cref="IConnectWaypoints"/> instance, 
		/// which should also be the same location as <see cref="IConnectWaypoints.FinalWaypoint"/>.
		/// <para>If there are no user segments, this will return <c>PointOnPath.Empty</c></para>
		/// </remarks>
		public PointOnPath EndPoint
		{
			get {
				if (cw.UserPartitions.Count == 0)
					return PointOnPath.Empty;

				PartitionPathSegment segment = GetPathSegment(cw.UserPartitions[cw.UserPartitions.Count - 1]);
				return segment.EndPoint;
			}
		}

		/// <summary>
		/// Total length of the path. 
		/// </summary>
		/// <remarks>
		/// Calculated as the sum of all distances between user paritions.
		/// </remarks>
		public double Length
		{
			get
			{
				double dist = 0;
				foreach (IPathSegment seg in this)
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
		/// Distance between the two path points integrated along the path. If <paramref name="ptStart"/> is before 
		/// <paramref name="ptEnd"/>, this will be positive. If <paramref name="ptStart"/> is after <paramref name="ptEnd"/>, this will be negative.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the segment specified by either <paramref name="ptStart"/> or <paramref name="ptEnd"/> does not exist in the segments collection.
		/// </exception>
		public double DistanceBetween(PointOnPath ptStart, PointOnPath ptEnd)
		{
			// check if they're on different segments, which comes first, etc
			int startIndex = IndexOf(ptStart.segment);
			int endIndex = IndexOf(ptEnd.segment);

			if (startIndex == -1 || endIndex == -1)
				throw new ArgumentException();

			bool swapped = false;

			// if the order is reversed, swap
			if (startIndex > endIndex)
			{
				PointOnPath temp = ptStart;
				ptStart = ptEnd;
				ptEnd = temp;
				swapped = true;
			}

			PartitionPathSegment startLaneSegment = ptStart.segment as PartitionPathSegment;

			if (startIndex == endIndex)
			{
				return ptEnd.dist - ptStart.dist;
			}
			else
			{
				double dist = ptStart.segment.DistanceToGo(ptStart);
				foreach (IPathSegment seg in GetEnumeratorAfter(startLaneSegment.UserPartition))
				{
					if (seg.Equals(ptEnd.segment))
					{
						dist += ptEnd.dist;
						if (swapped)
							return -dist;
						else
							return dist;
					}
					else
					{
						dist += seg.Length;
					}
				}

				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Returns the closest point on the path in terms of Euclidean distance.
		/// </summary>
		/// <param name="pt">Point to test</param>
		/// <returns>
		/// Closest point on the path. If there are no path segments, will return <see cref="PointOnPath.Empty"/>.
		/// </returns>
		/// <remarks>
		/// This enumerates over all path segments and calls <see cref="IPathSegment.ClosestPoint(Coordinates)"/> on each. The
		/// segment and associated point that has the smallest distance to the target point will be returned.
		/// </remarks>
		public PointOnPath GetClosest(Coordinates pt)
		{
			double minDist = 1e100;
			PointOnPath minpop = new PointOnPath();
			foreach (IPathSegment seg in this)
			{
				PointOnPath pop = seg.ClosestPoint(pt);
				double dist = pop.pt.DistanceTo(pt);
				if (dist < minDist)
				{
					minDist = dist;
					minpop = pop;
				}
			}

			return minpop;
		}

		/// <summary>
		/// Returns the closest segment and point that is on or past the segment specified by <paramref name="prev"/>.
		/// </summary>
		/// <param name="pt">Target point.</param>
		/// <param name="prev"><see cref="PointOnPath"/> to start search from.</param>
		/// <returns>Closest segment and point past <paramref name="prev"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the segment specified by <paramref name="prev"/> is not a member of the path's segments collection.
		/// </exception>
		/// <remarks>
		/// Starting with the segment specified by <paramref name="prev"/>, will search forward until a segment's closest point
		/// is not it's end point, i.e. the target point is not past the end of the segment. Will return 
		/// <see cref="EndPoint"/> if none of the path segments satisify this condition, indicating that the point is past
		/// the end of the path.
		/// </remarks>
		public PointOnPath GetClosest(Coordinates pt, PointOnPath prev)
		{
			if (!Contains(prev.segment))
				throw new ArgumentException();

			PartitionPathSegment laneSegment = prev.segment as PartitionPathSegment;

			foreach (IPathSegment seg in GetEnumeratorFrom(laneSegment.UserPartition))
			{
				PointOnPath pop = seg.ClosestPoint(pt);
				if (pop != seg.EndPoint)
				{
					return pop;
				}
			}

			return EndPoint;
		}

		/// <summary>
		/// Advances down the path by <paramref name="dist"/> units.
		/// </summary>
		/// <param name="pt">Starting point on path</param>
		/// <param name="dist">
		/// Distance to advance. On exit, will be the distance that could not be satisfied or 0 if the request did not go past
		/// the end of the path.
		/// </param>
		/// <returns>
		/// New advanced <see cref="PointOnPath"/> or <see cref="EndPoint"/> if past the end of the path.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the segment specified by <paramref name="pt"/> is not in the segments collection.
		/// </exception>
		/// <remarks>
		/// Advances the point starting with the segment specified in <paramref name="pt"/> and moving forward along the path. 
		/// At each segment, the <see cref="IPathSegment.AdvancePoint(UrbanChallenge.Common.Path.PointOnPath, ref double)"/> method 
		/// is called, then moving to the next segment until dist is exhausted. 
		/// </remarks>
		/// <example>
		/// This shows an example advancing down the first thirty meters of a path and checking if the result is past the end.
		/// <code>
		/// PointOnPath start = path.StartPoint;
		/// double dist = 30;
		/// PointOnPath end = path.Advance(start, ref dist);
		/// if (dist > 0) {
		///   // the ending point should be the end point of the path
		///   Debug.Assert(end == path.EndPoint);
		///		Debug.WriteLine("Past the end of the path!");
		/// }
		/// else {
		///   // the ending point should not be the end point of the path
		///		Debug.Assert(end != path.EndPoint);
		///		Debug.WriteLine("Not past the end!");
		/// }
		/// </code>
		/// </example>
		public PointOnPath AdvancePoint(PointOnPath pt, ref double dist)
		{
			if (!Contains(pt.segment))
				throw new ArgumentException();

			PartitionPathSegment laneSegment = pt.segment as PartitionPathSegment;

			// handle the first segment
			pt = laneSegment.AdvancePoint(pt, ref dist);
			if (dist == 0)
				return pt;

			// enumerate over the remaining segments
			foreach (IPathSegment seg in GetEnumeratorAfter(laneSegment.UserPartition))
			{
				// set the point on path to the beginning point of the next segment
				pt = seg.StartPoint;
				// advance the point down the segment
				pt = seg.AdvancePoint(pt, ref dist);

				// check if we've depleted our target distance
				if (dist == 0)
					return pt;
			}

			// this will end up being the same as EndPoint if we've gone past the end
			return pt;
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
			PartitionPathSegment laneSeg = item as PartitionPathSegment;
			if (laneSeg == null)
				return -1;

			UserPartition partition = laneSeg.UserPartition;

			return partition.ParentPartition.UserPartitions.IndexOf(partition);
		}

		void IList<IPathSegment>.Insert(int index, IPathSegment item)
		{
			throw new NotSupportedException();
		}

		void IList<IPathSegment>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the segment at the specified index. 
		/// </summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the list.</exception>
		/// <exception cref="NotSupportedException">Throw when attempting to set the property.</exception>
		public IPathSegment this[int index]
		{
			get
			{
				return GetPathSegment(cw.UserPartitions[index]);
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region ICollection<IPathSegment> Members

		void ICollection<IPathSegment>.Add(IPathSegment item)
		{
			throw new NotSupportedException();
		}

		void ICollection<IPathSegment>.Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Checks if the path contains the specified segment.
		/// </summary>
		/// <param name="item">Segment to search for.</param>
		/// <returns><c>true</c> if the item is found in the path, <c>false</c> otherwise.</returns>
		public bool Contains(IPathSegment item)
		{
			PartitionPathSegment laneSegment = item as PartitionPathSegment;
			if (laneSegment == null)
				return false;

			return cw.UserPartitions.Contains(laneSegment.UserPartition);
		}

		void ICollection<IPathSegment>.CopyTo(IPathSegment[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the number of segments in the path.
		/// </summary>
		/// <value>
		/// The number of segments in the path.
		/// </value>
		public int Count
		{
			get { return cw.UserPartitions.Count; }
		}

		bool ICollection<IPathSegment>.IsReadOnly
		{
			get { return true; }
		}

		bool ICollection<IPathSegment>.Remove(IPathSegment item)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<IPathSegment> Members

		/// <summary>
		/// Returns a path segment enumerator starting with the specified <see cref="UserPartition"/>.
		/// </summary>
		/// <param name="partition">Starting <see cref="UserPartition"/> of the enumerator</param>
		/// <returns>An <see cref="IEnumerable{IPathSegment}"/> instance.</returns>
		/// <remarks>
		/// <para>Each <see cref="IPathSegment"/> of the enumerator will be a <see cref="PartitionPathSegment"/>
		/// instance wrapping a <see cref="UserPartition"/>.
		/// </para>
		/// <para>
		/// The enumerator will start at the user partition specified by <paramref name="partition"/>.
		/// </para>
		/// </remarks>
		/// <example>
		/// This code example enumerates over all the user partition starting with a given starting partition.
		/// <code>
		/// foreach (IPathSegment seg in path.GetEnumeratorFrom(startingPartition) {
		///		PartitionPathSegment partitionSeg = (PartitionPathSegment)seg;
		///		Debug.WriteLine("user parition id: + " + partitionSeg.UserPartition.PartitionID.ToString());
		///		Debug.WriteLine("start: " + seg.Start.ToString() + ", end: " + seg.End.ToString());
		/// }
		/// </code>
		/// </example>
		public IEnumerable<IPathSegment> GetEnumeratorFrom(UserPartition partition)
		{
			// enumerate over the first segment
			IConnectWaypoints parent = partition.ParentPartition;
			for (int i = parent.UserPartitions.IndexOf(partition); i < parent.UserPartitions.Count; i++)
			{
				yield return GetPathSegment(parent.UserPartitions[i]);
			}
		}

		/// <summary>
		/// Returns a path segment enumerator starting after the specified <see cref="UserPartition"/>.
		/// </summary>
		/// <param name="partition">Starting <see cref="UserPartition"/> of the enumerator</param>
		/// <returns>An <see cref="IEnumerable{IPathSegment}"/> instance.</returns>
		/// <remarks>
		/// <para>Each <see cref="IPathSegment"/> of the enumerator will be a <see cref="PartitionPathSegment"/>
		/// instance wrapping a <see cref="UserPartition"/>.
		/// </para>
		/// <para>
		/// The enumerator will start after the user partition specified by <paramref name="partition"/>.
		/// </para>
		/// </remarks>
		/// <example>
		/// This code example enumerates over all the user partitions after a given starting partition.
		/// <code>
		/// foreach (IPathSegment seg in path.GetEnumeratorAfter(startingPartition) {
		///		PartitionPathSegment partitionSeg = (PartitionPathSegment)seg;
		///		Debug.WriteLine("user parition id: + " + partitionSeg.UserPartition.PartitionID.ToString());
		///		Debug.WriteLine("start: " + seg.Start.ToString() + ", end: " + seg.End.ToString());
		/// }
		/// </code>
		/// </example>
		public IEnumerable<IPathSegment> GetEnumeratorAfter(UserPartition partition)
		{
			bool first = true;
			foreach (IPathSegment seg in GetEnumeratorFrom(partition))
			{
				if (first)
				{
					first = false;
				}
				else
				{
					yield return seg;
				}
			}
		}

		private IEnumerable<IPathSegment> GetEnumeratorInternal()
		{
			foreach (UserPartition partition in cw.UserPartitions)
			{
				yield return GetPathSegment(partition);
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through all the user partitions.
		/// </summary>
		/// <returns>IEnumerator that can be used to iterate through the user partitions.</returns>
		public IEnumerator<IPathSegment> GetEnumerator()
		{
			return GetEnumeratorInternal().GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IPath Members


		public IPath Clone() {
			throw new NotSupportedException();
		}

		#endregion
	}
}
