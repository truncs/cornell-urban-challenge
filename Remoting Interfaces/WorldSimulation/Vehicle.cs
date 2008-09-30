using System;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RoadNetwork;

namespace UrbanChallenge.WorldSimulation {

	// A simulated vehicle. Not that this is a marshal-by-value object to keep
	// overhead of property queries low. However, this also means that vehicle
	// objects have to be discarded after every update.
	[Serializable()]
	public class Vehicle {

		// Data object creator sets attribute values in constructor.
		public Vehicle(
			UInt32 id,
			Lane lane,
			Coordinates position,
			Double heading,
			Double velocity
		) {
			this.VehicleID = id;
			this.RoadID = lane.ParentWay.ParentRoad.RoadID;
			this.WayID = lane.ParentWay.WayID;
			this.LaneID = lane.LaneID;
			this.Position = position;
			this.Heading = heading;
			this.Velocity = velocity;
		}

		public readonly UInt32 VehicleID;
		public readonly UInt32 RoadID;
		public readonly UInt32 WayID;
		public readonly UInt32 LaneID;
		public Coordinates Position;
		public Double Heading;
		public Double Velocity;

	}

}
