using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Monitors;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection.Reasoning
{
	/// <summary>
	/// Monitors the area forward of a turn
	/// </summary>
	public class TurnForwardMonitor
	{
		/// <summary>
		/// Monitor of the entry we are going into
		/// </summary>
		public IEntryAreaMonitor EntryMonitor;

		/// <summary>
		/// Turn we are on
		/// </summary>
		public ArbiterInterconnect turn;

		/// <summary>
		/// Current vehicle we are tracking
		/// </summary>
		public VehicleAgent CurrentVehicle;

		/// <summary>
		/// Vehicles we wish to ignore
		/// </summary>
		public List<int> VehiclesToIgnore;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="turn"></param>
		/// <param name="entryMontitor"></param>
		public TurnForwardMonitor(ArbiterInterconnect turn, IEntryAreaMonitor entryMonitor)
		{
			this.turn = turn;
			this.EntryMonitor = entryMonitor;
			this.VehiclesToIgnore = new List<int>();
		}

		/// <summary>
		/// Updates the montitor with the closest forward vehicle along the lane
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <param name="final"></param>
		public void Update(VehicleState vehicleState, ArbiterWaypoint final, LinePath fullPath)
		{
			this.VehiclesToIgnore = new List<int>();

			if (TacticalDirector.VehicleAreas.ContainsKey(final.Lane))
			{
				List<VehicleAgent> laneVehicles = TacticalDirector.VehicleAreas[final.Lane];

				this.CurrentVehicle = null;
				double currentDistance = Double.MaxValue;

				foreach (VehicleAgent va in laneVehicles)
				{
					double endDistance = final.Lane.DistanceBetween(final.Position, va.ClosestPosition);					
					if (endDistance > 0)
						this.VehiclesToIgnore.Add(va.VehicleId);

					double tmpDist = endDistance;

					if (tmpDist > 0 && (this.CurrentVehicle == null || tmpDist < currentDistance))
					{
						this.CurrentVehicle = va;
						currentDistance = tmpDist;
					}
				}
			}
			else
				this.CurrentVehicle = null;
		}

		/// <summary>
		/// Tells if should use this monitor for parameterization help or not
		/// </summary>
		/// <returns></returns>
		public bool ShouldUseForwardTracker()
		{
			return this.CurrentVehicle != null;
		}

		/// <summary>
		/// Gets parameters for following forward vehicle
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <param name="fullPath"></param>
		/// <param name="followingSpeed"></param>
		/// <param name="distanceToGood"></param>
		public void Follow(VehicleState state, LinePath fullPath, ArbiterLane lane, ArbiterInterconnect turn, 
			out double followingSpeed, out double distanceToGood, out double xSep)
		{
			// get the maximum velocity of the segment we're closest to
			double segV = Math.Min(lane.CurrentMaximumSpeed(state.Front), turn.MaximumDefaultSpeed);
			double segMaxV = segV;
							
			// minimum distance
			double xAbsMin = TahoeParams.VL * 1.5;

			// retrieve the tracked vehicle's scalar absolute speed
			double vTarget = CurrentVehicle.StateMonitor.Observed.isStopped ? 0.0 : this.CurrentVehicle.Speed;
				
			// get the good distance behind the target, is xAbsMin if vehicle velocity is small enough
			double xGood = xAbsMin + (1.5 * (TahoeParams.VL / 4.4704) * vTarget);

			// get our current separation
			double xSeparation = state.Front.DistanceTo(this.CurrentVehicle.ClosestPosition);//lane.DistanceBetween(state.Front, this.CurrentVehicle.ClosestPosition);
			xSep = xSeparation;

			// determine the envelope to reason about the vehicle, that is, to slow from vMax to vehicle speed
			double xEnvelope = (Math.Pow(vTarget, 2.0) - Math.Pow(segMaxV, 2.0)) / (2.0 * -0.5);

			// the distance to the good
			double xDistanceToGood;

			// determine the velocity to follow the vehicle ahead
			double vFollowing;

			// check if we are basically stopped in the right place behind another vehicle
			if (vTarget < 1 && Math.Abs(xSeparation - xGood) < 1)
			{
				// stop
				vFollowing = 0;

				// good distance
				xDistanceToGood = 0;
			}
			// check if we are within the minimum distance
			else if (xSeparation <= xAbsMin)
			{
				// stop
				vFollowing = 0;

				// good distance
				xDistanceToGood = 0;
			}
			// determine if our separation is less than the good distance but not inside minimum
			else if (xAbsMin < xSeparation && xSeparation < xGood)
			{
				// get the distance we are from xMin
				double xAlong = xSeparation - xAbsMin;

				// get the total distance from xGood to xMin
				double xGoodToMin = xGood - xAbsMin;

				// slow to 0 by min
				vFollowing = (((xGoodToMin - xAlong) / xGoodToMin) * (-vTarget)) + vTarget;

				// good distance
				xDistanceToGood = 0;
			}
			// our separation is greater than the good distance but within the envelope
			else if (xGood <= xSeparation && xSeparation <= xEnvelope + xGood)
			{
				// get differences in max and target velocities
				double vDifference = Math.Max(segMaxV - vTarget, 0);

				// get the distance we are along the speed envolope from xGood
				double xAlong = xEnvelope - (xSeparation - xGood);

				// slow to vTarget by good
				vFollowing = (((xEnvelope - xAlong) / xEnvelope) * (vDifference)) + vTarget;

				// good distance
				xDistanceToGood = xSeparation - xGood;
			}
			// otherwise xSeparation > xEnvelope
			else
			{
				// can go max speed
				vFollowing = segMaxV;

				// good distance
				xDistanceToGood = xSeparation - xGood;
			}

			// set out params
			followingSpeed = vFollowing;
			distanceToGood = xDistanceToGood;
		}
	}
}
