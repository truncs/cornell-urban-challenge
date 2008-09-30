using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Blockage blocks a direction of travel on the road
	/// </summary>
	[Serializable]
	public class Blockage
	{
		/// <summary>
		/// Distance the of the blockage from hte LowerBound Waypoint
		/// </summary>
		public double DistanceFromLowerBound;

		/// <summary>
		/// Interconnect or Partition of Lane taht holds the blockage
		/// </summary>
		public IConnectWaypoints BlockageContainer;

		/// <summary>
		/// Identifier with which to modify the blockage
		/// </summary>
		public string BlockageID;

		/// <summary>
		/// Probability that the blockage exists
		/// </summary>
		public double ProbabilityExists;

		/// <summary>
		/// Waypoint blockage exists after
		/// </summary>
		public IWaypoint UpperBound;

		/// <summary>
		/// Waypoint blockage exists before
		/// </summary>
		public IWaypoint LowerBound;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="DistanceFromLowerBound">Distance the of the blockage from hte LowerBound Waypoint</param>
		/// <param name="BlockageID">Identifier with which to modify the blockage</param>
		/// <param name="LowerBound">Waypoint blockage exists after</param>
		/// <param name="UpperBound">Waypoint blockage exists before</param>
		/// <param name="ProbabilityExists">Initial probability with which the blockage exists</param>
		/// <param name="BlockageContainer">Interconnect or Partition of Lane taht holds the blockage</param>
		public Blockage(double DistanceFromLowerBound, IConnectWaypoints BlockageContainer, string BlockageID, IWaypoint LowerBound, IWaypoint UpperBound, double ProbabilityExists)
		{
			this.DistanceFromLowerBound = DistanceFromLowerBound;
			this.BlockageContainer = BlockageContainer;
			this.BlockageID = BlockageID;
			this.LowerBound = LowerBound;
			this.UpperBound = UpperBound;
			this.ProbabilityExists = ProbabilityExists;
		}
	}
}
