using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Route;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	/// <summary>
	/// Information about our vehicle relative to a lane
	/// </summary>
	public class LaneDescription
	{
		private RndfLocation laneLocation;
		private Route laneRoute;
		private bool isValid;
		private bool isOncoming;
		private LaneID laneID;

		[NonSerialized]
		private Lane lane;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isValid">Notification if the lane is valid or not</param>
		public LaneDescription(bool isValid)
		{
			this.isValid = isValid;
		}

		/// <summary>
		/// ID of the lane
		/// </summary>
		public LaneID LaneID
		{
			get { return laneID; }
		}

		/// <summary>
		/// lane
		/// </summary>
		public Lane Lane
		{
			get { return lane; }
			set 
			{ 
				lane = value;
				laneID = lane.LaneID;
			}
		}

		/// <summary>
		/// Notifies if hte lane is travelling in the opposite direction
		/// </summary>
		public bool IsOncoming
		{
			get { return isOncoming; }
			set { isOncoming = value; }
		}

		/// <summary>
		/// Notifies if hte lane exists
		/// </summary>
		public bool IsValid
		{
			get { return isValid; }
			set { isValid = value; }
		}

		/// <summary>
		/// Best route from traveling in the lane
		/// </summary>
		public Route LaneRoute
		{
			get { return laneRoute; }
			set { laneRoute = value; }
		}

		/// <summary>
		/// Our vehicle's location on the lane
		/// </summary>
		public RndfLocation LaneLocation
		{
			get { return laneLocation; }
			set { laneLocation = value; }
		}
	}
}
