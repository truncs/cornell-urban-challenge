using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Represents a direction of travel in a Segment
	/// </summary>
	[Serializable]
	public class Way
	{
		private WayID wayID;
		private Segment segment;
		private Dictionary<LaneID, Lane> lanes;

		/// <summary>
		/// Default Constructor
		/// </summary>
		public Way()
		{
			lanes = new Dictionary<LaneID, Lane>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="wayID">Identification about Way</param>
		/// <param name="segment">Segment containing this Way</param>
		public Way(WayID wayID, Segment segment)
		{
			this.wayID = wayID;
			this.segment = segment;
			lanes = new Dictionary<LaneID, Lane>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="wayID">Identification about Way</param>
		/// <param name="segment">Segment containing this Way</param>
		/// <param name="lanes">Lanes this Way contains</param>
		public Way(WayID wayID, Segment segment, ICollection<Lane> lanes)
		{
			this.wayID = wayID;
			this.segment = segment;

			// Create Dictionary
			this.lanes = new Dictionary<LaneID, Lane>();
			foreach (Lane lane in lanes)
			{
				this.lanes.Add(lane.LaneID, lane);
			}
		}

		/// <summary>
		/// Dictionary containing the Lanes of the Way
		/// </summary>
		public Dictionary<LaneID, Lane> Lanes
		{
			get { return lanes; }
			set { lanes = value; }
		}

		/// <summary>
		/// Segment containing this Way
		/// </summary>
		public Segment Segment
		{
			get { return segment; }
			set { segment = value; }
		}

		/// <summary>
		/// Field representing if this Way dentoes a valid direction of travel in the Segment
		/// </summary>
		public bool IsValid
		{
			get { return(lanes.Count > 0); }
		}

		/// <summary>
		/// Identification information about the way
		/// </summary>
		public WayID WayID
		{
			get { return wayID; }
			set { wayID = value; }
		}
	}
}
