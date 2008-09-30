using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Quadrants;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Reasoning
{
	/// <summary>
	/// Reasons when we are traveling in the wrong direction in a lane
	/// </summary>
	public class OpposingReasoning
	{
		/// <summary>
		/// Reasons to the left of us
		/// </summary>
		private ILateralReasoning leftLateralReasoning;

		/// <summary>
		/// Reasong to the right of us
		/// </summary>
		private ILateralReasoning rightLateralReasoning;

		/// <summary>
		/// Reasons when the lane explicitly to the right is not actually to the right
		/// </summary>
		private ILateralReasoning secondaryLateralReasoning;

		/// <summary>
		/// Lane associated with the reasoning
		/// </summary>
		public ArbiterLane Lane;

		/// <summary>
		/// Monitors road ahead
		/// </summary>
		public OpposingForwardQuadrantMonitor OpposingForwardMonitor;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="leftLateral"></param>
		/// <param name="rightLateral"></param>
		public OpposingReasoning(ILateralReasoning leftLateral, ILateralReasoning rightLateral, ArbiterLane lane)
		{
			this.Lane = lane;
			this.leftLateralReasoning = leftLateral;
			this.rightLateralReasoning = rightLateral;
			this.OpposingForwardMonitor = new OpposingForwardQuadrantMonitor();
		}

		/// <summary>
		/// Reset value held over time
		/// </summary>
		public void Reset()
		{
			if(this.OpposingForwardMonitor != null)
				this.OpposingForwardMonitor.Reset();

			if (this.leftLateralReasoning != null)
				this.leftLateralReasoning.Reset();

			if (this.rightLateralReasoning != null)
				this.rightLateralReasoning.Reset();

			if (this.secondaryLateralReasoning != null)
				this.secondaryLateralReasoning.Reset();
		}

		/// <summary>
		/// Plans the forward maneuver and secondary maneuver
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="p"></param>
		/// <param name="blockage"></param>
		/// <returns></returns>
		public Maneuver ForwardManeuver(ArbiterLane arbiterLane, ArbiterLane closestGood, VehicleState vehicleState, RoadPlan roadPlan,
			List<ITacticalBlockage> blockages)
		{
			// get primary maneuver
			Maneuver primary = this.OpposingForwardMonitor.PrimaryManeuver(arbiterLane, closestGood, vehicleState, blockages);

			// return primary for now
			return primary;
		}

		/// <summary>
		/// Behavior we would like to do other than going straight
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="vehicleState"></param>
		/// <param name="p"></param>
		/// <param name="blockages"></param>
		/// <returns></returns>
		/// <remarks>tries to go right, if not goest left if needs 
		/// to if forward vehicle ahead and we're stopped because of them</remarks>
		public Maneuver? SecondaryManeuver(ArbiterLane arbiterLane, ArbiterLane closestGood, VehicleState vehicleState, List<ITacticalBlockage> blockages,
			LaneChangeParameters? entryParameters)
		{
			// check blockages
			if (blockages != null && blockages.Count > 0 && blockages[0] is OpposingLaneBlockage)
			{
				// create the blockage state
				EncounteredBlockageState ebs = new EncounteredBlockageState(blockages[0], CoreCommon.CorePlanningState);

				// go to a blockage handling tactical
				return new Maneuver(new NullBehavior(), ebs, TurnDecorators.NoDecorators, vehicleState.Timestamp);
			}

			// check dist needed to complete
			double neededDistance = (Math.Abs(arbiterLane.LaneId.Number - closestGood.LaneId.Number) * 1.5 * TahoeParams.VL) +
			(-Math.Pow(CoreCommon.Communications.GetVehicleSpeed().Value, 2) / (4 * CoreCommon.MaximumNegativeAcceleration));

			// get upper bound
			LinePath.PointOnPath xFront = arbiterLane.LanePath().GetClosestPoint(vehicleState.Front);
			Coordinates xUpper = arbiterLane.LanePath().AdvancePoint(xFront, -neededDistance).Location;

			if (entryParameters.HasValue)
			{
				// check if we should get back, keep speed nice n' lowi fpassing failed
				if (entryParameters.Value.Reason == LaneChangeReason.FailedForwardVehicle)
				{
					double xToReturn = arbiterLane.DistanceBetween(entryParameters.Value.DefaultReturnLowerBound, vehicleState.Front);
					if(xToReturn >= 0.0)
						ArbiterOutput.Output("Distance until must return to lane: " + xToReturn);
					else
						ArbiterOutput.Output("Can return to lane from arbitrary upper bound: " + xToReturn);

					// check can return
					if (xToReturn < 0)
					{
						// check if right lateral exists exactly here
						if (this.rightLateralReasoning.ExistsExactlyHere(vehicleState) && this.rightLateralReasoning.LateralLane.Equals(closestGood))
						{
							ArbiterOutput.Output("Right lateral reasoning good and exists exactly here");
							return this.DefaultRightToGoodChange(arbiterLane, closestGood, vehicleState, blockages, xUpper, true);
						}
						else if (!this.rightLateralReasoning.ExistsRelativelyHere(vehicleState) && !this.rightLateralReasoning.LateralLane.Equals(closestGood))
						{
							ArbiterOutput.Output("Right lateral reasoning not good closest and does not exist here");

							if (this.secondaryLateralReasoning == null || !this.secondaryLateralReasoning.LateralLane.Equals(closestGood))
								this.secondaryLateralReasoning = new LateralReasoning(closestGood, UrbanChallenge.Common.Sensors.SideObstacleSide.Passenger);


							if (this.secondaryLateralReasoning.ExistsExactlyHere(vehicleState))
							{
								ILateralReasoning tmpReasoning = this.rightLateralReasoning;
								this.rightLateralReasoning = this.secondaryLateralReasoning;
								Maneuver? tmp = this.DefaultRightToGoodChange(arbiterLane, closestGood, vehicleState, blockages, xUpper, true);
								this.rightLateralReasoning = tmpReasoning;
								return tmp;
							}
							else
							{
								ArbiterOutput.Output("Cosest good lane does not exist here??");
								return null;
							}
						}
						else
						{
							ArbiterOutput.Output("Can't change lanes!!, RL exists exactly: " + this.rightLateralReasoning.ExistsExactlyHere(vehicleState).ToString() +
								", RL exists rel: " + this.rightLateralReasoning.ExistsRelativelyHere(vehicleState).ToString() + ", RL closest good: " + this.rightLateralReasoning.LateralLane.Equals(closestGood).ToString());
							return null;
						}
					}
					else
						return null;
				}
			}

			// lane change info
			LaneChangeInformation lci = new LaneChangeInformation(LaneChangeReason.Navigation, null);

			// notify
			ArbiterOutput.Output("In Opposing with no Previous state knowledge, attempting to return");

			// check if right lateral exists exactly here
			if (this.rightLateralReasoning.ExistsExactlyHere(vehicleState) && this.rightLateralReasoning.LateralLane.Equals(closestGood))
			{
				ArbiterOutput.Output("Right lateral reasoning good and exists exactly here");
				return this.DefaultRightToGoodChange(arbiterLane, closestGood, vehicleState, blockages, xUpper, false);
			}
			else if (!this.rightLateralReasoning.ExistsRelativelyHere(vehicleState) && !this.rightLateralReasoning.LateralLane.Equals(closestGood))
			{
				ArbiterOutput.Output("Right lateral reasoning not good closest and does not exist here");

				if (this.secondaryLateralReasoning == null || !this.secondaryLateralReasoning.LateralLane.Equals(closestGood))
					this.secondaryLateralReasoning = new LateralReasoning(closestGood, UrbanChallenge.Common.Sensors.SideObstacleSide.Passenger);

				if (this.secondaryLateralReasoning.ExistsExactlyHere(vehicleState))
				{
					ILateralReasoning tmpReasoning = this.rightLateralReasoning;
					this.rightLateralReasoning = this.secondaryLateralReasoning;
					Maneuver? tmp = this.DefaultRightToGoodChange(arbiterLane, closestGood, vehicleState, blockages, xUpper, false);
					this.rightLateralReasoning = tmpReasoning;
					return tmp;
				}
				else
				{
					ArbiterOutput.Output("Cosest good lane does not exist here??");
					return null;
				}
			}
			else
			{
				ArbiterOutput.Output("Can't change lanes!!, RL exists exactly: " + this.rightLateralReasoning.ExistsExactlyHere(vehicleState).ToString() +
					", RL exists rel: " + this.rightLateralReasoning.ExistsRelativelyHere(vehicleState).ToString() + ", RL closest good: " + this.rightLateralReasoning.LateralLane.Equals(closestGood).ToString());
				return null;
			}
		}

		/// <summary>
		/// Simple right lane change
		/// </summary>
		/// <param name="arbiterLane"></param>
		/// <param name="closestGood"></param>
		/// <param name="vehicleState"></param>
		/// <param name="blockages"></param>
		/// <returns></returns>
		private Maneuver? DefaultRightToGoodChange(ArbiterLane arbiterLane, ArbiterLane closestGood, VehicleState vehicleState, 
			List<ITacticalBlockage> blockages, Coordinates xUpper, bool forcedOpposing)
		{
			bool adjRearClear = this.rightLateralReasoning.AdjacentAndRearClear(vehicleState);
			if (adjRearClear)
			{
				// notify
				ArbiterOutput.Output("Opposing Secondary: Adjacent and Rear Clear");

				LaneChangeParameters lcp = new LaneChangeParameters(
				true, true, arbiterLane, true, closestGood, false, false, null,
				3 * TahoeParams.VL, null, TurnDecorators.RightTurnDecorator,
				this.OpposingForwardMonitor.CurrentParamters.Value, xUpper, new Coordinates(), new Coordinates(),
				new Coordinates(), LaneChangeReason.Navigation);

				lcp.ForcedOpposing = forcedOpposing;

				ChangeLanesState cls = new ChangeLanesState(lcp);

				return new Maneuver(this.OpposingForwardMonitor.CurrentParamters.Value.Behavior, cls, lcp.Decorators, vehicleState.Timestamp);
			}
			else
			{
				if (this.rightLateralReasoning.AdjacentVehicle != null &&
					!this.rightLateralReasoning.AdjacentVehicle.IsStopped &&
					this.rightLateralReasoning.AdjacentVehicle.Speed > CoreCommon.Communications.GetVehicleSpeed().Value - 2.0)
				{
					// notify
					ArbiterOutput.Output("Opposing Secondary: Adjacent and Rear NOT Clear, ADJACENT NOT STOPPED, vAdj: " + this.rightLateralReasoning.AdjacentVehicle.Speed.ToString("f1"));

					TravelingParameters tp = this.OpposingForwardMonitor.CurrentParamters.Value;
					if (tp.Behavior is StayInLaneBehavior)
					{
						StayInLaneBehavior silb = (StayInLaneBehavior)tp.Behavior;
						silb.SpeedCommand = new ScalarSpeedCommand(0.0);
					}

					return new Maneuver(tp.Behavior, tp.NextState, tp.Decorators, vehicleState.Timestamp);
				}
				else if (this.rightLateralReasoning.AdjacentVehicle != null &&
					this.OpposingForwardMonitor.NaviationParameters.DistanceToGo < TahoeParams.VL * 4.0)
				{
					// notify
					ArbiterOutput.Output("Opposing Secondary: Adjacent and Rear NOT Clear, ADJACENT FILLED, too close to nav stop");

					TravelingParameters tp = this.OpposingForwardMonitor.CurrentParamters.Value;
					if (tp.Behavior is StayInLaneBehavior)
					{
						StayInLaneBehavior silb = (StayInLaneBehavior)tp.Behavior;
						silb.SpeedCommand = new ScalarSpeedCommand(0.0);
					}

					return new Maneuver(tp.Behavior, tp.NextState, tp.Decorators, vehicleState.Timestamp);
				}
				else if (this.rightLateralReasoning.AdjacentVehicle == null && !adjRearClear)
				{
					// notify
					ArbiterOutput.Output("Opposing Secondary: REAR NOT Clear, WAITING");

					TravelingParameters tp = this.OpposingForwardMonitor.CurrentParamters.Value;
					if (tp.Behavior is StayInLaneBehavior)
					{
						StayInLaneBehavior silb = (StayInLaneBehavior)tp.Behavior;
						silb.SpeedCommand = new ScalarSpeedCommand(0.0);
					}

					return new Maneuver(tp.Behavior, tp.NextState, tp.Decorators, vehicleState.Timestamp);
				}
				else
					return null;
			}
		}
	}
}
