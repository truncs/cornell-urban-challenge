using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Arbiter.ArbiterRoads;
using RndfEditor.Forms;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.RndfNetwork;
using RndfToolkit;
using ArbiterTools;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using RndfEditor.Common;

namespace RndfEditor.Tools
{
	/// <summary>
	/// Pullout of the safety area
	/// </summary>
	[Serializable]
	public struct LanePoint
	{
		public ArbiterLane Al;
		public Coordinates Clicked;
	}

	/// <summary>
	/// pulls out an intersection
	/// </summary>
	/// <remarks>
	/// We can wrap intersection helper points, or the endpoints of safety zones to be a part of an intersection
	/// </remarks>
	public class IntersectionPulloutTool : IDisplayObject, IEditorTool
	{
		#region Intersection Tool

		// items to help with intersections
		private ArbiterRoadNetwork arn;
		private RoadDisplay rd;
		public Editor ed;
		private IntersectionToolbox it;
		
		// items to help with wrapping
		public List<Coordinates> WrappingHelpers;

		// what to use during wrapping
		public Coordinates? WrapInitial;
		public Coordinates? WrapFinal;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arn"></param>
		/// <param name="rd"></param>
		/// <param name="ed"></param>
		public IntersectionPulloutTool(ArbiterRoadNetwork arn, RoadDisplay rd, Editor ed, bool show)
		{
			// set helpers we can access
			this.arn = arn;
			this.rd = rd;
			this.ed = ed;

			// helpers to wrap intersections for polygons
			this.WrappingHelpers = new List<Coordinates>();

			// create toolbox
			if (show)
			{
				it = new IntersectionToolbox(arn, rd, ed);
				it.Show();
			}
		}

		public IntersectionPulloutTool(ArbiterRoadNetwork arn)
		{
			this.arn = arn;
		}

		/// <summary>
		/// Intersection toolbox
		/// </summary>
		public IntersectionToolbox Toolbox
		{
			get { return it; }
			set { it = value; }
		}

		/// <summary>
		/// the intersection toolbox mode
		/// </summary>
		public InterToolboxMode Mode
		{
			get { return it.Mode; }
		}

		/// <summary>
		/// finish the wrapping and cretae the intersection
		/// </summary>
		public void FinalizeIntersection(Polygon interPoly, ArbiterIntersection ai)
		{
			// check poly not null
			if (interPoly != null)
			{
				this.WrappingHelpers = new List<Coordinates>();

				Rect r = interPoly.CalculateBoundingRectangle();
				Coordinates bl = new Coordinates(r.x, r.y);
				Coordinates tr = new Coordinates(r.x + r.width, r.y + r.height);
				Polygon sqPoly = this.CreateSquarePolygon(bl, tr).Inflate(1.0);
				
				//interPoly = this.CreateIntersectionPolygon(interPoly);
				//interPoly = interPoly.Inflate(0.05);

				// retreive all exits involved in this intersection
				Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> exits = this.IntersectionExits(sqPoly);//interPoly);

				// make sure the inter contains an exit
				if (exits.Count > 0)
				{
					// make stopped exits, necessarily these are arbiter waypoints not perimeter points
					List<ArbiterStoppedExit> ases = this.CreateStoppedExits(exits.Values);

					// construct intersection id
					ITraversableWaypoint[] exitArray = new ITraversableWaypoint[exits.Count];
					exits.Values.CopyTo(exitArray, 0);
					ArbiterIntersectionId aii = new ArbiterIntersectionId(exitArray[0].Exits[0].InterconnectId);

					// determine incoming lanes
					Dictionary<ArbiterLane, LinePath.PointOnPath> incoming = this.DetermineIncoming(interPoly);

					// create the intersection
					ai.IntersectionPolygon = interPoly;
					ai.Center = interPoly.BoundingCircle.center;
					ai.StoppedExits = ases;
					ai.IncomingLanePoints = incoming;
					ai.AllExits = exits;
					ai.AllEntries = this.IntersectionEntries(sqPoly);
					ai.PriorityLanes = this.DetermineInvolved(exits.Values, incoming);					

					// update poly
					//this.UpdateIntersectionPolygon(ai);

					try
					{
						List<Polygon> ps = new List<Polygon>();
						foreach (ITraversableWaypoint itw in exits.Values)
						{
							foreach (ArbiterInterconnect ait in itw.Exits)
							{
								ps.Add(ait.TurnPolygon);
							}
						}
						ai.IntersectionPolygon = this.GetIntersectionPolygon(ps, ai.AllExits, ai.AllEntries);

						if (ai.IntersectionPolygon.IsComplex)
						{
							EditorOutput.WriteLine("Intersection polygon complex, defaulting");
							throw new Exception("complex polygon exception");
						}
					}
					catch (Exception)
					{
						EditorOutput.WriteLine("Error in union polygon generation, using better default");

						try
						{
							this.UpdateIntersectionPolygon(ai);
						}
						catch (Exception)
						{
							EditorOutput.WriteLine("Error in my simple polygon generation, plain default");
							List<Coordinates> cs = new List<Coordinates>();
							foreach (ITraversableWaypoint itw in ai.AllEntries.Values)
								cs.Add(itw.Position);
							foreach (ITraversableWaypoint itw in ai.AllExits.Values)
							{
								cs.Add(itw.Position);
								foreach (ArbiterInterconnect aint in itw.Exits)
								{
									cs.AddRange(aint.TurnPolygon);
								}
							}
							ai.IntersectionPolygon = Polygon.GrahamScan(cs);
						}
					}

					// add intersection
					/*arn.ArbiterIntersections.Add(aii, ai);

					// add to exit lookup
					foreach (IAreaSubtypeWaypointId awi in exits.Keys)
						arn.IntersectionLookup.Add(awi, ai);

					// add to display objects
					arn.DisplayObjects.Add(ai);
					rd.AddDisplayObject(ai);*/
				}
			}
		}

		/// <summary>
		/// finish the wrapping and cretae the intersection
		/// </summary>
		public void FinalizeIntersection()
		{
			// finalize the intersection polygon
			Polygon sq = this.CreateSquarePolygon(this.WrapInitial.Value, this.WrapFinal.Value).Inflate(1.0);
			Polygon interPoly = this.CreateIntersectionPolygon(sq);				

			// check poly not null
			if (interPoly != null)
			{
				// inflate the inter poly
				interPoly = interPoly.Inflate(0.05);

				// retreive all exits involved in this intersection
				Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> exits = this.IntersectionExits(sq);

				// make sure the inter contains an exit
				if (exits.Count > 0)
				{
					// make stopped exits, necessarily these are arbiter waypoints not perimeter points
					List<ArbiterStoppedExit> ases = this.CreateStoppedExits(exits.Values);

					// construct intersection id
					ITraversableWaypoint[] exitArray = new ITraversableWaypoint[exits.Count];
					exits.Values.CopyTo(exitArray, 0);
					ArbiterIntersectionId aii = new ArbiterIntersectionId(exitArray[0].Exits[0].InterconnectId);

					// determine incoming lanes
					Dictionary<ArbiterLane, LinePath.PointOnPath> incoming = this.DetermineIncoming(interPoly);

					// create the intersection
					ArbiterIntersection ai = new ArbiterIntersection(
						interPoly,
						ases,
						this.DetermineInvolved(exits.Values, incoming),
						incoming,
						exits,
						interPoly.Center,
						aii,
						arn,
						this.IntersectionEntries(sq)
						);

					// create safety zones
					this.CreateSafetyImplicitZones(ai);
										
					// update poly
					//this.UpdateIntersectionPolygon(ai);
					
					/*List<Polygon> ps = new List<Polygon>();
					foreach (ITraversableWaypoint itw in exits.Values)
					{
						foreach (ArbiterInterconnect ait in itw.Exits)
						{
							ps.Add(ait.TurnPolygon);
						}
					}
					ai.IntersectionPolygon = this.GetIntersectionPolygon(ps, ai.AllExits, ai.AllEntries);*/
					//ai.IntersectionPolygon = UrbanChallenge.Arbiter.Core.Common.Tools.PolygonToolkit.PolygonUnion(ps);
					try
					{
						List<Polygon> ps = new List<Polygon>();
						foreach (ITraversableWaypoint itw in exits.Values)
						{
							foreach (ArbiterInterconnect ait in itw.Exits)
							{
								ps.Add(ait.TurnPolygon);
							}
						}
						ai.IntersectionPolygon = this.GetIntersectionPolygon(ps, ai.AllExits, ai.AllEntries);

						if (ai.IntersectionPolygon.IsComplex)
						{
							EditorOutput.WriteLine("Intersection polygon complex, defaulting");
							throw new Exception("complex polygon exception");
						}
					}
					catch (Exception)
					{
						EditorOutput.WriteLine("Error in union polygon generation, using better default");

						try
						{
							this.UpdateIntersectionPolygon(ai);
						}
						catch (Exception)
						{
							EditorOutput.WriteLine("Error in my simple polygon generation, plain default");
							List<Coordinates> cs = new List<Coordinates>();
							foreach (ITraversableWaypoint itw in ai.AllEntries.Values)
								cs.Add(itw.Position);
							foreach (ITraversableWaypoint itw in ai.AllExits.Values)
							{
								cs.Add(itw.Position);
								foreach (ArbiterInterconnect aint in itw.Exits)
								{
									cs.AddRange(aint.TurnPolygon);
								}
							}
							ai.IntersectionPolygon = Polygon.GrahamScan(cs);
						}
					}

					try
					{
						// add intersection
						arn.ArbiterIntersections.Add(aii, ai);

						// add to exit lookup
						foreach (IAreaSubtypeWaypointId awi in exits.Keys)
						{
							if (arn.IntersectionLookup.ContainsKey(awi))
								arn.IntersectionLookup[awi] = ai;
							else
								arn.IntersectionLookup.Add(awi, ai);
						}

						// add to display objects
						arn.DisplayObjects.Add(ai);
						rd.AddDisplayObject(ai);
					}
					catch (Exception e)
					{
						EditorOutput.WriteLine("Error adding intersection: " + aii.ToString());
						EditorOutput.WriteLine("Error adding intersection: " + e.ToString());
					}
				}
			}
			
			// reset the tool
			this.it.ResetIcons();
			this.WrapFinal = null;
			this.WrapInitial = null;
		}

		private Polygon GetIntersectionPolygon(List<Polygon> turnPolygons,
			Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> exits,
			Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> entries)
		{
			// get segments involved
			List<ArbiterSegment> segments = new List<ArbiterSegment>();

			// exit and entry polygon
			List<Coordinates> eePolygonCoords = new List<Coordinates>();

			#region Segments Involved in the Intersection Polygon

			foreach (Polygon turnP in turnPolygons)
				eePolygonCoords.AddRange(turnP);

			foreach (ITraversableWaypoint itw in exits.Values)
			{
				//eePolygonCoords.Add(itw.Position);

				if (itw is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)itw;
					if (aw.NextPartition != null && !segments.Contains(aw.Lane.Way.Segment))
					{
						segments.Add(aw.Lane.Way.Segment);
					}
				}
			}
			foreach (ITraversableWaypoint itw in entries.Values)
			{
				//eePolygonCoords.Add(itw.Position);

				if (itw is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)itw;
					if (aw.PreviousPartition != null && !segments.Contains(aw.Lane.Way.Segment))
					{
						segments.Add(aw.Lane.Way.Segment);
					}
				}
			}



			#endregion

			// polygon of ee coords
			Polygon eePolygon = Polygon.GrahamScan(eePolygonCoords);

			// final polygon initial
			Polygon basePolygon = null;

			// determine intersection of ee polygon with lanes of segments
			foreach (ArbiterSegment asg in segments)
			{
				// segment polygon
				Polygon segmentPoly = null;

				//  lanes in seg
				foreach (ArbiterLane al in asg.Lanes.Values)
				{
					try
					{
						// get intersection of lane poly with ee poly
						Polygon iPoly = PolygonToolkit.PolygonIntersection(al.LanePolygon, eePolygon);

						// check area
						if (Math.Abs(iPoly.GetArea()) > 5.0)
						{
							// check if base poly exists
							if (segmentPoly == null)
								segmentPoly = iPoly;
							else
							{
								// check intersection of lane with the base polygon
								segmentPoly.AddRange(iPoly);
								segmentPoly = Polygon.GrahamScan(segmentPoly);
							}
						}
					}
					catch (Exception) { }
				}

				if (Math.Abs(segmentPoly.GetArea()) > 5.0)
				{
					if (basePolygon == null)
						basePolygon = segmentPoly;
					else
					{
						basePolygon = PolygonToolkit.PolygonUnion(new List<Polygon>(new Polygon[] { basePolygon, segmentPoly }));
					}
				}
			}

			// create final array
			List<Polygon> endPolys = new List<Polygon>();
			if(basePolygon != null)
				endPolys.Add(basePolygon);
			endPolys.AddRange(turnPolygons);
			Polygon finalTmp = PolygonToolkit.PolygonUnion(endPolys);
			endPolys.Insert(0, finalTmp);
			
			return PolygonToolkit.PolygonUnion(endPolys);
			//return eePolygon;
		}

		/// <summary>
		/// Determine the involved lanes with any interconnect
		/// </summary>
		/// <param name="exits"></param>
		/// <param name="incoming"></param>
		/// <returns></returns>
		/// <remarks>TODO: implement</remarks>
		private Dictionary<ArbiterInterconnect, List<IntersectionInvolved>> DetermineInvolved(IEnumerable<ITraversableWaypoint> exits, Dictionary<ArbiterLane, LinePath.PointOnPath> incoming)
		{
			// final mapping of interconnects to priority lanes that list of priority areas over each interconnect
			Dictionary<ArbiterInterconnect, List<IntersectionInvolved>> priority = new Dictionary<ArbiterInterconnect, List<IntersectionInvolved>>();

			// 1. Get list of all lanes incoming to the intersection
			List<ArbiterLane> als = new List<ArbiterLane>();
			foreach (ArbiterLane al in incoming.Keys)
			{
				als.Add(al);
			}

			// 2. Get all exits for each area
			Dictionary<IVehicleArea, List<ITraversableWaypoint>> exitLookup = new Dictionary<IVehicleArea, List<ITraversableWaypoint>>();
			foreach (ITraversableWaypoint aw in exits)
			{
				if (exitLookup.ContainsKey(aw.VehicleArea))
				{
					exitLookup[aw.VehicleArea].Add(aw);
				}
				else
				{
					// add all exits
					List<ITraversableWaypoint> laneExits = new List<ITraversableWaypoint>();
					laneExits.Add(aw);
					exitLookup.Add(aw.VehicleArea, laneExits);
				}
			}

			// 3. loop through exits and determine priority areas above them
			foreach (ITraversableWaypoint aw in exits)
			{
				/*if (aw is ArbiterWaypoint && ((ArbiterWaypoint)aw).WaypointId.Equals(new ArbiterWaypointId(19, new ArbiterLaneId(2, new ArbiterWayId(2, new ArbiterSegmentId(6))))))
				{
					Console.WriteLine("");
				}*/

				// 3.1 loop through interconnects from exits
				foreach (ArbiterInterconnect ai in aw.Exits)
				{
					// exit priority areas
					List<IntersectionInvolved> priorityAreas = new List<IntersectionInvolved>();

					// add explicit interconnects
					priorityAreas.AddRange(this.laneOverlaps(als, ai, exits));

					// implicit intersections
					List<IntersectionInvolved> implicitInvolved = this.nonStopOverlaps(exits, ai);
					foreach (IntersectionInvolved ii in implicitInvolved)
					{
						// make sure not contained already
						if (!priorityAreas.Contains(ii))
						{
							// add
							priorityAreas.Add(ii);
						}
						// if already contained, replace
						else if(ii.CompareTo(priorityAreas[priorityAreas.IndexOf(ii)]) == -1)
						{
							priorityAreas.Remove(ii);
							priorityAreas.Add(ii);
						}
					}

					// add the priority overlaps to the exits
					priority.Add(ai, priorityAreas);
				}

				// 3.2 check continuation if exists
				if(aw.IsStop)
				{
					// get waypoint
					ArbiterWaypoint waypoint = (ArbiterWaypoint)aw;

					// check if next partition exists
					if(waypoint.NextPartition != null)
					{
						// exit priority areas
						List<IntersectionInvolved> priorityAreas = new List<IntersectionInvolved>();

						// fake interconnect
						ArbiterInterconnect fakeAi = new ArbiterInterconnect(waypoint, waypoint.NextPartition.Final, ArbiterTurnDirection.Straight);

						#region New

						// list of next intersection ivolved
						List<IntersectionInvolved> nextII = new List<IntersectionInvolved>();

						// list of fake interconnects defining the continuation
						List<ArbiterInterconnect> fakeAis = new List<ArbiterInterconnect>();

						// add the next partition by default
						fakeAis.Add(fakeAi);

						// entries into lane of fake ai involved in the intersection
						List<ArbiterWaypoint> laneEntries = new List<ArbiterWaypoint>();						
						foreach (ITraversableWaypoint itw in exits)
						{
							foreach (ArbiterInterconnect aiTmp in itw.Exits)
							{
								if (aiTmp.FinalGeneric is ArbiterWaypoint)
								{
									ArbiterWaypoint awTmp = (ArbiterWaypoint)aiTmp.FinalGeneric;

									if (awTmp.Lane.Equals(waypoint.Lane) && !awTmp.Equals(waypoint.NextPartition.Final) && !laneEntries.Contains(awTmp))
									{
										ArbiterInterconnect tmpFake = new ArbiterInterconnect(waypoint, awTmp, ArbiterTurnDirection.Straight);
										if (!fakeAis.Contains(tmpFake))
										{
											laneEntries.Add(awTmp);
											fakeAis.Add(tmpFake);
										}
									}
								}
							}
						}

						// loop through fake ais adding ii
						foreach (ArbiterInterconnect fake in fakeAis)
						{
							// explicit and explicit add to tmp
							List<IntersectionInvolved> tmpIis = this.laneOverlaps(als, fake, exits);
							tmpIis.AddRange(this.nonStopOverlaps(exits, fake));

							// check and add
							foreach (IntersectionInvolved tmpIi in tmpIis)
							{
								if (!nextII.Contains(tmpIi))
									nextII.Add(tmpIi);
								else if (nextII[nextII.IndexOf(tmpIi)].Exit == null)
								{
									nextII.Remove(tmpIi);
									nextII.Add(tmpIi);
								}
							}
						}

						// add to priority areas
						priorityAreas.AddRange(nextII);

						#endregion

						#region Old

						/*
							// fake interconnect
							ArbiterInterconnect fakeAi = new ArbiterInterconnect(waypoint, waypoint.NextPartition.Final);

							// add explicit interconnects
							priorityAreas.AddRange(this.laneOverlaps(als, fakeAi));

							// add implicit interconnects
							priorityAreas.AddRange(this.nonStopOverlaps(exits, fakeAi));

							// add the priority overlaps to the exits
							priority.Add(fakeAi, priorityAreas);
						*/

						#endregion

						// add the priority overlaps to the exits
						if (priority.ContainsKey(fakeAi))
							EditorOutput.WriteLine("Error adding interconnect: " + fakeAi.ToString() + " to priority areas as key already existed, check priorities");
						else
							priority.Add(fakeAi, priorityAreas);
					}
				}
			}

			// return the final priorities
			return priority;
		}

		/// <summary>
		/// Get lanes that overlap with hte interconnect
		/// </summary>
		/// <param name="lanes"></param>
		/// <param name="ai"></param>
		/// <returns></returns>
		private List<IntersectionInvolved> laneOverlaps(List<ArbiterLane> lanes, ArbiterInterconnect ai, IEnumerable<ITraversableWaypoint> exits)
		{
			List<IntersectionInvolved> overlaps = new List<IntersectionInvolved>();
			LineSegment aiSegment = new LineSegment(ai.InitialGeneric.Position, ai.FinalGeneric.Position);

			if (ai.FinalGeneric is ArbiterWaypoint)
			{
				ArbiterWaypoint fin = (ArbiterWaypoint)ai.FinalGeneric;
				if (fin.PreviousPartition != null && !this.FoundStop(fin.PreviousPartition.Initial, exits, fin.Lane))
				{
					ArbiterWaypoint foundExit = null;
					foreach (ITraversableWaypoint itw in exits)
					{
						if (itw is ArbiterWaypoint && ((ArbiterWaypoint)itw).Lane.Equals(fin.Lane) && 
							(foundExit == null || itw.Position.DistanceTo(fin.Position) < foundExit.Position.DistanceTo(fin.Position)))
							foundExit = (ArbiterWaypoint)itw;
					}

					if (foundExit != null)
						overlaps.Add(new IntersectionInvolved(foundExit, fin.Lane, ArbiterTurnDirection.Straight));
					else
						overlaps.Add(new IntersectionInvolved(fin.Lane));
				}
			}

			foreach (ArbiterLane al in lanes)
			{
				if (!(ai.InitialGeneric is ArbiterWaypoint) || !((ArbiterWaypoint)ai.InitialGeneric).Lane.Equals(al))
				{
					foreach (LineSegment ls in al.LanePath().GetSegmentEnumerator())
					{
						Coordinates interCoord;
						bool intersect = ls.Intersect(aiSegment, out interCoord);

						/*if (intersect)
						{
							Console.WriteLine("");
						}*/

						if (intersect && ai.IsInside(interCoord) && !overlaps.Contains(new IntersectionInvolved(al)) &&
							ai.InterconnectPath.GetClosestPoint(interCoord).Location.DistanceTo(interCoord) < 0.00001 && al.IsInside(interCoord))
						{
							// get closest partition
							ArbiterLanePartition alp = al.GetClosestPartition(interCoord);
							if (!this.FoundStop(alp.Initial, exits, al))
							{
								ArbiterWaypoint foundExit = null;
								foreach (ITraversableWaypoint itw in exits)
								{
									if (itw is ArbiterWaypoint && ((ArbiterWaypoint)itw).Lane.Equals(alp.Lane) &&
										(foundExit == null || itw.Position.DistanceTo(interCoord) < foundExit.Position.DistanceTo(interCoord)))
										foundExit = (ArbiterWaypoint)itw;
								}

								if (foundExit != null)
									overlaps.Add(new IntersectionInvolved(foundExit, alp.Lane, ArbiterTurnDirection.Straight));
								else
									overlaps.Add(new IntersectionInvolved(al));
							}
						}
					}
				}
			}

			return overlaps;
		}

		/// <summary>
		/// Check if found a stop in a lane as part of exits or a test waypoint
		/// </summary>
		/// <param name="initialTest"></param>
		/// <param name="exits"></param>
		/// <param name="lane"></param>
		/// <returns></returns>
		private bool FoundStop(ArbiterWaypoint initialTest, IEnumerable<ITraversableWaypoint> exits, ArbiterLane lane)
		{
			if (initialTest != null && initialTest.IsStop)
				return true;

			foreach (ITraversableWaypoint exit in exits)
			{
				if (exit is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)exit;
					if (aw.IsStop && aw.Lane.Equals(lane))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Check for all waypoints who have exit interconnects that overlaps input and no stop
		/// </summary>
		/// <param name="exits"></param>
		/// <param name="ai"></param>
		/// <returns></returns>
		private List<IntersectionInvolved> nonStopOverlaps(IEnumerable<ITraversableWaypoint> exits, ArbiterInterconnect ai)
		{
			// list of exits that have an interconnect which overlaps the interconnect input
			List<IntersectionInvolved> nonStopOverlapWaypoints = new List<IntersectionInvolved>();

			// get line of the interconnect
			Line aiLine = new Line(ai.InitialGeneric.Position, ai.FinalGeneric.Position);

			// loop over all exits
			foreach (ITraversableWaypoint exit in exits)
			{
				// make sure not our exit and the exit is not a stop and if exit and other are both waypoints then ways not the same
				if (!exit.Equals(ai.InitialGeneric) && !exit.IsStop && 
					((!(ai.InitialGeneric is ArbiterWaypoint) || !(exit is ArbiterWaypoint))
					|| !((ArbiterWaypoint)ai.InitialGeneric).Lane.Way.Equals(((ArbiterWaypoint)exit).Lane.Way)))
				{
					// get all interconnects of the exit
					foreach (ArbiterInterconnect tmp in exit.Exits)
					{
						// check relative priority that these are equal or lesser priority
						if (ai.ComparePriority(tmp) != -1)
						{
							// simple check if the interconnect's final is same as input final
							if (tmp.FinalGeneric.Equals(ai.FinalGeneric))
							{
								// check not already added
								if (!nonStopOverlapWaypoints.Contains(new IntersectionInvolved(((ITraversableWaypoint)tmp.FinalGeneric).VehicleArea)))
								{
									// add exit
									nonStopOverlapWaypoints.Add(new IntersectionInvolved(exit, exit.VehicleArea, tmp.TurnDirection));
								}
							}
							// otherwise check overlap of interconnects
							else
							{
								// get line of tmp interconnect
								Line tmpLine = new Line(tmp.InitialGeneric.Position, tmp.FinalGeneric.Position);

								// position of cross
								Coordinates intersectionPoint;

								// check intersection
								bool intersects = aiLine.Intersect(tmpLine, out intersectionPoint) && ai.IsInside(intersectionPoint);
								if (intersects)
								{
									// check not already added
									if (!nonStopOverlapWaypoints.Contains(new IntersectionInvolved(((ITraversableWaypoint)tmp.FinalGeneric).VehicleArea)))
									{
										// add exit
										nonStopOverlapWaypoints.Add(new IntersectionInvolved(exit, exit.VehicleArea, tmp.TurnDirection));
									}
								}
							}
						}
					}
				}
			}

			return nonStopOverlapWaypoints;
		}
			
		/// <summary>
		/// Gets all the incoming points to the intersection on each lane
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private Dictionary<ArbiterLane, LinePath.PointOnPath> DetermineIncoming(Polygon p)
		{
			Dictionary<ArbiterLane, LinePath.PointOnPath> incomingPoints = 
				new Dictionary<ArbiterLane,LinePath.PointOnPath>();
						

			foreach (ArbiterInterconnect ai in arn.ArbiterInterconnects.Values)
			{
				if (ai.InitialGeneric is ArbiterWaypoint && !incomingPoints.ContainsKey(((ArbiterWaypoint)ai.InitialGeneric).Lane))
				{
					if (p.IsInside(ai.InitialGeneric.Position))
					{
						ArbiterLane al = ((ArbiterWaypoint)ai.InitialGeneric).Lane;
						incomingPoints.Add(al, al.GetClosestPoint(ai.InitialGeneric.Position));
					}
				}

				if (ai.FinalGeneric is ArbiterWaypoint && !incomingPoints.ContainsKey(((ArbiterWaypoint)ai.FinalGeneric).Lane))
				{
					if (p.IsInside(ai.FinalGeneric.Position))
					{
						ArbiterLane al = ((ArbiterWaypoint)ai.FinalGeneric).Lane;
						incomingPoints.Add(al, al.GetClosestPoint(ai.FinalGeneric.Position));
					}
				}
			}

			foreach (ArbiterSafetyZone asz in arn.ArbiterSafetyZones)
			{
				if (p.IsInside(asz.lane.LanePath().GetPoint(asz.safetyZoneEnd)))
				{
					if (!incomingPoints.ContainsKey(asz.lane))
						incomingPoints.Add(asz.lane, asz.safetyZoneEnd);
				}
			}

			foreach (IArbiterWaypoint iaw in arn.ArbiterWaypoints.Values)
			{
				if (iaw is ArbiterWaypoint)
				{
					ArbiterWaypoint aw = (ArbiterWaypoint)iaw;

					if (!incomingPoints.ContainsKey(aw.Lane) && p.IsInside(aw.Position))
					{
						incomingPoints.Add(aw.Lane, aw.Lane.GetClosestPoint(aw.Position));
					}
				}
			}

			return incomingPoints;
		}

		/// <summary>
		/// Creates stopepd exits for an intersection
		/// </summary>
		/// <param name="exits"></param>
		/// <returns></returns>
		private List<ArbiterStoppedExit> CreateStoppedExits(IEnumerable<ITraversableWaypoint> exits)
		{
			// list of stopped exits
			List<ArbiterStoppedExit> ases = new List<ArbiterStoppedExit>();
			
			// loop exits
			foreach (ITraversableWaypoint itw in exits)
			{
				// check if exit
				if (itw.IsExit && itw.IsStop)
				{
					// cast
					ArbiterWaypoint aw = (ArbiterWaypoint)itw;					

					// partition vector
					Coordinates dVec = aw.PreviousPartition.Vector();

					// front
					Coordinates front = aw.Position + dVec.Normalize(3.0);

					// go back tahoe vl * 1.5
					Coordinates back = front - dVec.Normalize(TahoeParams.VL * 1.5);

					// get a vector to the right of the lane
					Coordinates rVec = dVec.Normalize(Math.Max(TahoeParams.T, aw.Lane.Width / 2.0)).Rotate90();

					// make poly coords
					List<Coordinates> exitCoords = new List<Coordinates>();
					Coordinates p1 = front - rVec;
					Coordinates p2 = front + rVec;
					Coordinates p3 = back + rVec;
					Coordinates p4 = back - rVec;
					exitCoords.Add(p1);
					exitCoords.Add(p2);
					exitCoords.Add(p3);
					exitCoords.Add(p4);

					// create the exit's polygon
					Polygon exitP = Polygon.GrahamScan(exitCoords);

					// add the stopped exit
					ases.Add(new ArbiterStoppedExit(aw, exitP));
				}
			}

			// return
			return ases;
		}

		/// <summary>
		/// Gets exits within polygon
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> IntersectionExits(Polygon p)
		{
			Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> aws = new Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint>();

			foreach (IArbiterWaypoint iaw in arn.ArbiterWaypoints.Values)
			{
				if (iaw is ITraversableWaypoint)
				{
					ITraversableWaypoint itw = (ITraversableWaypoint)iaw;

					if (itw.IsExit && p.IsInside(iaw.Position))
					{
						aws.Add(iaw.AreaSubtypeWaypointId, itw);
					}
				}
			}

			return aws;
		}

		/// <summary>
		/// gets entries within polygon
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> IntersectionEntries(Polygon p)
		{
			Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint> entries = new Dictionary<IAreaSubtypeWaypointId, ITraversableWaypoint>();

			foreach (IArbiterWaypoint iaw in arn.ArbiterWaypoints.Values)
			{
				if (iaw is ITraversableWaypoint)
				{
					ITraversableWaypoint aw = (ITraversableWaypoint)iaw;

					if (aw.IsEntry && p.IsInside(aw.Position))
					{
						entries.Add(aw.AreaSubtypeWaypointId, aw);
					}
				}
			}

			return entries;
		}

		/// <summary>
		/// Creates intersection polygon from input quare polygon
		/// </summary>
		/// <param name="square"></param>
		/// <returns></returns>
		private Polygon CreateIntersectionPolygon(Polygon square)
		{
			// coordinates we hold inside of the square represented by p0 - p3
			List<Coordinates> cSq = new List<Coordinates>();

			// check waypoints
			foreach (IArbiterWaypoint iaw in arn.ArbiterWaypoints.Values)
			{
				if (square.IsInside(iaw.Position) && !cSq.Contains(iaw.Position))
				{
					cSq.Add(iaw.Position);
				}
			}

			// check safety areas
			foreach (ArbiterSafetyZone asz in arn.ArbiterSafetyZones)
			{
				if (square.IsInside(asz.lane.LanePath().GetPoint(asz.safetyZoneEnd)) && !asz.isExit && !cSq.Contains(asz.lane.LanePath().GetPoint(asz.safetyZoneEnd)))
				{
					cSq.Add(asz.lane.LanePath().GetPoint(asz.safetyZoneEnd));
				}
			}

			// check wrapping helpers
			foreach (Coordinates c in this.WrappingHelpers)
			{
				// check if the coordinate is inside the square
				if (square.IsInside(c) && !cSq.Contains(c))
				{
					cSq.Add(c);
				}
			}

			// check num of pts
			if (cSq.Count >= 3)
			{
				// wrap the polygon
				Polygon interPoly = Polygon.GrahamScan(cSq);

				// return
				return interPoly;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Creates intersection bounding polygon
		/// </summary>
		/// <param name="p0"></param>
		/// <param name="p3"></param>
		/// <returns></returns>
		private Polygon CreateSquarePolygon(Coordinates p0, Coordinates p3)
		{
			// coordinates of square p0 - p3
			Coordinates p1 = p0 + (new Coordinates(p3.X - p0.X, 0));
			Coordinates p2 = p0 + (new Coordinates(0, p3.Y - p0.Y));

			// create square coords
			List<Coordinates> sqCoords = new List<Coordinates>();
			sqCoords.Add(p0);
			sqCoords.Add(p1);
			sqCoords.Add(p3);
			sqCoords.Add(p2);

			// create square polygon
			Polygon square = new Polygon(sqCoords, CoordinateMode.AbsoluteProjected);

			// return 
			return square;
		}

		private void UpdateIntersectionPolygon(ArbiterIntersection aInt)
		{
			// get previous polygon
			Polygon interPoly = new Polygon();

			// add all turn points
			foreach (ArbiterInterconnect ai in aInt.PriorityLanes.Keys)
			{
				interPoly.AddRange(ai.TurnPolygon);
			}

			// wrap it to get intersection polygon
			interPoly = Polygon.GrahamScan(interPoly);

			// get outer edges of poly
			List<LinePath> polyEdges = new List<LinePath>();
			for (int i = 0; i < interPoly.Count; i++)
			{
				Coordinates init = interPoly[i];
				Coordinates fin = i == interPoly.Count - 1 ? interPoly[0] : interPoly[i + 1];
				polyEdges.Add(new LinePath(new Coordinates[] { init, fin }));
			}

			// get all edges of all the turns
			List<LinePath> other = new List<LinePath>();
			foreach (ArbiterInterconnect ai in aInt.PriorityLanes.Keys)
			{
				for (int i = 0; i < ai.TurnPolygon.Count; i++)
				{
					Coordinates init = ai.TurnPolygon[i];
					Coordinates fin = i == ai.TurnPolygon.Count - 1 ? ai.TurnPolygon[0] : ai.TurnPolygon[i + 1];
					other.Add(new LinePath(new Coordinates[] { init, fin }));
				}
			}

			// test points
			List<Coordinates> testPoints = new List<Coordinates>();

			// path segs of inner turns
			List<LinePath> innerTurnPaths = new List<LinePath>();

			// check all inner points against all turn edges
			foreach (ArbiterInterconnect ai in aInt.PriorityLanes.Keys)
			{
				// check for inner point
				if (ai.InnerCoordinates.Count == 3)
				{
					// inner point of the turn on the inside
					testPoints.Add(ai.InnerCoordinates[1]);

					// add to paths
					innerTurnPaths.Add(new LinePath(new Coordinates[] { ai.InnerCoordinates[0], ai.InnerCoordinates[1] }));
					innerTurnPaths.Add(new LinePath(new Coordinates[] { ai.InnerCoordinates[1], ai.InnerCoordinates[2] }));
				}
			}

			// list of used segments
			List<LinePath> closed = new List<LinePath>();
			
			// check all other intersections of turn polygon edges
			foreach (LinePath seg in innerTurnPaths)
			{
				foreach (LinePath otherEdge in other)
				{
					if (!seg.Equals(otherEdge) && !closed.Contains(otherEdge))
					{
						double x1 = seg[0].X;
						double y1 = seg[0].Y;
						double x2 = seg[1].X;
						double y2 = seg[1].Y;
						double x3 = otherEdge[0].X;
						double y3 = otherEdge[0].Y;
						double x4 = otherEdge[1].X;
						double y4 = otherEdge[1].Y;

						// get if inside both
						double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
						double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

						if (0.05 < ua && ua < 0.95 && 0.5 < ub && ub < 0.95)
						{
							double x = x1 + ua * (x2 - x1);
							double y = y1 + ua * (y2 - y1);
							testPoints.Add(new Coordinates(x, y));
						}
					}
				}

				closed.Add(seg);
			}

			// loop through test points
			foreach(Coordinates inner in testPoints)
			{
				// list of replacements
				List<LinePath> toReplace = new List<LinePath>();

				// loop through outer
				foreach (LinePath edge in polyEdges)
				{
					// flag to replace
					bool replace = false;

					// make sure this goes to a valid edge section
					LinePath.PointOnPath closest = edge.GetClosestPoint(inner);
					if (!closest.Equals(edge.StartPoint) && !closest.Equals(edge.EndPoint) &&
						!(closest.Location.DistanceTo(edge.StartPoint.Location)  < 0.5) &&
						!(closest.Location.DistanceTo(edge.EndPoint.Location)  < 0.5))
					{
						// create seg (extend a bit)
						Coordinates expansion = closest.Location - inner;
						LinePath seg = new LinePath(new Coordinates[] { inner, closest.Location + expansion.Normalize(1.0) });

						// set flag
						replace = true;

						// loop through other edges
						foreach (LinePath otherEdge in other)
						{
							double x1 = seg[0].X;
							double y1 = seg[0].Y;
							double x2 = seg[1].X;
							double y2 = seg[1].Y;
							double x3 = otherEdge[0].X;
							double y3 = otherEdge[0].Y;
							double x4 = otherEdge[1].X;
							double y4 = otherEdge[1].Y;

							// get if inside both
							double ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
							double ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));

							if (0.01 < ua && ua < 0.99 && 0 < ub && ub < 1)
							{
								replace = false;
							}
						}
					}

					// check if should replace
					if (replace)
					{
						// add analyzed to adjacent
						toReplace.Add(edge);
					}
				}

				// loop through replacements
				foreach (LinePath ll in toReplace)
				{
					LinePath[] tmpArrayPoly = new LinePath[polyEdges.Count];					
					polyEdges.CopyTo(tmpArrayPoly);
					List<LinePath> tmpPoly = new List<LinePath>(tmpArrayPoly);

					// get index of edge
					int index = tmpPoly.IndexOf(ll);

					// remove
					tmpPoly.RemoveAt(index);

					// add correctly to outer
					tmpPoly.Insert(index, new LinePath(new Coordinates[] { ll[0], inner }));
					tmpPoly.Insert(index + 1, new LinePath(new Coordinates[] { inner, ll[1] }));

					// poly
					Polygon temp = new Polygon();
					foreach(LinePath lpTemp in tmpPoly)
						temp.Add(lpTemp[1]);
					temp.Inflate(0.5);

					// make sure none of original outside
					bool ok = true;
					foreach (LinePath lp in other)
					{
						if (!temp.IsInside(lp[1]) && !temp.Contains(lp[1]))
							ok = false;
					}

					// set if created ok
					if (ok)
						polyEdges = tmpPoly;
				}				
			}

			// create final
			List<Coordinates> finalPoly = new List<Coordinates>();
			foreach (LinePath outerEdge in polyEdges)
				finalPoly.Add(outerEdge[1]);
			interPoly = new Polygon(finalPoly);

			aInt.IntersectionPolygon = interPoly;
			aInt.Center = interPoly.GetCentroid();
		}

		/// <summary>
		/// Shuts down the tool
		/// </summary>
		public void ShutDown()
		{
			if (!it.IsDisposed)
			{
				it.Close();
			}
		}

		public void CreateSafetyImplicitZones(ArbiterIntersection ai)
		{
			if (ai.StoppedExits.Count > 0)
			{
				foreach (ITraversableWaypoint itw in ai.AllExits.Values)
				{
					if (itw is ArbiterWaypoint && !itw.IsStop)
					{
						ArbiterWaypoint aw = (ArbiterWaypoint)itw;
						ArbiterLane al = aw.Lane;
						LinePath.PointOnPath end = aw.Lane.LanePath().GetClosestPoint(aw.Position);

						double dist = -30;
						LinePath.PointOnPath begin = al.LanePath().AdvancePoint(end, ref dist);
						if (dist != 0)
						{
							EditorOutput.WriteLine("safety zone too close to start of lane, setting start to start of lane: " + aw.ToString());
							begin = al.LanePath().StartPoint;
						}
						ArbiterSafetyZone asz = new ArbiterSafetyZone(al, end, begin);
						al.SafetyZones.Add(asz);
						al.Way.Segment.RoadNetwork.DisplayObjects.Add(asz);
						al.Way.Segment.RoadNetwork.ArbiterSafetyZones.Add(asz);

						if (aw != null && aw.IsExit == true)
						{
							asz.isExit = true;
							asz.Exit = aw;
						}

						// add to display
						this.rd.displayObjects.Add(asz);
					}
				}
			}
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			// wrapping helpers
			foreach (Coordinates c in this.WrappingHelpers)
			{
				DrawingUtility.DrawControlPoint(c, DrawingUtility.ColorArbiterIntersectionWrappingHelpers,
					null, System.Drawing.ContentAlignment.BottomCenter, ControlPointStyle.SmallCircle, g, t);
			}

			if (WrapFinal != null && WrapInitial != null)
			{
				// square
				Polygon square = this.CreateSquarePolygon(this.WrapInitial.Value, this.WrapFinal.Value);
				DrawingUtility.DrawControlPolygon(square, DrawingUtility.ColorArbiterIntersection, System.Drawing.Drawing2D.DashStyle.Solid, g, t);

				// inter poly
				Polygon interPoly = this.CreateIntersectionPolygon(square);

				// make sure inter poly not null before continuing
				if (interPoly != null)
				{
					DrawingUtility.DrawControlPolygon(interPoly, DrawingUtility.ColorArbiterIntersectionBoundaryPolygon, System.Drawing.Drawing2D.DashStyle.DashDotDot, g, t);
				}
				/*
				Coordinates c0 = WrapInitial.Value;
				Coordinates c1 = WrapInitial.Value + (new Coordinates(WrapFinal.Value.X - WrapInitial.Value.X, 0));
				Coordinates c2 = WrapInitial.Value + (new Coordinates(0, WrapFinal.Value.Y - WrapInitial.Value.Y));
				Coordinates c3 = WrapFinal.Value;
				List<Coordinates> cs = new List<Coordinates>();
				cs.Add(c0);
				cs.Add(c1);
				cs.Add(c3);
				cs.Add(c2);
				Polygon p = new Polygon(cs, CoordinateMode.AbsoluteProjected);

				DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterIntersection, System.Drawing.Drawing2D.DashStyle.Solid, c0, c1, g, t);
				DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterIntersection, System.Drawing.Drawing2D.DashStyle.Solid, c1, c3, g, t);
				DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterIntersection, System.Drawing.Drawing2D.DashStyle.Solid, c0, c2, g, t);
				DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterIntersection, System.Drawing.Drawing2D.DashStyle.Solid, c2, c3, g, t);

				List<Coordinates> interPolygonCoords = new List<Coordinates>();

				foreach (Coordinates c in this.WrappingHelpers)
				{
					if (p.IsInside(c))
					{
						interPolygonCoords.Add(c);
					}
				}

				Console.WriteLine("Coords");
				foreach (Coordinates c in interPolygonCoords)
				{
					Console.WriteLine(c.ToString());
				}
				Console.WriteLine("");

				List<BoundaryLine> boundaries = ArbiterTools.IntersectionToolkit.JarvisMarch(interPolygonCoords);
				List<Coordinates> newBounds = new List<Coordinates>();

				if (boundaries != null)
				{
					foreach (BoundaryLine bl in boundaries)
					{
						newBounds.Add(bl.p1);
						DrawingUtility.DrawColoredControlLine(DrawingUtility.ColorArbiterIntersection, System.Drawing.Drawing2D.DashStyle.DashDotDot, bl.p1, bl.p2, g, t);
					}
				}

				if (newBounds.Count >= 3)
				{
					p = new Polygon(newBounds, CoordinateMode.AbsoluteProjected);

					foreach (IArbiterWaypoint iaw in arn.ArbiterWaypoints.Values)
					{
						if (p.IsInside(iaw.Position))
						{
							DrawingUtility.DrawControlPoint(iaw.Position, DrawingUtility.ColorArbiterIntersection, null, System.Drawing.ContentAlignment.MiddleCenter, ControlPointStyle.SmallX, g, t);
						}
					}
				}*/
			}
		}

		public bool MoveAllowed
		{
			get { throw new Exception("The method or operation is not implemented."); }
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
			return true;
		}

		#endregion
	}
}
