using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning
{
	/// <summary>
	/// Interface for asking questions about changing lanes to a lane adjacent to current lane
	/// </summary>
	public interface ILateralReasoning
	{
		/// <summary>
		/// Lane the reasoning component monitors
		/// </summary>
		ArbiterLane LateralLane
		{
			get;
		}

		/// <summary>
		/// Reset the lateral reasoning component
		/// </summary>
		void Reset();

		/// <summary>
		/// If the lane exists laterally of us
		/// </summary>
		bool Exists
		{
			get;
		}

		/// <summary>
		/// Gets the adjacent vehicle
		/// </summary>
		VehicleAgent AdjacentVehicle
		{
			get;
		}

		/// <summary>
		/// Determines if the lane is clear to change into adjacent and rear of us in the lateral lane
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		bool AdjacentAndRearClear(VehicleState state);

		/// <summary>
		/// Checks if lane is clear
		/// </summary>
		/// <param name="state"></param>
		/// <param name="usForwards"></param>
		/// <param name="usDistanceToTravel"></param>
		/// <param name="usAvgSpeed"></param>
		/// <param name="timeClear"></param>
		/// <param name="distanceClear"></param>
		/// <param name="vehicle"></param>
		/// <returns></returns>
		bool ForwardClear(VehicleState state, double usDistanceToTravel, double usAvgSpeed, LaneChangeInformation information, Coordinates minReturn);

		/// <summary>
		/// Checks if curent lane is opposing relative or input
		/// </summary>
		/// <param name="goodLane"></param>
		/// <returns></returns>
		bool IsOpposing
		{
			get;
		}

		/// <summary>
		/// The forward vehicle laterally if exists
		/// </summary>
		VehicleAgent ForwardVehicle(VehicleState state);

		/// <summary>
		/// Side of the vehicle this reasoning component represents
		/// </summary>
		SideObstacleSide VehicleSide
		{
			get;
		}

		bool ExistsExactlyHere(VehicleState state);

		bool ExistsRelativelyHere(VehicleState state);
	}

	/// <summary>
	/// Information about why we are seeking a lane change
	/// </summary>
	public struct LaneChangeInformation
	{
		/// <summary>
		/// Reason we are seeking the lane change
		/// </summary>
		public LaneChangeReason Reason;

		/// <summary>
		/// Forward vehicle we are passing because if necessary
		/// </summary>
		public VehicleAgent ForwardVehicle;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reason"></param>
		/// <param name="forwardVehicle"></param>
		public LaneChangeInformation(LaneChangeReason reason, VehicleAgent forwardVehicle)
		{
			this.Reason = reason;
			this.ForwardVehicle = forwardVehicle;
		}

		/// <summary>
		/// Shows correct string for lci
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Reason.ToString();
		}
	}	
}
