using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using System.Diagnostics;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.Communications;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	/// <summary>
	/// Designates a monitor of a stop sign coming into the intersection
	/// </summary>
	public class SubmissiveEntryMonitor : IIntersectionQueueable, IComparable
	{
		#region Submissive Entry Monitor Members

		/// <summary>
		/// Global monitor of the intersection
		/// </summary>
		private IntersectionMonitor globalMonitor;

		/// <summary>
		/// Stop sign this monitor is associated with
		/// </summary>
		private ArbiterStoppedExit stoppedExit;

		/// <summary>
		/// The current vehicle associated with the stop
		/// </summary>
		private VehicleAgent currentVehicle;

		/// <summary>
		/// Current timer
		/// </summary>
		private Stopwatch timer;

		/// <summary>
		/// Flag if the monitor is filled with our vehicle
		/// </summary>
		private bool isOurs;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="globalMonitor"></param>
		/// <param name="stop"></param>
		public SubmissiveEntryMonitor(IntersectionMonitor globalMonitor, ArbiterWaypoint stop, bool isOurs)
		{
			this.globalMonitor = globalMonitor;			
			this.timer = new Stopwatch();

			this.isOurs = isOurs;

			foreach (ArbiterStoppedExit ase in this.globalMonitor.Intersection.StoppedExits)
			{
				if (ase.Waypoint.Equals(stop))
				{
					this.stoppedExit = ase;
				}
			}
			
			if (this.stoppedExit == null)
				throw new Exception("needs stopped exit");
		}

		#endregion

		#region IIntersectionQueueable Members

		/// <summary>
		/// Checks if this area is completely clear of vehicles and no chance of one coming in soon
		/// </summary>
		/// <param name="ourState"></param>
		/// <returns></returns>
		public bool IsCompletelyClear(VehicleState ourState)
		{
			// amke sure not ours
			if (!isOurs)
			{
				// get the possible vehicles that could be in this stop
				if (TacticalDirector.VehicleAreas.ContainsKey(this.stoppedExit.Waypoint.Lane))
				{
					// get vehicles in the the lane of the exit
					List<VehicleAgent> possibleVehicles = TacticalDirector.VehicleAreas[this.stoppedExit.Waypoint.Lane];

					// inflate the polygon
					Polygon exitPolygon = this.stoppedExit.ExitPolygon.Inflate(TahoeParams.VL);

					// check vehicles in lane
					foreach (VehicleAgent va in possibleVehicles)
					{
						// check if vehicle inside inflated exit polygon
						if (exitPolygon.IsInside(va.ClosestPosition))
						{
							return false;
						}
					}					
				}

				// clear if made it this far
				return true;
			}
			else
			{
				// ours is always clear relative to us
				return true;
			}
		}

		/// <summary>
		/// Waypoint this stopped exit represents
		/// </summary>
		public ITraversableWaypoint Waypoint
		{
			get { return this.stoppedExit.Waypoint; }
		}

		/// <summary>
		/// If this is clear or not at this current timestep
		/// </summary>
		public bool Clear(VehicleState ourState)
		{
			if (!isOurs)
				return this.currentVehicle == null;
			else
				return false;
		}

		/// <summary>
		/// If we did not find a vehicle  on the previous timestep
		/// </summary>
		public int NotFoundPrevious = 10;
		private int notFoundDefault = 10;

		/// <summary>
		/// Current vehicle tracked in this monitor
		/// </summary>
		public VehicleAgent Vehicle
		{
			get { return this.currentVehicle; }
		}

		/// <summary>
		/// Updates this monitor
		/// </summary>
		public void Update(VehicleState ourState)
		{
			// make sure not our vehicle
			if (!isOurs)
			{
				// get a list of vehicles possibly in the exit
				List<VehicleAgent> stopVehicles = new List<VehicleAgent>();

				// check all valid vehicles
				foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
				{	
					// check if any of the point inside the polygon
					for (int i = 0; i < va.StateMonitor.Observed.relativePoints.Length; i++)
					{
						// get abs coord
						Coordinates c = va.TransformCoordAbs(va.StateMonitor.Observed.relativePoints[i], ourState);

						// check if inside poly
						if (this.stoppedExit.ExitPolygon.IsInside(c) && !stopVehicles.Contains(va))
						{
							// add vehicle
							stopVehicles.Add(va);
						}
					}
				}

				// check occluded
				foreach (VehicleAgent va in TacticalDirector.OccludedVehicles.Values)
				{
					// check if any of the point inside the polygon
					for (int i = 0; i < va.StateMonitor.Observed.relativePoints.Length; i++)
					{
						// get abs coord
						Coordinates c = va.TransformCoordAbs(va.StateMonitor.Observed.relativePoints[i], ourState);

						// check if inside poly
						if (this.stoppedExit.ExitPolygon.IsInside(c) && !stopVehicles.Contains(va))
						{
							// add vehicle
							ArbiterOutput.Output("Added occluded vehicle: " + va.ToString() + " to SEM: " + this.stoppedExit.ToString());
							stopVehicles.Add(va);
						}
					}
				}

				// check if we already have a vehicle we are referencing
				if (this.currentVehicle != null && !stopVehicles.Contains(this.currentVehicle))
				{
					// get the polygon of the current vehicle	and inflate a tiny bit?
					Polygon p = this.currentVehicle.GetAbsolutePolygon(ourState);
					p = p.Inflate(1.0);

					// Vehicle agent to switch to if exists
					VehicleAgent newer = null;
					double distance = Double.MaxValue;

					// check for closest vehicle to exist in that polygon
					foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
					{						
						// check if vehicle is stopped
						if (stopVehicles.Contains(va))
						{
							// get distance to stop
							double stopLineDist;
							va.ClosestPointTo(this.stoppedExit.Waypoint.Position, ourState, out stopLineDist);

							// check all vehicle points
							foreach (Coordinates rel in va.StateMonitor.Observed.relativePoints)
							{
								Coordinates abs = va.TransformCoordAbs(rel, ourState);
								double tmpDist = stopLineDist;

								// check for the clsoest with points inside vehicle
								if (p.IsInside(abs) && (newer == null || tmpDist < distance))
								{
									newer = va;
									distance = tmpDist;
								}
							}
						}
					}

					// check if we can switch vehicles
					if (newer != null)
					{
						ArbiterOutput.Output("For StopLine: " + this.stoppedExit.ToString() + " switched vehicleId: " + this.currentVehicle.VehicleId.ToString() + " for vehicleId: " + newer.VehicleId.ToString());
						this.currentVehicle = newer;
						this.NotFoundPrevious = notFoundDefault;
					}
					else if(NotFoundPrevious == 0)
					{
						// set this as not having a vehicle
						this.currentVehicle = null;

						// set not found before
						this.NotFoundPrevious = this.notFoundDefault;

						// stop timers
						this.timer.Stop();
						this.timer.Reset();
					}
					else
					{
						// set not found before
						this.NotFoundPrevious--;
						ArbiterOutput.Output("current not found, not found previous: " + this.NotFoundPrevious.ToString() + ", at stopped exit: " + this.stoppedExit.Waypoint.ToString()); 
					}
				}
				// update the current vehicle
				else if (this.currentVehicle != null && stopVehicles.Contains(this.currentVehicle))
				{
					// udpate current
					this.currentVehicle = stopVehicles[stopVehicles.IndexOf(this.currentVehicle)];

					// set as found previous
					this.NotFoundPrevious = this.notFoundDefault;

					// check if need to update timer
					if (this.currentVehicle.IsStopped && !this.timer.IsRunning)
					{
						this.timer.Stop();
						this.timer.Reset();
						this.timer.Start();
					}
					// otherwise check if moving
					else if (!this.currentVehicle.IsStopped && this.timer.IsRunning)
					{
						this.timer.Stop();
						this.timer.Reset();
					}
				}
				// otherwise if we have no vehicle and exist vehicles in stop area
				else if (this.currentVehicle == null && stopVehicles.Count > 0)
				{
					// get closest vehicle to the stop
					VehicleAgent toTrack = null;
					double distance = Double.MaxValue;

					// set as found previous
					this.NotFoundPrevious = this.notFoundDefault;

					// loop through
					foreach (VehicleAgent va in stopVehicles)
					{						
						// check if vehicle is stopped
						if (va.IsStopped)
						{
							// distance of vehicle to stop line
							double distanceToStop;
							Coordinates closestToStop = va.ClosestPointTo(this.stoppedExit.Waypoint.Position, ourState, out distanceToStop).Value;

							// check if we don't have one or tmp closer than tracked
							if (toTrack == null || distanceToStop < distance)
							{
								toTrack = va;
								distance = distanceToStop;
							}
						}
					}

					// check if we have one to track
					if (toTrack != null)
						this.currentVehicle = toTrack;
				}
			}
		}

		/// <summary>
		/// Resets the timers of this monitor
		/// </summary>
		public void ResetTiming()
		{
			this.timer.Stop();
			this.timer.Reset();
			this.NotFoundPrevious = notFoundDefault;
		}

		/// <summary>
		/// Our vehicle or not
		/// </summary>
		public bool IsOurs
		{
			get { return this.isOurs; }
		}

		/// <summary>
		/// Area this is associated with
		/// </summary>
		public IVehicleArea Area
		{
			get { return this.stoppedExit.Waypoint.Lane; }
		}

		#endregion

		#region Standard Equalities

		public override bool Equals(object obj)
		{
			if (obj is SubmissiveEntryMonitor)
			{
				SubmissiveEntryMonitor other = (SubmissiveEntryMonitor)obj;

				if (other.stoppedExit.Waypoint.Equals(this.stoppedExit.Waypoint))
					return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			if (!isOurs)
			{
				string clearString = this.Vehicle == null ? "Clear" : "Filled: " + this.currentVehicle.VehicleId.ToString();
				return this.stoppedExit.Waypoint.ToString() + ", " + clearString;
			}
			else
			{
				return this.stoppedExit.Waypoint.ToString() + ", Ours";
			}
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj is SubmissiveEntryMonitor)
			{
				SubmissiveEntryMonitor other = (SubmissiveEntryMonitor)obj;

				bool containsThis = this.globalMonitor.IntersectionPriorityQueue.Contains(this);
				bool containsOther = this.globalMonitor.IntersectionPriorityQueue.Contains(other);

				if (containsThis && containsOther)
				{
					int indexThis = this.globalMonitor.IntersectionPriorityQueue.IndexOf(this);
					int indexOther = this.globalMonitor.IntersectionPriorityQueue.IndexOf(other);

					return indexThis < indexOther ? -1 : 1;
				}
				else if (containsThis && !containsOther)
				{
					return -1;
				}
				else if (!containsThis && containsOther)
				{
					return 1;
				}
				else
				{
					if (this.IsOurs)
						return 1;
					else if (other.IsOurs)
						return -1;
					else
						return 0;
				}
			}
			else if (obj is DominantLaneEntryMonitor || obj is DominantZoneEntryMonitor)
				return 1;
			else
				return -1;

		}

		#endregion
	}
}
