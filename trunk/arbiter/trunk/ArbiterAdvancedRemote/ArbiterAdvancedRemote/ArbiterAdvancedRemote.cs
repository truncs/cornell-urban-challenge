using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Arbiter.Core.Common;
using System.Runtime.Remoting.Messaging;

namespace UrbanChallenge.Arbiter.Core.Remote
{
	/// <summary>
	/// Remoting facade for teh advanced version of the arbiter
	/// </summary>
	[Serializable]
	public abstract class ArbiterAdvancedRemote : MarshalByRefObject
	{
		/// <summary>
		/// Pings the arbiter to see if there is a response
		/// </summary>
		public abstract bool Ping();

		/// <summary>
		/// Spools up the ai with a new road network and mission
		/// </summary>
		/// <param name="roadNetwork"></param>
		/// <param name="mission"></param>
		[OneWay]
		public abstract void JumpstartArbiter(ArbiterRoadNetwork roadNetwork, ArbiterMissionDescription mission);

		/// <summary>
		/// Udpates teh ai's mission
		/// </summary>
		/// <param name="mission"></param>
		/// <returns></returns>
		/// <remarks>Vehicle needs to be in pause</remarks>
		public abstract bool UpdateMission(ArbiterMissionDescription mission);

		/// <summary>
		/// Sets the ai's mode
		/// </summary>
		/// <param name="mode"></param>
		[OneWay]
		public abstract void SetAiMode(ArbiterMode mode);
				
		/// <summary>
		/// Set the road network
		/// </summary>
		/// <param name="roadNetwork"></param>
		/// <returns></returns>
		[OneWay]
		public abstract void Reset();

		/// <summary>
		/// Starts a new log
		/// </summary>
		[OneWay]
		public abstract void BeginNewLog();

		/// <summary>
		/// Pauses the vehicle from the ai's perspective
		/// </summary>
		[OneWay]
		public abstract void PauseFromAi();

		/// <summary>
		/// Goes into emergency stop mode
		/// </summary>
		[OneWay]
		public abstract void EmergencyStop();

		/// <summary>
		/// Gets teh road network
		/// </summary>
		/// <returns></returns>
		public abstract ArbiterRoadNetwork GetRoadNetwork();

		/// <summary>
		/// Gets the missions description
		/// </summary>
		/// <returns></returns>
		public abstract ArbiterMissionDescription GetMissionDescription();

		/// <summary>
		/// Reconnect to stuff
		/// </summary>
		public abstract void Reconnect();

		public abstract void RemoveNextCheckpoint();
	}
}
