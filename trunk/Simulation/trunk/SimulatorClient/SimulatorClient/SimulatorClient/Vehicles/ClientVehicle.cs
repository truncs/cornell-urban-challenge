using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Engine;
using UrbanChallenge.Simulator.Client.World;

namespace UrbanChallenge.Simulator.Client.Vehicles
{
	/// <summary>
	/// Contains information about the client vehicle
	/// </summary>
	[Serializable]
	public class ClientVehicle
	{
		/// <summary>
		/// Vehicle id
		/// </summary>
		public SimVehicleId VehicleId;

		/// <summary>
		/// Current state of the vehicle associated with the client
		/// </summary>
		public SimVehicleState CurrentState;

		/// <summary>
		/// Update the vehicle
		/// </summary>
		/// <param name="worldState"></param>
		public void Update(WorldState worldState)
		{
			// update the current state held in the sim
			this.CurrentState = worldState.Vehicles[VehicleId];
		}
	}
}
