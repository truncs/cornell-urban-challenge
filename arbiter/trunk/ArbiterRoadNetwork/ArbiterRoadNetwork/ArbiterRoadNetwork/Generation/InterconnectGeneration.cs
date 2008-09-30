using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.DarpaRndf;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Splines;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Generates interconnection information into an arbiter road network
	/// </summary>
	[Serializable]
	public class InterconnectGeneration
	{
		private IRndf xyRndf;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="xyRndf"></param>
		public InterconnectGeneration(IRndf xyRndf)
		{
			this.xyRndf = xyRndf;
		}

		/// <summary>
		/// Generates interconnects into the road network
		/// </summary>
		/// <param name="arn"></param>
		/// <returns></returns>
		public ArbiterRoadNetwork GenerateInterconnects(ArbiterRoadNetwork arn)
		{
			// list of all exit entries in the xy rndf
			List<SimpleExitEntry> sees = new List<SimpleExitEntry>();

			// zones
			if(xyRndf.Zones != null)
			{
				// loop over zones
				foreach (SimpleZone sz in xyRndf.Zones)
				{
					// add all ee's
					sees.AddRange(sz.Perimeter.ExitEntries);
				}
			}

			// segments
			if (xyRndf.Segments != null)
			{
				// loop over segments
				foreach (SimpleSegment ss in xyRndf.Segments)
				{
					// lanes
					foreach (SimpleLane sl in ss.Lanes)
					{
						// add all ee's
						sees.AddRange(sl.ExitEntries);
					}
				}
			}

			// loop over ee's and create interconnects
			foreach (SimpleExitEntry see in sees)
			{
				IArbiterWaypoint initial = arn.LegacyWaypointLookup[see.ExitId];
				IArbiterWaypoint final = arn.LegacyWaypointLookup[see.EntryId];
				ArbiterInterconnect ai = new ArbiterInterconnect(initial, final);
				arn.ArbiterInterconnects.Add(ai.InterconnectId, ai);
				arn.DisplayObjects.Add(ai);

				if (initial is ITraversableWaypoint)
				{
					ITraversableWaypoint initialWaypoint = (ITraversableWaypoint)initial;

					initialWaypoint.IsExit = true;

					if (initialWaypoint.Exits == null)
						initialWaypoint.Exits = new List<ArbiterInterconnect>();

					initialWaypoint.Exits.Add(ai);
				}
				else
				{
					throw new Exception("initial wp of ee: " + see.ExitId + " is not ITraversableWaypoint");
				}

				if (final is ITraversableWaypoint)
				{
					ITraversableWaypoint finalWaypoint = (ITraversableWaypoint)final;

					finalWaypoint.IsEntry = true;

					if (finalWaypoint.Entries == null)
						finalWaypoint.Entries = new List<ArbiterInterconnect>();

					finalWaypoint.Entries.Add(ai);
				}
				else
				{
					throw new Exception("final wp of ee: " + see.EntryId + " is not ITraversableWaypoint");
				}

				// set the turn direction
				this.SetTurnDirection(ai);

				// interconnectp olygon stuff
				this.GenerateInterconnectPolygon(ai);
				if (ai.TurnPolygon.IsComplex)
				{
					Console.WriteLine("Found complex polygon for interconnect: " + ai.ToString());
					ai.TurnPolygon = ai.DefaultPoly();
				}
			}

			return arn;
		}

		public void SetTurnDirection(ArbiterInterconnect ai)
		{
			#region Turn Direction

			if (ai.InitialGeneric is ArbiterWaypoint && ai.FinalGeneric is ArbiterWaypoint)
			{
				ArbiterWaypoint initWp = (ArbiterWaypoint)ai.InitialGeneric;
				ArbiterWaypoint finWp = (ArbiterWaypoint)ai.FinalGeneric;

				// check not uturn
				if (!initWp.Lane.Way.Segment.Equals(finWp.Lane.Way.Segment) || initWp.Lane.Way.Equals(finWp.Lane.Way))
				{
					Coordinates iVec = initWp.PreviousPartition != null ? initWp.PreviousPartition.Vector().Normalize(1.0) : initWp.NextPartition.Vector().Normalize(1.0);
					double iRot = -iVec.ArcTan;

					Coordinates fVec = finWp.NextPartition != null ? finWp.NextPartition.Vector().Normalize(1.0) : finWp.PreviousPartition.Vector().Normalize(1.0);
					fVec = fVec.Rotate(iRot);
					double fDeg = fVec.ToDegrees();

					double arcTan = Math.Atan2(fVec.Y, fVec.X) * 180.0 / Math.PI;

					if (arcTan > 45.0)
						ai.TurnDirection = ArbiterTurnDirection.Left;
					else if (arcTan < -45.0)
						ai.TurnDirection = ArbiterTurnDirection.Right;
					else
						ai.TurnDirection = ArbiterTurnDirection.Straight;
				}
				else
				{
					Coordinates iVec = initWp.PreviousPartition != null ? initWp.PreviousPartition.Vector().Normalize(1.0) : initWp.NextPartition.Vector().Normalize(1.0);
					double iRot = -iVec.ArcTan;

					Coordinates fVec = finWp.NextPartition != null ? finWp.NextPartition.Vector().Normalize(1.0) : finWp.PreviousPartition.Vector().Normalize(1.0);
					fVec = fVec.Rotate(iRot);
					double fDeg = fVec.ToDegrees();

					double arcTan = Math.Atan2(fVec.Y, fVec.X) * 180.0 / Math.PI;

					if (arcTan > 45.0 && arcTan < 135.0)
						ai.TurnDirection = ArbiterTurnDirection.Left;
					else if (arcTan < -45.0 && arcTan > -135.0)
						ai.TurnDirection = ArbiterTurnDirection.Right;
					else if (Math.Abs(arcTan) < 45.0)
						ai.TurnDirection = ArbiterTurnDirection.Straight;
					else
						ai.TurnDirection = ArbiterTurnDirection.UTurn;
				}
			}
			else
			{
				Coordinates iVec = new Coordinates();
				double iRot = 0.0;
				Coordinates fVec = new Coordinates();
				double fDeg = 0.0;

				if (ai.InitialGeneric is ArbiterWaypoint)
				{
					ArbiterWaypoint initWp = (ArbiterWaypoint)ai.InitialGeneric;
					iVec = initWp.PreviousPartition != null ? initWp.PreviousPartition.Vector().Normalize(1.0) : initWp.NextPartition.Vector().Normalize(1.0);
					iRot = -iVec.ArcTan;
				}
				else if (ai.InitialGeneric is ArbiterPerimeterWaypoint)
				{
					ArbiterPerimeterWaypoint apw = (ArbiterPerimeterWaypoint)ai.InitialGeneric;
					Coordinates centerPoly = apw.Perimeter.PerimeterPolygon.CalculateBoundingCircle().center;
					iVec = apw.Position - centerPoly;
					iVec = iVec.Normalize(1.0);
					iRot = -iVec.ArcTan;
				}

				if (ai.FinalGeneric is ArbiterWaypoint)
				{
					ArbiterWaypoint finWp = (ArbiterWaypoint)ai.FinalGeneric;
					fVec = finWp.NextPartition != null ? finWp.NextPartition.Vector().Normalize(1.0) : finWp.PreviousPartition.Vector().Normalize(1.0);
					fVec = fVec.Rotate(iRot);
					fDeg = fVec.ToDegrees();
				}
				else if (ai.FinalGeneric is ArbiterPerimeterWaypoint)
				{
					ArbiterPerimeterWaypoint apw = (ArbiterPerimeterWaypoint)ai.FinalGeneric;
					Coordinates centerPoly = apw.Perimeter.PerimeterPolygon.CalculateBoundingCircle().center;
					fVec = centerPoly - apw.Position;
					fVec = fVec.Normalize(1.0);
					fVec = fVec.Rotate(iRot);
					fDeg = fVec.ToDegrees();
				}

				double arcTan = Math.Atan2(fVec.Y, fVec.X) * 180.0 / Math.PI;

				if (arcTan > 45.0)
					ai.TurnDirection = ArbiterTurnDirection.Left;
				else if (arcTan < -45.0)
					ai.TurnDirection = ArbiterTurnDirection.Right;
				else
					ai.TurnDirection = ArbiterTurnDirection.Straight;
			}

			#endregion
		}

		public void GenerateInterconnectPolygon(ArbiterInterconnect ai)
		{
			List<Coordinates> polyPoints = new List<Coordinates>();
			try
			{
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
					double centerWidth = width + width * 2.0 * Math.Abs(arcTan) / 90.0;

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
			catch (Exception e)
			{
				Console.WriteLine("error generating turn polygon: " + ai.ToString());
				ai.TurnPolygon = ai.DefaultPoly();
			}
		}
	}
}