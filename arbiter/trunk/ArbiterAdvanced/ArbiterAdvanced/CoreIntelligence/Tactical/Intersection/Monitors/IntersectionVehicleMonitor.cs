using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using System.Diagnostics;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	/// <summary>
	/// Monitors a vehicle inside the intersection for being a failed or reasonable vehicle
	/// </summary>
	public class IntersectionVehicleMonitor
	{
		/// <summary>
		/// vehicle we are monitoring inside the intersection
		/// </summary>
		private VehicleAgent intersectionVehicle;

		/// <summary>
		/// intersection we are monitoring on a whole
		/// </summary>
		private IntersectionMonitor globalMonitor;

		/// <summary>
		/// times the vehicle
		/// </summary>
		private Stopwatch stopwatch;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vehicle"></param>
		public IntersectionVehicleMonitor(IntersectionMonitor global, VehicleAgent vehicle)
		{
			this.stopwatch = new Stopwatch();
			this.intersectionVehicle = vehicle;
			this.globalMonitor = global;
		}

		/// <summary>
		/// Update the monitor
		/// </summary>
		public void Update()
		{
			// flag if vehicle exists anymore
			bool contains = false;

			// get updated agent form tactical direction
			foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
			{
				if (this.intersectionVehicle.Equals(va))
				{
					Polygon vaP = va.GetAbsolutePolygon(CoreCommon.Communications.GetVehicleState());	
					if(this.globalMonitor.Intersection.IntersectionPolygon.IsInside(vaP))
					{
						this.intersectionVehicle = va;
						contains = true;
					}
				}
			}

			if (contains)
			{
				if (this.intersectionVehicle.StateMonitor.Observed.speedValid && this.intersectionVehicle.IsStopped && !this.stopwatch.IsRunning)
				{
					this.ResetTimer();
					this.stopwatch.Start();
				}
				else if(!this.intersectionVehicle.StateMonitor.Observed.speedValid ||
					!this.intersectionVehicle.StateMonitor.Observed.isStopped)
				{
					this.ResetTimer();
				}
			}
			else
			{
				this.intersectionVehicle = null;
				this.ResetTimer();
			}
		}

		/// <summary>
		/// Resets the current timer
		/// </summary>
		public void ResetTimer()
		{
			this.stopwatch.Stop();
			this.stopwatch.Reset();
		}

		/// <summary>
		/// Checks if the vehicle failed
		/// </summary>
		/// <param name="oursTopPriority"></param>
		/// <returns></returns>
		public bool Failed(bool oursTopPriority)
		{
			double seconds = ((double)this.stopwatch.ElapsedMilliseconds)/1000;

			if (this.stopwatch.IsRunning && seconds > 10 && !oursTopPriority)
				return true;
			else if (this.stopwatch.IsRunning && seconds > 6 && oursTopPriority)
				return true;
			else
				return false;
		}

		/// <summary>
		/// If the vehicle should be deleted
		/// </summary>
		public bool ShouldDelete()
		{
			return this.intersectionVehicle == null;
		}

		/// <summary>
		/// Vehicle monitored
		/// </summary>
		public VehicleAgent Vehicle
		{
			get
			{
				return this.intersectionVehicle;
			}
		}

		#region Standard Equalities		

		public override string ToString()
		{
			if (this.Vehicle != null)
			{
				if (this.stopwatch.IsRunning)
				{
					double time = (double)this.stopwatch.ElapsedMilliseconds / 1000.0;
					return this.Vehicle.VehicleId.ToString() + ": stopped for " + time.ToString("F2");
				}
				else
					return this.Vehicle.VehicleId.ToString() + ": NOT stopped";
			}
			else
				return "";
		}

		#endregion
	}
}
