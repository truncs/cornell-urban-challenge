using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Simulator.Client {
	/// <summary>
	/// Implemented by clients of the simulation (operational, arbiter) that 
	/// want to be aware of step mode changes
	/// </summary>
	[Serializable]
	public abstract class ClientRunControlFacade : MarshalByRefObject {
		public const int ArbiterStepOrder = 10;
		public const int OperationalStepOrder = 20;

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
		/// Invoked when the simulation is performing a step when in step mode.
		/// </summary>
		/// <remarks>
		/// This function should only be called when the simulation is in step mode.
		/// </remarks>
		public abstract void Step();
	}
}
