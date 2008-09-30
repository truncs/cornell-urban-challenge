using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Intersection;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Zone;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Blockage;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical
{
	/// <summary>
	/// Directs all tactical layers
	/// </summary>
	public class TacticalDirector
	{
		public RoadTactical roadTactical;
		public IntersectionTactical intersectionTactical;
		public ZoneTactical zoneTactical;
		public BlockageTactical blockageTactical;

		/// <summary>
		/// Valid vehicles for this timestep
		/// </summary>
		public static Dictionary<int, VehicleAgent> ValidVehicles;

		/// <summary>
		/// Valid vehicles for this timestep
		/// </summary>
		public static Dictionary<int, VehicleAgent> OccludedVehicles;

		/// <summary>
		/// Map of areas to vehicles in those areas
		/// </summary>
		public static Dictionary<IVehicleArea, List<VehicleAgent>> VehicleAreas;

		/// <summary>
		/// Vehicles new to the ai this planning iteration
		/// </summary>
		public static List<VehicleAgent> NewVehicles;

		/// <summary>
		/// Constructor
		/// </summary>
		public TacticalDirector()
		{
			roadTactical = new RoadTactical();
			intersectionTactical = new IntersectionTactical();
			zoneTactical = new ZoneTactical();
			this.blockageTactical = new BlockageTactical(this);
			TacticalDirector.ValidVehicles = new Dictionary<int, VehicleAgent>();
			TacticalDirector.OccludedVehicles = new Dictionary<int, VehicleAgent>();
			TacticalDirector.VehicleAreas = new Dictionary<IVehicleArea, List<VehicleAgent>>();
		}

		/// <summary>
		/// Generic plan
		/// </summary>
		/// <param name="planningState"></param>
		/// <param name="navigationalPlan"></param>
		/// <param name="vehicleState"></param>
		/// <param name="vehicles"></param>
		/// <param name="obstacles"></param>
		/// <param name="blockage"></param>
		/// <returns></returns>
		public Maneuver Plan(IState planningState, INavigationalPlan navigationalPlan, VehicleState vehicleState,
			SceneEstimatorTrackedClusterCollection vehicles, SceneEstimatorUntrackedClusterCollection obstacles, 
			List<ITacticalBlockage> blockages, double vehicleSpeed)
		{
			// update stuff
			this.Update(vehicles, vehicleState);

			#region Plan Roads

			if (planningState is TravelState)
			{
				Maneuver roadFinal = roadTactical.Plan(
					planningState,
					(RoadPlan)navigationalPlan,
					vehicleState,
					vehicles,
					obstacles,
					blockages,
					vehicleSpeed);

				// return 
				return roadFinal;
			}

			#endregion

			#region Plan Intersection

			else if (planningState is IntersectionState)
			{
				Maneuver intersectionFinal = intersectionTactical.Plan(
					planningState,
					navigationalPlan,
					vehicleState,
					vehicles,
					obstacles,
					blockages);

				// return 
				return intersectionFinal;
			}

			#endregion

			#region Plan Zone

			else if (planningState is ZoneState)
			{
				Maneuver zoneFinal = zoneTactical.Plan(
					planningState,
					navigationalPlan,
					vehicleState,
					vehicles,
					obstacles,
					blockages);

				// return 
				return zoneFinal;
			}

			#endregion

			#region Plan Blockage

			else if (planningState is BlockageState)
			{
				Maneuver blockageFinal = blockageTactical.Plan(
					planningState,
					vehicleState,
					vehicleSpeed,
					blockages,
					navigationalPlan);

				return blockageFinal;
			}

			#endregion

			#region Unknown

			else
			{
				throw new Exception("Unknown planning state type: " + planningState.GetType().ToString());
			}

			#endregion
		}

		/// <summary>
		/// Updates vehicles
		/// </summary>
		/// <param name="vehicles"></param>
		/// <param name="state"></param>
		public void Update(SceneEstimatorTrackedClusterCollection vehicles, VehicleState state)
		{
			// vehicle population
			this.PopulateValidVehicles(vehicles);
			this.PopulateAreaVehicles();
			this.UpdateQueuingMonitors(state.Timestamp);
		}

		/// <summary>
		/// Populates mapping of vehicles we want to track given state
		/// </summary>
		/// <param name="vehicles"></param>
		public void PopulateValidVehicles(SceneEstimatorTrackedClusterCollection vehicles)
		{
			TacticalDirector.NewVehicles = new List<VehicleAgent>();
			TacticalDirector.OccludedVehicles = new Dictionary<int, VehicleAgent>();

			if (TacticalDirector.ValidVehicles == null)
				TacticalDirector.ValidVehicles = new Dictionary<int, VehicleAgent>();

			List<int> toRemove = new List<int>();
			foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
			{
				bool found = false;
				for (int i = 0; i < vehicles.clusters.Length; i++)
				{
					if (vehicles.clusters[i].id.Equals(va.VehicleId))
						found = true;
				}

				if (!found)
					toRemove.Add(va.VehicleId);				
			}
			foreach (int r in toRemove)
				TacticalDirector.ValidVehicles.Remove(r);

			for(int i = 0; i < vehicles.clusters.Length; i++)
			{
				SceneEstimatorTrackedCluster ov = vehicles.clusters[i];
				bool clusterStopped = ov.isStopped;
				if (ov.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE || 
					(ov.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_PART && !clusterStopped) ||
					(ov.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_FULL && !clusterStopped))
				{
					if (ov.targetClass == SceneEstimatorTargetClass.TARGET_CLASS_CARLIKE)
					{
						if (TacticalDirector.ValidVehicles.ContainsKey(ov.id))
							TacticalDirector.ValidVehicles[ov.id].StateMonitor.Observed = ov;
						else
						{
							VehicleAgent ovNew = new VehicleAgent(ov);
							TacticalDirector.ValidVehicles.Add(ovNew.VehicleId, ovNew);
							TacticalDirector.NewVehicles.Add(ovNew);
						}
					}
					else
					{
						if (TacticalDirector.ValidVehicles.ContainsKey(ov.id))
							TacticalDirector.ValidVehicles.Remove(ov.id);
					}
				}
				else
				{
					if (TacticalDirector.ValidVehicles.ContainsKey(ov.id))
						TacticalDirector.ValidVehicles.Remove(ov.id);
				}

				if ((ov.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_PART && clusterStopped) ||
					(ov.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_FULL && clusterStopped))
					TacticalDirector.OccludedVehicles.Add(ov.id, new VehicleAgent(ov));
			}
		}

		/// <summary>
		/// Populates mapping of areas to vehicles
		/// </summary>
		public void PopulateAreaVehicles()
		{
			TacticalDirector.VehicleAreas = new Dictionary<IVehicleArea,List<VehicleAgent>>();
			VehicleState vs = CoreCommon.Communications.GetVehicleState();

			Circle circ = new Circle(TahoeParams.T + 0.3, new Coordinates());
			Polygon conv = circ.ToPolygon(32);

			Circle circ1 = new Circle(TahoeParams.T + 0.6, new Coordinates());
			Polygon conv1 = circ1.ToPolygon(32);

			Circle circ2 = new Circle(TahoeParams.T + 1.4, new Coordinates());
			Polygon conv2 = circ2.ToPolygon(32);


			foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
			{
				#region Standard Areas

				List<AreaProbability> AreaProbabilities = new List<AreaProbability>();

				#region Add up probablities

				for (int i = 0; i < va.StateMonitor.Observed.closestPartitions.Length; i++)
				{
					SceneEstimatorClusterPartition secp = va.StateMonitor.Observed.closestPartitions[i];

					if (CoreCommon.RoadNetwork.VehicleAreaMap.ContainsKey(secp.partitionID))
					{
						IVehicleArea iva = CoreCommon.RoadNetwork.VehicleAreaMap[secp.partitionID];

						bool found = false;
						for (int j = 0; j < AreaProbabilities.Count; j++)
						{
							AreaProbability ap = AreaProbabilities[j];

							if (ap.Key.Equals(iva))
							{
								ap.Value = ap.Value + secp.probability;
								found = true;
							}
						}

						if (!found)
						{
							AreaProbabilities.Add(new AreaProbability(iva, secp.probability));
						}
					}
					else
					{
						ArbiterOutput.Output("Core Intelligence thread caught exception, partition: " + secp.partitionID + " not found in Vehicle Area Map");
					}
				}

				#endregion

				#region Assign

				if (AreaProbabilities.Count > 0)
				{
					double rP = 0.0;
					foreach (AreaProbability ap in AreaProbabilities)
					{
						if (ap.Key is ArbiterLane)
							rP += ap.Value;
					}

					if (rP > 0.1)
					{
						foreach (AreaProbability ap in AreaProbabilities)
						{
							if (ap.Key is ArbiterLane)
							{
								// get lane
								ArbiterLane al = (ArbiterLane)ap.Key;

								// probability says in area
								double vP = ap.Value / rP;
								if (vP > 0.3)
								{
									#region Check if obstacle enough in area

									bool ok = false;
									if (ap.Key is ArbiterLane)
									{	
										Coordinates closest = va.ClosestPointToLine(al.LanePath(), vs).Value;

										// dist to closest
										double distanceToClosest = vs.Front.DistanceTo(closest);

										// get our dist to closest
										if (30.0 < distanceToClosest && distanceToClosest < (30.0 + ((5.0 / 2.24) * Math.Abs(CoreCommon.Communications.GetVehicleSpeed().Value))))
										{
											if (al.LanePolygon != null)
												ok = this.VehicleExistsInsidePolygon(va, al.LanePolygon, vs);
											else
												ok = al.LanePath().GetClosestPoint(closest).Location.DistanceTo(closest) < al.Width / 2.0;
										}
										else if (distanceToClosest <= 30.0)
										{
											if (al.LanePolygon != null)
											{
												#warning Darpa is an asshole
												if (!va.IsStopped && this.VehicleAllInsidePolygon(va, al.LanePolygon, vs))
													ok = true;
												else
												{
													if (va.IsStopped)
													{
														// check vehicle is in a safety zone
														bool isInSafety = false;
														try
														{
															foreach (ArbiterIntersection ai in CoreCommon.RoadNetwork.ArbiterIntersections.Values)
															{
																if (ai.IntersectionPolygon.IsInside(va.ClosestPosition))
																	isInSafety = true;
															}
															foreach (ArbiterSafetyZone asz in al.SafetyZones)
															{
																if (asz.IsInSafety(va.ClosestPosition))
																	isInSafety = true;
															}
														}
														catch (Exception) { }

														if (isInSafety)
														{
															if (!this.VehiclePassableInPolygon(al, va, al.LanePolygon, vs, conv1))
																ok = true;
														}
														else
														{
															if (!this.VehiclePassableInPolygon(al, va, al.LanePolygon, vs, conv))
																ok = true;
														}
													}
													else
													{
														if (!this.VehiclePassableInPolygon(al, va, al.LanePolygon, vs, conv2))
															ok = true;
													}
												}
											}
											else
												ok = al.LanePath().GetClosestPoint(closest).Location.DistanceTo(closest) < al.Width / 2.0;
										}
										else
										{
											ok = true;
										}
									}

									#endregion

									if (ok)
									{
										if (TacticalDirector.VehicleAreas.ContainsKey(ap.Key))
											TacticalDirector.VehicleAreas[ap.Key].Add(va);
										else
										{
											List<VehicleAgent> vas = new List<VehicleAgent>();
											vas.Add(va);
											TacticalDirector.VehicleAreas.Add(ap.Key, vas);
										}
									}
								}
							}
						}
					}
				}

				#endregion

				#endregion

				#region Interconnect Area Mappings

				foreach (ArbiterInterconnect ai in CoreCommon.RoadNetwork.ArbiterInterconnects.Values)
				{
					if (ai.TurnPolygon.IsInside(va.StateMonitor.Observed.closestPoint))
					{
						if (TacticalDirector.VehicleAreas.ContainsKey(ai) && !TacticalDirector.VehicleAreas[ai].Contains(va))
							TacticalDirector.VehicleAreas[ai].Add(va);
						else
						{
							List<VehicleAgent> vas = new List<VehicleAgent>();
							vas.Add(va);
							TacticalDirector.VehicleAreas.Add(ai, vas);
						}

						// check if uturn
						if (ai.TurnDirection == ArbiterTurnDirection.UTurn &&
							ai.InitialGeneric is ArbiterWaypoint && ai.FinalGeneric is ArbiterWaypoint)
						{
							// get the lanes the uturn is a part of
							ArbiterLane initialLane = ((ArbiterWaypoint)ai.InitialGeneric).Lane;
							ArbiterLane targetLane = ((ArbiterWaypoint)ai.FinalGeneric).Lane;

							if (TacticalDirector.VehicleAreas.ContainsKey(initialLane))
							{
								if (!TacticalDirector.VehicleAreas[initialLane].Contains(va))
									TacticalDirector.VehicleAreas[initialLane].Add(va);
							}
							else
							{
								List<VehicleAgent> vas = new List<VehicleAgent>();
								vas.Add(va);
								TacticalDirector.VehicleAreas.Add(initialLane, vas);
							}

							if (TacticalDirector.VehicleAreas.ContainsKey(targetLane))
							{
								if (!TacticalDirector.VehicleAreas[targetLane].Contains(va))
									TacticalDirector.VehicleAreas[targetLane].Add(va);
							}
							else
							{
								List<VehicleAgent> vas = new List<VehicleAgent>();
								vas.Add(va);
								TacticalDirector.VehicleAreas.Add(targetLane, vas);
							}
						}
					}
				}

				#endregion
			}
		}

		#region Vehicle Area Checks

		public bool VehicleExistsInsidePolygon(VehicleAgent va, Polygon p, VehicleState ourState)
		{
			for (int i = 0; i < va.StateMonitor.Observed.relativePoints.Length; i++)
			{
				Coordinates c = va.TransformCoordAbs(va.StateMonitor.Observed.relativePoints[i], ourState);
				if (p.IsInside(c))
					return true;
			}

			return false;
		}

		public bool VehicleAllInsidePolygon(VehicleAgent va, Polygon p, VehicleState ourState)
		{
			for (int i = 0; i < va.StateMonitor.Observed.relativePoints.Length; i++)
			{
				Coordinates c = va.TransformCoordAbs(va.StateMonitor.Observed.relativePoints[i], ourState);
				if (!p.IsInside(c))
					return false;
			}

			return true;
		}

		public bool VehiclePassableInPolygon(ArbiterLane al, VehicleAgent va, Polygon p, VehicleState ourState, Polygon circ)
		{			
			Polygon vehiclePoly = va.GetAbsolutePolygon(ourState);
			vehiclePoly = Polygon.ConvexMinkowskiConvolution(circ, vehiclePoly);
			List<Coordinates> pointsOutside = new List<Coordinates>();
			ArbiterLanePartition alp = al.GetClosestPartition(va.ClosestPosition);			

			foreach(Coordinates c in vehiclePoly)
			{
				if (!p.IsInside(c))
					pointsOutside.Add(c);
			}

			foreach (Coordinates m in pointsOutside)
			{
				foreach (Coordinates n in pointsOutside)
				{
					if(!m.Equals(n))
					{
						if (GeneralToolkit.TriangleArea(alp.Initial.Position, m, alp.Final.Position) *
							GeneralToolkit.TriangleArea(alp.Initial.Position, n, alp.Final.Position) < 0)
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		#endregion

		/// <summary>
		/// Updates the queuing monitors of all the vehicles
		/// </summary>
		public void UpdateQueuingMonitors(double currentTs)
		{
			List<VehicleAgent> updatedLowPri = new List<VehicleAgent>();

			foreach (KeyValuePair<IVehicleArea, List<VehicleAgent>> areaVehicles in TacticalDirector.VehicleAreas)
			{
				if (areaVehicles.Key is ArbiterLane)
				{
					ArbiterLane al = (ArbiterLane)areaVehicles.Key;

					foreach (VehicleAgent va in areaVehicles.Value)
					{
						if (!updatedLowPri.Contains(va))
						{
							if (va.IsStopped && va.StateMonitor.Observed.speedValid)
							{
								bool inSafety = false;
								bool inInter = false;

								foreach (ArbiterSafetyZone asz in al.SafetyZones)
								{
									if (asz.IsInSafety(va.ClosestPosition))
									{
										inSafety = true;
									}
								}

								foreach (ArbiterIntersection ai in CoreCommon.RoadNetwork.ArbiterIntersections.Values)
								{
									if (ai.IntersectionPolygon.IsInside(va.ClosestPosition))
									{
										inInter = true;
									}
								}

								if (!inSafety && !inInter)
								{
									va.QueuingState.Update(QueueingUpdate.Queueing, currentTs);
									updatedLowPri.Add(va);
								}
								else
									va.QueuingState.Update(QueueingUpdate.NotQueueing, currentTs);
							}
							else
							{
								va.QueuingState.Update(QueueingUpdate.NotQueueing, currentTs);
							}
						}
					}
				}				
			}
		}

		/// <summary>
		/// Refreshes the default queuing state of all vehicles
		/// </summary>
		public static void RefreshQueuingMonitors()
		{
			if (TacticalDirector.ValidVehicles != null)
			{
				foreach (VehicleAgent va in TacticalDirector.ValidVehicles.Values)
				{
					QueueingUpdate qu = va.IsStopped ? QueueingUpdate.NotQueueing : QueueingUpdate.Queueing;
					va.QueuingState.Reset();
				}
			}
		}

		public void ResetTimers()
		{
			if (blockageTactical != null)
				blockageTactical.Reset();
		}
	}

	/// <summary>
	/// Holder for an area and its probability
	/// </summary>
	public class AreaProbability
	{
		public IVehicleArea Key;
		public double Value;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public AreaProbability(IVehicleArea key, double value)
		{
			this.Key = key;
			this.Value = value;
		}

		/// <summary>
		/// String representation of this area probability
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Key.ToString() + ": " + Value.ToString("F3");
		}
	}
}
