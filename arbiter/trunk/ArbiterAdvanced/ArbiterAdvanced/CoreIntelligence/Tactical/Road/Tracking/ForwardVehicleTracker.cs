using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Tools;
using UrbanChallenge.Arbiter.Core.Common.State;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Reasons about how to follow the vehicle ahead
	/// </summary>
	public class ForwardVehicleTracker
	{
		/// <summary>
		/// Current vehicle we are tracking
		/// </summary>
		public VehicleAgent CurrentVehicle;

		/// <summary>
		/// Current distance we are following the vehicle at
		/// </summary>
		public double currentDistance;

		/// <summary>
		/// current following
		/// </summary>
		private TravelingParameters followingParameters;

		/// <summary>
		/// Current vehicle control
		/// </summary>
		public ForwardVehicleTrackingControl ForwardControl;

		/// <summary>
		/// Flag if stopped behind forward vehicle
		/// </summary>
		public bool StoppedBehindForwardVehicle;

		/// <summary>
		/// Ignorable vehicles
		/// </summary>
		public List<int> VehiclesToIgnore;

		/// <summary>
		/// Constructor
		/// </summary>
		public ForwardVehicleTracker()
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
		/// Parameters to follow the forward vehicle
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public TravelingParameters Follow(IFQMPlanable lane, VehicleState state, List<ArbiterWaypoint> ignorable)
		{
			// travelling parameters
			TravelingParameters tp = new TravelingParameters();

			// get control parameters
			ForwardVehicleTrackingControl fvtc = GetControl(lane, state, ignorable);
			this.ForwardControl = fvtc;

			// initialize the parameters
			tp.DistanceToGo = fvtc.xDistanceToGood;
			tp.NextState = CoreCommon.CorePlanningState;
			tp.RecommendedSpeed = fvtc.vFollowing;
			tp.Type = TravellingType.Vehicle;
			tp.Decorators = new List<BehaviorDecorator>();

			// ignore the forward vehicles
			tp.VehiclesToIgnore = this.VehiclesToIgnore;

			#region Following Control

			#region Immediate Stop

			// need to stop immediately
			if (fvtc.vFollowing == 0.0)
			{
				// speed command
				SpeedCommand sc = new ScalarSpeedCommand(0.0);
				tp.SpeedCommand = sc;
				tp.UsingSpeed = true;

				if (lane is ArbiterLane)
				{
					// standard path following behavior
					ArbiterLane al = ((ArbiterLane)lane);
					Behavior final = new StayInLaneBehavior(al.LaneId, sc, this.VehiclesToIgnore, al.LanePath(), al.Width, al.NumberOfLanesLeft(state.Front, true), al.NumberOfLanesRight(state.Front, true));
					final.Decorators = tp.Decorators;
					tp.Behavior = final;
				}
				else
				{
					SupraLane sl = (SupraLane)lane;
					StayInSupraLaneState sisls = (StayInSupraLaneState)CoreCommon.CorePlanningState;
					Behavior final = sisls.GetBehavior(sc, state.Front, this.VehiclesToIgnore);
					final.Decorators = tp.Decorators;
					tp.Behavior = final;
				}
			}

			#endregion

			#region Stopping at Distance

			// stop at distance
			else if (fvtc.vFollowing < 0.7 &&
				CoreCommon.Communications.GetVehicleSpeed().Value <= 2.24 &&
				fvtc.xSeparation > fvtc.xAbsMin)
			{
				// speed command
				SpeedCommand sc = new StopAtDistSpeedCommand(fvtc.xDistanceToGood);
				tp.SpeedCommand = sc;
				tp.UsingSpeed = false;

				if (lane is ArbiterLane)
				{
					ArbiterLane al = (ArbiterLane)lane;

					// standard path following behavior
					Behavior final = new StayInLaneBehavior(al.LaneId, sc, this.VehiclesToIgnore, lane.LanePath(), al.Width, al.NumberOfLanesLeft(state.Front, true), al.NumberOfLanesRight(state.Front, true));
					final.Decorators = tp.Decorators;
					tp.Behavior = final;
				}
				else
				{
					SupraLane sl = (SupraLane)lane;
					StayInSupraLaneState sisls = (StayInSupraLaneState)CoreCommon.CorePlanningState;
					Behavior final = sisls.GetBehavior(sc, state.Front, this.VehiclesToIgnore);
					final.Decorators = tp.Decorators;
					tp.Behavior = final;
				}
			}

			#endregion

			#region Normal Following

			// else normal
			else
			{
				// speed command
				SpeedCommand sc = new ScalarSpeedCommand(fvtc.vFollowing);
				tp.DistanceToGo = fvtc.xDistanceToGood;
				tp.NextState = CoreCommon.CorePlanningState;
				tp.RecommendedSpeed = fvtc.vFollowing;
				tp.Type = TravellingType.Vehicle;
				tp.UsingSpeed = true;
				tp.SpeedCommand = sc;

				if (lane is ArbiterLane)
				{
					ArbiterLane al = ((ArbiterLane)lane);
					// standard path following behavior
					Behavior final = new StayInLaneBehavior(al.LaneId, sc, this.VehiclesToIgnore, lane.LanePath(), al.Width, al.NumberOfLanesLeft(state.Front, true), al.NumberOfLanesRight(state.Front, true));
					final.Decorators = tp.Decorators;
					tp.Behavior = final;
				}
				else
				{
					SupraLane sl = (SupraLane)lane;
					StayInSupraLaneState sisls = (StayInSupraLaneState)CoreCommon.CorePlanningState;
					Behavior final = sisls.GetBehavior(sc, state.Front, this.VehiclesToIgnore);
					final.Decorators = tp.Decorators;
					tp.Behavior = final;
				}
			}

			#endregion

			#endregion

			#region Check for Oncoming Vehicles

			// check if need to add current lane oncoming vehicle decorator
			if (false && this.CurrentVehicle.PassedDelayedBirth && fvtc.forwardOncoming && fvtc.xSeparation > TahoeParams.VL && fvtc.xSeparation < 30)
			{
				// check valid lane area
				if (lane is ArbiterLane || ((SupraLane)lane).ClosestComponent(this.CurrentVehicle.ClosestPosition) == SLComponentType.Initial)
				{
					// get distance to and speed of the forward vehicle
					double fvDistance = fvtc.xSeparation;
					double fvSpeed = fvtc.vTarget;

					// create the 5mph behavior
					ScalarSpeedCommand updated = new ScalarSpeedCommand(2.24);

					// set that we are using speed
					tp.UsingSpeed = true;
					tp.RecommendedSpeed = updated.Speed;
					tp.DistanceToGo = fvtc.xSeparation;

					// create the decorator
					OncomingVehicleDecorator ovd = new OncomingVehicleDecorator(updated, fvDistance, fvSpeed);

					// add the decorator
					tp.Behavior.Decorators.Add(ovd);
					tp.Decorators.Add(ovd);
				}
			}

			#endregion

			// set current
			this.followingParameters = tp;

			// return parameterization
			return tp;
		}

		/// <summary>
		/// Returns if we should pass or nor
		/// </summary>
		/// <returns></returns>
		public bool ShouldPass(out LaneChangeInformation lci)
		{
			// passing reason set to none by default
			lci = new LaneChangeInformation(LaneChangeReason.NotApplicable, this.CurrentVehicle);

			// check the queuing state of the forward vehicle
			if (this.CurrentVehicle.QueuingState.Queuing == QueuingState.Failed)
			{
				lci = new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, this.CurrentVehicle);
				return true;
			}
			else
			{
				// check if moving much too slow for the next stop for long period of time
				#warning need to implement slow moving vehicle pass after beta

				// for now return false
				return false;
			}

		}

		/// <summary>
		/// Figure out standard control to follow
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public ForwardVehicleTrackingControl GetControl(IFQMPlanable lane, VehicleState state, List<ArbiterWaypoint> ignorable)
		{
			// check exists a vehicle to track
			if (this.CurrentVehicle != null)
			{
				// get the maximum velocity of the segment we're closest to
				double segV = lane.CurrentMaximumSpeed(state.Front);
				double segMaxV = segV;

				// flag if the forward vehicles is in a stop waypoint type safety zone
				bool safetyZone = false;
				ArbiterWaypoint nextStop = lane.GetNext(state.Front, WaypointType.Stop, ignorable);
				double distance = nextStop != null ? lane.DistanceBetween(state.Front, nextStop.Position) : Double.MaxValue;

				// check if no stop exists or distance > 30
				if (nextStop == null || distance > 30 || distance < 0)
				{
					// there is no next stop or the forward vehcile is not in a safety zone
					safetyZone = false;
				}
				// otherwise the tracked vehicle is in a safety zone
				else
				{
					// set the forward vehicle as being in a safety zone
					safetyZone = true;
				}

				// minimum distance
				double xAbsMin;

				// if we're in a safety zone
				if (safetyZone)
				{
					// set minimum to 1 times the tahoe's vehicle length
					xAbsMin = TahoeParams.VL * 1.5;
				}
				// otherwise we're not in a safety zone
				else
				{
					// set minimum to 2 times the tahoe's vehicle length
					xAbsMin = TahoeParams.VL * 2.0;
				}

				// retrieve the tracked vehicle's scalar absolute speed
				double vTarget = CurrentVehicle.StateMonitor.Observed.isStopped ? 0.0 : this.CurrentVehicle.Speed;

				// check if vehicle is moving away from us
				VehicleDirectionAlongPath vdap = CurrentVehicle.VehicleDirectionAlong(lane);

				// if moving perpendic, set speed to 0.0
				if (vdap == VehicleDirectionAlongPath.Perpendicular)
					vTarget = 0.0;

				#region Forward

				// can use normal tracker if vehicle not oncoming
				if (true)//vdap != VehicleDirectionAlongPath.Reverse)
				{
					// get the good distance behind the target, is xAbsMin if vehicle velocity is small enough
					double xGood = xAbsMin + (1.5 * (TahoeParams.VL / 4.4704) * vTarget);

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
					// our separation is greater than the good distance
					else
					{
						// good distance
						xDistanceToGood = xSeparation - xGood;

						// get differences in max and target velocities
						vFollowing = SpeedTools.GenerateSpeed(xDistanceToGood, vTarget, segMaxV);
					}

					// return
					return new ForwardVehicleTrackingControl(true, safetyZone, vFollowing, xSeparation,
						xDistanceToGood, vTarget, xAbsMin, xGood,false);
				}

				#endregion

				#region Oncoming
				/*
				else
				{
					// determine the distance for the other vehicle to stop
					double xTargetEnvelope =  -Math.Pow(vTarget, 2.0) / (2.0 * -0.5);

					// determine the distance for our vehicle to stop
					double xOurEnvelope = -Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2.0) / (2.0 * -0.5);

					// get the good distance behind the target, is xAbsMin if vehicle velocity is small enough
					double xGood = xAbsMin + (1.5 * (TahoeParams.VL / 4.4704) * vTarget) + xOurEnvelope + xTargetEnvelope;

					// get our current separation
					double xSeparation = this.currentDistance;

					// determine the envelope for us to slow to 0 by the good distance
					double xEnvelope = -Math.Pow(segMaxV, 2.0) / (2.0 * -0.5);

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
					// our separation is greater than the good distance but within the envelope
					else if (xGood <= xSeparation && xSeparation <= xEnvelope + xGood)
					{
						// get differences in max and target velocities
						double vDifference = segMaxV;

						// get the distance we are along the speed envolope from xGood
						double xAlong = xEnvelope - (xSeparation - xGood);

						// slow to vTarget by good
						vFollowing = (((xEnvelope - xAlong) / xEnvelope) * (vDifference));

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

					// return
					return new ForwardVehicleTrackingControl(true, safetyZone, vFollowing, xSeparation,
						xDistanceToGood, vTarget, xAbsMin, xGood, true);
				}

				*/
				#endregion
			}
			else
			{
				// return
				return new ForwardVehicleTrackingControl(false, false, Double.MaxValue, Double.MaxValue, Double.MaxValue, Double.MaxValue, 0.0, Double.MaxValue, false);
			}		
		}

		/// <summary>
		/// Updates the current vehicle
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		public void Update(IFQMPlanable lane, VehicleState state)
		{
			// get the forward path
			LinePath p = lane.LanePath();

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

			// set the vehicles to ignore
			this.VehiclesToIgnore = new List<int>();

			// get clsoest
			foreach (VehicleAgent va in vas)
			{
				// get position of front
				Coordinates frontPos = va.ClosestPosition;

				// check relatively inside
				if (lane.RelativelyInside(frontPos))
				{
					// distance to vehicle
					double frontDist = lane.DistanceBetween(f, frontPos);

					// check forward of us
					if (frontDist > 0)
					{
						// add to ignorable
						this.VehiclesToIgnore.Add(va.VehicleId);

						// check for closest
						if (frontDist < minDistance)
						{
							minDistance = frontDist;
							closest = va;
						}
					}
				}
			}

			this.CurrentVehicle = closest;
			this.currentDistance = minDistance;
		}

		/// <summary>
		/// How we can follow vehicle ahead
		/// </summary>
		public TravelingParameters FollowingParameters
		{
			get { return followingParameters; }
		}

		/// <summary>
		/// Distance from coord to vehicle along lane
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public double DistanceToVehicle(IFQMPlanable lane, Coordinates c)
		{
			if (CurrentVehicle != null)
				return lane.DistanceBetween(c, CurrentVehicle.ClosestPosition);
			else
				return Double.MaxValue;
		}

		/// <summary>
		/// Checks if we should pass the forward vehicle
		/// </summary>
		/// <param name="lci"></param>
		/// <param name="lane"></param>
		/// <returns></returns>
		public bool ShouldPass(out LaneChangeInformation lci, ArbiterLane lane)
		{
			// passing reason set to none by default
			lci = new LaneChangeInformation(LaneChangeReason.NotApplicable, this.CurrentVehicle);

			// check the queuing state of the forward vehicle
			if (this.CurrentVehicle.QueuingState.Queuing == QueuingState.Failed)
			{
				lci = new LaneChangeInformation(LaneChangeReason.FailedForwardVehicle, this.CurrentVehicle);
				return true;
			}

			// check inside any safety zone
			foreach (ArbiterSafetyZone asz in lane.SafetyZones)
			{
				if (asz.IsInSafety(this.CurrentVehicle.ClosestPosition))
					return false;
			}
			foreach (ArbiterIntersection ai in CoreCommon.RoadNetwork.ArbiterIntersections.Values)
			{
				if (ai.IntersectionPolygon.IsInside(this.CurrentVehicle.ClosestPosition))
					return false;
			}

			if ((this.CurrentVehicle.Speed < CoreCommon.Communications.GetVehicleSpeed().Value || 
				(this.CurrentVehicle.IsStopped && this.CurrentVehicle.StateMonitor.Observed.speedValid)) &&
				this.CurrentVehicle.Speed < 0.7 * lane.Way.Segment.SpeedLimits.MaximumSpeed)
			{
				lci = new LaneChangeInformation(LaneChangeReason.SlowForwardVehicle, this.CurrentVehicle);
				return true;
			}
			
			// fall out
			return false;
		}
	}
}
