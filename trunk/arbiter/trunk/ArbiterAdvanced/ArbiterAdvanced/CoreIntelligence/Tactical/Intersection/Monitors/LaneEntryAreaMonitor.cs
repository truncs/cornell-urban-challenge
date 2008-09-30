using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Path;
using System.Diagnostics;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	/// <summary>
	/// Monitors the netry area of an interconnect in a lane for containing a vehicle
	/// </summary>
	public class LaneEntryAreaMonitor : IEntryAreaMonitor, IIntersectionQueueable
	{
		/// <summary>
		/// Final waypoint of the turn
		/// </summary>
		private ArbiterWaypoint turnFinalWaypoint;

		/// <summary>
		/// Current vehicle we are tracking
		/// </summary>
		private VehicleAgent currentVehicle;

		/// <summary>
		/// Times the current vehicle
		/// </summary>
		private Stopwatch timer;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="turnFinal"></param>
		public LaneEntryAreaMonitor(ArbiterWaypoint turnFinal)
		{
			this.turnFinalWaypoint = turnFinal;
			this.timer = new Stopwatch();
		}

		/// <summary>
		/// Checks if the vehicle has been stopped for a helluva long time there and is blocking the entry
		/// </summary>
		public bool Failed
		{
			get
			{
				if (this.currentVehicle != null)
				{
					if (this.timer.IsRunning && this.timer.ElapsedMilliseconds > 25000)
					{
						return true;
					}
				}
				
				return false;
			}
		}

		#region IIntersectionQueueable Members

		public bool IsCompletelyClear(UrbanChallenge.Common.Vehicle.VehicleState ourState)
		{
			this.Update(ourState);
			return this.Clear(ourState);
		}

		public bool Clear(UrbanChallenge.Common.Vehicle.VehicleState ourState)
		{
			return (this.currentVehicle == null || 
				(this.currentVehicle.StateMonitor.Observed.speedValid && !this.currentVehicle.IsStopped && this.currentVehicle.Speed > 2.0));
		}

		public UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles.VehicleAgent Vehicle
		{
			get { return this.currentVehicle; }
		}

		public void Update(UrbanChallenge.Common.Vehicle.VehicleState ourState)
		{
			// get lane
			ArbiterLane lane = this.turnFinalWaypoint.Lane;

			// get the possible vehicles that could be in this stop
			if (TacticalDirector.VehicleAreas.ContainsKey(lane))
			{
				// get vehicles in the the lane of the exit
				List<VehicleAgent> possibleVehicles = TacticalDirector.VehicleAreas[lane];

				// get the point we are looking from
				LinePath.PointOnPath referencePoint = lane.LanePath().GetClosestPoint(this.turnFinalWaypoint.Position);

				// tmp
				VehicleAgent tmp = null;
				double tmpDist = Double.MaxValue;

				// check over all possible vehicles
				foreach (VehicleAgent possible in possibleVehicles)
				{
					// get vehicle point on lane
					LinePath.PointOnPath vehiclePoint = lane.LanePath().GetClosestPoint(possible.ClosestPosition);

					// check if the vehicle is in front of the reference point
					double vehicleDist = lane.LanePath().DistanceBetween(referencePoint, vehiclePoint);
					if (vehicleDist >= 0 && vehicleDist < TahoeParams.VL * 2.0)
					{
						tmp = possible;
						tmpDist = vehicleDist;
					}
				}

				if (tmp != null)
				{
					if (this.currentVehicle == null || !this.currentVehicle.Equals(tmp))
					{
						this.timer.Stop();
						this.timer.Reset();
						this.currentVehicle = tmp;
					}
					else
						this.currentVehicle = tmp;

					if (this.currentVehicle.IsStopped && !this.timer.IsRunning)
					{
						this.timer.Stop();
						this.timer.Reset();
						this.timer.Start();
					}
					else if (!this.currentVehicle.IsStopped && this.timer.IsRunning)
					{
						this.timer.Stop();
						this.timer.Reset();
					}
				}
				else
				{
					this.currentVehicle = null;
					this.timer.Stop();
					this.timer.Reset();
				}
			}
			else
			{
				this.timer.Stop();
				this.timer.Reset();
				this.currentVehicle = null;
			}
		}

		public void ResetTiming()
		{
			this.timer.Stop();
			this.timer.Reset();			
		}

		public bool IsOurs
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public UrbanChallenge.Arbiter.ArbiterRoads.ITraversableWaypoint Waypoint
		{
			get { return this.turnFinalWaypoint; }
		}

		public UrbanChallenge.Arbiter.ArbiterRoads.IVehicleArea Area
		{
			get { return this.turnFinalWaypoint.Lane; }
		}

		#endregion
	}
}
