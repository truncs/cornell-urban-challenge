using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Sensors;
using RndfEditor.Display.Utilities;
using System.Drawing;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using System.Drawing.Drawing2D;
using RemoraAdvanced.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Common.Tools;

namespace RemoraAdvanced.Display.DisplayObjects
{
	public class SensedVehicleDisplay : IDisplayObject
	{
		private SceneEstimatorTrackedCluster trackedCluster;

		public SensedVehicleDisplay(SceneEstimatorTrackedCluster trackedCluster)
		{
			this.trackedCluster = trackedCluster;
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			// Determine size of bounding box
			float scaled_offset = 1 / wt.Scale;

			// invert the scale
			float scaled_size = DrawingUtility.cp_large_size;

			// assume that the world transform is currently applied correctly to the graphics
			RectangleF rect = new RectangleF((float)this.trackedCluster.closestPoint.X - scaled_size / 2, (float)this.trackedCluster.closestPoint.Y - scaled_size / 2, scaled_size, scaled_size);

			// return
			return rect;
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			if (this.trackedCluster.targetClass == SceneEstimatorTargetClass.TARGET_CLASS_CARLIKE)
			{
				bool clusterStopped = this.trackedCluster.isStopped;
				bool occluded = ((this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_FULL && clusterStopped) ||
					(this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_PART && clusterStopped));

				Color c = this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE ?
					DrawingUtility.ColorSimTrafficCar :
					DrawingUtility.ColorSimDeletedCar;

				if(occluded)
					c = Color.Chocolate;
				else if ((this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_FULL && !clusterStopped) ||
					(this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_OCCLUDED_PART && !clusterStopped))
				{
					occluded = true;
					c = Color.DarkOrange;
				}

				if (this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE
					&& this.trackedCluster.isStopped && this.trackedCluster.speedValid)
					c = Color.Red;
				else if (this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE
					&& this.trackedCluster.isStopped && !this.trackedCluster.speedValid)
					c = Color.SkyBlue;

				bool draw = this.trackedCluster.statusFlag == SceneEstimatorTargetStatusFlag.TARGET_STATE_ACTIVE || occluded ?
					DrawingUtility.DrawSimCars :
					DrawingUtility.DrawSimCars && DrawingUtility.DrawSimCarDeleted;

				if (draw)
				{
					Coordinates heading = (new Coordinates(1,0)).Rotate(this.trackedCluster.absoluteHeading).Normalize(3.0);
					Coordinates headingCoord = this.trackedCluster.closestPoint + heading;

					if(trackedCluster.headingValid)
						DrawingUtility.DrawColoredControlLine(Color.Blue, DashStyle.Solid, this.trackedCluster.closestPoint, headingCoord, g, t);

					if (DrawingUtility.DrawSimCarId)
					{
						DrawingUtility.DrawControlPoint(
							this.trackedCluster.closestPoint,
							c,
							this.trackedCluster.id.ToString(),
							ContentAlignment.TopLeft,
							ControlPointStyle.LargeBox,
							g, t);

						DrawingUtility.DrawControlPoint(
							this.trackedCluster.closestPoint,
							c,
							this.trackedCluster.speed.ToString("f1"),
							ContentAlignment.BottomLeft,
							ControlPointStyle.LargeBox,
							g, t);

						DrawingUtility.DrawControlPoint(
							this.trackedCluster.closestPoint,
							c,
							this.PartitionIdString(),
							ContentAlignment.BottomRight,
							ControlPointStyle.LargeBox,
							g, t);
					}
					else
					{
						DrawingUtility.DrawControlPoint(
							this.trackedCluster.closestPoint,
							c,
							null,
							ContentAlignment.MiddleCenter,
							ControlPointStyle.LargeBox,
							g, t);
					}
				}
			}
		}

		public string PartitionIdString()
		{
			string final = "";

			#region Standard Areas

			List<AreaProbability> AreaProbabilities = new List<AreaProbability>();

			Circle circ = new Circle(TahoeParams.T + 0.3, new Coordinates());
			Polygon conv = circ.ToPolygon(32);

			Circle circ1 = new Circle(TahoeParams.T + 0.6, new Coordinates());
			Polygon conv1 = circ1.ToPolygon(32);

			Circle circ2 = new Circle(TahoeParams.T + 1.4, new Coordinates());
			Polygon conv2 = circ2.ToPolygon(32);

			for (int i = 0; i < this.trackedCluster.closestPartitions.Length; i++)
			{
				SceneEstimatorClusterPartition secp = this.trackedCluster.closestPartitions[i];

				if (RemoraCommon.RoadNetwork.VehicleAreaMap.ContainsKey(secp.partitionID))
				{
					IVehicleArea iva = RemoraCommon.RoadNetwork.VehicleAreaMap[secp.partitionID];

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
					RemoraOutput.WriteLine("Remora caught exception, partition: " + secp.partitionID + " not found in Vehicle Area Map", OutputType.Remora);
				}
			}

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
							// proabbility says in area
							double vP = ap.Value / rP;
							if (vP > 0.3)
							{
								#region Check if obstacle enough in area

								bool ok = false;
								if (ap.Key is ArbiterLane)
								{
									VehicleState vs = RemoraCommon.Communicator.GetVehicleState();

									ArbiterLane al = (ArbiterLane)ap.Key;
									Coordinates closest = this.ClosestPointToLine(al.LanePath(), vs).Value;

									// dist to closest
									double distanceToClosest = vs.Front.DistanceTo(closest);

									// get our dist to closest
									if (30.0 < distanceToClosest && distanceToClosest < (30.0 + ((5.0 / 2.24) * Math.Abs(RemoraCommon.Communicator.GetVehicleSpeed().Value))))
									{
										if (al.LanePolygon != null)
											ok = this.VehicleExistsInsidePolygon(al.LanePolygon, vs);
										else
											ok = al.LanePath().GetClosestPoint(closest).Location.DistanceTo(closest) < al.Width / 2.0;
									}
									else if (distanceToClosest <= 30.0)
									{
										if (al.LanePolygon != null)
										{
											if (!this.trackedCluster.isStopped && this.VehicleAllInsidePolygon(al.LanePolygon, vs))
												ok = true;
											else
											{
												if (this.trackedCluster.isStopped)
												{
													bool isInSafety = false;
													foreach (ArbiterIntersection ai in RemoraCommon.RoadNetwork.ArbiterIntersections.Values)
													{
														if (ai.IntersectionPolygon.IsInside(this.trackedCluster.closestPoint))
															isInSafety = true;
													}
													foreach (ArbiterSafetyZone asz in al.SafetyZones)
													{
														if (asz.IsInSafety(this.trackedCluster.closestPoint))
															isInSafety = true;
													}

													if (isInSafety)
													{
														if (!this.VehiclePassableInPolygon(al, al.LanePolygon, vs, conv1))
															ok = true;
													}
													else
													{
														if (!this.VehiclePassableInPolygon(al, al.LanePolygon, vs, conv))
															ok = true;
													}
												}
												else
												{
													if (!this.VehiclePassableInPolygon(al, al.LanePolygon, vs, conv2))
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

									if (ok)
									{
										final = final + ap.Key.ToString() + ": " + vP.ToString("F4") + "\n";
									}
								}

								#endregion
							}
						}
					}
				}
			}

			#endregion

			#region Interconnect Area Mappings

			foreach (ArbiterInterconnect ai in RemoraCommon.RoadNetwork.ArbiterInterconnects.Values)
			{
				if (ai.TurnPolygon.IsInside(this.trackedCluster.closestPoint))
				{
					final = final + ai.ToString() + "\n";

					if (ai.TurnDirection == ArbiterTurnDirection.UTurn && ai.InitialGeneric is ArbiterWaypoint && ai.FinalGeneric is ArbiterWaypoint)
					{
						// initial
						ArbiterLane initialLane = ((ArbiterWaypoint)ai.InitialGeneric).Lane;
						ArbiterLane targetLane = ((ArbiterWaypoint)ai.FinalGeneric).Lane;
						final = final + "UTurn2Lane: " + initialLane.ToString() + " & " + targetLane.ToString() + "\n";
					}
				}
			}

			#endregion

			#region Intersections

			foreach (ArbiterIntersection aInt in RemoraCommon.RoadNetwork.ArbiterIntersections.Values)
			{
				if (aInt.IntersectionPolygon.IsInside(this.trackedCluster.closestPoint))
				{
					final = final + "Inter: " + aInt.ToString() + "\n";
				}
			}

			#endregion

			return final;
		}

		#region Vehicle Area Checks

		public bool VehicleExistsInsidePolygon(Polygon p, VehicleState ourState)
		{
			for (int i = 0; i < this.trackedCluster.relativePoints.Length; i++)
			{
				Coordinates c = this.TransformCoordAbs(this.trackedCluster.relativePoints[i], ourState);
				if (p.IsInside(c))
					return true;
			}

			return false;
		}

		public bool VehicleAllInsidePolygon(Polygon p, VehicleState ourState)
		{
			for (int i = 0; i < this.trackedCluster.relativePoints.Length; i++)
			{
				Coordinates c = this.TransformCoordAbs(this.trackedCluster.relativePoints[i], ourState);
				if (!p.IsInside(c))
					return false;
			}

			return true;
		}

		public bool VehiclePassableInPolygon(ArbiterLane al, Polygon p, VehicleState ourState, Polygon circ)
		{
			List<Coordinates> vhcCoords = new List<Coordinates>();
			for (int i = 0; i < this.trackedCluster.relativePoints.Length; i++)
				vhcCoords.Add(this.TransformCoordAbs(this.trackedCluster.relativePoints[i], ourState));
			Polygon vehiclePoly = Polygon.GrahamScan(vhcCoords);
			vehiclePoly = Polygon.ConvexMinkowskiConvolution(circ, vehiclePoly);
			ArbiterLanePartition alp = al.GetClosestPartition(this.trackedCluster.closestPoint);
			List<Coordinates> pointsOutside = new List<Coordinates>();

			foreach (Coordinates c in vehiclePoly)
			{
				if (!p.IsInside(c))
					pointsOutside.Add(c);
			}

			foreach (Coordinates m in pointsOutside)
			{
				foreach (Coordinates n in pointsOutside)
				{
					if (!m.Equals(n))
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

		public bool MoveAllowed
		{
			get { return false; }
		}

		public void BeginMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public SelectionType Selected
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public IDisplayObject Parent
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public bool CanDelete
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public List<IDisplayObject> Delete()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawSimCars;
		}

		#endregion

		public Coordinates? ClosestPointToLine(LinePath path, VehicleState vs)
		{
			Coordinates? closest = null;
			double minDist = Double.MaxValue;

			for (int i = 0; i < this.trackedCluster.relativePoints.Length; i++)
			{
				Coordinates c = this.TransformCoordAbs(this.trackedCluster.relativePoints[i], vs);
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

		public Coordinates TransformCoordAbs(Coordinates c, VehicleState state)
		{
			c = c.Rotate(state.Heading.ArcTan);
			c = c + state.Position;
			return c;
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
