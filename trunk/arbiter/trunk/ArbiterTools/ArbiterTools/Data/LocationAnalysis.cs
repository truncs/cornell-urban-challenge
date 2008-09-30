using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common;

namespace ArbiterTools.Data
{
	/// <summary>
	/// End result of analysis of a position relative to rndf
	/// </summary>
	[Serializable]
	public class LocationAnalysis
	{
		private double distanceFromLowerBound;
		private IConnectWaypoints partition;
		private IWaypoint lowerBound;
		private IWaypoint upperBound;
		private double offset;
		private Coordinates relativeRndfPosition;
		private double error;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="distanceFromLowerBound">distance of the vehicle's projected absolutePosition from the closest lowerBound IWaypoint on the Lane</param>
		/// <param name="partition">the lanePartition of Interconnect the vehicle can reference itself in</param>
		/// <param name="lowerBound">closest user or rndf waypoint previous to absolutePosition on lane</param>
		/// <param name="upperBound">closest user or rndf waypoint after absolutePosition on lane</param>
		/// <param name="offset">distance of hte vehicle's absolute absolutePosition from the lane's supposed absolutePosition</param>
		/// <param name="relativeRndfPosition">the absolute absolutePosition of the vehicle on the rndf in x,y</param>
		/// <param name="error">if the absolute absolutePosition is not perfectly offset, there is an error</param>
		public LocationAnalysis(double distanceFromLowerBound, IConnectWaypoints partition, IWaypoint lowerBound, IWaypoint upperBound, double offset, Coordinates relativeRndfPosition, double error)
		{
			this.distanceFromLowerBound = distanceFromLowerBound;
			this.partition = partition;
			this.lowerBound = lowerBound;
			this.upperBound = upperBound;
			this.offset = offset;
			this.relativeRndfPosition = relativeRndfPosition;
			this.error = error;
		}

		/// <summary>
		/// if the absolute absolutePosition is not perfectly offset, there is an error
		/// </summary>
		public double Error
		{
			get { return error; }
		}

		/// <summary>
		/// the absolute absolutePosition of the vehicle on the rndf in x,y
		/// </summary>
		public Coordinates RelativeRndfPosition
		{
			get { return relativeRndfPosition; }
		}

		/// <summary>
		/// distance of hte vehicle's absolute absolutePosition from the lane's supposed absolutePosition
		/// </summary>
		public double Offset
		{
			get { return offset; }
		}

		/// <summary>
		/// closest user or rndf waypoint after absolutePosition on lane
		/// </summary>
		public IWaypoint UpperBound
		{
			get { return upperBound; }
		}

		/// <summary>
		/// closest user or rndf waypoint previous to absolutePosition on lane
		/// </summary>
		public IWaypoint LowerBound
		{
			get { return lowerBound; }
		}

		/// <summary>
		/// the lanePartition of Interconnect the vehicle can reference itself in
		/// </summary>
		public IConnectWaypoints Partition
		{
			get { return partition; }
			set { partition = value; }
		}

		/// <summary>
		/// distance of the vehicle's projected absolutePosition from the closest lowerBound IWaypoint on the Lane
		/// </summary>
		public double DistanceFromLowerBound
		{
			get { return distanceFromLowerBound; }
		}
	}
}
