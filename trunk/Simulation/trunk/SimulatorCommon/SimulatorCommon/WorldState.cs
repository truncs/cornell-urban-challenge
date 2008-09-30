using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Engine;
using Simulator.Engine.Obstacles;

namespace UrbanChallenge.Simulator.Client.World
{
	/// <summary>
	/// State of the world
	/// </summary>
	[Serializable]
	public struct WorldState
	{
		/// <summary>
		/// Vehicles in the sim
		/// </summary>
		public Dictionary<SimVehicleId, SimVehicleState> Vehicles;

		/// <summary>
		/// Obstacles in the sime
		/// </summary>
		public Dictionary<SimObstacleId, SimObstacleState> Obstacles;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vehicles"></param>
		/// <param name="obstacles"></param>
		public WorldState(Dictionary<SimVehicleId, SimVehicleState> vehicles, Dictionary<SimObstacleId, SimObstacleState> obstacles)
		{
			this.Vehicles = vehicles;
			this.Obstacles = obstacles;
		}
	}
}
