using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using System.Diagnostics;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	/// <summary>
	/// Represents a dominant lane that has priority over an interconnect
	/// </summary>
	public class DominantLaneEntryMonitor : IDominantMonitor
	{
		/// <summary>
		/// Global monitor of the intersection
		/// </summary>
		private IntersectionMonitor globalMonitor;

		/// <summary>
		/// Exit this lane entry monitor is associated with
		/// </summary>
		private ArbiterWaypoint exit;

		/// <summary>
		/// Lane this is associated with
		/// </summary>
		private ArbiterLane lane;

		/// <summary>
		/// The current vehicle associated with the stop
		/// </summary>
		private VehicleAgent currentVehicle;

		/// <summary>
		/// Current timer
		/// </summary>
		public Stopwatch waitingTimer;

		/// <summary>
		/// Intersection involved
		/// </summary>
		private IntersectionInvolved involved;

		/// <summary>
		/// Polygon of the exit
		/// </summary>
		private Polygon exitPolygon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="globalMonitor"></param>
		/// <param name="involved"></param>
		public DominantLaneEntryMonitor(IntersectionMonitor globalMonitor, IntersectionInvolved involved)
		{
			this.waitingTimer = new Stopwatch();
			this.globalMonitor = globalMonitor;
			this.lane = (ArbiterLane)involved.Area;
			this.involved = involved;

			if (involved.Exit != null)
				this.exit = (ArbiterWaypoint)involved.Exit;
			else
				this.exit = null;

			if (this.exit != null)
			{
				// create the polygon
				this.exitPolygon = this.ExitPolygon();
			}
		}

		#region IIntersectionQueueable Members

		/// <summary>
		/// Checks if the monitor is completely clear of vehicles
		/// </summary>
		/// <param name="ourState"></param>
		/// <returns></returns>
		public bool IsCompletelyClear(VehicleState ourState)
		{
			// update this
			this.Update(ourState);
			
			// clear if no vehicle referenced
			return currentVehicle == null;
		}

		/// <summary>
		/// Checks if vehicle is far enough away to leave
		/// </summary>
		public bool Clear(VehicleState ourState)
		{
			// if no current vehicle, the stuff is fine
			if (currentVehicle == null)
				return true;
			else
			{
				// check exit exists
				if (this.exit != null)
				{
					// check not inside polygon
					double distToExit;
					if (this.exitPolygon.IsInside(this.currentVehicle.ClosestPointTo(this.exit.Position, ourState, out distToExit).Value))
					{
						if (!this.LargerExcessiveWaiting)
						{
							ArbiterOutput.Output("DLEM: " + this.lane.ToString() + " not clear, vehicle " + this.currentVehicle.ToString() + " in exit polygon" +
								", speed valid: " + this.currentVehicle.StateMonitor.Observed.speedValid.ToString() +
								", is stopped: " + this.currentVehicle.IsStopped);
							return false;
						}
						else
						{
							ArbiterOutput.Output("DLEM: " + this.lane.ToString() + " not clear, vehicle " + this.currentVehicle.ToString() + " in exit polygon" +
								", speed valid: " + this.currentVehicle.StateMonitor.Observed.speedValid.ToString() +
								", is stopped: " + this.currentVehicle.IsStopped + ", passed large excessive wait test so going");
							return true;
						}
					}
				}
				
				// get the point we are looking from
				LinePath.PointOnPath referencePoint;

				// reference from exit if exists
				if (exit != null)
				{
					LinePath.PointOnPath pop = lane.LanePath().GetClosestPoint(exit.Position);
					referencePoint = lane.LanePath().AdvancePoint(pop, 3.0);
				}
				// otherwise look from where we are closest
				else
					referencePoint = lane.LanePath().GetClosestPoint(ourState.Front);

				// vehicle point
				LinePath.PointOnPath vehiclePoint = lane.LanePath().GetClosestPoint(currentVehicle.ClosestPosition);

				// distance
				double distance = lane.LanePath().DistanceBetween(vehiclePoint, referencePoint);

				if (distance < 30 && (!this.currentVehicle.StateMonitor.Observed.speedValid || !this.currentVehicle.IsStopped))
				{
					ArbiterOutput.Output("DLEM: " + this.lane.ToString() + " not clear, moving vehicle " + this.currentVehicle.ToString() + " within 30m of intersection" +
							", speed valid: " + this.currentVehicle.StateMonitor.Observed.speedValid.ToString() +
							", is stopped: " + this.currentVehicle.IsStopped);
					return false;
				}
				else if (distance > 0)
				{
					// vehicles speed
					double vehicleSpeed = this.currentVehicle.StateMonitor.Observed.speedValid ? Math.Abs(currentVehicle.Speed) : lane.Way.Segment.SpeedLimits.MaximumSpeed;
					if (!this.currentVehicle.StateMonitor.Observed.speedValid)
						ArbiterOutput.Output("DLEM: " + this.lane.ToString() + ", vehicle: " + this.currentVehicle.ToString() + " speed not valid, set to: " + lane.Way.Segment.SpeedLimits.MaximumSpeed.ToString("F2"));

					// check time
					double time = vehicleSpeed != 0 ? distance / vehicleSpeed : 777;

					// clear if time greater than 7.0 seconds
					if (time > 8.0)
					{
						ArbiterOutput.Output("DLEM: " + this.lane.ToString() + " clear, vehicle " + this.currentVehicle.ToString() +
							", speed valid: " + this.currentVehicle.StateMonitor.Observed.speedValid.ToString() +
							", speed: " + this.currentVehicle.Speed.ToString("F2") +
							", time to collision: " + time.ToString("F2") + " > 8");
						return true;
					}
					else
					{
						ArbiterOutput.Output("DLEM: " + this.lane.ToString() + " NOT clear, vehicle " + this.currentVehicle.ToString() +
							", speed valid: " + this.currentVehicle.StateMonitor.Observed.speedValid.ToString() +
							", speed: " + this.currentVehicle.Speed.ToString("F2") +
							", time to collision: " + time.ToString("F2") + " < 8");
						return false;
					}
				}
				else
				{
					ArbiterOutput.Output("Exception: distance to vehicle negative in dlem is clear: " + distance.ToString("F3"));
					return false;
				}
			}
		}

		/// <summary>
		/// Current vehicle we are tracking
		/// </summary>
		public VehicleAgent Vehicle
		{
			get { return this.currentVehicle; }
		}

		/// <summary>
		/// Polygon of exit
		/// </summary>
		private Polygon ExitPolygon()
		{
			// cast
			ArbiterWaypoint aw = (ArbiterWaypoint)this.exit;					

			// partition vector
			Coordinates dVec = aw.PreviousPartition.Vector();

			// front
			Coordinates front = aw.Position + dVec.Normalize(3.0);

			// go back tahoe vl * 1.5
			Coordinates back = front - dVec.Normalize(TahoeParams.VL * 1.5);

			// get a vector to the right of the lane
			Coordinates rVec = dVec.Normalize(Math.Max(TahoeParams.T, aw.Lane.Width / 2.0)).Rotate90();

			// make poly coords
			List<Coordinates> exitCoords = new List<Coordinates>();
			Coordinates p1 = front - rVec;
			Coordinates p2 = front + rVec;
			Coordinates p3 = back + rVec;
			Coordinates p4 = back - rVec;
			exitCoords.Add(p1);
			exitCoords.Add(p2);
			exitCoords.Add(p3);
			exitCoords.Add(p4);

			// create the exit's polygon
			Polygon exitP = Polygon.GrahamScan(exitCoords);

			// add the stopped exit
			return exitP;
		}

		/// <summary>
		/// Gets closest vehicle on lane before either the exit or where we are clsoest
		/// </summary>
		/// <param name="ourState"></param>
		public void Update(VehicleState ourState)
		{
			#region Exit Polygon

			// check the exit polygon
			if (exit != null)
			{
				// create the polygon
				Polygon p = this.exitPolygon;

				// closest vehicle
				VehicleAgent closest = null;
				double closestDist = Double.MinValue;

				// check all inside the polygon
				foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
				{
					// check if stopped
					if (va.IsStopped)// && va.StateMonitor.Observed.speedValid)
					{
						// get distance to exit
						double distToExit;
						Coordinates c = va.ClosestPointTo(this.exit.Position, ourState, out distToExit).Value;

						// check inside polygon
						if (p.IsInside(c))
						{
							// check dist
							if (closest == null || distToExit < closestDist)
							{
								closest = va;
								closestDist = distToExit;
							}
						}
					}
				}

				// check closest not null
				if (closest != null)
				{
					// check vehicle switch
					if (currentVehicle != closest)
					{
						this.ResetTiming();
					}

					// check if waiting timer not running
					if (!this.waitingTimer.IsRunning)
					{
						this.ResetTiming();
						this.waitingTimer.Start();
					}

					// set the new vehicle
					this.currentVehicle = closest;

					// return to stop update
					return;
				}
				else
				{
					// check if we were waiting for something in this area
					if (this.globalMonitor.PreviouslyWaiting.Contains(this))
					{
						// remove the previous waiting
						this.globalMonitor.PreviouslyWaiting.Remove(this);
					}
				}
			}

			#endregion

			#region Lane Vehicles

			// get the possible vehicles that could be in this stop
			if (TacticalDirector.VehicleAreas.ContainsKey(this.lane))
			{
				// get vehicles in the the lane of the exit
				List<VehicleAgent> possibleVehicles = TacticalDirector.VehicleAreas[this.lane];

				// get the point we are looking from
				LinePath.PointOnPath referencePoint;

				// reference from exit if exists
				if (exit != null)
				{
					LinePath.PointOnPath pop = lane.LanePath().GetClosestPoint(exit.Position);
					referencePoint = lane.LanePath().AdvancePoint(pop, 3.0);
				}
				// otherwise look from where we are closest
				else
					referencePoint = lane.LanePath().GetClosestPoint(ourState.Front);

				// tmp
				VehicleAgent tmp = null;
				double tmpDist = Double.MaxValue;

				// check over all possible vehicles in the lane
				foreach (VehicleAgent possible in possibleVehicles)
				{
					// get vehicle point on lane
					LinePath.PointOnPath vehiclePoint = lane.LanePath().GetClosestPoint(possible.ClosestPosition);

					// check if the vehicle is behind the reference point
					double vehicleDist = lane.LanePath().DistanceBetween(vehiclePoint, referencePoint);

					// check for closest
					if (vehicleDist > 0 && (tmp == null || vehicleDist < tmpDist))
					{
						tmp = possible;
						tmpDist = vehicleDist;
					}
				}

				// set the closest
				this.currentVehicle = tmp;
			}
			else
			{
				// set the closest
				this.currentVehicle = null;
			}

			#endregion

			// reset the timing if got this far
			this.ResetTiming();
		}

		/// <summary>
		/// resets all timers
		/// </summary>
		public void ResetTiming()
		{
			this.waitingTimer.Stop();
			this.waitingTimer.Reset();
		}

		/// <summary>
		/// Checks if represents us
		/// </summary>
		public bool IsOurs
		{
			get { return false; }
		}

		/// <summary>
		/// Waypoint this is referenced to
		/// </summary>
		public ITraversableWaypoint Waypoint
		{
			get { return this.exit; }
		}

		/// <summary>
		/// Area this is associated with
		/// </summary>
		public IVehicleArea Area
		{
			get { return this.lane; }
		}

		#endregion

		#region IDomininantMonitor Members

		/// <summary>
		/// Intersection involved input
		/// </summary>
		public IntersectionInvolved Involved
		{
			get { return this.involved; }
		}

		/// <summary>
		/// Compares itself to other dominint monitors
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareToOtherDominant(IDominantMonitor other)
		{
			if (this.involved.TurnDirection == ArbiterTurnDirection.Left &&
				this.involved.Exit != null && this.involved.Exit is ArbiterWaypoint &&
				other.Involved.Exit != null && other.Involved.Exit is ArbiterWaypoint)
			{
				ArbiterWaypoint one = (ArbiterWaypoint)this.involved.Exit;
				ArbiterWaypoint two = (ArbiterWaypoint)other.Involved.Exit;

				if (one.Lane.Way.Segment.Equals(two.Lane.Way.Segment) &&
					!one.Lane.Way.Equals(two.Lane.Way) && two.Exits != null && two.Exits.Count > 0)
				{
					foreach (ArbiterInterconnect ai in two.Exits)
					{
						if (ai.TurnDirection == ArbiterTurnDirection.Left)
							return 0;
					}
				}
			}

			if (this.involved.TurnDirection < other.Involved.TurnDirection)
				return -1;
			else if (this.involved.TurnDirection == other.Involved.TurnDirection)
				return 0;
			else
				return 1;
		}
		
		/// <summary>
		/// Determines if this car is waiting
		/// </summary>
		public bool Waiting
		{
			get
			{
				return this.Vehicle != null && this.waitingTimer.IsRunning && (this.waitingTimer.ElapsedMilliseconds / 1000.0) > 4.0;
			}
		}

		/// <summary>
		/// Checks if the waiting timer is running
		/// </summary>
		public bool WaitingTimerRunning
		{
			get
			{
				return this.waitingTimer.IsRunning;
			}
		}

		/// <summary>
		/// Checks if the vehicle is waiting excessively
		/// </summary>
		public bool ExcessiveWaiting
		{
			get
			{
				return this.Vehicle != null && this.waitingTimer.IsRunning && (this.waitingTimer.ElapsedMilliseconds / 1000.0) > 10.0;
			}
		}

		/// <summary>
		/// Checks if the vehicle is waiting excessively
		/// </summary>
		public bool LargerExcessiveWaiting
		{
			get
			{
				return this.Vehicle != null && this.waitingTimer.IsRunning && (this.waitingTimer.ElapsedMilliseconds / 1000.0) > 25.0;
			}
		}

		#endregion

		#region Equalities

		public override bool Equals(object obj)
		{
			if(obj is DominantLaneEntryMonitor)
			{
				DominantLaneEntryMonitor other = (DominantLaneEntryMonitor)obj;
				return this.Area.Equals(other.Area);
			}
			else
				return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			string s = this.Vehicle != null ? this.Vehicle.ToString() + ", Speed: " + this.Vehicle.StateMonitor.Observed.speed.ToString("f2") : "None";
			return "DLEM: " + Area.ToString() + ", " + s + ", Waiting: " + this.Waiting.ToString() + ", Excessive Wait: " + this.ExcessiveWaiting.ToString() + ", Larger Excessive Wait: " + this.LargerExcessiveWaiting.ToString(); 
		}

		#endregion

		#region IDominantMonitor Members

		public int SecondsWaiting
		{
			get { return (int)(this.waitingTimer.ElapsedMilliseconds / 1000.0); }
		}

		#endregion
	}
}
