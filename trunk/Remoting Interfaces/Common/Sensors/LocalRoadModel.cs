using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors
{

	public enum LocalRoadModelMessageID : int
	{
		LOCAL_ROAD_MODEL = 10
	}

	public static class LocalRoadModelChannelNames
	{
		public const string LocalRoadModelChannelName = "LocalRoadModelChannel";
		public const string LMLocalRoadModelChannelName = "LocalRoadModelChannelLM";		
	}

	public class LocalRoadModel
	{
		public const int MAX_LANE_POINTS = 100;
		/// <summary>
		/// Vehicle timestamp in seconds of when this model was created.
		/// </summary>
		public double timestamp;
		/// <summary>
		/// Probability the road model is valid, 0 to 1
		/// </summary>
		public float probabilityRoadModelValid;
		/// <summary>
		/// Probability the center lane exists, 0 to 1
		/// </summary>
		public float probabilityCenterLaneExists;
		/// <summary>
		/// Probability the left lane exists, 0 to 1
		/// </summary>
		public float probabilityLeftLaneExists;
		/// <summary>
		/// Probability the right lane exists, 0 to 1
		/// </summary>
		public float probabilityRightLaneExists;		
		/// <summary>
		/// Width of the center lane in meters.
		/// </summary>
		public float laneWidthCenter;
		/// <summary>
		/// Variance on the width of the center lane.
		/// </summary>
		public float laneWidthCenterVariance;
		/// <summary>
		/// Width of the right Lane in meters.
		/// </summary>
		public float laneWidthRight;
		/// <summary>
		/// Variance on the width of the right Lane.
		/// </summary>
		public float laneWidthRightVariance;
		/// <summary>
		/// Width of the left Lane in meters.
		/// </summary>
		public float laneWidthLeft;
		/// <summary>
		/// Variance on the width of the left Lane.
		/// </summary>
		public float laneWidthLeftVariance;
	

		/// <summary>
		/// Vehicle Relative Points that are spaced approximately every 0.5m
		/// </summary>
		public Coordinates[] LanePointsCenter;
		/// <summary>
		/// Vehicle Relative Points that are spaced approximately every 0.5m
		/// </summary>
		public Coordinates[] LanePointsRight;
		/// <summary>
		/// Vehicle Relative Points that are spaced approximately every 0.5m
		/// </summary>		
		public Coordinates[] LanePointsLeft;

		/// <summary>
		/// Variance on the vehicle relative center points.
		/// </summary>
		public float[] LanePointsCenterVariance;
		/// <summary>
		/// Variance on the vehicle relative right points.
		/// </summary>
		public float[] LanePointsRightVariance;
		/// <summary>
		/// Variance on the vehicle relative left points.
		/// </summary>
		public float[] LanePointsLeftVariance;
	}
}
