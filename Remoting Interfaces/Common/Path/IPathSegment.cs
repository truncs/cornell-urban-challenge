using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Path
{
	/// <summary>
	/// Basic interface for path segments for an <see cref="IPath"/> instace.
	/// <seealso cref="PointOnPath"/>
	/// <seealso cref="IPath"/>
	/// </summary>
	public interface IPathSegment : IEquatable<IPathSegment>
	{
		/// <summary>
		/// Gets and sets the starting coordinates of the segment.
		/// </summary>
		Coordinates Start { get; set; }

		/// <summary>
		/// Gets the starting PointOnPath of the segment.
		/// </summary>
		PointOnPath StartPoint { get; }

		/// <summary>
		/// Gets and sets the ending coordinates of the segment.
		/// </summary>
		Coordinates End { get; set; }

		/// <summary>
		/// Gets the ending PointOnPath of the segment.
		/// </summary>
		PointOnPath EndPoint { get; }

		/// <summary>
		/// Gets the total length of the segment.
		/// </summary>
		double Length { get; }

		/// <summary>
		/// Calculates the distance remaining to the end of the path segment.
		/// </summary>
		/// <param name="pt">Point to test</param>
		/// <returns>Distance remaining to the end of the path segment integrated along the segment.</returns>
		double DistanceToGo(PointOnPath pt);

		/// <summary>
		/// Calculates the distance off the path from the closest point.
		/// </summary>
		/// <param name="pt">Point to test</param>
		/// <returns>Distance off from the closest point on the path.</returns>
		double DistanceOffPath(Coordinates pt);

		/// <summary>
		/// Determines the closest point on the path segment, capped to either end.
		/// </summary>
		/// <param name="pt">Point to search for.</param>
		/// <returns>Closest point on the segment.</returns>
		PointOnPath ClosestPoint(Coordinates pt);

		/// <summary>
		/// Move the specefied <see cref="PointOnPath"/> down the segment.
		/// </summary>
		/// <param name="pt">Point to test.</param>
		/// <param name="dist">
		/// Non-negative distance to advance. 
		/// <para>On exit, will be 0 if the request did not extend past the end of the path, the remaining distance otherwise.</para>
		/// </param>
		/// <returns>New advanced path point.</returns>
		/// <remarks>
		/// If <paramref name="dist"/> is greater than the distance remaining, this will return <see cref="EndPoint"/>. 
		/// </remarks>
		PointOnPath AdvancePoint(PointOnPath pt, ref double dist);

		/// <summary>
		/// Returns the tangent of the curve at the point on the path.
		/// </summary>
		/// <param name="pt">Point to find tangent at.</param>
		/// <returns>Tangent as a normalized vector.</returns>
		Coordinates Tangent(PointOnPath pt);

		/// <summary>
		/// Returns the curvature of the curve at the point on the path. This 
		/// is 1/radius of the osculating circle at the point.
		/// </summary>
		/// <param name="pt">Point to find curvature at.</param>
		/// <returns>1/radius of the osculating circle at the point, positive for left-pointing circle.</returns>
		double Curvature(PointOnPath pt);

		/// <summary>
		/// Makes a deep copy of the path segment.
		/// </summary>
		/// <returns>A copy of the path segment.</returns>
		IPathSegment Clone();

		/// <summary>
		/// Transforms the segment using the specified transformation matrix.
		/// </summary>
		/// <param name="m"></param>
		void Transform(Matrix3 m);
	}
}
