using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.RndfNetwork;

namespace Remora.Vehicles
{	
	/// <summary>
	/// Defines general vehicle
	/// </summary>
	public class Vehicle
	{
		private Coordinates initialPosition;
		private Coordinates initialVelocity;
		private double initialAcceleration;

		protected Coordinates position;
		protected Coordinates velocity;
		protected double acceleration;

		protected bool reset;

		/// <summary>
		/// Default Constructor
		/// </summary>
		public Vehicle()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="acceleration">1-D acceleration</param>
		public Vehicle(Coordinates position, Coordinates velocity, double acceleration, bool reset)
		{
			this.position = position;
			this.velocity = velocity;
			this.acceleration = acceleration;

			this.initialPosition = position;
			this.initialVelocity = velocity;
			this.initialAcceleration = acceleration;

			this.reset = reset;
		}

		/// <summary>
		/// Position of vehicle
		/// </summary>
		public Coordinates Position
		{
			get { return position; }
			set { position = value; }
		}

		/// <summary>
		/// Heading in radians
		/// </summary>
		public double Heading
		{
			get { return ((velocity.ToDegrees() * Math.PI) / 180); }
		}

		/// <summary>
		/// Speed in m/s
		/// </summary>
		public double Speed
		{
			get { return velocity.Length; }
		}

		public Coordinates Velocity
		{
			get { return velocity; }
		}

		/*public Coordinates InitialPosition
		{
			get { return initialPosition; }			
		}
		public Coordinates InitialVelocity
		{
			get { return initialVelocity; }		
		}

		public double InitialAcceleration
		{
			get { return initialAcceleration; }			
		}*/

		/// <summary>
		/// Reseat vehicle to initial Position
		/// </summary>
		public void Reset()
		{
			this.position = initialPosition;
			this.velocity = initialVelocity;
			this.acceleration = initialAcceleration;
		}

		/// <summary>
		/// Increment Position as 1-D based upon p,v,a
		/// </summary>
		/// <param name="time">time in seconds</param>
		public void StraightIncrement(double time)
		{
			Coordinates changePos = velocity.Normalize(Speed * time);
			position = position + changePos;
			Console.WriteLine("Position: " + position.ToString());
		}
	}

}
