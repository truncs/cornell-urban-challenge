using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RndfToolkit;
using UrbanChallenge.Common;
using RndfEditor.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;

namespace RndfEditor.Files
{
	/// <summary>
	/// Transforms an arbiter road network into a road graph
	/// </summary>
	[Serializable]
	public class SceneRoadGraphGeneration
	{
		private ArbiterRoadNetwork roadNetwork;
		private double stopLineSearchDist = 30;
		private double nearbyPartitionsDist = 30;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="roadNetwork"></param>
		public SceneRoadGraphGeneration(ArbiterRoadNetwork roadNetwork)
		{
			this.roadNetwork = roadNetwork;
		}

		/// <summary>
		/// Generates a road graph to a file from the internal road network
		/// </summary>
		/// <param name="fileName"></param>
		public void GenerateRoadGraph(string fileName)
		{
			// create file
			FileStream fs = new FileStream(fileName, FileMode.Create);

			// create writer
			StreamWriter sw = new StreamWriter(fs);

			// notify
			EditorOutput.WriteLine("Generating road graph to file: " + fileName);

			// notify
			EditorOutput.WriteLine("Writing general information");

			// general
			this.WriteGeneralInformation(sw);

			// notify
			EditorOutput.WriteLine("Writing waypoint information");

			// waypoint
			this.WriteWaypointInformation(sw);

			// notify
			EditorOutput.WriteLine("Writing partition information");

			// partitions
			this.WritePartitionInformation(sw);			

			// end rndf
			sw.WriteLine("End_Rndf");

			// write final
			sw.Close();

			// release holds
			fs.Dispose();

			// notify
			EditorOutput.WriteLine("Generated road graph to file: " + fileName);
		}

		/// <summary>
		/// Write partition informaton
		/// </summary>
		/// <param name="sw"></param>
		private void WritePartitionInformation(StreamWriter sw)
		{
			// get list of all partitions that connect waypoints
			List<IConnectAreaWaypoints> icaws = new List<IConnectAreaWaypoints>();

			#region Populate partitions

			// get lane partitions
			foreach (ArbiterSegment asg in roadNetwork.ArbiterSegments.Values)
			{
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					foreach (ArbiterLanePartition alp in al.Partitions)
					{
						icaws.Add(alp);
					}
				}
			}

			// get interconnects
			foreach (ArbiterInterconnect ai in roadNetwork.ArbiterInterconnects.Values)
			{
				icaws.Add(ai);
			}

			// zones (holy stuff what a hack)
			foreach (ArbiterZone az in roadNetwork.ArbiterZones.Values)
			{
				icaws.Add(new SceneZonePartition(az));
			}

			#endregion

			// notify
			sw.WriteLine("NumberOfPartitions" + "\t" + icaws.Count.ToString());
			string completionPercent = "";

			#region Create Partitions in Road Graph

			// write each
			for(int i = 0; i < icaws.Count; i++)
			{
				IConnectAreaWaypoints icaw = icaws[i];
				sw.WriteLine("Partition");

				string id = "";
				if (icaw is SceneZonePartition)
					id = ("PartitionId" + "\t" + ((SceneZonePartition)icaw).Zone.ToString());
				else
					id = ("PartitionId" + "\t" + icaw.ConnectionId.ToString());
				sw.WriteLine(id);

				// notify
				double percent = ((double)i) / ((double)icaws.Count) * 100.0;
				string tmpP = percent.ToString("F0") + "% Complete";
				if (tmpP != completionPercent)
				{
					completionPercent = tmpP;
					EditorOutput.WriteLine(completionPercent);
				}

				#region Interconnect

				if (icaw is ArbiterInterconnect)
				{
					ArbiterInterconnect ai = (ArbiterInterconnect)icaw;
					sw.WriteLine("PartitionType" + "\t" + "Interconnect");
					sw.WriteLine("Sparse" + "\t" + "False");
					sw.WriteLine("FitType" + "\t" + "Line");

					Coordinates c = ai.FinalGeneric.Position - ai.InitialGeneric.Position;
					sw.WriteLine("FitParameters" + "\t" + c.ArcTan.ToString("F6"));
					sw.WriteLine("LeftBoundary" + "\t" + "None");
					sw.WriteLine("RightBoundary" + "\t" + "None");
					sw.WriteLine("NumberOfPoints" + "\t" + "2");
					sw.WriteLine("Points");
					sw.WriteLine(ai.InitialGeneric.ToString());
					sw.WriteLine(ai.FinalGeneric.ToString());
					sw.WriteLine("End_Points");

					List<ArbiterWaypoint> aws = this.GetNearbyStops(ai);
					sw.WriteLine("NumberOfNearbyStoplines" + "\t" + aws.Count);
					if (aws.Count != 0)
					{
						sw.WriteLine("NearbyStoplines");
						foreach (ArbiterWaypoint aw in aws)
						{
							sw.WriteLine(aw.ToString());
						}
						sw.WriteLine("End_NearbyStoplines");
					}

					#region Adjacent

					List<string> adjacentPartitions = new List<string>();

					// add current
					adjacentPartitions.Add(ai.ToString());

					#region Initial 

					if (icaw.InitialGeneric is ArbiterWaypoint)
					{
						// wp
						ArbiterWaypoint aw = (ArbiterWaypoint)icaw.InitialGeneric;

						// prev
						if (aw.PreviousPartition != null)
							adjacentPartitions.Add(aw.PreviousPartition.ToString());

						// next
						if (aw.NextPartition != null)
							adjacentPartitions.Add(aw.NextPartition.ToString());

						// exits
						if(aw.IsExit)
						{
							foreach (ArbiterInterconnect ais in aw.Exits)
							{
								if(!ais.Equals(ai))
									adjacentPartitions.Add(ais.ToString());
							}
						}

						if (aw.IsEntry)
						{
							foreach (ArbiterInterconnect ais in aw.Entries)
							{
								if (!ais.Equals(ai))
									adjacentPartitions.Add(ais.ToString());
							}
						}
					}
					else if (icaw.InitialGeneric is ArbiterPerimeterWaypoint)
					{
						adjacentPartitions.Add((new SceneZonePartition(((ArbiterPerimeterWaypoint)icaw.InitialGeneric).Perimeter.Zone)).ToString());
					}

					#endregion

					#region Final

					if (icaw.FinalGeneric is ArbiterWaypoint)
					{
						// wp
						ArbiterWaypoint aw = (ArbiterWaypoint)icaw.FinalGeneric;

						// prev
						if (aw.PreviousPartition != null)
							adjacentPartitions.Add(aw.PreviousPartition.ToString());

						// next
						if (aw.NextPartition != null)
							adjacentPartitions.Add(aw.NextPartition.ToString());

						// exits
						if (aw.IsExit)
						{
							foreach (ArbiterInterconnect ais in aw.Exits)
							{
								adjacentPartitions.Add(ais.ToString());
							}
						}

						if (aw.IsEntry)
						{
							foreach (ArbiterInterconnect ais in aw.Entries)
							{
								if (!ais.Equals(ai))
									adjacentPartitions.Add(ais.ToString());
							}
						}
					}
					else if (icaw.FinalGeneric is ArbiterPerimeterWaypoint)
					{
						adjacentPartitions.Add((new SceneZonePartition(((ArbiterPerimeterWaypoint)icaw.FinalGeneric).Perimeter.Zone)).ToString());
					}

					#endregion

					sw.WriteLine("NumberOfLaneAdjacentPartitions" + "\t" + adjacentPartitions.Count.ToString());
					if (adjacentPartitions.Count != 0)
					{
						sw.WriteLine("LaneAdjacentPartitions");
						foreach (string s in adjacentPartitions)
						{
							sw.WriteLine(s);
						}
						sw.WriteLine("End_LaneAdjacentPartitions");
					}

					#endregion

					sw.WriteLine("NumberOfLeftLaneAdjacentPartitions" + "\t" + "0");					
					sw.WriteLine("NumberOfRightLaneAdjacentPartitions" + "\t" + "0");

					List<IConnectAreaWaypoints> nearby = this.GetNearbyPartitions(ai, icaws);
					sw.WriteLine("NumberOfNearbyPartitions" + "\t" + nearby.Count.ToString());
					if (nearby.Count != 0)
					{
						sw.WriteLine("NearbyPartitions");
						foreach (IConnectAreaWaypoints tmp in nearby)
						{
							sw.WriteLine(tmp.ToString());
						}
						sw.WriteLine("End_NearbyPartitions");
					}

					sw.WriteLine("End_Partition");					
				}

				#endregion

				#region Zone

				else if (icaw is SceneZonePartition)
				{
					SceneZonePartition szp = (SceneZonePartition)icaw;
					sw.WriteLine("PartitionType" + "\t" + "Zone");
					sw.WriteLine("Sparse" + "\t" + "False");
					sw.WriteLine("FitType" + "\t" + "Polygon");

					string count = szp.Zone.Perimeter.PerimeterPoints.Count.ToString();
					string wps = "";
					foreach (ArbiterPerimeterWaypoint apw in szp.Zone.Perimeter.PerimeterPoints.Values)
					{
						wps = wps + "\t" + apw.Position.X.ToString("f6") + "\t" + apw.Position.Y.ToString("f6");
					}
					sw.WriteLine("FitParameters" + "\t" + count + wps);

					sw.WriteLine("LeftBoundary" + "\t" + "None");
					sw.WriteLine("RightBoundary" + "\t" + "None");
					sw.WriteLine("NumberOfPoints" + "\t" + szp.Zone.Perimeter.PerimeterPoints.Count.ToString());
					sw.WriteLine("Points");
					foreach (ArbiterPerimeterWaypoint apw in szp.Zone.Perimeter.PerimeterPoints.Values)
					{
						sw.WriteLine(apw.WaypointId.ToString());
					}
					sw.WriteLine("End_Points");

					List<ArbiterWaypoint> aws = this.GetNearbyStops(szp);
					sw.WriteLine("NumberOfNearbyStoplines" + "\t" + aws.Count);
					if (aws.Count != 0)
					{
						sw.WriteLine("NearbyStoplines");
						foreach (ArbiterWaypoint aw in aws)
						{
							sw.WriteLine(aw.ToString());
						}
						sw.WriteLine("End_NearbyStoplines");
					}

					#region Adjacent

					List<string> adjacentStrings = new List<string>();

					// add current
					adjacentStrings.Add(szp.ToString());

					foreach (ArbiterPerimeterWaypoint apw in szp.Zone.Perimeter.PerimeterPoints.Values)
					{
						if (apw.IsExit)
						{
							foreach (ArbiterInterconnect ai in apw.Exits)
							{
								adjacentStrings.Add(ai.ToString());
							}
						}

						if (apw.IsEntry)
						{
							foreach (ArbiterInterconnect ais in apw.Entries)
							{
								adjacentStrings.Add(ais.ToString());
							}
						}
					}

					sw.WriteLine("NumberOfLaneAdjacentPartitions" + "\t" + adjacentStrings.Count.ToString());
					if (adjacentStrings.Count != 0)
					{
						sw.WriteLine("LaneAdjacentPartitions");
						foreach (string s in adjacentStrings)
							sw.WriteLine(s);
						sw.WriteLine("End_LaneAdjacentPartitions");
					}


					#endregion

					sw.WriteLine("NumberOfLeftLaneAdjacentPartitions" + "\t" + "0");
					sw.WriteLine("NumberOfRightLaneAdjacentPartitions" + "\t" + "0");

					List<IConnectAreaWaypoints> nearby = this.GetNearbyPartitions(szp, icaws);
					sw.WriteLine("NumberOfNearbyPartitions" + "\t" + nearby.Count.ToString());
					if (nearby.Count != 0)
					{
						sw.WriteLine("NearbyPartitions");
						foreach (IConnectAreaWaypoints tmp in nearby)
						{
							sw.WriteLine(tmp.ToString());
						}
						sw.WriteLine("End_NearbyPartitions");
					}

					sw.WriteLine("End_Partition");
				}

				#endregion

				#region Lane

				else if (icaw is ArbiterLanePartition)
				{
					ArbiterLanePartition alp = (ArbiterLanePartition)icaw;
					sw.WriteLine("PartitionType" + "\t" + "Lane");
					string sparseString = alp.Type == PartitionType.Sparse ? "True" : "False";
					sw.WriteLine("Sparse" + "\t" + sparseString);

					if (alp.Type != PartitionType.Sparse)//alp.UserPartitions.Count <= 1)
					{
						sw.WriteLine("FitType" + "\t" + "Line");
						sw.WriteLine("FitParameters" + "\t" + alp.Vector().ArcTan.ToString("F6"));
					}
					else
					{
						sw.WriteLine("FitType" + "\t" + "Polygon");
						
						/*List<Coordinates> polyCoords = new List<Coordinates>();
						polyCoords.Add(alp.Initial.Position);
						polyCoords.AddRange(alp.NotInitialPathCoords());
						LinePath lpr = (new LinePath(polyCoords)).ShiftLateral(-TahoeParams.VL * 3.0);
						LinePath lpl = (new LinePath(polyCoords)).ShiftLateral(TahoeParams.VL * 3.0);
						List<Coordinates> finalCoords = new List<Coordinates>(polyCoords.ToArray());
						finalCoords.AddRange(lpr);
						finalCoords.AddRange(lpl);
						Polygon p = Polygon.GrahamScan(finalCoords);*/

						if (alp.SparsePolygon == null)
							alp.SetDefaultSparsePolygon();

						string coordinateString = "";
						foreach (Coordinates c in alp.SparsePolygon)
							coordinateString = coordinateString + "\t" + c.X.ToString("F6") + "\t" + c.Y.ToString("F6");

						sw.WriteLine("FitParameters" + "\t" + alp.SparsePolygon.Count.ToString() + coordinateString);
					}

					sw.WriteLine("LaneWidth" + "\t" + alp.Lane.Width.ToString("F6"));
					sw.WriteLine("LeftBoundary" + "\t" + alp.Lane.BoundaryLeft.ToString());
					sw.WriteLine("RightBoundary" + "\t" + alp.Lane.BoundaryRight.ToString());
					sw.WriteLine("NumberOfPoints" + "\t" + "2");
					sw.WriteLine("Points");
					sw.WriteLine(alp.InitialGeneric.ToString());
					sw.WriteLine(alp.FinalGeneric.ToString());
					sw.WriteLine("End_Points");

					List<ArbiterWaypoint> aws = this.GetNearbyStops(alp);
					sw.WriteLine("NumberOfNearbyStoplines" + "\t" + aws.Count);
					if (aws.Count != 0)
					{
						sw.WriteLine("NearbyStoplines");
						foreach (ArbiterWaypoint aw in aws)
						{
							sw.WriteLine(aw.ToString());
						}
						sw.WriteLine("End_NearbyStoplines");
					}

					#region Adjacent

					List<string> adjacentPartitions = new List<string>();

					// add current
					adjacentPartitions.Add(alp.ToString());

					#region Initial

					if (icaw.InitialGeneric is ArbiterWaypoint)
					{
						// wp
						ArbiterWaypoint aw = (ArbiterWaypoint)icaw.InitialGeneric;

						// prev
						if (aw.PreviousPartition != null)
							adjacentPartitions.Add(aw.PreviousPartition.ToString());

						// next
						if (aw.NextPartition != null && !aw.NextPartition.Equals(alp))
							adjacentPartitions.Add(aw.NextPartition.ToString());

						// exits
						if (aw.IsExit)
						{
							foreach (ArbiterInterconnect ais in aw.Exits)
							{
								adjacentPartitions.Add(ais.ToString());
							}
						}

						if (aw.IsEntry)
						{
							foreach (ArbiterInterconnect ais in aw.Entries)
							{
								adjacentPartitions.Add(ais.ToString());
							}
						}
					}

					#endregion

					#region Final

					if (icaw.FinalGeneric is ArbiterWaypoint)
					{
						// wp
						ArbiterWaypoint aw = (ArbiterWaypoint)icaw.FinalGeneric;

						// prev
						if (aw.PreviousPartition != null && !aw.PreviousPartition.Equals(alp))
							adjacentPartitions.Add(aw.PreviousPartition.ToString());

						// next
						if (aw.NextPartition != null)
							adjacentPartitions.Add(aw.NextPartition.ToString());

						// exits
						if (aw.IsExit)
						{
							foreach (ArbiterInterconnect ais in aw.Exits)
							{
								adjacentPartitions.Add(ais.ToString());
							}
						}

						if (aw.IsEntry)
						{
							foreach (ArbiterInterconnect ais in aw.Entries)
							{
								adjacentPartitions.Add(ais.ToString());
							}
						}
					}

					#endregion

					sw.WriteLine("NumberOfLaneAdjacentPartitions" + "\t" + adjacentPartitions.Count.ToString());
					if (adjacentPartitions.Count != 0)
					{
						sw.WriteLine("LaneAdjacentPartitions");
						foreach (string s in adjacentPartitions)
						{
							sw.WriteLine(s);
						}
						sw.WriteLine("End_LaneAdjacentPartitions");
					}

					#endregion

					List<string> leftAlps = new List<string>();
					List<string> rightAlps = new List<string>();

					foreach (ArbiterLanePartition tmpAlp in alp.NonLaneAdjacentPartitions)
					{
						if (tmpAlp.Lane.Equals(alp.Lane.LaneOnLeft))
						{
							leftAlps.Add(tmpAlp.ToString());
						}
						else
						{
							rightAlps.Add(tmpAlp.ToString());
						}
					}

					sw.WriteLine("NumberOfLeftLaneAdjacentPartitions" + "\t" + leftAlps.Count.ToString());
					if (leftAlps.Count != 0)
					{
						sw.WriteLine("LeftLaneAdjacentPartitions");
						foreach (string s in leftAlps)
							sw.WriteLine(s);
						sw.WriteLine("End_LeftLaneAdjacentPartitions");
					}

					sw.WriteLine("NumberOfRightLaneAdjacentPartitions" + "\t" + rightAlps.Count.ToString());
					if (rightAlps.Count != 0)
					{
						sw.WriteLine("RightLaneAdjacentPartitions");
						foreach (string s in rightAlps)
							sw.WriteLine(s);
						sw.WriteLine("End_RightLaneAdjacentPartitions");
					}

					List<IConnectAreaWaypoints> nearby = this.GetNearbyPartitions(alp, icaws);
					sw.WriteLine("NumberOfNearbyPartitions" + "\t" + nearby.Count.ToString());
					if (nearby.Count != 0)
					{
						sw.WriteLine("NearbyPartitions");
						foreach (IConnectAreaWaypoints tmp in nearby)
						{
							sw.WriteLine(tmp.ToString());
						}
						sw.WriteLine("End_NearbyPartitions");						
					}

					sw.WriteLine("End_Partition");
				}

				#endregion
			}

			#endregion
		}

		/// <summary>
		/// get nearby
		/// </summary>
		/// <param name="icaw"></param>
		/// <param name="icaws"></param>
		/// <returns></returns>
		private List<IConnectAreaWaypoints> GetNearbyPartitions(IConnectAreaWaypoints icaw, List<IConnectAreaWaypoints> icaws)
		{
			List<IConnectAreaWaypoints> final = new List<IConnectAreaWaypoints>();

			foreach (IConnectAreaWaypoints tmp in icaws)
			{
				double dist = this.nearbyPartitionsDist;
				if (icaw is SceneZonePartition)
					dist = ((SceneZonePartition)icaw).Zone.Perimeter.PerimeterPolygon.CalculateBoundingCircle().r + this.nearbyPartitionsDist;
				else if(tmp is SceneZonePartition)
					dist = ((SceneZonePartition)tmp).Zone.Perimeter.PerimeterPolygon.CalculateBoundingCircle().r + this.nearbyPartitionsDist;

				if (icaw.DistanceTo(tmp) <= dist)
				{
					final.Add(tmp);
				}
			}

			return final;
		}

		/// <summary>
		/// gets nearby stops to the connection
		/// </summary>
		/// <param name="icaw"></param>
		/// <returns></returns>
		private List<ArbiterWaypoint> GetNearbyStops(IConnectAreaWaypoints icaw)
		{
			List<ArbiterWaypoint> aws = new List<ArbiterWaypoint>();

			foreach (IArbiterWaypoint iaw in this.roadNetwork.ArbiterWaypoints.Values)
			{
				if (iaw is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)iaw;
					if (aw.IsStop)
					{
						if (icaw.DistanceTo(aw.Position) < this.stopLineSearchDist)
						{
							aws.Add(aw);
						}
					}
				}
			}

			return aws;
		}

		/// <summary>
		/// Writes waypoint informaton
		/// </summary>
		/// <param name="sw"></param>
		private void WriteWaypointInformation(StreamWriter sw)
		{
			// list of all waypoints
			List<IArbiterWaypoint> waypoints = new List<IArbiterWaypoint>();

			// add all
			foreach (IArbiterWaypoint iaw in roadNetwork.ArbiterWaypoints.Values)
			{
				waypoints.Add(iaw);
			}
			
			// notify
			sw.WriteLine("NumberOfWaypoints" + "\t" + waypoints.Count.ToString());
			sw.WriteLine("Waypoints");

			// loop
			for (int i = 0; i < waypoints.Count; i++)
			{
				// stop
				string isStop = "IsNotStop";
				if(waypoints[i] is ArbiterWaypoint && ((ArbiterWaypoint)waypoints[i]).IsStop)
					isStop = "IsStop";

				// info
				sw.WriteLine(
					waypoints[i].ToString() + "\t" +
					isStop + "\t" +					
					waypoints[i].Position.X.ToString("F6") + "\t" +
					waypoints[i].Position.Y.ToString("F6"));

				List<string> memberPartitions = new List<string>();

				if (waypoints[i] is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)waypoints[i];

					if (aw.NextPartition != null)
						memberPartitions.Add(aw.NextPartition.ToString());

					if (aw.PreviousPartition != null)
						memberPartitions.Add(aw.PreviousPartition.ToString());

					if (aw.IsExit)
					{
						foreach (ArbiterInterconnect ai in aw.Exits)
							memberPartitions.Add(ai.ToString());
					}

					if (aw.IsEntry)
					{
						foreach (ArbiterInterconnect ai in aw.Entries)
							memberPartitions.Add(ai.ToString());
					}
				}
				else if (waypoints[i] is ArbiterPerimeterWaypoint)
				{
					ArbiterPerimeterWaypoint apw = (ArbiterPerimeterWaypoint)waypoints[i];
					memberPartitions.Add((new SceneZonePartition(apw.Perimeter.Zone)).ToString());

					if (apw.IsExit)
					{
						foreach (ArbiterInterconnect ai in apw.Exits)
							memberPartitions.Add(ai.ToString());
					}

					if (apw.IsEntry)
					{
						foreach (ArbiterInterconnect ai in apw.Entries)
							memberPartitions.Add(ai.ToString());
					}
				}
				else if (waypoints[i] is ArbiterParkingSpotWaypoint)
				{
					ArbiterParkingSpotWaypoint apsw = (ArbiterParkingSpotWaypoint)waypoints[i];
					memberPartitions.Add((new SceneZonePartition(apsw.ParkingSpot.Zone)).ToString());
				}

				sw.WriteLine("NumberOfMemberPartitions" + "\t" + memberPartitions.Count.ToString());

				if (memberPartitions.Count > 0)
				{
					sw.WriteLine("MemberPartitions");

					foreach (string s in memberPartitions)
						sw.WriteLine(s);

					sw.WriteLine("EndMemberPartitions");
				}
			}

			// notify
			sw.WriteLine("End_Waypoints");
		}

		/// <summary>
		/// Writes general information
		/// </summary>
		/// <param name="sw"></param>
		private void WriteGeneralInformation(StreamWriter sw)
		{
			sw.WriteLine("RndfName" + "\t" + roadNetwork.Name);
			sw.WriteLine("RndfCreationDate" + "\t" + roadNetwork.CreationDate);
			sw.WriteLine("RoadGraphCreationDate" + "\t" + DateTime.Now.ToString());

			LLACoord origin = GpsTools.XyToLlaDegrees(new Coordinates(0,0), roadNetwork.PlanarProjection);
			sw.WriteLine("ProjectionOrigin" + "\t" + origin.lat.ToString("F6") + "\t" + origin.lon.ToString("F6"));			
		}

		/// <summary>
		/// Segment info
		/// </summary>
		/// <param name="sw"></param>
		private void WriteSegmentInformation(StreamWriter sw)
		{
			foreach (ArbiterSegment asg in roadNetwork.ArbiterSegments.Values)
			{
				sw.WriteLine("Segment");
				sw.WriteLine("SegmentId" + "\t" + asg.SegmentId.ToString());

				foreach (ArbiterWay aw in asg.Ways.Values)
				{
					sw.WriteLine("Way");
					sw.WriteLine("WayId" + "\t" + aw.WayId.ToString());

					foreach(ArbiterLane al in aw.Lanes.Values)
					{
						sw.WriteLine("Lane");
						sw.WriteLine("LaneId" + "\t" + al.LaneId.ToString());

						// lane adjacency
						if (al.LaneOnLeft != null)
							sw.WriteLine("LaneOnLeft" + "\t" + al.LaneOnLeft.ToString());
						if (al.LaneOnRight != null)
							sw.WriteLine("LaneOnRight" + "\t" + al.LaneOnRight.ToString());
 
						// lane boundaries
						sw.WriteLine("LeftBoundary" + "\t" + al.BoundaryLeft.ToString());
						sw.WriteLine("RightBoundary" + "\t" + al.BoundaryRight.ToString());

						foreach (ArbiterWaypoint awp in al.Waypoints.Values)
						{
							if(awp.IsStop)
								sw.WriteLine("Stop" + "\t" + awp.WaypointId);
						}

						sw.WriteLine("Waypoints");

						foreach (ArbiterWaypoint awp in al.Waypoints.Values)
						{
							sw.WriteLine(awp.WaypointId + "\t" + awp.Position.X.ToString("F6") + "\t" + awp.Position.Y.ToString("F6"));
						}

						sw.WriteLine("End_Waypoints");

						foreach (ArbiterLanePartition alp in al.Partitions)
						{
							sw.WriteLine("Partition");

							sw.WriteLine("PartitionId" + "\t" + alp.Initial.WaypointId.ToString() + "--" + alp.Final.WaypointId.ToString());

							if (alp.NonLaneAdjacentPartitions != null && alp.NonLaneAdjacentPartitions.Count != 0)
							{
								sw.WriteLine("NonLaneAdjacentPartitions");

								foreach (ArbiterLanePartition nlap in alp.NonLaneAdjacentPartitions)
								{
									sw.WriteLine(nlap.Initial.WaypointId.ToString() + "--" + nlap.Final.WaypointId.ToString());
								}

								sw.WriteLine("End_NonLaneAdjacentPartitions");
							}

							sw.WriteLine("End_Partition");
						}

						sw.WriteLine("End_Lane");
					}

					sw.WriteLine("End_Way");
				}

				sw.WriteLine("End_Segment");
			}
		}

		/// <summary>
		/// Writes information about zones to the graph
		/// </summary>
		/// <param name="sw"></param>
		private void WriteZoneInformation(StreamWriter sw)
		{
			foreach (ArbiterZone az in roadNetwork.ArbiterZones.Values)
			{
				sw.WriteLine("Zone");
				sw.WriteLine("ZoneId" + "\t" + az.ZoneId.ToString());

				sw.WriteLine("Perimeter");
				sw.WriteLine("PerimeterId" + "\t" + az.Perimeter.PerimeterId.ToString());

				foreach (ArbiterPerimeterWaypoint apw in az.Perimeter.PerimeterPoints.Values)
				{
					sw.WriteLine(apw.WaypointId.ToString() + "\t" + apw.Position.X.ToString("F6") + "\t" + apw.Position.Y.ToString("F6"));
				}

				sw.WriteLine("End_Perimeter");

				sw.WriteLine("End_Zone");
			}
		}

		/// <summary>
		/// Writes interconnect information to the graph
		/// </summary>
		/// <param name="sw"></param>
		private void WriteInterconnectInformation(StreamWriter sw)
		{
			sw.WriteLine("Interconnects");

			foreach (ArbiterInterconnect ai in roadNetwork.ArbiterInterconnects.Values)
			{
				sw.WriteLine(ai.InitialGeneric.ToString() + "\t" + ai.FinalGeneric.ToString());
			}

			sw.WriteLine("End_Interconnects");
		}
	}

	[Serializable]
	public class SceneZonePartition : IConnectAreaWaypoints
	{
		public ArbiterZone Zone;
		private ArbiterPerimeterWaypoint[] apws;

		public SceneZonePartition(ArbiterZone az)
		{
			this.Zone = az;
			apws = new ArbiterPerimeterWaypoint[Zone.Perimeter.PerimeterPoints.Count];
			Zone.Perimeter.PerimeterPoints.Values.CopyTo(apws, 0);
		}

		#region IConnectAreaWaypoints Members

		public IConnectAreaWaypointsId ConnectionId
		{
			get 
			{
				return new ArbiterInterconnectId(apws[0].WaypointId, apws[apws.Length - 1].WaypointId);
			}
		}

		public IArbiterWaypoint InitialGeneric
		{
			get { return apws[0]; }
		}

		public IArbiterWaypoint FinalGeneric
		{
			get { return apws[apws.Length - 1]; }
		}

		public List<ArbiterUserPartition> UserPartitions
		{
			get
			{
				return new List<ArbiterUserPartition>();
			}
			set
			{
				
			}
		}

		public double DistanceTo(Coordinates loc)
		{
			return this.Zone.Perimeter.PerimeterPolygon.Center.DistanceTo(loc);
		}

		public double DistanceTo(IConnectAreaWaypoints icaw)
		{
			return icaw.DistanceTo(this.Zone.Perimeter.PerimeterPolygon.Center);
		}

		#endregion

		public override string ToString()
		{
			return Zone.ToString();
		}

		#region IConnectAreaWaypoints Members


		public NavigationBlockage Blockage
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

		#endregion

		#region IConnectAreaWaypoints Members


		public ArbiterInterconnect ToInterconnect
		{
			get { return new ArbiterInterconnect(this.InitialGeneric, this.FinalGeneric); }
		}

		#endregion
	}
}
