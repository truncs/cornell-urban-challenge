using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Sensors.LocalRoadEstimate
{


	/// <summary>
	/// Provides a full estimate of all the paramters of the lane
	/// </summary>
	[Serializable]	
	public struct LaneEstimate
	{		
		/// <summary>
		/// The id of the segment this lane corresponds to
		/// </summary>
		[Obsolete]
		public string id;
		/// <summary>
		/// distance to the center of the lane in vehicle coordinates
		/// </summary>
		/// 
		[Obsolete]
		public double center;
		/// <summary>
		/// The width of the lane
		/// </summary>
		[Obsolete]
		public double width;
		/// <summary>
		/// indicates any of the fields in the message are valid
		/// </summary>
		[Obsolete]
		public bool exists;
		/// <summary>
		/// MSE on the center estimate
		/// </summary>
		[Obsolete]
		public double centerVar;
		/// <summary>
		/// MSE on the width estimate
		/// </summary>
		[Obsolete]
		public double widthVar;	
	}

	/// <summary>
	/// Provides Information as to the location and existance of a stopline.
	/// </summary>.
	[Serializable]
	public struct StopLineEstimate
	{
		/// <summary>
		/// indicates that any of the other fields are valid
		/// </summary>
		public bool stopLineExists;
		/// <summary>
		/// Distance to the stopline in meters
		/// </summary>
		public double distToStopline;
		/// <summary>
		/// MSE Uncertainty
		/// </summary>
		public double distToStoplineVar;
	}

	/// <summary>
	/// Provides a full estimate of all paramters of the road and all lanes on the road. Also includes stopline information.
	/// </summary>
	[Serializable]
	public class LocalRoadEstimate
	{
		public const string ChannelName = "OperationalSceneEstimatorRoadInfoChannel";

		/// <summary>
		/// Synchronized vehicle time in seconds.
		/// </summary>
		public double timestamp;
		/// <summary>
		/// Indicates that any of the below paramters are valid. The O-stuff flag.
		/// </summary>
		[Obsolete]
		public bool isModelValid;
		/// <summary>
		/// Heading of the road in radians
		/// </summary>
		[Obsolete]
		public double roadHeading;
		/// <summary>
		/// Curvature of the road in m^(-1)
		/// </summary>
		[Obsolete]
		public double roadCurvature;
		/// <summary>
		/// Uncertainty of Heading (MSE)
		/// </summary>
		[Obsolete]
		public double roadHeadingVar;
		/// <summary>
		/// Uncertainty of Curvature (MSE)
		/// </summary>
		[Obsolete]
		public double roadCurvatureVar;
		/// <summary>
		/// The Right Lane Estimate
		/// </summary>
		[Obsolete]
		public LaneEstimate rightLaneEstimate;
		/// <summary>
		/// The Center Lane Estimate
		/// </summary>
		[Obsolete]
		public LaneEstimate centerLaneEstimate;
		/// <summary>
		/// The Left Lane Estimate
		/// </summary>
		[Obsolete]
		public LaneEstimate leftLaneEstimate;

		/// <summary>
		/// Estimate of the nearest stopline
		/// </summary>
		public StopLineEstimate stopLineEstimate;
	}
}
