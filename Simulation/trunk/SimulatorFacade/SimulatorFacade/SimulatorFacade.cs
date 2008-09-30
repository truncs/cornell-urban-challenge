using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator
{
	/// <summary>
	/// Facade for interacting with the simulator
	/// </summary>
	[Serializable]
	public abstract class SimulatorFacade : MarshalByRefObject
	{
		/// <summary>
		/// Lets others know the simulation is alive
		/// </summary>
		/// <returns></returns>
		public abstract bool Ping();

		/// <summary>
		/// Registers a client to the simulation
		/// </summary>
		/// <param name="clientName"></param>
		/// <returns></returns>
		/// <remarks>the name can then be bound to the simulation</remarks>
		public abstract bool Register(string clientName);

		/// <summary>
		/// Attempt to get the client machines
		/// </summary>
		/// <returns></returns>
		public abstract Dictionary<string, int> GetClientMachines();

		/// <summary>
		/// Sets the simulation into step mode
		/// </summary>
		public abstract bool StepMode { get; set; }

		/// <summary>
		/// Sets the real-time rate factor. For values less than 1, the sim runs in less than real time
		/// </summary>
		public abstract double RealtimeFactor { get; set; }

		/// <summary>
		/// When in step mode, cause the simulator to execute a single step.
		/// </summary>
		public abstract void Step();
	}
}
