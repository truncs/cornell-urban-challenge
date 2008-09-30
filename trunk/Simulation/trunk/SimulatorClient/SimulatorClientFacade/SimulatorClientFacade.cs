using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using System.Windows.Forms;
using UrbanChallenge.Simulator.Client.World;
using Simulator.Engine;

namespace UrbanChallenge.Simulator.Client
{
	/// <summary>
	/// Remoting interface for a client of the simulator
	/// </summary>
	[Serializable]
	public abstract class SimulatorClientFacade : MarshalByRefObject
	{
		/// <summary>
		/// Gets the computer name of the client
		/// </summary>
		/// <returns></returns>
		public abstract string Name();

		/// <summary>
		/// Pings the client
		/// </summary>
		/// <returns></returns>
		public abstract bool Ping();

		/// <summary>
		/// Sets the mode of the car
		/// </summary>
		/// <param name="mode"></param>
		public abstract void SetCarMode(CarMode mode);

		/// <summary>
		/// Kill the client
		/// </summary>
		public abstract void Kill();

		/// <summary>
		/// Update the client
		/// </summary>
		public abstract SimVehicleState Update(WorldState worldState, double dt);

		/// <summary>
		/// Sets the road network and mission for the car
		/// </summary>
		/// <param name="roadNetwork"></param>
		/// <param name="mission"></param>
		public abstract void SetRoadNetworkAndMission(ArbiterRoadNetwork roadNetwork, ArbiterMissionDescription mission);

		/// <summary>
		/// Gets the item to be displayed as part of the list view
		/// </summary>
		public abstract ListViewItem ViewableItem();

		/// <summary>
		/// Sets the vehicle in the client
		/// </summary>
		/// <param name="vehicleId"></param>
		public abstract void SetVehicle(SimVehicleId vehicleId);

		/// <summary>
		/// Sets the client into step mode.
		/// </summary>
		public abstract void SetStepMode();

		/// <summary>
		/// Sets the clients into a continuous operation mode
		/// </summary>
		/// <param name="realtimeFactor">
		/// Factor by which time is scaled. For values less than 1, sim 
		/// is running slower than real time. For values greater than 1, 
		/// sim is running faster than real time.
		/// </param>
		public abstract void SetContinuousMode(double realtimeFactor);

		/// <summary>
		/// Called by a client (i.e. operational, arbiter) to register as supporting run control feedback
		/// </summary>
		/// <param name="client">Client object to call with run control messages</param>
		/// <param name="stepOrder">Order client should be invoked</param>
		/// <remarks>
		/// The order is used so that the arbiter can run first and then the operational. As a convention
		/// we'll assign the the following numbers to the order with space for things to be added later if 
		/// we need to: 10 = arbiter, 20 = operational.
		/// </remarks>
		public abstract void RegisterSteppableClient(ClientRunControlFacade client, int stepOrder);

		/// <summary>
		/// equality
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is SimulatorClientFacade)
			{
				return ((SimulatorClientFacade)obj).Name() == this.Name();
			}
			else
				return false;
		}

		/// <summary>
		/// hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.Name().GetHashCode();
		}

		/// <summary>
		/// Startup the vehicle
		/// </summary>
		/// <returns></returns>
		public abstract bool StartupVehicle();

    /// <summary>
    /// Resets the sim vehicle
    /// </summary>
    public abstract void ResetSim();
	}
}
