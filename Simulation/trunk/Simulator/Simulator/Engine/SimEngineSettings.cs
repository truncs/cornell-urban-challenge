using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Simulator.Engine
{
	/// <summary>
	/// Settings for the enging made accessible
	/// </summary>
	[Serializable]
	public class SimEngineSettingsAccessor
	{
		[CategoryAttribute("Default Sensor Settings"), DescriptionAttribute("Distance to search for obstacles in meters from the ai vehicle")]
		public double VehicleDistance
		{
			get { return SimEngineSettings.sensorsVehicleDistance; }
			set { SimEngineSettings.sensorsVehicleDistance = value; }
		}

		[CategoryAttribute("Default Sensor Settings"), DescriptionAttribute("Distance to search for vehicles in meters from the ai vehicle")]
		public double ObstacleDistance
		{
			get { return SimEngineSettings.sensorsObstacleDistance; }
			set { SimEngineSettings.sensorsObstacleDistance = value; }
		}

		[CategoryAttribute("Default Sensor Settings"), DescriptionAttribute("Divergence in radians of simulated lidar beam")]
		public double BeamDivergence
		{
			get { return SimEngineSettings.BeamSeparationAngle; }
			set { SimEngineSettings.BeamSeparationAngle = value; }
		}

		[CategoryAttribute("Simulation Settings"), DescriptionAttribute("Cycle time of the simulation in ms")]
		public int SimCycleTime
		{
			get { return SimEngineSettings.SimCycleTime; }
			set { SimEngineSettings.SimCycleTime = value; }
		}

		[Category("Simulation Settings"), Description("Determines if the simulation runs in single step increments or in a continuous fashion")]
		public bool StepMode {
			get { return SimEngineSettings.StepMode; }
			set { SimEngineSettings.StepMode = value; }
		}
	}

	/// <summary>
	/// Settings for hte engine
	/// </summary>
	[Serializable]
	public static class SimEngineSettings
	{
		/// <summary>
		/// Distance to search for obstacles in meters from the ai vehicle
		/// </summary>
		public static double sensorsObstacleDistance = 30;

		/// <summary>
		/// Distance to search for vehicles in meters from the ai vehicle
		/// </summary>
		public static double sensorsVehicleDistance = 50;

		/// <summary>
		/// Divergence in radians of "lidar" beam when looking for static obstacles
		/// </summary>
		public static double BeamSeparationAngle = Math.PI / 36;

		/// <summary>
		/// Cycle time of the simulation in ms
		/// </summary>
		public static int SimCycleTime = 100;

		public static Simulation simForm;

		public static bool StepMode {
			get { return simForm.simEngine.StepMode; }
			set { simForm.simEngine.StepMode = value; }
		}
	}
}
