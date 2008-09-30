using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Tools;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Tracking
{
	/// <summary>
	/// Reasons about how to follw the vehicle ahead in an opposing lane
	/// </summary>
	public class OpposingForwardVehicleTracker
	{
		/// <summary>
		/// Current Vehicle we wish to track
		/// </summary>
		public VehicleAgent CurrentVehicle;

		/// <summary>
		/// Distance to the forward vehicle
		/// </summary>
		private double currentDistance;

		/// <summary>
		/// Constructor
		/// </summary>
		public OpposingForwardVehicleTracker()
		{
			this.Reset();
		}

		/// <summary>
		/// Resets values held over time
		/// </summary>
		public void Reset()
		{
			if (this.CurrentVehicle != null)
				this.CurrentVehicle.Reset();

			this.CurrentVehicle = null;
			this.currentDistance = Double.MaxValue;
		}

		/// <summary>
		/// Flag if we should use the forward tracker
		/// </summary>
		/// <returns></returns>
		public bool ShouldUseForwardTracker
		{
			get
			{
				return this.CurrentVehicle != null;
			}
		}

		/// <summary>
		/// Updates the current vehicle
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		public void Update(ArbiterLane lane, VehicleState state)
		{
			// get the forward path
			LinePath p = lane.LanePath().Clone();
			p.Reverse();

			// get our position
			Coordinates f = state.Front;

			// get all vehicles associated with those components
			List<VehicleAgent> vas = new List<VehicleAgent>();
			foreach (IVehicleArea iva in lane.AreaComponents)
			{
				if (TacticalDirector.VehicleAreas.ContainsKey(iva))
					vas.AddRange(TacticalDirector.VehicleAreas[iva]);
			}

			// get the closest forward of us
			double minDistance = Double.MaxValue;
			VehicleAgent closest = null;

			// get clsoest
			foreach (VehicleAgent va in vas)
			{
				// get position of front
				Coordinates frontPos = va.ClosestPosition;

				// gets distance from other vehicle to us along the lane
				double frontDist = lane.DistanceBetween(frontPos, f);

				if (frontDist >= 0 && frontDist < minDistance)
				{
					minDistance = frontDist;
					closest = va;
				}
			}

			this.CurrentVehicle = closest;
			this.currentDistance = minDistance;
		}

		/// <summary>
		/// Parameters to follow the forward vehicle
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public TravelingParameters Follow(ArbiterLane lane, VehicleState state)
		{
			// travelling parameters
			TravelingParameters tp = new TravelingParameters();

			// ignorable obstacles
			List<int> ignoreVehicle = new List<int>();
			ignoreVehicle.Add(CurrentVehicle.VehicleId);

			// get control parameters
			ForwardVehicleTrackingControl fvtc = GetControl(lane, state);

			// init params
			tp.DistanceToGo = fvtc.xDistanceToGood;
			tp.NextState = CoreCommon.CorePlanningState;
			tp.RecommendedSpeed = fvtc.vFollowing;
			tp.Type = TravellingType.Vehicle;
			tp.Decorators = TurnDecorators.NoDecorators;

			// flag to ignore forward vehicle
			bool ignoreForward = false;

			// reversed lane path
			LinePath lp = lane.LanePath().Clone();
			lp.Reverse();			

			#region Following Control

			#region Immediate Stop

			// need to stop immediately
			if (fvtc.vFollowing == 0.0)
			{
				// don't ignore forward
				ignoreForward = false;

				// speed command
				SpeedCommand sc = new ScalarSpeedCommand(0.0);
				tp.UsingSpeed = true;
				
				// standard path following behavior
				Behavior final = new StayInLaneBehavior(lane.LaneId, sc, ignoreVehicle, lp, lane.Width, lane.NumberOfLanesLeft(state.Front, false), lane.NumberOfLanesRight(state.Front, false));
				tp.Behavior = final;
				tp.SpeedCommand = sc;
			}

			#endregion

			#region Distance Stop

			// stop at distance
			else if (fvtc.vFollowing < 0.7 &&
				CoreCommon.Communications.GetVehicleSpeed().Value <= 2.24 &&
				fvtc.xSeparation > fvtc.xAbsMin)
			{
				// ignore forward vehicle
				ignoreForward = true;

				// speed command
				SpeedCommand sc = new StopAtDistSpeedCommand(fvtc.xDistanceToGood);
				tp.UsingSpeed = false;

				// standard path following behavior
				Behavior final = new StayInLaneBehavior(lane.LaneId, sc, ignoreVehicle, lp, lane.Width, lane.NumberOfLanesLeft(state.Front, false), lane.NumberOfLanesRight(state.Front, false));
				tp.Behavior = final;
				tp.SpeedCommand = sc;
			}

			#endregion

			#region Normal Following

			// else normal
			else
			{
				// ignore the forward vehicle as we are tracking properly
				ignoreForward = true;

				// speed command
				SpeedCommand sc = new ScalarSpeedCommand(fvtc.vFollowing);
				tp.DistanceToGo = fvtc.xDistanceToGood;
				tp.NextState = CoreCommon.CorePlanningState;
				tp.RecommendedSpeed = fvtc.vFollowing;
				tp.Type = TravellingType.Vehicle;
				tp.UsingSpeed = true;

				// standard path following behavior
				Behavior final = new StayInLaneBehavior(lane.LaneId, sc, ignoreVehicle, lp, lane.Width, lane.NumberOfLanesLeft(state.Front, false), lane.NumberOfLanesRight(state.Front, false));
				tp.Behavior = final;
				tp.SpeedCommand = sc;
			}

			#endregion

			#endregion

			// check ignore
			if (ignoreForward)
			{
				List<int> ignorable = new List<int>();
				ignorable.Add(this.CurrentVehicle.VehicleId);
				tp.VehiclesToIgnore = ignorable;
			}
			else
				tp.VehiclesToIgnore = new List<int>();

			// return parameterization
			return tp;
		}

		/// <summary>
		/// Figure out standard control to follow
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public ForwardVehicleTrackingControl GetControl(ArbiterLane lane, VehicleState state)
		{
			// check exists a vehicle to track
			if (this.CurrentVehicle != null)
			{
				// get the maximum velocity of the segment we're closest to
				double segV = lane.CurrentMaximumSpeed(state.Front);
				double segMaxV = segV;

				// minimum distance
				double xAbsMin = TahoeParams.VL * 2.0;

				// retrieve the tracked vehicle's scalar absolute speed
				double vTarget = this.CurrentVehicle.StateMonitor.Observed.speedValid ? this.CurrentVehicle.Speed : lane.Way.Segment.SpeedLimits.MaximumSpeed;

				// check if vehicle is moving away from us
				VehicleDirectionAlongPath vdap = CurrentVehicle.VehicleDirectionAlong(lane);

				// get the good distance behind the target, is xAbsMin if vehicle velocity is small enough
				double xGood = xAbsMin + (1.5 * (2 * TahoeParams.VL) * vTarget);

				// get our current separation
				double xSeparation = this.currentDistance;

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
				else if (xSeparation <= xGood)
				{
					// stop
					vFollowing = 0;

					// good distance
					xDistanceToGood = 0;
				}
				// determine if our separation is less than the good distance but not inside minimum
				else if (xAbsMin < xSeparation && xSeparation < xGood)
				{
					// get the distance we are from xGood to xMin
					double xAlong = xSeparation - xAbsMin;

					// get the total distance from xGood to xMin
					double xGoodToMin = xGood - xAbsMin;

					// slow to 0 by min
					vFollowing = (((xGoodToMin - xAlong) / xGoodToMin) * (-vTarget)) + vTarget;

					// good distance
					xDistanceToGood = 0;
				}
				// otherwise xSeparation > xEnvelope
				else
				{
					// good distance
					xDistanceToGood = xSeparation - xGood;

					// get differences in max and target velocities
					vFollowing = SpeedTools.GenerateSpeed(xDistanceToGood, vTarget, segMaxV);
				}

				// return
				return new ForwardVehicleTrackingControl(true, false, vFollowing, xSeparation,
					xDistanceToGood, vTarget, xAbsMin, xGood, true);
			}
			else
			{
				// return
				return new ForwardVehicleTrackingControl(false, false, Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue, 0.0, Double.MaxValue, false);
			}
		}
	}
}
