using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using RemoraAdvanced.Communications;
using RemoraAdvanced.Display.DisplayObjects;

namespace RemoraAdvanced.Common
{
	/// <summary>
	/// Common files for remora
	/// </summary>
	public static class RemoraCommon
	{
		/// <summary>
		/// Current Road Network
		/// </summary>
		public static ArbiterRoadNetwork RoadNetwork;

		/// <summary>
		/// Current mission
		/// </summary>
		public static ArbiterMissionDescription Mission;

		/// <summary>
		/// Communications
		/// </summary>
		public static Communicator Communicator;

		/// <summary>
		/// Information from the ai
		/// </summary>
		public static ArbiterInformationDisplay aiInformation;
	}
}
