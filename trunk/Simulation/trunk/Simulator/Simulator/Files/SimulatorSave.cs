using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using Simulator.Engine;
using UrbanChallenge.Arbiter.ArbiterMission;

namespace Simulator.Files
{
	/// <summary>
	/// Save point of the rndf editor
	/// </summary>
	[Serializable]
	public class SimulatorSave
	{
		// save of the road display
		public DisplaySave displaySave;

		// index of the grid size
		public int GridSizeIndex;

		/// <summary>
		/// Arbiter road network
		/// </summary>
		public ArbiterRoadNetwork ArbiterRoads;

		/// <summary>
		/// back end of the simulation
		/// </summary>
		public SimEngine SimEngine;

		/// <summary>
		/// Default mission
		/// </summary>
		public ArbiterMissionDescription Mission;
	}
}
