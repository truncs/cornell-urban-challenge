using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Remora
{
	/// <summary>
	/// Current state of the simulation
	/// </summary>
	public enum RunState { Normal, Pause }

	/// <summary>
	/// Defines some global things to use
	/// </summary>
	public static class Globals
	{
		/// <summary>
		/// Width of Vehicle
		/// </summary>
		public static readonly double VehicleWidth = 2.5;

		/// <summary>
		/// Length of Vehicle
		/// </summary>
		public static readonly double VehicleLength = 5;

		/// <summary>
		/// Setting to track vehicle
		/// </summary>
		public static bool TrackVehicle = true;

		
	}
}
