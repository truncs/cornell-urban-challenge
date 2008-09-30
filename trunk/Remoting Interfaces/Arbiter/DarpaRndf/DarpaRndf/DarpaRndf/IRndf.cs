using System;
using System.Collections.Generic;
using UrbanChallenge.Common;

namespace UrbanChallenge.DarpaRndf
{

	[Serializable]
	public class IRndf
	{
		public string Name;
		public int NumSegs;
		public int NumZones;
		public string FormatVersion;
		public string CreationDate;
		public ICollection<SimpleSegment> Segments;
		public ICollection<SimpleZone> Zones;
	}

	[Serializable]
	public class SimpleWaypoint : IComparable<SimpleWaypoint>
	{
		public string ID;
		public string CheckpointID;
		public Coordinates Position;
		public bool IsStop;
		public bool IsCheckpoint;
		public int CompareTo(SimpleWaypoint other)
		{
			int c = this.ID.CompareTo(other.ID);
			if (c == 0)
				return this.CheckpointID.CompareTo(other.CheckpointID);
			else
				return c;
		}
	}

	[Serializable]
	public class SimpleExitEntry
	{
		public string ExitId;
		public string EntryId;
	}

	[Serializable]
	public class SimpleCheckpoint
	{
		public string WaypointId;
		public string CheckpointId;
	}

	[Serializable]
	public class SimpleLane
	{
		public string Id;
		public int NumWaypoints;
		public double LaneWidth;
		public string LeftBound;
		public string RightBound;
		public IList<SimpleWaypoint> Waypoints;
		public IList<SimpleExitEntry> ExitEntries;
		public IList<SimpleCheckpoint> Checkpoints;
		public ICollection<string> Stops;
	}

	[Serializable]
	public class SimpleSegment
	{
		public string Name;
		public int NumLanes;
		public string Id;
		public IList<SimpleLane> Lanes;
		public List<SimpleLane> Way1Lanes;
		public List<SimpleLane> Way2Lanes;
	}

	[Serializable]
	public class SpeedLimit
	{
		public string SegmentID;
		public double MinimumVelocity;
		public double MaximumVelocity;
	}

	[Serializable]
	public class SimpleZone
	{
		public string ZoneID;
		public int NumParkingSpots;
		public string Name;
		public ZonePerimeter Perimeter;
		public IList<ParkingSpot> ParkingSpots;
	}

	[Serializable]
	public class ParkingSpot
	{
		public string SpotID;
		public string SpotWidth;
		public string CheckpointWaypointID;
		public string CheckpointID;
		public SimpleWaypoint Waypoint1;
		public SimpleWaypoint Waypoint2;
	}

	[Serializable]
	public class ZonePerimeter
	{
		public string PerimeterID;
		public int NumPerimeterPoints;
		public IList<PerimeterPoint> PerimeterPoints;
		public IList<SimpleExitEntry> ExitEntries;
	}

	[Serializable]
	public class PerimeterPoint
	{
		public string ID;
		public Coordinates position;
	}
}
