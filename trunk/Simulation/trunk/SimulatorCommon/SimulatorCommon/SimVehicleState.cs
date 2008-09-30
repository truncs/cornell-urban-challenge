using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Shapes;

namespace Simulator.Engine
{
	/// <summary>
	/// State of a vehicle
	/// </summary>
	[Serializable]
	public class SimVehicleState
	{
		/// <summary>
		/// Identifies if the Vehicle is Bound to a Lane or Not
		/// </summary>
		public bool IsBound;

		/// <summary>
		/// Unique Id of the Vehicle
		/// </summary>
		public SimVehicleId VehicleID;

		/// <summary>
		/// Position of the Vehicle
		/// </summary>
		public Coordinates Position;

		/// <summary>
		/// Heading of the Vehicle
		/// </summary>
		public Coordinates Heading;

		/// <summary>
		/// Closest area of the vehicle
		/// </summary>
		public int AreaId;

		/// <summary>
		/// Subtype ofthe area
		/// </summary>
		public int AreaSubtypeId;

		/// <summary>
		/// Speed of the Vehicle
		/// </summary>
		public double Speed;

		/// <summary>
		/// set if the vehicle can move
		/// </summary>
		public bool canMove = true;

		/// <summary>
		/// Maximum speed set for hte vehicle
		/// </summary>
		public double MaximumSpeed = 0;

		/// <summary>
		/// Flag to determine whether to use set speed
		/// </summary>
		public bool UseMaximumSpeed = false;

		/// <summary>
		/// identifier to lock the speed fo the vehicle
		/// </summary>
		public bool LockSpeed = false;

		/// <summary>
		/// Mode of the vehicle
		/// </summary>
		public CarMode CarMode = CarMode.Pause;

		/// <summary>
		/// Length of vehicle
		/// </summary>
		public double Length;

		/// <summary>
		/// Width of vehicle
		/// </summary>
		public double Width;

		public bool SpeedValid;

		/// <summary>
		/// Default constructor
		/// </summary>
		public SimVehicleState()
		{
			this.Position = new Coordinates();
			this.Heading = new Coordinates(1, 0);
			this.Speed = 0.0;
			this.SpeedValid = true;
		}

		/// <summary>
		/// checks if represent the same vehicle
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is SimVehicleState)
				return this.VehicleID.Equals(((SimVehicleState)obj).VehicleID);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return this.VehicleID.GetHashCode();
		}

		public override string ToString()
		{
			return this.VehicleID.ToString();
		}

		public Polygon ToPolygon() {
			double halfWidth = Width/2;
			double halfLength = Length/2;

			Coordinates heading90 = Heading.Rotate90();

			Coordinates l = Heading*halfLength;
			Coordinates w = heading90*halfWidth;

			Coordinates pt1 = Position - l - w;
			Coordinates pt2 = Position + l - w;
			Coordinates pt3 = Position + l + w;
			Coordinates pt4 = Position - l + w;

			Polygon poly = new Polygon(4);
			poly.Add(pt1);
			poly.Add(pt2);
			poly.Add(pt3);
			poly.Add(pt4);

			return poly;
		}
	}
}
