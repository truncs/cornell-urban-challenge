using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.Common.Tools
{
	public static class LaneTools
	{
		public static Polygon DefaultLanePolygon(ArbiterLane al)
		{
			// fist get the right boundary of the lane
			LinePath lb = al.LanePath().ShiftLateral(-al.Width / 2.0);
			LinePath rb = al.LanePath().ShiftLateral(al.Width / 2.0);
			rb.Reverse();
			List<Coordinates> defaultPoly = lb;
			defaultPoly.AddRange(rb);

			// start the polygon
			Polygon poly = new Polygon(defaultPoly);
			return poly;
		}

		public static Polygon LanePolygon(ArbiterLane al)
		{
			// fist get the right boundary of the lane
			LinePath lb = al.LanePath().ShiftLateral(-al.Width / 2.0);
			LinePath rb = al.LanePath().ShiftLateral(al.Width / 2.0);
			rb.Reverse();
			List<Coordinates> defaultPoly = lb;
			defaultPoly.AddRange(rb);

			// start the polygon
			Polygon poly = new Polygon(defaultPoly);

			// loop through partitions
			foreach (ArbiterLanePartition alp in al.Partitions)
			{
				//if (alp.Initial.PreviousPartition != null && alp.Final.NextPartition != null)
				//{
					// get the good polygon
					Polygon pPoly = PartitionPolygon(alp);

					// check not null
					if (pPoly != null)
					{
						poly = PolygonToolkit.PolygonUnion(new List<Polygon>(new Polygon[] { poly, pPoly }));
					}
				//}
			}

			// circles for intersections of partitions			
			foreach (ArbiterLanePartition alp in al.Partitions)
			{
				if (alp.Final.NextPartition != null)
				{
					double interAngle = Math.Abs(FinalIntersectionAngle(alp));
					if (interAngle > 15)
					{
						Circle connect = new Circle((alp.Lane.Width / 2.0) + (interAngle / 15.0 * 0.5), alp.Final.Position);
						poly = PolygonToolkit.PolygonUnion(new List<Polygon>(new Polygon[] { poly, connect.ToPolygon(16) }));
					}
				}
			}

			// return the polygon
			return poly;
		}

		public static Polygon PartitionPolygon(ArbiterLanePartition alp)
		{
			if (alp.Initial.PreviousPartition != null && 
				alp.Final.NextPartition != null &&
				alp.Length < 30.0 && alp.Length > 4.0)
			{
				// get partition turn direction
				ArbiterTurnDirection pTD = PartitionTurnDirection(alp);

				// check if angles of previous and next are such that not straight through
				if (pTD != ArbiterTurnDirection.Straight)
				{
					// get partition poly
					ArbiterInterconnect tmpAi = alp.ToInterconnect;
					tmpAi.TurnDirection = pTD;
					GenerateInterconnectPolygon(tmpAi);
					Polygon pPoly = tmpAi.TurnPolygon;

					// here is default partition polygon
					LinePath alplb = alp.PartitionPath.ShiftLateral(-alp.Lane.Width / 2.0);
					LinePath alprb = alp.PartitionPath.ShiftLateral(alp.Lane.Width / 2.0);
					alprb.Reverse();
					List<Coordinates> alpdefaultPoly = alplb;
					alpdefaultPoly.AddRange(alprb);

					// get full poly
					pPoly.AddRange(alpdefaultPoly);
					pPoly = Polygon.GrahamScan(pPoly);

					return pPoly;
				}
			}
			else if (alp.Length >= 30)
			{
				Polygon pBase = GenerateSimplePartitionPolygon(alp, alp.PartitionPath, alp.Lane.Width);

				if (alp.Initial.PreviousPartition != null && Math.Abs(FinalIntersectionAngle(alp.Initial.PreviousPartition)) > 15)
				{
					// initial portion					
					Coordinates i1 = alp.Initial.Position - alp.Initial.PreviousPartition.Vector().Normalize(15.0);
					Coordinates i2 = alp.Initial.Position;
					Coordinates i3 = i2 + alp.Vector().Normalize(15.0);
					LinePath il12 = new LinePath(new Coordinates[] { i1, i2 });
					LinePath il23 = new LinePath(new Coordinates[] { i2, i3 });
					LinePath il13 = new LinePath(new Coordinates[] { i1, i3 });
					Coordinates iCC = il13.GetClosestPoint(i2).Location;					
					if (GeneralToolkit.TriangleArea(i1, i2, i3) < 0)
						il13 = il13.ShiftLateral(iCC.DistanceTo(i2) + alp.Lane.Width / 2.0);
					else
						il13 = il13.ShiftLateral(-iCC.DistanceTo(i2) + alp.Lane.Width / 2.0);
					LinePath.PointOnPath iCCP = il13.GetClosestPoint(iCC);
					iCCP = il13.AdvancePoint(iCCP, -10.0);
					il13 = il13.SubPath(iCCP, 20.0);
					Polygon iBase = GenerateSimplePolygon(il23, alp.Lane.Width);
					iBase.Add(il13[1]);
					Polygon iP = Polygon.GrahamScan(iBase);
					pBase = PolygonToolkit.PolygonUnion(new List<Polygon>(new Polygon[] { pBase, iP }));
				}

				if (alp.Final.NextPartition != null && Math.Abs(FinalIntersectionAngle(alp)) > 15)
				{
					// initial portion					
					Coordinates i1 = alp.Final.Position - alp.Vector().Normalize(15.0);
					Coordinates i2 = alp.Final.Position;
					Coordinates i3 = i2 + alp.Final.NextPartition.Vector().Normalize(15.0);
					LinePath il12 = new LinePath(new Coordinates[] { i1, i2 });
					LinePath il23 = new LinePath(new Coordinates[] { i2, i3 });
					LinePath il13 = new LinePath(new Coordinates[] { i1, i3 });
					Coordinates iCC = il13.GetClosestPoint(i2).Location;
					if (GeneralToolkit.TriangleArea(i1, i2, i3) < 0)
						il13 = il13.ShiftLateral(iCC.DistanceTo(i2) + alp.Lane.Width / 2.0);
					else
						il13 = il13.ShiftLateral(-iCC.DistanceTo(i2) + alp.Lane.Width / 2.0);
					LinePath.PointOnPath iCCP = il13.GetClosestPoint(iCC);
					iCCP = il13.AdvancePoint(iCCP, -10.0);
					il13 = il13.SubPath(iCCP, 20.0);
					Polygon iBase = GenerateSimplePolygon(il12, alp.Lane.Width);
					iBase.Add(il13[0]);
					Polygon iP = Polygon.GrahamScan(iBase);
					pBase = PolygonToolkit.PolygonUnion(new List<Polygon>(new Polygon[] { pBase, iP }));
				}

				return pBase;
			}
			
			// fall out
			return null;
		}

		public static ArbiterTurnDirection PartitionTurnDirection(ArbiterLanePartition alp)
		{
			ArbiterWaypoint initWp = alp.Initial;
			ArbiterWaypoint finWp = alp.Final;

			Coordinates iVec = initWp.PreviousPartition != null ? initWp.PreviousPartition.Vector().Normalize(1.0) : initWp.NextPartition.Vector().Normalize(1.0);
			double iRot = -iVec.ArcTan;

			Coordinates fVec = finWp.NextPartition != null ? finWp.NextPartition.Vector().Normalize(1.0) : finWp.PreviousPartition.Vector().Normalize(1.0);
			fVec = fVec.Rotate(iRot);
			double fDeg = fVec.ToDegrees();

			double arcTan = Math.Atan2(fVec.Y, fVec.X) * 180.0 / Math.PI;

			if (arcTan > 20.0)
				return ArbiterTurnDirection.Left;
			else if (arcTan < -20.0)
				return ArbiterTurnDirection.Right;
			else
				return ArbiterTurnDirection.Straight;
		}

		private static double FinalIntersectionAngle(ArbiterLanePartition alp1)
		{
			Coordinates iVec = alp1.Vector().Normalize(1.0);
			double iRot = -iVec.ArcTan;

			Coordinates fVec = alp1.Final.NextPartition.Vector().Normalize(1.0);
			fVec = fVec.Rotate(iRot);
			double fDeg = fVec.ToDegrees();

			double arcTan = Math.Atan2(fVec.Y, fVec.X) * 180.0 / Math.PI;
			return arcTan;
		}

		private static Polygon GenerateMiddlePathPolygon(LinePath initial, LinePath middle, LinePath final, ArbiterLane lane)
		{
			// wp's
			ArbiterWaypoint w1 = new ArbiterWaypoint(initial[0], new ArbiterWaypointId(1, lane.LaneId));
			ArbiterWaypoint w2 = new ArbiterWaypoint(initial[1], new ArbiterWaypointId(2, lane.LaneId));
			ArbiterWaypoint w3 = new ArbiterWaypoint(final[0], new ArbiterWaypointId(3, lane.LaneId));
			ArbiterWaypoint w4 = new ArbiterWaypoint(final[1], new ArbiterWaypointId(4, lane.LaneId));

			// set lane
			w1.Lane = lane;
			w2.Lane = lane;
			w3.Lane = lane;
			w4.Lane = lane;

			// alps
			ArbiterLanePartition alp1 = new ArbiterLanePartition(new ArbiterLanePartitionId(w1.WaypointId, w2.WaypointId, lane.LaneId), w1, w2, lane.Way.Segment);
			ArbiterLanePartition alp2 = new ArbiterLanePartition(new ArbiterLanePartitionId(w2.WaypointId, w3.WaypointId, lane.LaneId), w2, w3, lane.Way.Segment);
			ArbiterLanePartition alp3 = new ArbiterLanePartition(new ArbiterLanePartitionId(w3.WaypointId, w4.WaypointId, lane.LaneId), w3, w4, lane.Way.Segment);

			// set links
			w1.NextPartition = alp1;
			w2.NextPartition = alp2;
			w3.NextPartition = alp3;
			w4.PreviousPartition = alp3;
			w3.PreviousPartition = alp2;
			w2.PreviousPartition = alp1;

			// get poly
			ArbiterTurnDirection atd = PartitionTurnDirection(alp2);
			
			if (atd != ArbiterTurnDirection.Straight)
			{
				ArbiterInterconnect ai = alp2.ToInterconnect;
				ai.TurnDirection = atd;
				GenerateInterconnectPolygon(ai);
				return ai.TurnPolygon;
			}

			return null;
		}

		private static Polygon GenerateSimplePartitionPolygon(ArbiterLanePartition alp, LinePath path, double width)
		{
			// here is default partition polygon
			LinePath alplb = path.ShiftLateral(-width / 2.0);
			LinePath alprb = path.ShiftLateral(width / 2.0);
			alprb.Reverse();
			List<Coordinates> alpdefaultPoly = alplb;
			alpdefaultPoly.AddRange(alprb);
			foreach (ArbiterUserWaypoint auw in alp.UserWaypoints)
				alpdefaultPoly.Add(auw.Position);
			return Polygon.GrahamScan(alpdefaultPoly);
		}

		private static Polygon GenerateSimplePolygon(LinePath path, double width)
		{
			// here is default partition polygon
			LinePath alplb = path.ShiftLateral(-width / 2.0);
			LinePath alprb = path.ShiftLateral(width / 2.0);
			alprb.Reverse();
			List<Coordinates> alpdefaultPoly = alplb;
			alpdefaultPoly.AddRange(alprb);
			return new Polygon(alpdefaultPoly);
		}

		private static void GenerateInterconnectPolygon(ArbiterInterconnect ai)
		{
			List<Coordinates> polyPoints = new List<Coordinates>();

			// width
			double width = 3.0;
			if (ai.InitialGeneric is ArbiterWaypoint)
			{
				ArbiterWaypoint aw = (ArbiterWaypoint)ai.InitialGeneric;
				width = width < aw.Lane.Width ? aw.Lane.Width : width;
			}
			if (ai.FinalGeneric is ArbiterWaypoint)
			{
				ArbiterWaypoint aw = (ArbiterWaypoint)ai.FinalGeneric;
				width = width < aw.Lane.Width ? aw.Lane.Width : width;
			}

			if (ai.TurnDirection == ArbiterTurnDirection.UTurn ||
				ai.TurnDirection == ArbiterTurnDirection.Straight ||
				!(ai.InitialGeneric is ArbiterWaypoint) ||
				!(ai.FinalGeneric is ArbiterWaypoint))
			{
				LinePath lp = ai.InterconnectPath.ShiftLateral(width / 2.0);
				LinePath rp = ai.InterconnectPath.ShiftLateral(-width / 2.0);
				polyPoints.AddRange(lp);
				polyPoints.AddRange(rp);
				ai.TurnPolygon = Polygon.GrahamScan(polyPoints);

				if (ai.TurnDirection == ArbiterTurnDirection.UTurn)
				{
					List<Coordinates> updatedPts = new List<Coordinates>();
					LinePath interTmp = ai.InterconnectPath.Clone();
					Coordinates pathVec = ai.FinalGeneric.Position - ai.InitialGeneric.Position;
					interTmp[1] = interTmp[1] + pathVec.Normalize(width / 2.0);
					interTmp[0] = interTmp[0] - pathVec.Normalize(width / 2.0);
					lp = interTmp.ShiftLateral(TahoeParams.VL);
					rp = interTmp.ShiftLateral(-TahoeParams.VL);
					updatedPts.AddRange(lp);
					updatedPts.AddRange(rp);
					ai.TurnPolygon = Polygon.GrahamScan(updatedPts);
				}
			}
			else
			{
				// polygon points
				List<Coordinates> interPoints = new List<Coordinates>();

				// waypoint
				ArbiterWaypoint awI = (ArbiterWaypoint)ai.InitialGeneric;
				ArbiterWaypoint awF = (ArbiterWaypoint)ai.FinalGeneric;

				// left and right path
				LinePath leftPath = new LinePath();
				LinePath rightPath = new LinePath();

				// some initial points
				LinePath initialPath = new LinePath(new Coordinates[] { awI.PreviousPartition.Initial.Position, awI.Position });
				LinePath il = initialPath.ShiftLateral(width / 2.0);
				LinePath ir = initialPath.ShiftLateral(-width / 2.0);
				leftPath.Add(il[1]);
				rightPath.Add(ir[1]);

				// some final points
				LinePath finalPath = new LinePath(new Coordinates[] { awF.Position, awF.NextPartition.Final.Position });
				LinePath fl = finalPath.ShiftLateral(width / 2.0);
				LinePath fr = finalPath.ShiftLateral(-width / 2.0);
				leftPath.Add(fl[0]);
				rightPath.Add(fr[0]);

				// initial and final paths
				Line iPath = new Line(awI.PreviousPartition.Initial.Position, awI.Position);
				Line fPath = new Line(awF.Position, awF.NextPartition.Final.Position);

				// get where the paths intersect and vector to normal path
				Coordinates c;
				iPath.Intersect(fPath, out c);
				Coordinates vector = ai.InterconnectPath.GetClosestPoint(c).Location - c;
				Coordinates center = c + vector.Normalize((vector.Length / 2.0));

				// get width expansion
				Coordinates iVec = awI.PreviousPartition != null ? awI.PreviousPartition.Vector().Normalize(1.0) : awI.NextPartition.Vector().Normalize(1.0);
				double iRot = -iVec.ArcTan;
				Coordinates fVec = awF.NextPartition != null ? awF.NextPartition.Vector().Normalize(1.0) : awF.PreviousPartition.Vector().Normalize(1.0);
				fVec = fVec.Rotate(iRot);
				double fDeg = fVec.ToDegrees();
				double arcTan = Math.Atan2(fVec.Y, fVec.X) * 180.0 / Math.PI;
				double centerWidth = width + width * 1.0 * Math.Abs(arcTan) / 90.0;

				// get inner point (small scale)
				Coordinates innerPoint = center + vector.Normalize(centerWidth / 4.0);

				// get outer
				Coordinates outerPoint = center - vector.Normalize(centerWidth / 2.0);

				if (ai.TurnDirection == ArbiterTurnDirection.Right)
				{
					rightPath.Insert(1, innerPoint);
					ai.InnerCoordinates = rightPath;
					leftPath.Reverse();
					leftPath.Insert(1, outerPoint);
					Polygon p = new Polygon(leftPath.ToArray());
					p.AddRange(rightPath.ToArray());
					ai.TurnPolygon = p;
				}
				else
				{
					leftPath.Insert(1, innerPoint);
					ai.InnerCoordinates = leftPath;
					rightPath.Reverse();
					rightPath.Insert(1, outerPoint);
					Polygon p = new Polygon(leftPath.ToArray());
					p.AddRange(rightPath.ToArray());
					ai.TurnPolygon = p;
				}
			}
		}
	}
}
