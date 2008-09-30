using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common {
	[Serializable]
	public enum CarMode {
		/// <summary>
		/// Car is under human control
		/// </summary>
		/// <remarks>
		/// Actuation is disabled
		/// </remarks>
		Human,
		/// <summary>
		/// Car is currenttly in pause mode
		/// </summary>
		/// <remarks>
		/// Actuation is disengaged and will not respond to commands. 
		/// </remarks>
		Pause,
		/// <summary>
		/// Car is transitioning to pause mode 
		/// </summary>
		/// <remarks>
		/// This state is active when the car is coming to a stop after a pause command
		/// has been issued but the car has not yet come to a stop. Steering commands are 
		/// still accepted, but brake, throttle and transmission commands are ignored.
		/// </remarks>
		TransitioningToPause,
		/// <summary>
		/// Car is transitioning from Pause mode to Run mode
		/// </summary>
		/// <remarks>
		/// This state is active after the car has received a Run command from the DARPA
		/// E-Stop system but is in the waiting period before it is allowed to transition (~5 secs).
		/// Commands are ignored during in this state.
		/// </remarks>
		TransitioningFromPause,
		/// <summary>
		/// Car is under computer control
		/// </summary>
		/// <remarks>
		/// Acutation is engaged and system should execute commands
		/// </remarks>
		Run,
		/// <summary>
		/// Emergency stop has been activated
		/// </summary>
		/// <remarks>
		/// Brakes are applied and system is disabled
		/// </remarks>
		EStop,
		/// <summary>
		/// Car mode is currently unknown
		/// </summary>
		/// <remarks>
		/// Most likely because lower level systems are not responding
		/// </remarks>
		Unknown
	}
}
