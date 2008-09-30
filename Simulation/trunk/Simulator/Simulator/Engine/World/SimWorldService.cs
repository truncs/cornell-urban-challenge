using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using System.Collections;
using UrbanChallenge.Simulator.Client.World;
using Simulator.Engine.Obstacles;
using UrbanChallenge.Common.Vehicle;

namespace Simulator.Engine
{
	/// <summary>
	/// Provides access to information about the world
	/// </summary>
	[Serializable]
	public class SimWorldService
	{
		#region Private Members

		private SimEngine simEngine;

		#endregion

		#region Public Members

		/// <summary>
		/// Obstacles in the sim
		/// </summary>
		public Dictionary<SimObstacleId, SimObstacle> Obstacles;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arn"></param>
		public SimWorldService(SimEngine se)
		{
			// set engine
			this.simEngine = se;

			// populate obstacles
			this.Obstacles = new Dictionary<SimObstacleId, SimObstacle>();
		}

		#endregion

		#region Accessors and Modifiers

		#endregion

		#region Functions

		/// <summary>
		/// Gets the world state
		/// </summary>
		/// <returns></returns>
		public WorldState GetWorldState()
		{
			Dictionary<SimVehicleId, SimVehicleState> vehicleStates = new Dictionary<SimVehicleId,SimVehicleState>();
			Dictionary<SimObstacleId, SimObstacleState> obstacleStates = new Dictionary<SimObstacleId,SimObstacleState>();

			foreach (SimVehicle sv in this.simEngine.Vehicles.Values)
			{
				vehicleStates.Add(sv.SimVehicleState.VehicleID, sv.SimVehicleState);

				/*SimObstacleState sos = new SimObstacleState();
				sos.Heading = sv.Heading.Normalize();
				sos.IsBlockage = false;
				sos.Length = sv.Length;
				sos.ObstacleId = new SimObstacleId(sv.VehicleId.Number);
				sos.Position = sv.Position + sv.Heading.Normalize(TahoeParams.FL - (sv.Length / 2.0));
				sos.Width = sv.Width;
				obstacleStates.Add(sos.ObstacleId, sos);*/
			}

			foreach (SimObstacle so in this.Obstacles.Values)
			{
				SimObstacleState sos = new SimObstacleState();
				sos.Heading = so.Heading.Normalize();
				sos.IsBlockage = so.Blockage;
				sos.Length = so.Length;
				sos.ObstacleId = so.ObstacleId;
				sos.Position = so.Position;
				sos.Width = so.Width;
				obstacleStates.Add(sos.ObstacleId, sos);
			}

			return new WorldState(vehicleStates, obstacleStates);
		}

		#endregion
	}
}
