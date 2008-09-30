using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using System.Diagnostics;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors
{
	/// <summary>
	/// Monitors where a zone interesects an intersection to see if we can go
	/// </summary>
	public class DominantZoneEntryMonitor : IDominantMonitor, IEntryAreaMonitor
	{
		/// <summary>
		/// Polygon monitoring this entry
		/// </summary>
		private Polygon entryPolygon; 

		/// <summary>
		/// Waypoint we are monitoring
		/// </summary>
		private ArbiterPerimeterWaypoint finalWaypoint;

		/// <summary>
		/// monitors time the vehicle stopped
		/// </summary>
		private Stopwatch failedTimer;

		/// <summary>
		/// Vehicle we are trackign in this entry
		/// </summary>
		private VehicleAgent currentVehicle;

		/// <summary>
		/// Fkag uf this represents our vehicle
		/// </summary>
		private bool isOurs;

		/// <summary>
		/// Global monitor of the intersection
		/// </summary>
		private IntersectionMonitor globalMonitor;

		/// <summary>
		/// Intersection involved
		/// </summary>
		private IntersectionInvolved involved;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="turnFinal"></param>
		public DominantZoneEntryMonitor(ArbiterPerimeterWaypoint turnFinal, ArbiterInterconnect ai, bool isOurs, 
			IntersectionMonitor globalMonitor, IntersectionInvolved involved)
		{
			this.finalWaypoint = turnFinal;
			this.entryPolygon = this.GenerateEntryMonitorPolygon(ai);
			this.failedTimer = new Stopwatch();
			this.isOurs = isOurs;
			this.globalMonitor = globalMonitor;
			this.involved = involved;
		}

		/// <summary>
		/// Makes polygon representing the entry
		/// </summary>
		public Polygon GenerateEntryMonitorPolygon(ArbiterInterconnect ai)
		{
			Coordinates aiVector = ai.FinalGeneric.Position - ai.InitialGeneric.Position;
			Coordinates aiEntry = this.finalWaypoint.Position + aiVector.Normalize(TahoeParams.VL * 1.5);

			Coordinates centerVector = this.finalWaypoint.Perimeter.PerimeterPolygon.CalculateBoundingCircle().center - this.finalWaypoint.Position;
			Coordinates centerEntry = this.finalWaypoint.Position + centerVector.Normalize(TahoeParams.VL * 1.5);

			List<Coordinates> boundingCoords = new List<Coordinates>(new Coordinates[] { aiEntry, centerEntry, finalWaypoint.Position });
			return Polygon.GrahamScan(boundingCoords).Inflate(2.0 * TahoeParams.T);
		}

		#region IEntryAreaMonitor Members

		/// <summary>
		/// Determines if vehicles are stopped for long time in polygon
		/// </summary>
		public bool Failed
		{
			get { return this.failedTimer.IsRunning && (failedTimer.ElapsedMilliseconds / 1000.0) > 15.0; }
		}

		#endregion

		#region IIntersectionQueueable Members

		public bool IsCompletelyClear(VehicleState ourState)
		{
			return this.Clear(ourState);
		}

		public bool Clear( VehicleState ourState)
		{
			return this.currentVehicle == null;
		}

		public VehicleAgent Vehicle
		{
			get { return this.currentVehicle; }
		}

		public void Update(VehicleState ourState)
		{
			if (TacticalDirector.VehicleAreas.ContainsKey(this.finalWaypoint.Perimeter.Zone))
			{
				VehicleAgent closest = null;
				double tmpDist = Double.MaxValue;

				foreach (VehicleAgent va in TacticalDirector.VehicleAreas[this.finalWaypoint.Perimeter.Zone])
				{
					if (this.entryPolygon.IsInside(va.ClosestPosition))
					{
						double tmp = va.ClosestPosition.DistanceTo(this.finalWaypoint.Position);
						if (closest == null || tmp < tmpDist)
						{
							tmpDist = tmp;
							closest = va;
						}
					}
				}

				if (closest != null && closest.IsStopped)
				{
					if (this.currentVehicle == null || !this.currentVehicle.Equals(closest))
					{
						this.currentVehicle = closest;
						this.ResetTiming();
						this.failedTimer.Start();
					}
					else
					{
						this.currentVehicle = closest;
					}
				}
				else
				{
					this.currentVehicle = closest;
					this.ResetTiming();
				}
			}
			else
			{
				this.currentVehicle = null;
				this.ResetTiming();
			}
		}

		public void ResetTiming()
		{
			this.failedTimer.Stop();
			this.failedTimer.Reset();
		}

		public bool IsOurs
		{
			get { return this.isOurs; }
		}

		public UrbanChallenge.Arbiter.ArbiterRoads.ITraversableWaypoint Waypoint
		{
			get { return this.finalWaypoint; }
		}

		public UrbanChallenge.Arbiter.ArbiterRoads.IVehicleArea Area
		{
			get { return this.finalWaypoint.Perimeter.Zone; }
		}

		#endregion

		#region IDominantMonitor Members

		public IntersectionInvolved Involved
		{
			get { return this.involved; }
		}

		public int CompareToOtherDominant(IDominantMonitor other)
		{
			return this.involved.TurnDirection.CompareTo(other.Involved.TurnDirection);
		}

		public bool Waiting
		{
			get { return this.Failed; }
		}

		public bool WaitingTimerRunning
		{
			get { return this.failedTimer.IsRunning; }
		}

		#endregion

		#region IDominantMonitor Members

		public bool ExcessiveWaiting
		{
			get { return this.failedTimer.IsRunning && (failedTimer.ElapsedMilliseconds / 1000.0) > 6.0; }
		}

		public int SecondsWaiting
		{
			get { return (int)(failedTimer.ElapsedMilliseconds / 1000.0); }
		}

		#endregion
	}
}
