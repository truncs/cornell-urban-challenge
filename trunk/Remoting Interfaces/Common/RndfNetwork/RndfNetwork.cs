using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.EarthModel;

namespace UrbanChallenge.Common.RndfNetwork
{
    /// <summary>
    /// Map based upon the Rndf with extra additions to provide for adding metadata to
    /// originally invisible structures
    /// </summary>
    [Serializable]
    public class RndfNetwork
    {
		private Dictionary<SegmentID, Segment> segments;
		private Dictionary<ZoneID, Zone> zones;
		private Dictionary<RndfWaypointID, RndfWayPoint> waypoints;
		private Dictionary<int, Goal> goals;
		private Dictionary<InterconnectID, Interconnect> interconnects;
		private Dictionary<InterconnectLanes, Interconnect> interconnectLanes;
		public Dictionary<RndfWaypointID, Intersection> intersections;
			public PlanarProjection projection;

		/// <summary>
		/// Constructor
		/// </summary>
		public RndfNetwork()
		{
			waypoints = new Dictionary<RndfWaypointID, RndfWayPoint>();
			goals = new Dictionary<int, Goal>();
		}

		/// <summary>
		/// Waypoints in the rndf
		/// </summary>
		public Dictionary<RndfWaypointID, RndfWayPoint> Waypoints
		{
			get { return waypoints; }
			set { waypoints = value; }
		}

		/// <summary>
		/// Goals in the rndf
		/// </summary>
		public Dictionary<int, Goal> Goals
		{
			get { return goals; }
			set { goals = value; }
		}

		/// <summary>
		/// Zones contained within the Rndf
		/// </summary>
		public Dictionary<ZoneID, Zone> Zones
		{
			get { return zones; }
			set { zones = value; }
		}

		/// <summary>
		/// Connections in the rndfNetwork
		/// </summary>
		public Dictionary<InterconnectID, Interconnect> Interconnects
		{
			get { return interconnects; }
			set { interconnects = value; }
		}


		public Dictionary<InterconnectLanes, Interconnect> InterconnectLanes
		{
			get { return interconnectLanes; }
			set { interconnectLanes = value; }
		}

		/// <summary>
		/// Roads contained within the Rndf
		/// </summary>
		public Dictionary<SegmentID, Segment> Segments
		{
			get { return segments; }
			set { segments = value; }
		}

		/// <summary>
		/// Set speed information for all segments from Mdf
		/// </summary>
		/// <param name="speedLimits"></param>
		public void SetSpeedLimits(List<SpeedInformation> speedLimits)
		{
			foreach (SpeedInformation speedInformation in speedLimits)
			{
				segments[speedInformation.SegmentID].SpeedInformation = speedInformation;
			}
		}
    }
}
