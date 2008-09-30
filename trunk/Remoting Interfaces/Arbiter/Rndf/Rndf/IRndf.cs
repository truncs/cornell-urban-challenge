using System;
using System.Collections.Generic;
using UrbanChallenge.Common;

namespace UrbanChallenge.Rndf {

    [Serializable]
    public class IRndf {
        public string Name;
        public int NumSegs;
        public int NumZones;
        public string FormatVersion;
        public string CreationDate;
        public ICollection<Segment> Segments;
        public ICollection<Zone> Zones;
    }

	[Serializable]
	public class Waypoint : IComparable<Waypoint> {
		public string ID;
		public string CheckpointID;
		public Coordinates Position;
		public bool IsStop;
		public bool IsCheckpoint;
		public int CompareTo(Waypoint other) {
			int c = this.ID.CompareTo(other.ID);
			if (c == 0)
				return this.CheckpointID.CompareTo(other.CheckpointID);
			else
				return c;
		}
	}

	[Serializable]
	public class ExitEntry {
		public string ExitId;
		public string EntryId;
	}

	[Serializable]
	public class Checkpoint {
		public string WaypointId;
		public string CheckpointId;
	}

	[Serializable]
	public class Lane {
		public string Id;
		public int NumWaypoints;
		public double LaneWidth;
		public string LeftBound;
		public string RightBound;
		public IList<Waypoint> Waypoints;
		public IList<ExitEntry> ExitEntries;
		public IList<Checkpoint> Checkpoints;
		public ICollection<string> Stops;
	}

	[Serializable]
	public class Segment {
		public string Name;
		public int NumLanes;
		public string Id;
		public IList<Lane> Lanes;
	}

	[Serializable]
	public class SpeedLimit {
		public string SegmentID;
		public double MinimumVelocity;
		public double MaximumVelocity;
	}

    [Serializable]
    public class Zone {
        public string ZoneID;
        public int NumParkingSpots;
        public string Name;
        public ZonePerimeter Perimeter;
        public IList<ParkingSpot> ParkingSpots;
    }

    [Serializable]
    public class ParkingSpot {
        public string SpotID;
        public string SpotWidth;
        public string CheckpointWaypointID;
        public string CheckpointID;
        public Waypoint Waypoint1;
        public Waypoint Waypoint2;
    }

    [Serializable]
    public class ZonePerimeter {
        public string PerimeterID;
        public int NumPerimeterPoints;
        public IList<PerimeterPoint> PerimeterPoints;
        public IList<ExitEntry> ExitEntries;
    }

    [Serializable]
    public class PerimeterPoint {
        public string ID;
        public Coordinates position;
    }
}
