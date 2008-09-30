using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using System.Diagnostics;

namespace UrbanChallenge.Common.Path
{

	/// <summary>
	/// Represents a path over a Lane
	/// </summary>
	/// <remarks>
	/// This class is useful for treating a lane as a series of line segments. This uses the <see cref="UserPartition"/> objects 
	/// between each Rndf waypoint for determining the geometry of the path.
	/// </remarks>
	[Serializable]
	public class LanePath : IPath
	{
		private Lane lane;

		/// <summary>
		/// Constructs a LanePath object.
		/// </summary>
		/// <param name="lane"><see cref="Lane"/> to base the path on.</param>
		public LanePath(Lane lane)
		{
			this.lane = lane;
		}

		/// <summary>
		/// <see cref="Lane"/> instance of the current <see cref="LanePath"/>
		/// </summary>
		public Lane Lane
		{
			get { return lane; }
		}

		public CoordinateMode CoordinateMode {
			get { return CoordinateMode.AbsoluteProjected; }
		}

		/// <summary>
		/// Returns the <see cref="PointOnPath"/> associated with the supplied <see cref="RndfWayPoint"/>.
		/// </summary>
		/// <param name="waypoint"><see cref="RndfWayPoint"/> to find on the path.</param>
		/// <returns><see cref="PointOnPath"/> instance of <paramref name="waypoint"/>.</returns>
		/// <remarks>
		/// The methods does not do any searching but returns the <see cref="PointOnPath"/> object directly. It will put
		/// the point at the end of a <see cref="PartitionPathSegment"/> if possible or at the beginning if the waypoint
		/// is the first in the lane.
		/// </remarks>
		/// <exception cref="ArgumentException">
		/// Throw when the waypoint does not belong to the <see cref="Lane"/> associated with the <see cref="LanePath"/>.
		/// </exception>
		public PointOnPath GetWaypointPoint(RndfWayPoint waypoint)
		{
			if (!lane.Equals(waypoint.Lane))
				throw new ArgumentException("Waypoint does not belong to the current lane");

			// get the user partition for that stuff
			if (waypoint.PreviousLanePartition != null)
			{
				LanePartition laneParition = waypoint.PreviousLanePartition;
				PartitionPathSegment pathSeg = GetPathSegment(laneParition.UserPartitions[laneParition.UserPartitions.Count - 1]);
				return pathSeg.EndPoint;
			}
			else
			{
				LanePartition laneParition = waypoint.NextLanePartition;
				PartitionPathSegment pathSeg = GetPathSegment(laneParition.UserPartitions[0]);
				return pathSeg.StartPoint;
			}
		}

		/// <summary>
		/// Returns the <see cref="PointOnPath"/> associated with the supplied <see cref="UserWaypoint"/>.
		/// </summary>
		/// <param name="waypoint"><see cref="UserWaypoint"/> to find on the path.</param>
		/// <returns><see cref="PointOnPath"/> instance of <paramref name="waypoint"/>.</returns>
		/// <remarks>
		/// The methods does not do any searching but returns the <see cref="PointOnPath"/> object directly. It will put
		/// the point at the end of a <see cref="PartitionPathSegment"/> if possible or at the beginning if the waypoint
		/// is the first in the lane.
		/// </remarks>
		public PointOnPath GetWaypointPoint(UserWaypoint waypoint)
		{
			if (!lane.LaneID.Equals(waypoint.WaypointID.LanePartitionID.LaneID))
				throw new ArgumentException("Waypoint does not belong to the current lane");

			if (waypoint.PreviousUserPartition != null)
			{
				return GetPathSegment(waypoint.PreviousUserPartition).EndPoint;
			}
			else
			{
				return GetPathSegment(waypoint.NextUserPartition).StartPoint;
			}
		}

		#region IPath Members

		/// <summary>
		/// Returns the first point of the lane. 
		/// </summary>
		/// <remarks>
		/// This will be the starting point of the first <see cref="RndfWayPoint"/> in the <see cref="Lane"/> instance, 
		/// which should also be the same location as initial waypoint of the first <see cref="LanePartition"/> 
		/// of the current lane's lane paritions.
		/// <para>If there are no user segments, this will return <c>PointOnPath.Empty</c></para>
		/// </remarks>
		public PointOnPath StartPoint
		{
			get { 
				UserPartition partition = lane.LanePartitions[0].UserPartitions[0];
				PartitionPathSegment pathSeg = GetPathSegment(partition);
				return new PointOnPath(pathSeg, 0, pathSeg.Start);
			}
		}

		/// <summary>
		/// Returns the final point of the lane. 
		/// </summary>
		/// <remarks>
		/// This will be the point of the last <see cref="RndfWayPoint"/> in the <see cref="Lane"/> instance, 
		/// which should also be the same location as final waypoint of the last <see cref="LanePartition"/> 
		/// of the current lane's lane paritions.
		/// <para>If there are no user segments, this will return <c>PointOnPath.Empty</c></para>
		/// </remarks>
		public PointOnPath EndPoint
		{
			get
			{
				LanePartition lanePartition = lane.LanePartitions[lane.LanePartitions.Count-1];
				UserPartition partition = lanePartition.UserPartitions[lanePartition.UserPartitions.Count - 1];
				PartitionPathSegment pathSeg = GetPathSegment(partition);
				return new PointOnPath(pathSeg, pathSeg.Length, pathSeg.End);
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
		/// Advances down the lane by <paramref name="dist"/> units.
		/// </summary>
		/// <param name="pt">Starting point on lane</param>
		/// <param name="dist">
		/// Distance to advance. On exit, will be the distance that could not be satisfied or 0 if the request did not go past
		/// the end of the lane.
		/// </param>
		/// <returns>
		/// New advanced <see cref="PointOnPath"/> or <see cref="EndPoint"/> if past the end of the lane.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the segment specified by <paramref name="pt"/> is not on the lane.
		/// </exception>
		/// <remarks>
		/// Advances the point starting with the segment specified in <paramref name="pt"/> and moving forward along the path. 
		/// At each segment, the <see cref="IPathSegment.AdvancePoint(UrbanChallenge.Common.Path.PointOnPath, ref double)"/> method 
		/// is called, then moving to the next segment until dist is exhausted. 
		/// </remarks>
		/// <example>
		/// This shows an example advancing down the first thirty meters of a lane and checking if the result is past the end.
		/// <code>
		/// LanePath path = new LanePath(someLane);
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

		/// <summary>
		/// Total length of the lane. 
		/// </summary>
		/// <remarks>
		/// Calculated as the sum of all distances between user paritions.
		/// </remarks>
		public double Length
		{
			get
			{
				double length = 0;
				foreach (IPathSegment seg in this)
				{
					length += seg.Length;
				}
				return length;
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

		private PartitionPathSegment GetPathSegment(UserPartition partition)
		{
			return new PartitionPathSegment(partition.InitialWaypoint.Position, partition.FinalWaypoint.Position, partition);
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
			LanePartition lanePartition = partition.ParentPartition as LanePartition;
			Debug.Assert(lanePartition.Lane.Equals(lane));

			int index = partition.ParentPartition.UserPartitions.IndexOf(partition);
			int laneIndex = lanePartition.Lane.LanePartitions.IndexOf(lanePartition);
			while (--laneIndex > 0)
			{
				index += lane.LanePartitions[laneIndex].UserPartitions.Count;
			}
			return index;
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
				foreach (LanePartition lanePartition in lane.LanePartitions)
				{
					if (lanePartition.UserPartitions.Count >= index)
					{
						index -= lanePartition.UserPartitions.Count;
					}
					else
					{
						UserPartition userPartition = lanePartition.UserPartitions[index];
						return GetPathSegment(userPartition);
					}
				}

				throw new ArgumentOutOfRangeException();
			}
			set { throw new NotSupportedException(); }
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

			LanePartition lanePartition = laneSegment.UserPartition.ParentPartition as LanePartition;
			if (lanePartition == null)
				return false;

			return lanePartition.Lane.Equals(lane);
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
		/// <remarks>
		/// Note that this must enumerate over all <see cref="LanePartition"/> instance
		/// in the lane to be calculated, which may be fairly expensive.
		/// </remarks>
		public int Count
		{
			get {
				int count = 0;
				foreach (LanePartition lanePartition in lane.LanePartitions)
				{
					count += lanePartition.UserPartitions.Count;
				}
				return count;
			}
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
			LanePartition lanePartition = partition.ParentPartition as LanePartition;
			if (lanePartition == null)
				throw new InvalidOperationException();

			for (int i = lanePartition.UserPartitions.IndexOf(partition); i < lanePartition.UserPartitions.Count; i++)
			{
				yield return GetPathSegment(lanePartition.UserPartitions[i]);
			}

			// enumerate over the later lane partitions
			for (int i = lane.LanePartitions.IndexOf(lanePartition) + 1; i < lane.LanePartitions.Count; i++)
			{
				foreach (UserPartition userPartition in lane.LanePartitions[i].UserPartitions)
				{
					yield return GetPathSegment(userPartition);
				}
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
			foreach (LanePartition lanePartition in lane.LanePartitions)
			{
				foreach (UserPartition userPartition in lanePartition.UserPartitions)
				{
					yield return GetPathSegment(userPartition);
				}
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
