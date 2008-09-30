using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Sensors.Vehicle
{
	/// <summary>
	/// The observation state of an observed vehicle
	/// </summary>
	public enum ObservedVehicleState
	{
		Normal = 0,
		Deleted = 99,
		Occluded = 1
	}

	/// <summary>
	/// The type of behavior the observed vehicle is exhibiting
	/// </summary>
	public enum ObservedVehicleStatus
	{
		Normal = 1,
		Erratic = 0
	}

	/// <summary>
	/// Defines a vehicle observed by the Scene Estimator
	/// </summary>
	[Serializable]
	public struct ObservedVehicle
	{
		/// <summary>
		/// Unique Id of the Vehicle
		/// </summary>
		public int Id;

		/// <summary>
		/// Length of the Vehicle in m (Rear to Front)
		/// </summary>
		public double Length;

		/// <summary>
		/// Width of the Vehicle in m (Side to Side)
		/// </summary>
		public double Width;

		/// <summary>
		/// Absolute Position of the Vehicle
		/// </summary>
		public Coordinates AbsolutePosition;

		/// <summary>
		/// Heading of the Vehicle
		/// </summary>
		public Coordinates Heading;

		/// <summary>
		/// Speed of the Vehicle along Heading
		/// </summary>
		public double Speed;

		/// <summary>
		/// The observation state of the vehicle
		/// </summary>
		public ObservedVehicleState ObservationState;

		/// <summary>
		/// The kind of behavior we observe the vehicle to be exhibiting
		/// </summary>
		public ObservedVehicleStatus ObservationStatus;

		/// <summary>
		/// Estimates of the area of the vehicle
		/// </summary>
		public AreaEstimate[] AreaEstimates;

		/// <summary>
		/// Flag if vehicle is stopped or not
		/// </summary>
		public bool IsStopped;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id"></param>
		/// <param name="length"></param>
		/// <param name="width"></param>
		/// <param name="absolutePosition"></param>
		/// <param name="heading"></param>
		/// <param name="speed"></param>
		/// <param name="observationState"></param>
		/// <param name="observationStatus"></param>
		public ObservedVehicle(int id, double length, double width, Coordinates absolutePosition,
			Coordinates heading, double speed, ObservedVehicleState observationState,
			ObservedVehicleStatus observationStatus, AreaEstimate[] areaEstimates, bool isStopped)
		{
			this.Id = id;
			this.Length = length;
			this.Width = width;
			this.AbsolutePosition = absolutePosition;
			this.Heading = heading;
			this.Speed = speed;
			this.ObservationState = observationState;
			this.ObservationStatus = observationStatus;
			this.AreaEstimates = areaEstimates;
			this.IsStopped = isStopped;
		}
	}
}
