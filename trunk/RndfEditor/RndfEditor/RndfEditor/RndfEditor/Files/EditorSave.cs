using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;

namespace RndfEditor.Files
{
	/// <summary>
	/// Save point of the rndf editor
	/// </summary>
	[Serializable]
	public class EditorSave
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
		/// Mission
		/// </summary>
		public ArbiterMissionDescription Mission;
	}
}
