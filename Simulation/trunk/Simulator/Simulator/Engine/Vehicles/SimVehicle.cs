using System;
using System.Collections.Generic;
using System.Text;
using Simulator.Display.DisplayObjects;
using System.ComponentModel;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;

namespace Simulator.Engine
{
	/// <summary>
	/// Represents a traffic vehicle
	/// </summary>
	[Serializable]
	public class SimVehicle : CarDisplayObject
	{
		#region Private Members

		private SelectionType selection = SelectionType.NotSelected;
		private double sensedVehicleDistance = SimEngineSettings.sensorsVehicleDistance;
		private double sensedObstacleDistance = SimEngineSettings.sensorsObstacleDistance;
		private double sensorBeamDivergence = SimEngineSettings.BeamSeparationAngle;
		private bool randomizeMission = false;

		#endregion

		#region Public Members

		/// <summary>
		/// State of the vehicle
		/// </summary>
		public SimVehicleState VehicleState;

		#endregion

		#region Constructor

		/// <summary>
		/// COnstructor
		/// </summary>
		/// <param name="state"></param>
		/// <param name="length"></param>
		/// <param name="width"></param>
		public SimVehicle(SimVehicleState state, double length, double width)
		{
			this.VehicleState = state;
			this.VehicleState.Length = length;
			this.VehicleState.Width = width;
		}

		#endregion

		#region Car Display Members

		[Browsable(false)]
		public override RearAxleType RearAxleType
		{
			get { return RearAxleType.Rear; }
		}

		[Browsable(false)]
		public override UrbanChallenge.Common.Coordinates Position
		{
			get { return this.VehicleState.Position; }
			set { this.VehicleState.Position = value; }
		}

		[Browsable(false)]
		public override UrbanChallenge.Common.Coordinates Heading
		{
			get { return this.VehicleState.Heading; }
			set { this.VehicleState.Heading = value; }
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Width of the vehicle")]
		public override double Width
		{
			get { return this.SimVehicleState.Width; }
			set { this.SimVehicleState.Width = value; }
		}

		[Browsable(false)]
		protected override System.Drawing.Color color
		{
			get 
			{
				if (this.selectionType == SelectionType.SingleSelected)
					return DrawingUtility.ColorSimSelectedVehicle;
				if (this.VehicleState.IsBound)
					return DrawingUtility.ColorSimTrafficCar;
				else
					return DrawingUtility.ColorSimUnboundCar;
			}
		}

		[Browsable(false)]
		protected override RndfEditor.Display.Utilities.SelectionType selectionType
		{
			get { return selection; }
		}

		[Browsable(false)]
		protected override float steeringAngle
		{
			get { return 0; }
		}

		[Browsable(false)]
		protected override string Id
		{
			get { return this.VehicleState.VehicleID.ToString(); }
		}

		[Browsable(false)]
		public override bool MoveAllowed
		{
			get { return !this.VehicleState.IsBound; }
		}

		public override void BeginMove(UrbanChallenge.Common.Coordinates orig, RndfEditor.Display.Utilities.WorldTransform t)
		{	
		}

		public override void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, RndfEditor.Display.Utilities.WorldTransform t)
		{
			this.VehicleState.Position = orig + offset;
		}

		public override void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, RndfEditor.Display.Utilities.WorldTransform t)
		{
			this.VehicleState.Position = orig + offset;
		}

		public override void CancelMove(UrbanChallenge.Common.Coordinates orig, RndfEditor.Display.Utilities.WorldTransform t)
		{
			this.VehicleState.Position = orig;
		}

		[Browsable(false)]
		public override RndfEditor.Display.Utilities.SelectionType Selected
		{
			get
			{
				return selection;
			}
			set
			{
				selection = value;
			}
		}

		[Browsable(false)]
		public override bool CanDelete
		{
			get { return true; }
		}

		public override List<RndfEditor.Display.Utilities.IDisplayObject> Delete()
		{
			return new List<IDisplayObject>();
		}

		#endregion

		#region ISimVehicle Members

		/// <summary>
		/// Vehicle id
		/// </summary>
		[Browsable(false)]
		public SimVehicleId VehicleId
		{
			get { return this.VehicleState.VehicleID; }
		}

		#endregion

		#region Other Properties

		[CategoryAttribute("State Settings"), DescriptionAttribute("Heading of the Vehicle in Degrees")]
		public int VehicleHeading
		{
			get 
			{ 
				this.SimVehicleState.Heading = this.SimVehicleState.Heading.Normalize();
				return (int)this.SimVehicleState.Heading.ToDegrees();				
			}
			set 
			{
				double i = (double)value;
				Coordinates c = new Coordinates(1, 0);
				c = c.Rotate(i * Math.PI / 180.0);
				c = c.Normalize();
				this.SimVehicleState.Heading = c;
			}
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Length of the vehicle")]
		public override double Length
		{
			get { return this.SimVehicleState.Length; }
			set { this.SimVehicleState.Length = value; }
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Speed of the vehicle")]
		public double Speed
		{
			get { return this.VehicleState.Speed; }
			set { this.VehicleState.Speed = value; }
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Set Maximum speed of the vehicle if decide to use the maximum speed")]
		public double MaximumSpeed
		{
			get { return this.VehicleState.MaximumSpeed; }
			set { this.VehicleState.MaximumSpeed = value; }
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Whether to use the defined maximum speed or the mdf's speed limits")]
		public bool UseMaximumSpeed
		{
			get { return this.VehicleState.UseMaximumSpeed; }
			set { this.VehicleState.UseMaximumSpeed = value; }
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Locks the vehicles set speed")]
		public bool LockSpeed
		{
			get { return this.VehicleState.LockSpeed; }
			set { this.VehicleState.LockSpeed = value; }
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Flag if speed is valid or not")]
		public bool SpeedValid
		{
			get { return this.VehicleState.SpeedValid; }
			set { this.VehicleState.SpeedValid = value; }
		}

		[CategoryAttribute("Vehicle Settings"), DescriptionAttribute("Whether to the vehicle can move or it is stuck in its current position")]
		public bool CanMove
		{
			get { return this.VehicleState.canMove; }
			set { this.VehicleState.canMove = value; }
		}

		[Browsable(false)]
		public SimVehicleState SimVehicleState
		{
			get { return this.VehicleState; }
			set { this.VehicleState = value; }
		}

		[CategoryAttribute("Sensor Settings"), DescriptionAttribute("Distance to search for obstacles in meters from the ai vehicle")]
		public double VehicleDistance
		{
			get { return this.sensedVehicleDistance; }
			set { this.sensedVehicleDistance = value; }
		}

		[CategoryAttribute("Sensor Settings"), DescriptionAttribute("Distance to search for vehicles in meters from the ai vehicle")]
		public double ObstacleDistance
		{
			get { return this.sensedObstacleDistance; }
			set { this.sensedObstacleDistance = value; }
		}

		[CategoryAttribute("Sensor Settings"), DescriptionAttribute("Divergence in radians of simulated lidar beam")]
		public double BeamDivergence
		{
			get { return this.sensorBeamDivergence; }
			set { this.sensorBeamDivergence = value; }
		}

		[CategoryAttribute("Vehicle State"), DescriptionAttribute("Mode of operation of the vehicle")]
		public CarMode CarMode
		{
			get { return this.VehicleState.CarMode; }
			set { this.VehicleState.CarMode = value; }
		}

		[CategoryAttribute("Intelligence"), DescriptionAttribute("Whether the vehicle follows the default mdf or is given a random one")]
		public bool RandomMission
		{
			get { return randomizeMission; }
			set { randomizeMission = value; }
		}

		#endregion

		#region Standard Equalities

		public override bool Equals(object obj)
		{
			if (obj is SimVehicle)
			{
				return ((SimVehicle)obj).VehicleId.Equals(this.VehicleId);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return this.VehicleId.GetHashCode();
		}

		public override string ToString()
		{
			return this.VehicleId.ToString();
		}

		#endregion
	}
}
