using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Sensors.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors;
using System.Diagnostics;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Reasoning;
using UrbanChallenge.Arbiter.Core.Communications;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection
{
	/// <summary>
	/// Monitors an intersection situation
	/// </summary>
	public class IntersectionMonitor
	{
		/// <summary>
		/// Base intersection
		/// </summary>
		public ArbiterIntersection Intersection;

		/// <summary>
		/// Our monitor
		/// </summary>
		public IIntersectionQueueable OurMonitor;

		/// <summary>
		/// Monitors of the various parts of the intersection
		/// </summary>
		public List<IIntersectionQueueable> PriorityMonitors;

		/// <summary>
		/// Priority queue representing hte internal state of the intersection
		/// </summary>
		public List<IIntersectionQueueable> IntersectionPriorityQueue;

		/// <summary>
		/// Monitors vehicles inside the intersection but not piked up by other monitors
		/// </summary>
		public List<IntersectionVehicleMonitor> InsideIntersectionVehicles;

		/// <summary>
		/// Areas overlapping the interconnect that have priority over ourselves
		/// </summary>
		public List<IDominantMonitor> NonStopPriorityAreas;

		/// <summary>
		/// Areas previously waiting for us (interconnect exit with same priority as we have)
		/// </summary>
		public List<IDominantMonitor> PreviouslyWaiting;

		/// <summary>
		/// Monitors the entry area of the turn
		/// </summary>
		public IEntryAreaMonitor EntryAreaMonitor;

		/// <summary>
		/// ALl minotors of the intersection
		/// </summary>
		public List<IIntersectionQueueable> AllMonitors;

		/// <summary>
		/// Timer monitoring cooldown
		/// </summary>
		public Stopwatch cooldownTimer;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ourExit"></param>
		public IntersectionMonitor(ITraversableWaypoint ourExit, ArbiterIntersection ai, VehicleState ourState, IConnectAreaWaypoints desired)
		{
			// set intersection
			this.Intersection = ai;

			// create monitors
			this.AllMonitors = new List<IIntersectionQueueable>();
			this.PriorityMonitors = new List<IIntersectionQueueable>();
			this.IntersectionPriorityQueue = new List<IIntersectionQueueable>();
			this.InsideIntersectionVehicles = new List<IntersectionVehicleMonitor>();
			this.NonStopPriorityAreas = new List<IDominantMonitor>();
			this.PreviouslyWaiting = new List<IDominantMonitor>();
			this.AllMonitors = new List<IIntersectionQueueable>();
			this.cooldownTimer = new Stopwatch();

			#region Stopped Exits

			// get ours
			IIntersectionQueueable ourMonitor = null;

			// get stopped exits
			foreach(ArbiterStoppedExit ase in ai.StoppedExits)
			{
				// add a monitor
				SubmissiveEntryMonitor sem = new SubmissiveEntryMonitor(this, ase.Waypoint, ase.Waypoint.Equals(ourExit));

				// add to monitors
				PriorityMonitors.Add(sem);

				// check not our
				if(!sem.IsOurs)
				{
					// initial update
					sem.Update(ourState);

					// check if add first
					if(!sem.Clear(ourState))
						IntersectionPriorityQueue.Add(sem);
				}
				else
				{
					// set ours
					ourMonitor = sem;
					this.OurMonitor = ourMonitor;
				}
			}

			// check if our monitor exists
			if (ourMonitor != null)
			{
				// add ours
				this.IntersectionPriorityQueue.Add(ourMonitor);
			}

			#endregion

			#region Priority Areas Over Interconnect

			// check contains priority lane for this
			if (ai.PriorityLanes.ContainsKey(desired.ToInterconnect))
			{
				// loop through all other priority monitors over this interconnect
				foreach (IntersectionInvolved ii in ai.PriorityLanes[desired.ToInterconnect])
				{
					// check area type if lane
					if (ii.Area is ArbiterLane)
					{
						// add lane to non stop priority areas
						this.NonStopPriorityAreas.Add(new DominantLaneEntryMonitor(this, ii));
					}
					// otherwise is zone
					else if (ii.Area is ArbiterZone)
					{
						// add lane to non stop priority areas
						this.NonStopPriorityAreas.Add(
							new DominantZoneEntryMonitor((ArbiterPerimeterWaypoint)ii.Exit, ((ArbiterInterconnect)desired), false, this, ii));
					}
					else
					{
						throw new ArgumentException("unknown intersection involved area", "ii");
					}
				}
			}
			// otherwise be like, wtf?
			else
			{
				ArbiterOutput.Output("Exception: intersection: " + this.Intersection.ToString() + " priority lanes does not contain interconnect: " + desired.ToString() + " returning can go");
				//ArbiterOutput.Output("Error in intersection monitor!!!!!!!!!@Q!!, desired interconnect: " + desired.ToInterconnect.ToString() + " not found in intersection: " + ai.ToString() + ", not setting any dominant monitors");
			}

			#endregion

			#region Entry Area

			if (desired.FinalGeneric is ArbiterWaypoint)
			{
				this.EntryAreaMonitor = new LaneEntryAreaMonitor((ArbiterWaypoint)desired.FinalGeneric);
			}
			else if (desired.FinalGeneric is ArbiterPerimeterWaypoint)
			{
				this.EntryAreaMonitor = new ZoneAreaEntryMonitor((ArbiterPerimeterWaypoint)desired.FinalGeneric, (ArbiterInterconnect)desired,
					false, this, new IntersectionInvolved(ourExit, ((ArbiterPerimeterWaypoint)desired.FinalGeneric).Perimeter.Zone, 
					((ArbiterInterconnect)desired).TurnDirection));
			}
			else
			{
				throw new Exception("unhandled desired interconnect final type");
			}

			#endregion

			#region Our Monitor

			// check ours
			if (ourExit is ArbiterWaypoint)
			{
				this.OurMonitor = new DominantLaneEntryMonitor(this,
					new IntersectionInvolved(ourExit, ((ArbiterWaypoint)ourExit).Lane,
					desired is ArbiterInterconnect ? ((ArbiterInterconnect)desired).TurnDirection : ArbiterTurnDirection.Straight));
			}
			else if (ourExit is ArbiterPerimeterWaypoint)
			{
				// add lane to non stop priority areas
				this.OurMonitor = 
					new DominantZoneEntryMonitor((ArbiterPerimeterWaypoint)desired.InitialGeneric, ((ArbiterInterconnect)desired), true, this,
					new IntersectionInvolved(ourExit, ((ArbiterPerimeterWaypoint)desired.InitialGeneric).Perimeter.Zone,
					((ArbiterInterconnect)desired).TurnDirection));
			}

			#endregion

			// add to all
			this.AllMonitors.AddRange(this.PriorityMonitors);
			foreach (IIntersectionQueueable iiq in this.NonStopPriorityAreas)
				this.AllMonitors.Add(iiq);
			this.AllMonitors.Add(this.EntryAreaMonitor);

			// update all
			this.Update(ourState);

			// check if we need to populate previously waiting
			if(this.OurMonitor is IDominantMonitor)
			{
				// cast our
				IDominantMonitor ours = (IDominantMonitor)this.OurMonitor;

				// loop through to determine those previously waiting
				foreach (IDominantMonitor idm in this.NonStopPriorityAreas)
				{
					if(idm.Waypoint != null && !idm.Waypoint.IsStop && idm.WaitingTimerRunning)
						this.PreviouslyWaiting.Add(idm);
				}
			}
		}

		/// <summary>
		/// Updates the monitors
		/// </summary>
		/// <param name="ourExit"></param>
		public void Update(VehicleState ourState)
		{
			#region Intersection Priority Queue Update

			// new queue
			List<IIntersectionQueueable> updated = new List<IIntersectionQueueable>();

			// update all monitors
			foreach (IIntersectionQueueable iiq in this.PriorityMonitors)
			{
				iiq.Update(ourState);				
			}

			// update old queue			
			for (int i = 0; i < this.IntersectionPriorityQueue.Count; i++)
			{
				if (!this.IntersectionPriorityQueue[i].Clear(ourState))
					updated.Add(this.IntersectionPriorityQueue[i]);
			}

			// add in new elts
			foreach (IIntersectionQueueable monitor in this.PriorityMonitors)
			{
				if (monitor is SubmissiveEntryMonitor && !monitor.Clear(ourState) && !updated.Contains(monitor))
					updated.Add(monitor);
			}

			// set
			this.IntersectionPriorityQueue = updated;	

			#endregion

			#region Non Stop Priority Areas

			// loop through non-stops
			foreach (IIntersectionQueueable iiq in NonStopPriorityAreas)
			{
				// update each
				iiq.Update(ourState);
			}

			#endregion

			#region Entry Area

			this.EntryAreaMonitor.Update(ourState);

			#endregion

			#region Inside Intersection Vehicles

			// now we get the polygon of the intersection
			Polygon intersectionPolygon = this.Intersection.IntersectionPolygon;

			// updated list
			List<IntersectionVehicleMonitor> updatedIntersectionVehicles = new List<IntersectionVehicleMonitor>();

			// update all old and add remaining
			foreach (IntersectionVehicleMonitor ivm in this.InsideIntersectionVehicles)
			{
				// uypdate
				ivm.Update();

				if (!ivm.ShouldDelete())
				{
					// make sure not part of other monitors
					bool containedInOther = false;
					foreach (IIntersectionQueueable monitor in this.AllMonitors)
					{
						if (!(monitor is IntersectionVehicleMonitor) && monitor.Vehicle != null && monitor.Vehicle.Equals(ivm.Vehicle))
							containedInOther = true;
					}

					if (!containedInOther)
					{
						updatedIntersectionVehicles.Add(ivm);
					}
				}
			}

			// check over all vehicles not in intersection
			foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
			{
				// check birth and if all inside polygon
				if (va.PassedDelayedBirth && intersectionPolygon.IsInside(va.GetAbsolutePolygon(ourState)))
				{
					// make sure not part of other monitors
					bool containedInOther = false;
					foreach (IIntersectionQueueable monitor in this.AllMonitors)
					{
						if (monitor.Vehicle != null && monitor.Vehicle.Equals(va))
							containedInOther = true;
					}

					// make sure not already montirored
					if (!containedInOther)
					{
						// make sure not already specced
						bool specced = false;
						foreach (IntersectionVehicleMonitor ivm in updatedIntersectionVehicles)
						{
							if (ivm.Vehicle.Equals(va))
								specced = true;
						}

						// add
						if (!specced)
						{
							IntersectionVehicleMonitor tmp = new IntersectionVehicleMonitor(this, va);
							tmp.Update();

							if(!tmp.ShouldDelete())
								updatedIntersectionVehicles.Add(tmp);
						}
					}
				}
			}

			// set vehicle
			this.InsideIntersectionVehicles = updatedIntersectionVehicles;

			#endregion			
		}

		/// <summary>
		/// Reset timers of all
		/// </summary>
		public void ResetTimers()
		{
			foreach (IIntersectionQueueable iiq in this.PriorityMonitors)
				iiq.ResetTiming();

			foreach (IntersectionVehicleMonitor ivm in this.InsideIntersectionVehicles)
				ivm.ResetTimer();

			foreach (IIntersectionQueueable iiq in this.NonStopPriorityAreas)
				iiq.ResetTiming();
		}

		/// <summary>
		/// Checks if it is available for us to traverse the interconnect passed in
		/// </summary>
		/// <param name="ai"></param>
		/// <returns></returns>
		public bool CanTraverse(IConnectAreaWaypoints ai, VehicleState ourState)
		{
			// if we can go
			bool? canGo = null;

			// check for bug where interconnect not in intersection
			if (!this.Intersection.PriorityLanes.ContainsKey(ai.ToInterconnect))
			{
				ArbiterOutput.Output("Exception: intersection: " + this.Intersection.ToString() + " priority lanes does not contain interconnect: " + ai.ToString() + " returning can go");
				//ArbiterOutput.Output("Exception: intersection: " + this.Intersection.ToString() + " priority lanes does not contain interconnect: " + ai.ToString() + " returning can go");
				//return true;
			}

			// check entry clear
			if (!this.EntryAreaMonitor.Clear(ourState))
				canGo = false;

			// Check if we have priority if we're a stop
			if (ai.InitialGeneric is ArbiterWaypoint && ((ArbiterWaypoint)ai.InitialGeneric).IsStop && IntersectionPriorityQueue[0].IsOurs)
			{
				// check if intersection clear at the moment
				foreach (IntersectionVehicleMonitor ivm in this.InsideIntersectionVehicles)
				{
					if (!ivm.Failed(true))
						canGo = false;
				}

				// check all priorities over us if clear
				foreach (IIntersectionQueueable iiq in this.NonStopPriorityAreas)
				{
					if (!iiq.Clear(ourState))
						canGo = false;
				}

				// clear
				if(!canGo.HasValue)
					canGo = true;
			}
			// check if not a stop if clear
			else if (ai.InitialGeneric is ITraversableWaypoint && !((ITraversableWaypoint)ai.InitialGeneric).IsStop)
			{
				// get our monitor
				IDominantMonitor ours = (IDominantMonitor)this.OurMonitor;

				// check if intersection clear at the moment
				foreach (IntersectionVehicleMonitor ivm in this.InsideIntersectionVehicles)
				{
					if (!ivm.Failed(true))
						canGo = false;
				}

				// check all priorities over us if clear
				foreach (IDominantMonitor iiq in this.NonStopPriorityAreas)
				{
					// checks if clear by default
					bool isClear = iiq.Clear(ourState);

					if (!isClear && !iiq.Waiting)
						canGo = false;

					if (iiq.Waiting)
					{
						ArbiterOutput.Output("DLEM: " + iiq.Area.ToString() + " vehicle waiting, hence clear");
					}
				}

				// quick check 
				if (canGo.HasValue && this.PreviouslyWaiting.Count > 0)
					canGo = false;
				else
				{
					// check if previously waiting doesn't exist
					foreach (IDominantMonitor idm in this.PreviouslyWaiting)
					{
						// check if waiting for this person excessively
						if (!idm.ExcessiveWaiting)
							canGo = false;
					}
				}

				// clear
				if (!canGo.HasValue)
					canGo = true;
			}
			
			// nope, can't go if fall through
			if (!canGo.HasValue)
				canGo = false;

			#region Cooldown

			// final boolean if we can go
			bool finalGo = canGo.Value;

			if (finalGo)
			{
				ArbiterOutput.Output("Can Traverse, Cooling Down");

				if(cooldownTimer.IsRunning)
				{
					if((cooldownTimer.ElapsedMilliseconds / 1000.0) > 1.0)
						return true;
					else
						return false;
				}
				else
				{
					cooldownTimer.Reset();
					cooldownTimer.Start();
					return false;
				}
			}
			else
			{
				if(cooldownTimer.IsRunning)
				{
					cooldownTimer.Stop();
					cooldownTimer.Reset();					
				}

				return false;
			}

			#endregion
		}

		/// <summary>
		/// Checks if everything is clear (stops and priorities over the interconnect) so
		/// that we don't have to stop
		/// </summary>
		/// <param name="ai"></param>
		/// <returns></returns>
		public bool IsAllClear(IConnectAreaWaypoints ai, VehicleState ourState)
		{
			// check if intersection clear at the moment
			if (this.InsideIntersectionVehicles.Count > 0)
				return false;

			// check all monitors
			foreach (IIntersectionQueueable iiq in this.AllMonitors)
			{
				if (!iiq.IsCompletelyClear(ourState))
					return false;
			}

			// clear
			return true;
		}

		/// <summary>
		/// State string of the intersection
		/// </summary>
		/// <returns></returns>
		public string IntersectionStateString()
		{
			string s = "\n";
			
			s = s + "Intersection Queue: ";
			for (int i = 0; i < this.IntersectionPriorityQueue.Count; i++)
				s = s + (i+1) + ": " + this.IntersectionPriorityQueue[i].ToString() + "; ";

			s = s + "\n";

			s = s + "Inside Intersection Vehicles: ";
			foreach (IntersectionVehicleMonitor ivm in this.InsideIntersectionVehicles)
				s = s + ivm.ToString();

			s = s + "\n";

			s = s + "Non Stop Priority Area: ";
			foreach (IDominantMonitor iiq in this.NonStopPriorityAreas)
			{
				s = s + iiq.ToString() + "; ";
			}

			s = s + "\n";

			s = s + "Previously Waiting: ";
			foreach (IDominantMonitor idm in this.PreviouslyWaiting)
			{
				s = s + idm.ToString() + "; ";
			}

			s = s + "\n";

			string tmpEAM = this.EntryAreaMonitor.Vehicle == null ? "None" : this.EntryAreaMonitor.Vehicle.ToString() + ", Failed: " + this.EntryAreaMonitor.Failed;
			s = s + "Entry Area Monitor: " + tmpEAM;
			
			return s;
		}

		/// <summary>
		/// Checks if intersection contains entry ownstream in lane from waypoint
		/// </summary>
		/// <param name="aw"></param>
		/// <returns></returns>
		public ArbiterWaypoint EntryDownstream(ArbiterWaypoint aw)
		{
			foreach (ITraversableWaypoint itw in this.Intersection.AllEntries.Values)
			{
				if (itw is ArbiterWaypoint)
				{
					ArbiterWaypoint testWaypoint = (ArbiterWaypoint)itw;
					if (aw.Lane.Equals(testWaypoint.Lane) && aw.WaypointId.Number < testWaypoint.WaypointId.Number)
					{
						return testWaypoint;
					}
				}
			}

			return null;
		}

		#region Old

		/// <summary>
		/// Pupulates all the priority monitors given the information we know right at this moment
		/// </summary>
		public void PopulateMonitor(IArbiterWaypoint ourExit)
		{
			// 1. Generate dependencies for each lane

			// 2. Assign stopped vehicles at stops to stop lanes

			// 3. Given dependencies populate the stop queues for each stopped vehicle

			// 4. Assign stopped vehicles close to exits in priority lanes to their priority monitor

			// 5. given our exit assign our dependencies

		}

		/// <summary>
		/// Runs an inference cycle over the intersection incrementing the timers by dt
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="ai">interconnect we are looking at proceeding through</param>
		public void Update(double dt, ObservedVehicle[] vehicles)
		{
			// 1. Assign or remove vehicles given new sensor data

			// 2. Priority lanes we get closest vehicle to intersection exit of that priority lane and determine if we are monitoring that

			// 3. For all priority montiors, update time waiting by dt

			// 4. determine state of intersection and which exits are blocked, update failure state of interconnects that are blocked to be feasible

			// 4. Determine for all if should go or not

			// 5. For all that should go and are not moving increase failure time by dt (not that our exit or lane never fails)
		}

		#endregion

		/// <summary>
		/// Resets the interconnect we wish to take
		/// </summary>
		/// <param name="reset"></param>
		public void ResetDesired(ArbiterInterconnect desired)
		{
			// create monitors
			this.AllMonitors = this.IntersectionPriorityQueue;
			this.NonStopPriorityAreas = new List<IDominantMonitor>();
			this.PreviouslyWaiting = new List<IDominantMonitor>();
			this.cooldownTimer = new Stopwatch();

			#region Priority Areas Over Interconnect

			// check contains priority lane for this
			if (this.Intersection.PriorityLanes.ContainsKey(desired.ToInterconnect))
			{
				// loop through all other priority monitors over this interconnect
				foreach (IntersectionInvolved ii in this.Intersection.PriorityLanes[desired.ToInterconnect])
				{
					// check area type if lane
					if (ii.Area is ArbiterLane)
					{
						// add lane to non stop priority areas
						this.NonStopPriorityAreas.Add(new DominantLaneEntryMonitor(this, ii));
					}
					// otherwise is zone
					else if (ii.Area is ArbiterZone)
					{
						// add lane to non stop priority areas
						this.NonStopPriorityAreas.Add(
							new DominantZoneEntryMonitor((ArbiterPerimeterWaypoint)ii.Exit, ((ArbiterInterconnect)desired), false, this, ii));
					}
					else
					{
						throw new ArgumentException("unknown intersection involved area", "ii");
					}
				}
			}
			// otherwise be like, wtf?
			else
			{
				ArbiterOutput.Output("Exception: intersection: " + this.Intersection.ToString() + " priority lanes does not contain interconnect: " + desired.ToString() + " returning can go");
				//ArbiterOutput.Output("Error in intersection monitor!!!!!!!!!@Q!!, desired interconnect: " + desired.ToInterconnect.ToString() + " not found in intersection: " + ai.ToString() + ", not setting any dominant monitors");
			}

			#endregion

			#region Entry Area

			if (desired.FinalGeneric is ArbiterWaypoint)
			{
				this.EntryAreaMonitor = new LaneEntryAreaMonitor((ArbiterWaypoint)desired.FinalGeneric);
			}
			else if (desired.FinalGeneric is ArbiterPerimeterWaypoint)
			{
				this.EntryAreaMonitor = new ZoneAreaEntryMonitor((ArbiterPerimeterWaypoint)desired.FinalGeneric, (ArbiterInterconnect)desired,
					false, this, new IntersectionInvolved(this.OurMonitor.Waypoint, ((ArbiterPerimeterWaypoint)desired.FinalGeneric).Perimeter.Zone,
					((ArbiterInterconnect)desired).TurnDirection));
			}
			else
			{
				throw new Exception("unhandled desired interconnect final type");
			}

			#endregion

			// add to all
			foreach (IIntersectionQueueable iiq in this.NonStopPriorityAreas)
				this.AllMonitors.Add(iiq);
			this.AllMonitors.Add(this.EntryAreaMonitor);

			// update all
			this.Update(CoreCommon.Communications.GetVehicleState());
		}
	}
}
