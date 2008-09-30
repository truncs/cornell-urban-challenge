using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;

namespace OperationalLayer.Tracking.Steering {
	interface IRelativePath {
		/// <summary>
		/// Retrieves the relevant steering control data assuming the vehicle is at the origin
		/// </summary>
		/// <returns>Populated steering data</returns>
		SteeringControlData GetSteeringControlData(SteeringControlDataOptions opts);

		/// <summary>
		/// Transforms the path to the current timestamp
		/// </summary>
		void TransformPath(CarTimestamp timestamp);

		/// <summary>
		/// Car timestamp relative path is currently referenced to
		/// </summary>
		CarTimestamp CurrentTimestamp { get; }

		/// <summary>
		/// Car timestamp realtive path was originally referenced to
		/// </summary>
		CarTimestamp StartingTimestamp { get; }

		LinePath.PointOnPath ZeroPoint { get; }

		LinePath.PointOnPath AdvancePoint(LinePath.PointOnPath startPoint, double dist);
		LinePath.PointOnPath AdvancePoint(LinePath.PointOnPath startPoint, ref double dist);

		double DistanceBetween(LinePath.PointOnPath startPoint, LinePath.PointOnPath endPoint);
		double DistanceTo(LinePath.PointOnPath endPoint);

		/// <summary>
		/// Returns true if the vehicle has travelled past the end of the path
		/// </summary>
		bool IsPastEnd { get; }
	}
}
