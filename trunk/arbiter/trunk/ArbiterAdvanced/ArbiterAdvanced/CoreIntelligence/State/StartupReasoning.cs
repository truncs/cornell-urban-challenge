using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.State
{
	/// <summary>
	/// Help with starting up in unknown areas
	/// </summary>
	public class StartupReasoning
	{
		/// <summary>
		/// Lane agent of the behavioral layer
		/// </summary>
		private LaneAgent behavioralLaneAgent;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="laneAgent"></param>
		public StartupReasoning(LaneAgent laneAgent)
		{
			this.behavioralLaneAgent = laneAgent;
		}

		/// <summary>
		/// Startup the vehicle from a certain position, pass the next state back, 
		/// and initialize the lane agent if possible
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <returns></returns>
		public IState Startup(VehicleState vehicleState, CarMode carMode)
		{
			// check car mode
			if (carMode == CarMode.Run)
			{
				// check areas
				ArbiterLane al = CoreCommon.RoadNetwork.ClosestLane(vehicleState.Front);
				ArbiterZone az = CoreCommon.RoadNetwork.InZone(vehicleState.Front);
				ArbiterIntersection ain = CoreCommon.RoadNetwork.InIntersection(vehicleState.Front);
				ArbiterInterconnect ai = CoreCommon.RoadNetwork.ClosestInterconnect(vehicleState.Front, vehicleState.Heading);

				if (az != null)
				{
					// special zone startup
					return new ZoneStartupState(az);
				}
				
				if (ain != null)
				{
					if (al != null)
					{
						// check lane stuff
						PointOnPath lanePoint = al.PartitionPath.GetClosest(vehicleState.Front);

						// get distance from front of car
						double dist = lanePoint.pt.DistanceTo(vehicleState.Front);

						// check dist to lane
						if (dist < al.Width + 1.0)
						{
							// check orientation relative to lane
							Coordinates laneVector = al.GetClosestPartition(vehicleState.Front).Vector().Normalize();

							// get our heading
							Coordinates ourHeading = vehicleState.Heading.Normalize();

							// forwards or backwards
							bool forwards = true;

							// check dist to each other
							if (laneVector.DistanceTo(ourHeading) > Math.Sqrt(2.0))
							{
								// not going forwards
								forwards = false;
							}

							// stay in lane if forwards
							if (forwards)
							{
								ArbiterOutput.Output("Starting up in lane: " + al.ToString());
								return new StayInLaneState(al, new Probability(0.7, 0.3), true, CoreCommon.CorePlanningState);
							}
							else
							{
								// Opposing lane
								return new OpposingLanesState(al, true, CoreCommon.CorePlanningState, vehicleState);
							}
						}
					}

					// startup intersection state
					return new IntersectionStartupState(ain);
				}
				
				if (al != null)
				{
					// get a startup chute
					ArbiterLanePartition startupChute = this.GetStartupChute(vehicleState);

					// check if in a startup chute
					if (startupChute != null && !startupChute.IsInside(vehicleState.Front))
					{
						ArbiterOutput.Output("Starting up in chute: " + startupChute.ToString());
						return new StartupOffChuteState(startupChute);							 
					}
					// not in a startup chute
					else
					{
						PointOnPath lanePoint = al.PartitionPath.GetClosest(vehicleState.Front);

						// get distance from front of car
						double dist = lanePoint.pt.DistanceTo(vehicleState.Front);

						// check orientation relative to lane
						Coordinates laneVector = al.GetClosestPartition(vehicleState.Front).Vector().Normalize();

						// get our heading
						Coordinates ourHeading = vehicleState.Heading.Normalize();

						// forwards or backwards
						bool forwards = true;

						// check dist to each other
						if (laneVector.DistanceTo(ourHeading) > Math.Sqrt(2.0))
						{
							// not going forwards
							forwards = false;
						}

						// stay in lane if forwards
						if (forwards)
						{
							ArbiterOutput.Output("Starting up in lane: " + al.ToString());
							return new StayInLaneState(al, new Probability(0.7, 0.3), true, CoreCommon.CorePlanningState);
						}
						else
						{
							// opposing
							return new OpposingLanesState(al, true, CoreCommon.CorePlanningState, vehicleState);
						}
					}
				}

				// fell out
				ArbiterOutput.Output("Cannot find area to startup in");
				return CoreCommon.CorePlanningState;
			}
			else
			{
				return CoreCommon.CorePlanningState;
			}
		}

		/// <summary>
		/// Get partition of startup chute we might be upon
		/// </summary>
		/// <param name="vehicleState"></param>
		/// <returns></returns>
		public ArbiterLanePartition GetStartupChute(VehicleState vehicleState)
		{
			// current road network
			ArbiterRoadNetwork arn = CoreCommon.RoadNetwork;
						
			// get startup chutes
			List<ArbiterLanePartition> startupChutes = new List<ArbiterLanePartition>();
			foreach (ArbiterSegment asg in arn.ArbiterSegments.Values)
			{
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					foreach (ArbiterLanePartition alp in al.Partitions)
					{
						if (alp.Type == PartitionType.Startup)
							startupChutes.Add(alp);
					}
				}
			}

			// figure out which startup chute we are in
			foreach (ArbiterLanePartition alp in startupChutes)
			{
				// check if within 40m of it
				if(vehicleState.Front.DistanceTo(alp.Initial.Position) < 40.0)
				{
					// check orientation relative to lane
					Coordinates laneVector = alp.Vector().Normalize();

					// get our heading
					Coordinates ourHeading = vehicleState.Heading.Normalize();

					// check dist to each other to make sure forwards
					if (laneVector.DistanceTo(ourHeading) < Math.Sqrt(2.0))
					{
						// figure out extension
						Coordinates backC = alp.Initial.Position - alp.Vector().Normalize(40.0);

						// generate new line path
						LinePath lp = new LinePath(new Coordinates[] { backC, alp.Final.Position });
						LinePath lpL = lp.ShiftLateral(alp.Lane.Width / 2.0);
						LinePath lpR = lp.ShiftLateral(-alp.Lane.Width / 2.0);
						List<Coordinates> poly = new List<Coordinates>();
						poly.AddRange(lpL);
						poly.AddRange(lpR);
						Polygon chutePoly = Polygon.GrahamScan(poly);

						// check if we're inside the chute
						if (chutePoly.IsInside(vehicleState.Front))
						{
							// return the chute
							return alp;
						}
					}
				}
			}

			// fallout
			return null;
		}
	}
}
