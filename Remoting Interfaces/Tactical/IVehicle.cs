using System;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RoadNetwork;

namespace UrbanChallenge.Tactical {

	[Serializable()]
	public struct VehicleStatus {
		public uint RoadID;
		public uint WayID;
		public uint LaneID;					// 0 if IVehicle is in a Zone.
		public Coordinates CurrentPosition;	// Absolute position.
		public Coordinates TargetPosition;	// Next position which IVehicle is trying to hit.
		public Double Heading;				// Direction of movement in radian.
		public Double Velocity;				// Coordinates per second.
		public Behavior CurrentBehavior;	// Current action the vehicle is taking.
	}

	[Serializable()]
	public enum Behavior {
		NONE,			// Initial behavior after creation.
		STAY_IN_LANE,
		STAY_IN_LANE_AND_STOP,
		CHANGE_LANE,
		TURN
	};

	public interface IVehicle {

		// Listener registering and unregistering operations (observer pattern).
		UInt32 RegisterListener(IVehicleListener listener);
		void UnregisterListener(UInt32 token);

		// Abstract ID property. Used to uniquely identify vehicles.
		UInt32 ID { get; }

		void GetStatus(out VehicleStatus status);
		
		void StayInLane(Coordinates maxVelocity, Coordinates minVelocity);

		void StayInLaneAndStop(
			Coordinates maxVelocity,		// maxVelocity to hold to in this segment
			Coordinates minVelocity,		// min velocity for this segment except for if stopping at a stop sign
			Coordinates approxDistToEnd,	// approximate distance until we need to stop
			Coordinates stopSignPos			// the stop sign's position
		);

		void Turn(
			Coordinates maxVelocity,	// Maximum velocity.
			Coordinates minVelocity,	// Minimum velocity.
			UInt32 nextLane,			// Rndf defined lane id of lane we are turning into (to help define end tangent info).
			UInt32 nextSegment,			// Rndf defined seg id of Rndf defined segment we are turning into.
			Coordinates waypoint		// The waypoint we are turning to, to provide where to turn and where to take tangent info.
		);

		void ChangeLane(
			Coordinates maxVelocity,	// Maximum velocity.
			Coordinates minVelocity,	// Minimum velocity.
			UInt32 nextLane,			// Rndf defined lane id of lane we are changing into (to help define end tangent info) (note seg # same).
			// these used in absense of input after lane change
			Boolean hasStop,			// If there are stop signs in this and final lane.
			Coordinates distToEnd		// Distance to where we need to stop/turn.
		);

	}

}
