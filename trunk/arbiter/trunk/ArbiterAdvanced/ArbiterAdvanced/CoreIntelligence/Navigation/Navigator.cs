using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.Communications;
using System.Threading;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using System.Diagnostics;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation
{
	/// <summary>
	/// Controls navigation
	/// </summary>
	public class Navigator
	{
		/// <summary>
		/// Thread to control blockage updates
		/// </summary>
		public Thread blockageUpdateThread;

		/// <summary>
		/// Non-blocking seed planner thread
		/// </summary>
		public Thread seedPlannerThread;

		/// <summary>
		/// Points of interest
		/// </summary>
		public KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>> currentTimes;

		/// <summary>
		/// Constructor
		/// </summary>
		public Navigator()
		{
		}

		#region Blockages

		/// <summary>
		/// Start updating blockages
		/// </summary>
		public void JumpstartBlockageThread()
		{
			ArbiterOutput.Output("Preprocessing Blockage Costs: Simple Version");

			// need to calculate blockage costs
			foreach (ArbiterSegment asg in CoreCommon.RoadNetwork.ArbiterSegments.Values)
			{
				//ArbiterOutput.Output("  Segment: " + asg.SegmentId.ToString());

				foreach (ArbiterWay aw in asg.Ways.Values)
				{
					foreach (ArbiterLane al in aw.Lanes.Values)
					{
						foreach (ArbiterLanePartition alp in al.Partitions)
						{
							this.SetBlockageCost(alp);
						}
					}
				}
			}

			ArbiterOutput.Output("Preprocessing of blockage costs completed");

			// blockage thread stuff
			blockageUpdateThread = new Thread(BlockageUpdate);
			blockageUpdateThread.IsBackground = true;
			blockageUpdateThread.Priority = ThreadPriority.Normal;
			blockageUpdateThread.Start();
		}

		/// <summary>
		/// Updates blockages every minute
		/// </summary>
		public void BlockageUpdate()
		{
			while (true)
			{
				try
				{
					foreach (ArbiterSegment asg in CoreCommon.RoadNetwork.ArbiterSegments.Values)
					{
						foreach (ArbiterLane al in asg.Lanes.Values)
						{
							foreach (ArbiterLanePartition alp in al.Partitions)
							{
								if (alp.Blockage.ComputeBlockageExists())
								{
									alp.Blockage.SecondsSinceObserved += 60.0;
								}
							}
						}
					}

					foreach (ArbiterInterconnect ai in CoreCommon.RoadNetwork.ArbiterInterconnects.Values)
					{
						if (ai.Blockage.ComputeBlockageExists())
							ai.Blockage.SecondsSinceObserved += 60.0;
					}
				}
				catch (Exception e)
				{
					ArbiterOutput.Output("Error in blockage update");
					ArbiterOutput.WriteToLog("Error: " + e.ToString() + "\n Stack:" + e.StackTrace.ToString());
				}

				Thread.Sleep(60000);
			}
		}

		/// <summary>
		/// Set blockage time cost for the partition
		/// </summary>
		/// <param name="partition"></param>
		/// <remarks>Should set interconnect blockage to be initial lane partition of next segment</remarks>
		public void SetBlockageCost(IConnectAreaWaypoints partition)
		{
			// default time
			partition.Blockage.BlockageTimeCost = 1800;

			#region Old

			/*

			#region Lane Partition

			// check if segment partition
			if (partition is ArbiterLanePartition)
			{
				// get partition
				ArbiterLanePartition alp = (ArbiterLanePartition)partition;

				// make sure the other way is valid
				bool otherValid = false;

				// check 1
				if (alp.Lane.Way.WayId.Number == 1)
				{
					if (alp.Lane.Way.Segment.Way2.IsValid)
						otherValid = true;
				}

				// check 2
				if (alp.Lane.Way.WayId.Number == 2)
				{
					if (alp.Lane.Way.Segment.Way1.IsValid)
						otherValid = true;
				}

				// check other way valid
				if (otherValid)
				{
					// determine adjacent partitions
					List<NavigableEdge> toRemove = this.AdjacentPartitions(alp);

					// get final waypoint of partition in other segment closest to partition's initial waypoint to plan from
					ArbiterWay otherWay = null;

					// check 1
					if (alp.Lane.Way.WayId.Number == 1)
						otherWay = alp.Lane.Way.Segment.Way2;

					// check 2
					if (alp.Lane.Way.WayId.Number == 2)
						otherWay = alp.Lane.Way.Segment.Way1;

					// get lanes of other way
					ArbiterLane al = null;
					foreach (ArbiterLane alTmp in otherWay.Lanes.Values)
					{
						al = alTmp;
					}

					// get closest waypoint
					ArbiterWaypoint awOther = al.GetClosestPartition(alp.Initial.Position).Final;

					// get replan point
					List<DownstreamPointOfInterest> dpoi = this.Downstream(awOther.PreviousPartition.Initial.Position + awOther.PreviousPartition.Vector().Normalize(awOther.PreviousPartition.Length / 2.0), awOther.Lane.Way, true, new List<ArbiterWaypoint>());
					ArbiterWaypoint nextOtherExit = dpoi != null && dpoi.Count > 0 ? dpoi[0].PointOfInterest : null;

					// get next entry into lane
					ArbiterWaypoint nextEntry = this.NextEntry(alp.Final);

					if (nextEntry == null || nextOtherExit == null)
					{
						// default time
						partition.Blockage.BlockageTimeCost = 10000;
					}
					else
					{
						// plan and set time
						aStar planner = new aStar(nextOtherExit, nextEntry, toRemove);
						planner.Plan();
						partition.Blockage.BlockageTimeCost = planner.TotalTime;
					}
				}
				else
				{
					// default time
					partition.Blockage.BlockageTimeCost = 10000;
				}
			}

			#endregion

			*/

			#endregion
		}

		/// <summary>
		/// Add blockage to partition
		/// </summary>
		/// <param name="partition"></param>
		/// <param name="c"></param>
		public void AddBlockage(IConnectAreaWaypoints partition, Coordinates c, bool acrossSegment)
		{
			partition.Blockage.BlockageExists = true;
			partition.Blockage.BlockageCoordinates = c;
			partition.Blockage.SecondsSinceObserved = 0.0;
			partition.Blockage.BlockageTimeCost = 1800;

			if (partition.Blockage.BlockageHasExisted)
				partition.Blockage.BlockageLifetime = partition.Blockage.BlockageLifetime * 4.0;

			if (acrossSegment && partition is ArbiterLanePartition)
			{
				foreach (ArbiterLanePartition alp in ((ArbiterLanePartition)partition).NonLaneAdjacentPartitions)
				{
					if (alp.IsInside(c))
					{
						alp.Blockage.BlockageExists = true;
						alp.Blockage.BlockageCoordinates = c;
						alp.Blockage.SecondsSinceObserved = 0.0;
						alp.Blockage.BlockageTimeCost = 1800;

						if (alp.Blockage.BlockageHasExisted)
							alp.Blockage.BlockageLifetime = partition.Blockage.BlockageLifetime * 4.0;
					}
				}
			}

			// reset navigation costs
			this.currentTimes = new KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>>();
		}

		/// <summary>
		/// Add a costly blockage across the road
		/// </summary>
		/// <param name="partition"></param>
		/// <param name="c"></param>
		public void AddHarshBlockageAcrossSegment(ArbiterLanePartition partition, Coordinates c)
		{
			partition.Blockage.BlockageExists = true;
			partition.Blockage.BlockageCoordinates = c;
			partition.Blockage.SecondsSinceObserved = 0.0;
			partition.Blockage.BlockageTimeCost = 3600;
			partition.Blockage.BlockageLifetime = partition.Blockage.BlockageLifetime * 4.0;
			
			foreach (ArbiterLanePartition alp in ((ArbiterLanePartition)partition).NonLaneAdjacentPartitions)
			{
				if (alp.IsInside(c))
				{
					alp.Blockage.BlockageExists = true;
					alp.Blockage.BlockageCoordinates = c;
					alp.Blockage.SecondsSinceObserved = 0.0;
					alp.Blockage.BlockageTimeCost = 3600;
					alp.Blockage.BlockageLifetime = alp.Blockage.BlockageLifetime * 4.0;
				}
			}

			// reset navigation costs
			this.currentTimes = new KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>>();
		}

		/// <summary>
		/// Remove blockage from a partition
		/// </summary>
		/// <param name="partition"></param>
		public void RemoveBlockage(IConnectAreaWaypoints partition)
		{
			partition.Blockage.BlockageExists = false;
			partition.Blockage.SecondsSinceObserved = 0.0;

			if (partition is ArbiterLanePartition)
			{
				foreach (ArbiterLanePartition alp in ((ArbiterLanePartition)partition).NonLaneAdjacentPartitions)
				{
					if (alp.Blockage.BlockageExists && alp.Blockage.BlockageCoordinates.DistanceTo(partition.Blockage.BlockageCoordinates) < 1.0)
					{
						alp.Blockage.BlockageExists = false;
						alp.Blockage.SecondsSinceObserved = 0.0;
					}
				}
			}
		}

		/// <summary>
		/// Add blockage to interconnect
		/// </summary>
		/// <param name="arbiterInterconnect"></param>
		public void AddInterconnectBlockage(ArbiterInterconnect arbiterInterconnect)
		{
			this.AddBlockage(arbiterInterconnect, CoreCommon.Communications.GetVehicleState().Position, false);
		}

		/// <summary>
		/// Try to plan the intersection heavily penalizing the interconnect
		/// </summary>
		/// <param name="iTraversableWaypoint"></param>
		/// <param name="iArbiterWaypoint"></param>
		/// <param name="arbiterInterconnect"></param>
		/// <returns></returns>
		public IntersectionPlan PlanIntersectionWithoutInterconnect(ITraversableWaypoint exit, IArbiterWaypoint goal, ArbiterInterconnect interconnect)
		{
			// save old blockage
			NavigationBlockage tmpBlockage = interconnect.Blockage;

			// create new
			NavigationBlockage newerBlockage = new NavigationBlockage(Double.MaxValue);
			newerBlockage.BlockageExists = true;
			interconnect.Blockage = newerBlockage;

			KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>> tmpCurrentTimes = currentTimes;
			this.currentTimes = new KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>>();

			// plan
			IntersectionPlan ip = this.PlanIntersection(exit, goal);

			this.currentTimes = tmpCurrentTimes;

			// reset interconnect blockage
			interconnect.Blockage = tmpBlockage;

			// return plan
			return ip;
		}

		/// <summary>
		/// Try to plan the intersection heavily penalizing the interconnect
		/// </summary>
		/// <param name="iTraversableWaypoint"></param>
		/// <param name="iArbiterWaypoint"></param>
		/// <param name="arbiterInterconnect"></param>
		/// <returns></returns>
		public IntersectionPlan PlanIntersectionWithoutInterconnect(ITraversableWaypoint exit, IArbiterWaypoint goal, ArbiterInterconnect interconnect, bool blockAllRelated)
		{
			ITraversableWaypoint entry = (ITraversableWaypoint)interconnect.FinalGeneric;

			if (!blockAllRelated)
				return this.PlanIntersectionWithoutInterconnect(exit, goal, interconnect);
			else
			{
				Dictionary<IConnectAreaWaypoints, NavigationBlockage> saved = new Dictionary<IConnectAreaWaypoints, NavigationBlockage>();
				if (entry.IsEntry)
				{
					foreach (ArbiterInterconnect ai in entry.Entries)
					{
						saved.Add(ai, ai.Blockage);

						// create new
						NavigationBlockage newerBlockage = new NavigationBlockage(Double.MaxValue);
						newerBlockage.BlockageExists = true;
						ai.Blockage = newerBlockage;
					}
				}

				if (entry is ArbiterWaypoint && ((ArbiterWaypoint)entry).PreviousPartition != null)
				{
					ArbiterLanePartition alp = ((ArbiterWaypoint)entry).PreviousPartition;
					saved.Add(alp, alp.Blockage);

					// create new
					NavigationBlockage newerBlockage = new NavigationBlockage(Double.MaxValue);
					newerBlockage.BlockageExists = true;
					alp.Blockage = newerBlockage;
				}

				KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>> tmpCurrentTimes = currentTimes;
				this.currentTimes = new KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>>();

				// plan
				IntersectionPlan ip = this.PlanIntersection(exit, goal);

				this.currentTimes = tmpCurrentTimes;

				// reset the blockages
				foreach (KeyValuePair<IConnectAreaWaypoints, NavigationBlockage> savedPair in saved)
					savedPair.Key.Blockage = savedPair.Value;

				// return plan
				return ip;
			}
		}

		/// <summary>
		/// Get a road plan while setting partition costs very high
		/// </summary>
		/// <param name="partition"></param>
		/// <param name="goal"></param>
		/// <param name="blockAdjacent"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public RoadPlan PlanRoadOppositeWithoutPartition(ArbiterLanePartition partition, ArbiterLanePartition opposite, IArbiterWaypoint goal, bool blockAdjacent, Coordinates c, bool sameWay)
		{
			KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>> tmpCurrentTimes = currentTimes;
			this.currentTimes = new KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>>();
			RoadPlan rp = null;

			if (!blockAdjacent)
			{
				NavigationBlockage nb = partition.Blockage;
				NavigationBlockage tmp = new NavigationBlockage(double.MaxValue);
				partition.Blockage = tmp;
				rp = this.PlanNavigableArea(partition.Lane, c, goal, new List<ArbiterWaypoint>());
				partition.Blockage = nb;
			}
			else
			{
				// save
				List<KeyValuePair<ArbiterLanePartition, NavigationBlockage>> savedBlockages = new List<KeyValuePair<ArbiterLanePartition, NavigationBlockage>>();

				// set
				savedBlockages.Add(new KeyValuePair<ArbiterLanePartition,NavigationBlockage>(partition, partition.Blockage));
				// create new
				NavigationBlockage anewerBlockage = new NavigationBlockage(Double.MaxValue);
				anewerBlockage.BlockageExists = true;
				partition.Blockage = anewerBlockage;

				foreach (ArbiterLanePartition alp in partition.NonLaneAdjacentPartitions)
				{
					if (alp.IsInside(c) && (!sameWay || (sameWay && partition.Lane.Way.Equals(alp.Lane.Way))))
					{
						savedBlockages.Add(new KeyValuePair<ArbiterLanePartition,NavigationBlockage>(alp, alp.Blockage));
					
						// create new
						NavigationBlockage newerBlockage = new NavigationBlockage(Double.MaxValue);
						newerBlockage.BlockageExists = true;
						alp.Blockage = newerBlockage;
					}
				}

				// plan
				rp = this.PlanNavigableArea(opposite.Lane, c, goal, new List<ArbiterWaypoint>());

				// restore
				foreach (KeyValuePair<ArbiterLanePartition, NavigationBlockage> saved in savedBlockages)
				{
					saved.Key.Blockage = saved.Value;
				}
			}

			// restore
			this.currentTimes = tmpCurrentTimes;

			// return
			return rp;
		}

		#endregion

		#region Travel Area Planner

		/// <summary>
		/// Plan given that we are starting on a road
		/// </summary>
		/// <param name="currentLane"></param>
		/// <param name="currentPosition"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		public RoadPlan PlanNavigableArea(INavigableTravelArea currentArea, Coordinates currentPosition, INavigableNode goal, List<ArbiterWaypoint> ignorable)
		{
			// get all downstream points of interest as well as the goal point of interest
			List<DownstreamPointOfInterest> downstreamPoints = currentArea.Downstream(currentPosition, ignorable, goal);			

			// so, for each exit downstream we need to plan from the end of each interconnect to the goal
			this.RouteTimes(downstreamPoints, goal);

			// get road plan
			RoadPlan rp = this.GetRoadPlan(downstreamPoints);

			#region Output

			// update arbiter information
			List<RouteInformation> routeInfo = rp.RouteInformation(currentPosition);

			// make sure we're in a road state
			if (CoreCommon.CorePlanningState == null || CoreCommon.CorePlanningState is UrbanChallenge.Arbiter.Core.Common.State.TravelState)
			{
				// check route 1
				if (routeInfo.Count > 0)
				{
					RouteInformation ri = routeInfo[0];
					CoreCommon.CurrentInformation.Route1 = ri;
					CoreCommon.CurrentInformation.Route1Time = ri.RouteTimeCost.ToString("F6");
					CoreCommon.CurrentInformation.Route1Wp = ri.Waypoint;
				}
			}

			#endregion

			// return road plan
			return rp;
		}

		/// <summary>
		/// Gets the road plan
		/// </summary>
		/// <param name="pointsOfInterest"></param>
		/// <returns></returns>
		private RoadPlan GetRoadPlan(List<DownstreamPointOfInterest> pointsOfInterest)
		{
			// road plan
			RoadPlan rp = new RoadPlan(new Dictionary<ArbiterLaneId, LanePlan>());

			// lane assigned points
			Dictionary<ArbiterLaneId, List<DownstreamPointOfInterest>> lanePoints = new Dictionary<ArbiterLaneId, List<DownstreamPointOfInterest>>();

			// assign dpoi to lanes
			foreach (DownstreamPointOfInterest dpoi in pointsOfInterest)
			{
				if (lanePoints.ContainsKey(dpoi.PointOfInterest.Lane.LaneId))
				{
					lanePoints[dpoi.PointOfInterest.Lane.LaneId].Add(dpoi);
				}
				else
				{
					lanePoints.Add(dpoi.PointOfInterest.Lane.LaneId, new List<DownstreamPointOfInterest>());
					lanePoints[dpoi.PointOfInterest.Lane.LaneId].Add(dpoi);
				}
			}

			// find the best in each lane
			foreach (KeyValuePair<ArbiterLaneId, List<DownstreamPointOfInterest>> pairs in lanePoints)
			{
				List<DownstreamPointOfInterest> dpois = pairs.Value;
				dpois.Sort();

				// choose best and add
				rp.LanePlans.Add(pairs.Key, new LanePlan(dpois[0]));
			}

			// return 
			return rp;
		}

		/// <summary>
		/// Route ploan for downstream exits
		/// </summary>
		/// <param name="downstreamPoints"></param>
		/// <param name="goal"></param>
		private void RouteTimes(List<DownstreamPointOfInterest> downstreamPoints, INavigableNode goal)
		{
			// check if we are planning over the correct goal
			if (this.currentTimes.Key != CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber
				|| this.currentTimes.Value == null)
			{
				// create new lookup
				this.currentTimes = new KeyValuePair<int, Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>>(
					CoreCommon.Mission.MissionCheckpoints.Peek().CheckpointNumber, new Dictionary<ArbiterWaypointId, DownstreamPointOfInterest>());
			}

			// so, for each exit downstream we need to plan from the end of each interconnect to the goal
			foreach (DownstreamPointOfInterest dpoi in downstreamPoints)
			{
				// container flag
				bool contains = this.currentTimes.Value.ContainsKey(dpoi.PointOfInterest.WaypointId);

				// check if exit
				if (dpoi.IsExit && !contains)
				{
					// fake node
					FakeExitNode fen = new FakeExitNode(dpoi.PointOfInterest);

					// init fields
					double timeCost;
					List<INavigableNode> routeNodes;

					// plan
					this.Plan(fen, goal, out routeNodes, out timeCost);

					// set best
					dpoi.RouteTime = timeCost;
					dpoi.BestRoute = routeNodes;
					dpoi.BestExit = routeNodes.Count > 1 ? fen.GetEdge(routeNodes[1]) : null;

					// add to keepers
					this.currentTimes.Value.Add(dpoi.PointOfInterest.WaypointId, dpoi.Clone());
				}
				else if (dpoi.IsExit && contains)
				{
					DownstreamPointOfInterest tmp = this.currentTimes.Value[dpoi.PointOfInterest.WaypointId];

					if (tmp.BestExit == null)
					{
						ArbiterOutput.Output("NAV RouteTimes: Removing exit with no valid route: " + dpoi.PointOfInterest.WaypointId.ToString());

						// remove
						this.currentTimes.Value.Remove(dpoi.PointOfInterest.WaypointId);
						dpoi.PointOfInterest = (ArbiterWaypoint)CoreCommon.RoadNetwork.ArbiterWaypoints[dpoi.PointOfInterest.WaypointId];

						// fake node
						FakeExitNode fen = new FakeExitNode(dpoi.PointOfInterest);

						// init fields
						double timeCost;
						List<INavigableNode> routeNodes;

						// plan
						this.Plan(fen, goal, out routeNodes, out timeCost);

						// set best
						dpoi.RouteTime = timeCost;
						dpoi.BestRoute = routeNodes;
						dpoi.BestExit = routeNodes.Count > 1 ? fen.GetEdge(routeNodes[1]) : null;
						
						// add to keepers
						this.currentTimes.Value.Add(dpoi.PointOfInterest.WaypointId, dpoi);
					}
					else
					{
						dpoi.RouteTime = tmp.RouteTime;
						dpoi.BestRoute = tmp.BestRoute;
						dpoi.BestExit = tmp.BestExit;
					}
				}			
			}
		}

		/// <summary>
		/// Seed the planner, making it non blocking
		/// </summary>
		/// <param name="downstreamPoints"></param>
		/// <param name="goal"></param>
		/// <param name="way"></param>
		private void SeedPlanner(List<DownstreamPointOfInterest> downstreamPoints, INavigableNode goal)
		{
			seedPlannerThread = new Thread(
				delegate()
				{
					this.RouteTimes(downstreamPoints, goal);					
				}
			);

			seedPlannerThread.IsBackground = true;
			seedPlannerThread.Priority = ThreadPriority.AboveNormal;
			seedPlannerThread.Start();
		}

		/// <summary>
		/// Seed the road planner
		/// </summary>
		/// <param name="seedLane"></param>
		/// <param name="seedPosition"></param>
		/// <param name="goal"></param>
		/// <param name="ignorable"></param>
		public void SeedRoadPlanner(INavigableTravelArea seed, Coordinates seedPosition, INavigableNode goal, List<ArbiterWaypoint> ignorable)
		{
			// get all downstream points of interest
			List<DownstreamPointOfInterest> downstreamPoints = seed.Downstream(seedPosition, ignorable, goal);

			// so, for each exit downstream we need to plan from the end of each interconnect to the goal
			this.SeedPlanner(downstreamPoints, goal);
		}

		/// <summary>
		/// Plan in a zone
		/// </summary>
		/// <param name="az"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		public ZonePlan PlanZone(ArbiterZone az, INavigableNode start, INavigableNode goal)
		{
			// zone plan
			ZonePlan zp = new ZonePlan();
			zp.Zone = az;
			zp.Start = start;

			// given start and goal, get plan
			double time;
			List<INavigableNode> nodes;
			this.Plan(start, goal, out nodes, out time);

			zp.Time = time;

			// final path
			LinePath recommendedPath = new LinePath();
			List<INavigableNode> pathNodes = new List<INavigableNode>();

			// start and end counts
			int startCount = 0;
			int endCount = 0;

			// check start type
			if(start is ArbiterParkingSpotWaypoint)
				startCount = 2;

			// check end type
			if(goal is ArbiterParkingSpotWaypoint && 
				((ArbiterParkingSpotWaypoint)goal).ParkingSpot.Zone.Equals(az))
				endCount = -2;

			// loop through nodes
			for(int i = startCount; i < nodes.Count + endCount; i++)
			{
				// go to parking spot or endpoint
				if(nodes[i] is ArbiterParkingSpotWaypoint)
				{
					// set zone goal
					zp.ZoneGoal = ((ArbiterParkingSpotWaypoint)nodes[i]).ParkingSpot.Checkpoint;
					
					// set path, return
					zp.RecommendedPath = recommendedPath;
					zp.PathNodes = pathNodes;

					// set route info
					RouteInformation ri = new RouteInformation(recommendedPath, time, goal.ToString());
					CoreCommon.CurrentInformation.Route1 = ri;
				
					// return the plan
					return zp;
				}
				// go to perimeter waypoint if this is one
				else if(nodes[i] is ArbiterPerimeterWaypoint && ((ArbiterPerimeterWaypoint)nodes[i]).IsExit)
				{
					// add
					recommendedPath.Add(nodes[i].Position);

					// set zone goal
					zp.ZoneGoal = nodes[i];
					
					// set path, return
					zp.RecommendedPath = recommendedPath;
					zp.PathNodes = pathNodes;

					// set route info
					RouteInformation ri = new RouteInformation(recommendedPath, time, goal.ToString());
					CoreCommon.CurrentInformation.Route1 = ri;
				
					// return the plan
					return zp;
				}
				// otherwise just add
				else
				{
					// add
					recommendedPath.Add(nodes[i].Position);
					pathNodes.Add(nodes[i]);
				}
			}

			// set zone goal
			zp.ZoneGoal = goal;
			
			// set path, return
			zp.RecommendedPath = recommendedPath;

			// set route info
			CoreCommon.CurrentInformation.Route1 = new RouteInformation(recommendedPath, time, goal.ToString());
		
			// return the plan
			return zp;
		}

		/// <summary>
		/// Plans over an intersection
		/// </summary>
		/// <param name="exitWaypoint"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		public IntersectionPlan PlanIntersection(ITraversableWaypoint exitWaypoint, INavigableNode goal)
		{
			// road plan if the itnersection has a road available to take from it
			RoadPlan rp = null;

			// check if road waypoint
			if (exitWaypoint is ArbiterWaypoint)
			{
				// get exit
				ArbiterWaypoint awp = (ArbiterWaypoint)exitWaypoint;

				// check if it has lane partition moving outwards
				if (awp.NextPartition != null)
				{
					// road plan ignoring exit
					List<ArbiterWaypoint> iws = RoadToolkit.WaypointsClose(awp.Lane.Way, awp.Position, awp);
					rp = this.PlanNavigableArea(awp.Lane, awp.Position, goal, iws);
				}
			}

			// get exit plan
			IntersectionPlan ip = this.GetIntersectionExitPlan(exitWaypoint, goal);

			// add road plan if exists
			ip.SegmentPlan = rp;

			// return the plan
			return ip;
		}

		/// <summary>
		/// Plan over all exits
		/// </summary>
		/// <param name="exitWaypoint"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		public IntersectionPlan GetIntersectionExitPlan(ITraversableWaypoint exitWaypoint, INavigableNode goal)
		{
			// initialize the intersection plan
			IntersectionPlan ip = new IntersectionPlan(exitWaypoint, new List<PlanableInterconnect>(), null);
			ip.ExitWaypoint = exitWaypoint;

			//check valid exit
			if (exitWaypoint.IsExit)
			{
				// plan over each interconnect
				foreach (ArbiterInterconnect ai in exitWaypoint.Exits)
				{
					// start of plan is the final wp of itner
					INavigableNode start = (INavigableNode)ai.FinalGeneric;

					// plan
					double time;
					List<INavigableNode> nodes;
					this.Plan(start, goal, out nodes, out time);
					time += ai.TimeCost();

					// create planned interconnect
					PlanableInterconnect pi = new PlanableInterconnect(ai, time, nodes);

					// add planned interconnect to the intersection plan
					ip.PossibleEntries.Add(pi);
				}
			}

			// return the plan
			return ip;
		}

		/// <summary>
		/// Determine intersection startup costs
		/// </summary>
		/// <param name="entries"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		public IntersectionStartupPlan PlanIntersectionStartup(IEnumerable<ITraversableWaypoint> entries, INavigableNode goal)
		{
			Dictionary<ITraversableWaypoint, double> entryCosts = new Dictionary<ITraversableWaypoint, double>();
			foreach (ITraversableWaypoint start in entries)
			{
				// given start and goal, get plan
				double time;
				List<INavigableNode> nodes;
				this.Plan(start, goal, out nodes, out time);
				entryCosts.Add(start, time);
			}
			return new IntersectionStartupPlan(entryCosts);
		}

		#endregion

		#region Old

		/// <summary>
		/// Plan given that we are starting on a road
		/// </summary>
		/// <param name="currentLane"></param>
		/// <param name="currentPosition"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		public RoadPlan PlanRoads(ArbiterLane currentLane, Coordinates currentPosition, INavigableNode goal, List<ArbiterWaypoint> ignorable)
		{
			// get all downstream points of interest
			List<DownstreamPointOfInterest> downstreamPoints = new List<DownstreamPointOfInterest>();

			// get exits downstream from this current position in the way
			downstreamPoints.AddRange(this.Downstream(currentPosition, currentLane.Way, true, ignorable));

			// determine if goal is downstream in a specific lane, add to possible route times to consider
			DownstreamPointOfInterest goalDownstream = this.IsGoalDownStream(currentLane.Way, currentPosition, goal);

			// add goal to points downstream if it exists
			if (goalDownstream != null)
				downstreamPoints.Add(goalDownstream);

			// so, for each exit downstream we need to plan from the end of each interconnect to the goal
			this.DetermineDownstreamPointRouteTimes(downstreamPoints, goal, currentLane.Way);

			// get road plan
			RoadPlan rp = this.GetRoadPlan(downstreamPoints, currentLane.Way);

			// update arbiter information
			List<RouteInformation> routeInfo = rp.RouteInformation(currentPosition);

			// make sure we're in a road state
			if (CoreCommon.CorePlanningState == null || 
				CoreCommon.CorePlanningState is TravelState ||
				CoreCommon.CorePlanningState is TurnState)
			{
				// check route 1
				if (routeInfo.Count > 0)
				{
					RouteInformation ri = routeInfo[0];
					CoreCommon.CurrentInformation.Route1 = ri;
					CoreCommon.CurrentInformation.Route1Time = ri.RouteTimeCost.ToString("F6");
					CoreCommon.CurrentInformation.Route1Wp = ri.Waypoint;
				}

				// check route 2
				if (routeInfo.Count > 1)
				{
					RouteInformation ri = routeInfo[1];
					CoreCommon.CurrentInformation.Route2 = ri;
					CoreCommon.CurrentInformation.Route2Time = ri.RouteTimeCost.ToString("F6");
					CoreCommon.CurrentInformation.Route2Wp = ri.Waypoint;
				}
			}

			// return road plan
			return rp;
		}

		/// <summary>
		/// Gets the road plan
		/// </summary>
		/// <param name="pointsOfInterest"></param>
		/// <returns></returns>
		private RoadPlan GetRoadPlan(List<DownstreamPointOfInterest> pointsOfInterest, ArbiterWay way)
		{
			// road plan
			RoadPlan rp = new RoadPlan(new Dictionary<ArbiterLaneId, LanePlan>());			

			// find the best in each lane
			foreach (ArbiterLane al in way.Lanes.Values)
			{
				// points in lane
				List<DownstreamPointOfInterest> lanePoints = new List<DownstreamPointOfInterest>();

				// loop over all points
				foreach (DownstreamPointOfInterest dpoi in pointsOfInterest)
				{
					if (dpoi.PointOfInterest.Lane.Equals(al))
						lanePoints.Add(dpoi);
				}

				// if exist any
				if (lanePoints.Count > 0)
				{
					// sort
					lanePoints.Sort();

					// choose best and add
					rp.LanePlans.Add(al.LaneId, new LanePlan(lanePoints[0]));
				}
			}

			// return 
			return rp;
		}

		/// <summary>
		/// Route ploan for downstream exits
		/// </summary>
		/// <param name="downstreamPoints"></param>
		/// <param name="goal"></param>
		private void DetermineDownstreamPointRouteTimes(List<DownstreamPointOfInterest> downstreamPoints, INavigableNode goal, ArbiterWay aw)
		{
			// so, for each exit downstream we need to plan from the end of each interconnect to the goal
			foreach (DownstreamPointOfInterest dpoi in downstreamPoints)
			{
				// check if exit
				if (dpoi.IsExit)
				{
					// current best time
					double bestCurrent = Double.MaxValue;
					List<INavigableNode> bestRoute = null;
					ArbiterInterconnect bestInterconnect = null;

					// fake node
					FakeExitNode fen = new FakeExitNode(dpoi.PointOfInterest);

					// init fields
					double timeCost;
					List<INavigableNode> routeNodes;

					// plan
					this.Plan(fen, goal, out routeNodes, out timeCost);
										
					bestCurrent = timeCost;
					bestRoute = routeNodes;
					bestInterconnect = routeNodes.Count > 1 ? fen.GetEdge(routeNodes[1]) : null;

					// plan from each interconnect to find the best time from exit
					/*foreach (ArbiterInterconnect ai in dpoi.PointOfInterest.Exits)
					{
						// init fields
						double timeCost;
						List<INavigableNode> routeNodes;

						// plan
						this.Plan(ai.End, goal, out routeNodes, out timeCost);						

						// check
						if (timeCost < bestCurrent)
						{
							bestCurrent = timeCost;
							bestRoute = routeNodes;
							bestInterconnect = ai;
						}
					}*/

					// set best
					dpoi.RouteTime = bestCurrent;
					dpoi.BestRoute = bestRoute;
					dpoi.BestExit = bestInterconnect;
				}
			}
		
		}

		/// <summary>
		/// Checks if a goal is downstream on the current road
		/// </summary>
		/// <param name="way"></param>
		/// <param name="currentPosition"></param>
		/// <param name="goal"></param>
		/// <returns></returns>
		private DownstreamPointOfInterest IsGoalDownStream(ArbiterWay way, Coordinates currentPosition, INavigableNode goal)
		{
			if (goal is ArbiterWaypoint)
			{
				ArbiterWaypoint goalWaypoint = (ArbiterWaypoint)goal;

				if (goalWaypoint.Lane.Way.Equals(way))
				{
					foreach (ArbiterLane lane in way.Lanes.Values)
					{
						if (lane.Equals(goalWaypoint.Lane))
						{
							Coordinates current = lane.GetClosest(currentPosition);
							Coordinates goalPoint = lane.GetClosest(goal.Position);

							if (lane.IsInside(current) || currentPosition.DistanceTo(lane.LanePath().StartPoint.Location) < 1.0)
							{
								double distToGoal = lane.DistanceBetween(current, goalPoint);

								if (distToGoal > 0)
								{
									DownstreamPointOfInterest dpoi = new DownstreamPointOfInterest();
									dpoi.DistanceToPoint = distToGoal;
									dpoi.IsGoal = true;
									dpoi.IsExit = false;
									dpoi.PointOfInterest = goalWaypoint;
									dpoi.TimeCostToPoint = this.TimeCostInLane(lane.GetClosestPartition(currentPosition).Initial, goalWaypoint);
									dpoi.RouteTime = 0.0;
									return dpoi;
								}
							}
						}
						else
						{
							Coordinates current = lane.GetClosest(currentPosition);
							Coordinates goalPoint = lane.GetClosest(goal.Position);

							if ((lane.IsInside(current) || currentPosition.DistanceTo(lane.LanePath().StartPoint.Location) < 1.0) && lane.IsInside(goalPoint))
							{
								double distToGoal = lane.DistanceBetween(current, goalPoint);

								if (distToGoal > 0)
								{
									DownstreamPointOfInterest dpoi = new DownstreamPointOfInterest();
									dpoi.DistanceToPoint = distToGoal;
									dpoi.IsGoal = true;
									dpoi.IsExit = false;
									dpoi.PointOfInterest = goalWaypoint;
									dpoi.TimeCostToPoint = this.TimeCostInLane(lane.GetClosestPartition(currentPosition).Initial, goalWaypoint);
									dpoi.RouteTime = 0.0;
									//return dpoi;
								}
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets time cost between two waypoints in the same lane
		/// </summary>
		/// <param name="initial"></param>
		/// <param name="final"></param>
		/// <returns></returns>
		private double TimeCostInLane(ArbiterWaypoint initial, ArbiterWaypoint final)
		{
			ArbiterWaypoint current = initial;
			double cost = 0.0;

			while (current != null)
			{
				if (current.Equals(final))
				{
					return cost;
				}
				else
				{
					if (current.NextPartition == null)
						return cost;

					cost += current.NextPartition.TimeCost();

					if (current.NextPartition != null)
						current = current.NextPartition.Final;
					else
						current = null;
				}
			}

			return Double.MaxValue;
		}

		/// <summary>
		/// Gets all exits downstream from a point
		/// </summary>
		/// <param name="position"></param>
		/// <param name="way"></param>
		/// <param name="exits">We are looking for exits</param>
		/// <returns></returns>
		private List<DownstreamPointOfInterest> Downstream(Coordinates position, ArbiterWay way, bool exits, List<ArbiterWaypoint> ignorable)
		{
			List<DownstreamPointOfInterest> waypoints = new List<DownstreamPointOfInterest>();

			foreach (ArbiterLane al in way.Lanes.Values)
			{
				LinePath.PointOnPath pop = al.GetClosestPoint(position);

				if (al.IsInside(position) || position.DistanceTo(al.LanePath().StartPoint.Location) < 1.0)
				{					
					ArbiterLanePartition currentPartition = al.GetClosestPartition(position);
					ArbiterWaypoint initial = currentPartition.Final;
					double initialCost = position.DistanceTo(currentPartition.Final.Position) / way.Segment.SpeedLimits.MaximumSpeed;

					do
					{
						if(((exits && currentPartition.Final.IsExit) || (!exits && currentPartition.Final.IsEntry)) && !ignorable.Contains(currentPartition.Final))
						{
							double timeCost = initialCost + this.TimeCostInLane(initial, currentPartition.Final);
							DownstreamPointOfInterest dpoi = new DownstreamPointOfInterest();
							dpoi.DistanceToPoint = al.LanePath().DistanceBetween(pop, al.GetClosestPoint(currentPartition.Final.Position));
							dpoi.IsExit = true;
							dpoi.IsGoal = false;
							dpoi.PointOfInterest = currentPartition.Final;
							dpoi.TimeCostToPoint = timeCost;
							waypoints.Add(dpoi);
						}
						
						currentPartition = currentPartition.Final.NextPartition;
					}
					while(currentPartition != null);
				}
			}

			return waypoints;
		}

		/// <summary>
		/// Gets adjacent lane partitions
		/// </summary>
		/// <param name="alp"></param>
		/// <returns></returns>
		private List<NavigableEdge> AdjacentPartitions(ArbiterLanePartition alp)
		{
			List<NavigableEdge> edges = new List<NavigableEdge>();
			edges.Add(alp);
			List<ArbiterLanePartition> pars = alp.NonLaneAdjacentPartitions;

			foreach (ArbiterLanePartition alps in pars)
				edges.Add(alps);

			return edges;
		}

		/// <summary>
		/// Gets next entry downstream from this waypoint
		/// </summary>
		/// <param name="waypoint"></param>
		/// <returns></returns>
		private ArbiterWaypoint NextEntry(ArbiterWaypoint waypoint)
		{
			if (waypoint.NextPartition != null)
			{
				List<DownstreamPointOfInterest> entriesDownstream = this.Downstream(waypoint.Position, waypoint.Lane.Way, false, new List<ArbiterWaypoint>());
				return entriesDownstream.Count > 0 ? entriesDownstream[0].PointOfInterest : null;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Plan between two nodes
		/// </summary>
		/// <param name="start"></param>
		/// <param name="goal"></param>
		/// <param name="nodes"></param>
		/// <param name="time"></param>
		public void Plan(INavigableNode start, INavigableNode goal, out List<INavigableNode> nodes, out double time)
		{
			aStar astar = new aStar(start, goal, new List<NavigableEdge>());
			nodes = astar.Plan();
			time = astar.TotalTime;
		}
		
		#endregion
	}
}
