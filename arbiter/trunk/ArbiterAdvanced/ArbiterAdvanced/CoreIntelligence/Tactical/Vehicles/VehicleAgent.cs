using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using System.Diagnostics;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles
{
	/// <summary>
	/// Monitors and reasons about a vehicle
	/// </summary>
	public class VehicleAgent
	{
		/// <summary>
		/// Monitors state of the vehicle
		/// </summary>
		public StateMonitor StateMonitor;

		/// <summary>
		/// Monitors queuing state of the vehicle
		/// </summary>
		public QueuingMonitor QueuingState;

		/// <summary>
		/// Id of vehicle
		/// </summary>
		public int VehicleId;

		public Stopwatch ObservedTimer;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vehicleId"></param>
		public VehicleAgent(int vehicleId)
		{
			this.VehicleId = vehicleId;
			this.StateMonitor = new StateMonitor();
			this.QueuingState = new QueuingMonitor();
			this.BirthTimer();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="vehicleId"></param>
		public VehicleAgent(SceneEstimatorTrackedCluster ov)
		{
			this.VehicleId = ov.id;
			this.StateMonitor = new StateMonitor();
			this.QueuingState = new QueuingMonitor();
			this.UpdateState(ov);
			this.BirthTimer();
		}

		public VehicleAgent(bool temp, bool temp2)
		{
			this.VehicleId = 5000;
			SceneEstimatorTrackedCluster setc = new SceneEstimatorTrackedCluster();
			setc.isStopped = true;
			setc.relativePoints = new Coordinates[0];
			this.StateMonitor = new StateMonitor();
			this.QueuingState = new QueuingMonitor();
			this.BirthTimer();
			this.StateMonitor.Observed = setc;
		}

		public Polygon GetAbsolutePolygon(VehicleState ourState)
		{
			List<Coordinates> absCoords = new List<Coordinates>();
			foreach (Coordinates c in this.StateMonitor.Observed.relativePoints)
			{
				absCoords.Add(this.TransformCoordAbs(c, ourState));
			}
			return Polygon.GrahamScan(absCoords);
		}

		private void BirthTimer()
		{
			this.ObservedTimer = new Stopwatch();
			this.ObservedTimer.Start();
		}

		public bool PassedDelayedBirth
		{
			get
			{
				return (this.ObservedTimer.ElapsedMilliseconds / 1000.0) > 0.4;
			}
		}

		public bool PassedLongDelayedBirth
		{
			get
			{
				return (this.ObservedTimer.ElapsedMilliseconds / 1000.0) > 2.0;
			}
		}

		/// <summary>
		/// Updates the vehicle's monitor
		/// </summary>
		/// <param name="vehicleUpdate"></param>
		/// <param name="speed"></param>
		public void UpdateState(SceneEstimatorTrackedCluster update)
		{
			this.StateMonitor.Update(update);			
		}

		/// <summary>
		/// Speed of the vehicle
		/// </summary>
		public double Speed
		{
			get
			{
				return StateMonitor.Observed.speed;
			}
		}

		/// <summary>
		/// Position of the rear of the vehicle
		/// </summary>
		public Coordinates ClosestPosition
		{
			get
			{
				return StateMonitor.Observed.closestPoint;
			}
		}

		/// <summary>
		/// Checks if the vehicle is stopped
		/// </summary>
		public bool IsStopped
		{
			get
			{
				return this.StateMonitor.Observed.isStopped;
			}
		}

		/// <summary>
		/// Resets saved over time info
		/// </summary>
		public void Reset()
		{
			this.QueuingState.Reset();
		}

		/// <summary>
		/// Get the vehicle direction along a lane
		/// </summary>
		/// <param name="lane"></param>
		/// <returns></returns>
		public VehicleDirectionAlongPath VehicleDirectionAlong(IFQMPlanable lane)
		{
			if (this.IsStopped || !this.StateMonitor.Observed.headingValid)
				return VehicleDirectionAlongPath.Forwards;

			// get point on the lane path
			LinePath.PointOnPath pop = lane.LanePath().GetClosestPoint(this.ClosestPosition);

			// get heading of the lane path there
			Coordinates pathVector = lane.LanePath().GetSegment(pop.Index).UnitVector;			

			// get vehicle heading
			Coordinates unit = new Coordinates(1, 0);
			Coordinates headingVector = unit.Rotate(this.StateMonitor.Observed.absoluteHeading);
			
			// rotate vehicle heading
			Coordinates relativeVehicle = headingVector.Rotate(-pathVector.ArcTan);

			// get path heading
			double relativeVehicleDegrees = relativeVehicle.ToDegrees() >= 180.0 ? Math.Abs(relativeVehicle.ToDegrees() - 360.0) : Math.Abs(relativeVehicle.ToDegrees());
			
			if (relativeVehicleDegrees < 70)
				return VehicleDirectionAlongPath.Forwards;
			else if (relativeVehicleDegrees > 70 && relativeVehicleDegrees < 110)
				return VehicleDirectionAlongPath.Perpendicular;
			else
				return VehicleDirectionAlongPath.Reverse;						
		}

		/// <summary>
		/// Clsoest point to vehicle from a certain point relative to vehicle state
		/// </summary>
		/// <param name="c"></param>
		/// <param name="state"></param>
		/// <param name="distance"></param>
		/// <returns></returns>
		public Coordinates? ClosestPointTo(Coordinates c, VehicleState state, out double distance)
		{
			Coordinates[] rel = this.StateMonitor.Observed.relativePoints;
			Coordinates? closest = null;
			double dist = double.MaxValue;

			for (int i = 0; i < rel.Length; i++)
			{
				if (!closest.HasValue)
				{
					Coordinates tmp = this.TransformCoordAbs(rel[i], state);
					closest = tmp;
					dist = tmp.DistanceTo(c);
				}
				else
				{
					Coordinates tmp = this.TransformCoordAbs(rel[i], state);
					double tmpDist = tmp.DistanceTo(c);

					if (tmpDist < dist)
					{
						dist = tmpDist;
						closest = tmp;
					}					
				}
			}

			distance = dist;
			return closest;
		}

		public Coordinates TransformCoordAbs(Coordinates c, VehicleState state)
		{
			c = c.Rotate(state.Heading.ArcTan);
			c = c + state.Position;
			return c;
		}

		public override bool Equals(object obj)
		{
			if(obj is VehicleAgent)
				return this.VehicleId.Equals(((VehicleAgent)obj).VehicleId);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			return this.VehicleId.ToString();
		}

		public Coordinates? ClosestPointToLine(LinePath path, VehicleState vs)
		{
			Coordinates? closest = null;
			double minDist = Double.MaxValue;

			for (int i = 0; i < this.StateMonitor.Observed.relativePoints.Length; i++)
			{
				Coordinates c = this.TransformCoordAbs(this.StateMonitor.Observed.relativePoints[i], vs);
				double dist = path.GetClosestPoint(c).Location.DistanceTo(c);

				if (!closest.HasValue)
				{
					closest = c;
					minDist = dist;
				}
				else if (dist < minDist)
				{
					closest = c;
					minDist = dist;
				}
			}

			return closest;
		}
	}

	public enum VehicleDirectionAlongPath
	{
		Forwards,
		Perpendicular,
		Reverse
	}
}
