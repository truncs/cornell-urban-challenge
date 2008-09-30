using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	/// <summary>
	/// Interface for all entry monitors that can participate in the global queue
	/// </summary>
	public interface IIntersectionQueueable
	{
		/// <summary>
		/// Determines if absolutely no vehicle could be involved in the monitor
		/// </summary>
		/// <param name="ourState"></param>
		/// <returns></returns>
		bool IsCompletelyClear(VehicleState ourState);

		/// <summary>
		/// Flag for if the entry monitor is
		/// in a state such that someone with 
		/// lower priority in the queue could go
		/// </summary>
		bool Clear(VehicleState ourState);

		/// <summary>
		/// Vehicle this monitor is currently 
		/// tracking
		/// </summary>
		VehicleAgent Vehicle
		{
			get;
		}

		/// <summary>
		/// Tells the monitor to update itself
		/// </summary>
		void Update(VehicleState ourState);

		/// <summary>
		/// Tells the monitor to reset all timers
		/// </summary>
		void ResetTiming();

		/// <summary>
		/// Flag if represents our vehicle
		/// </summary>
		bool IsOurs
		{
			get;
		}

		/// <summary>
		/// Waypoint associated with monitor if exists
		/// </summary>
		ITraversableWaypoint Waypoint
		{
			get;
		}

		/// <summary>
		/// Area this is associated with
		/// </summary>
		IVehicleArea Area
		{
			get;
		}
	}
}
