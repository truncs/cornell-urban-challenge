using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Vehicle
{
	/// <summary>
	/// Specific estimate of vehicle position on the rndfNetwork
	/// </summary>
	[Serializable]
	public class RndfLocation
	{
		private IConnectWaypoints partition;
		private IWaypoint lowerBound;
		private IWaypoint upperBound;
		private double offset;
		private double error;
		private Coordinates absolutePositionOnPartition;
		private double laneConfidence;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="partition">Partition of the RndfNetwork the vehicle is a part of</param>
		/// <param name="lowerBound">Specific previous waypoint bounding the vehicle</param>
		/// <param name="upperBound">Specific next waypoint bounding the vehicle</param>
		/// <param name="offset"></param>
		/// <param name="error">Error value associated with the location</param>
		/// <param name="absolute">Absolute position of the vehicle on the rndfNetwork map</param>
		/// <param name="laneConfidence">Confidence the vehicle should be processed according to this lane</param>
		public RndfLocation(IConnectWaypoints partition, IWaypoint lowerBound, IWaypoint upperBound, double offset, double error, Coordinates absolute, double laneConfidence)
		{
			this.partition = partition;
			this.lowerBound = lowerBound;
			this.upperBound = upperBound;
			this.offset = offset;
			this.error = error;
			this.absolutePositionOnPartition = absolute;
			this.laneConfidence = laneConfidence;
		}

		/// <summary>
		/// Confidence the vehicle should be processed according to this lane
		/// </summary>
		public double LaneConfidence
		{
			get { return laneConfidence; }
			set { laneConfidence = value; }
		}

		/// <summary>
		/// Absolute position of the vehicle on the rndfNetwork map (along the partion)
		/// </summary>
		public Coordinates AbsolutePositionOnPartition
		{
			get { return absolutePositionOnPartition; }
			set { absolutePositionOnPartition = value; }
		}

		/// <summary>
		/// Error value associated with the location
		/// </summary>
		public double Error
		{
			get { return error; }
			set { error = value; }
		}

		/// <summary>
		/// Partition of the RndfNetwork the vehicle is a part of
		/// </summary>
		public IConnectWaypoints Partition
		{
			get { return partition; }
			set { partition = value; }
		}

		/// <summary>
		/// Specific previous waypoint bounding the vehicle
		/// </summary>
		public IWaypoint LowerBound
		{
			get { return lowerBound; }
			set { lowerBound = value; }
		}

		/// <summary>
		/// Specific next waypoint bounding the vehivle
		/// </summary>
		public IWaypoint UpperBound
		{
			get { return upperBound; }
			set { upperBound = value; }
		}

		/// <summary>
		/// Offset of the actual position from the partition
		/// </summary>
		public double Offset
		{
			get { return offset; }
			set { offset = value; }
		}


	}

	/// <summary>
	/// General estimate of vehicle position on the rndfNetwork
	/// </summary>
	[Serializable]
	public class LaneEstimate
	{
		private LaneID initialLane;
		private LaneID targetLane;
		private double confidence;
		private bool inIntersection;

		

		/// <summary>
		/// true if the vehicle is in an intersection
		/// </summary>
		public bool InIntersection
		{
			get { return inIntersection; }
			set { inIntersection = value; }
		}

		/// <summary>
		/// Lane a vehicle seems to be originating from
		/// </summary>
		public LaneID InitialLane
		{
			get { return initialLane; }
			set { initialLane = value; }
		}

		/// <summary>
		/// Lane the vehicle appears to be going towards
		/// </summary>
		public LaneID TargetLane
		{
			get { return targetLane; }
			set { targetLane = value; }
		}

		/// <summary>
		/// Value between 0 and 1 representing confidence in this estimate
		/// </summary>
		public double Confidence
		{
			get { return confidence; }
			set { confidence = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initialLane">Lane of origination</param>
		/// <param name="targetLane">Target lane of vehicle</param>
		/// <param name="confidence">Confidence of this beign a proper estimate</param>
		public LaneEstimate(LaneID initialLane, LaneID targetLane, double confidence, bool inIntersection)
		{
			this.initialLane = initialLane;
			this.targetLane = targetLane;
			this.confidence = confidence;
			this.inIntersection = inIntersection;
		}

		/// <summary>
		/// Constructor.
		/// Assumes not in intersection and lane confidence 1
		/// </summary>
		/// <param name="initialLane"></param>
		/// <param name="targetLane"></param>
		public LaneEstimate(LaneID initialLane, LaneID targetLane)
		{
			this.initialLane = initialLane;
			this.targetLane = targetLane;
			this.confidence = 1;
			this.inIntersection = false;
		}
	}


	/// <summary>
	/// The current state of the vehicle on the Rndf
	/// 
	/// Initial:
	/// References a vehicle to its initialLane and targetLane. 
	/// If the two values are the same => the vehicle is in a specific Lane
	/// If the two values are different => the vehicle is between Lanes either by changing lanes, U-turn, or intersection
	/// 
	/// Full:
	/// References a vehicle between two IWaypoints on some IConnectWaypoints
	/// </summary>
	[Serializable]
	public class VehicleRndfState
	{
		private IList<LaneEstimate> laneEstimates;
		private IList<RndfLocation> rndfPositionEstimates;		

		/// <summary>
		/// Specific estimate of vehicle position on the rndfNetwork
		/// </summary>
		public IList<RndfLocation> RndfPositionEstimates
		{
			get { return rndfPositionEstimates; }
			set { rndfPositionEstimates = value; }
		}

		/// <summary>
		/// General estimate of vehicle position on the rndfNetwork
		/// </summary>
		public IList<LaneEstimate> LaneEstimates
		{
			get { return laneEstimates; }
			set { laneEstimates = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public VehicleRndfState()
		{
			this.rndfPositionEstimates = new List<RndfLocation>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneEstimates">generalized estimates of what lane the vehicle belongs to</param>
		public VehicleRndfState(IList<LaneEstimate> laneEstimates)
		{
			this.LaneEstimates = laneEstimates;
		}


	}
}
