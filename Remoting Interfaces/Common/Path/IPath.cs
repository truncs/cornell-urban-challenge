using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Path
{
	/// <summary>
	/// Represents a sequential collection of path segments with utility methods for working with that collection.
	/// </summary>
	public interface IPath : IList<IPathSegment>
	{
		/// <summary>
		/// Return the reference frame of the coordinates of the path.
		/// </summary>
		CoordinateMode CoordinateMode { get; }

		/// <summary>
		/// Returns the first point on the path. This will be the starting point of the first segment.
		/// </summary>
		/// <remarks>
		/// If there are no path segments, this will return <c>PointOnPath.Empty</c>
		/// </remarks>
		PointOnPath StartPoint { get; }

		/// <summary>
		/// Returns the final point on the path. This will be the ending point of the last segment.
		/// </summary>
		/// <remarks>
		/// If there are no path segments, this will return <c>PointOnPath.Empty</c>.
		/// </remarks>
		PointOnPath EndPoint { get; }

		/// <summary>
		/// Total length of the curve. 
		/// </summary>
		/// <remarks>
		/// Calculated as the sum of all segment lengths.
		/// </remarks>
		double Length { get; }

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
		double DistanceBetween(PointOnPath ptStart, PointOnPath ptEnd);

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
		PointOnPath GetClosest(Coordinates pt);

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
		PointOnPath GetClosest(Coordinates pt, PointOnPath prev);

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
		PointOnPath AdvancePoint(PointOnPath pt, ref double dist);

		/// <summary>
		/// Creates a deep-copy of the path. The new path will be completely seperate from
		/// the original path and can be modified.
		/// </summary>
		/// <returns>Copy of the path.</returns>
		IPath Clone();
	}
}
