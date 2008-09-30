using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;

using SimOperationalService;
using UrbanChallenge.Common.Pose;
using UrbanChallenge.Common.Shapes;
using OperationalLayer.Tracing;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.RoadModel {
	class PathLaneModel : ILaneModel {
		private LinePath path;
		private string laneID;
		private double width;
		private CarTimestamp timestamp;

		public PathLaneModel(CarTimestamp timestamp, LinePath path, double width) {
			this.timestamp = timestamp;
			this.path = path;
			this.width = width;
			this.laneID = "not specified";
		}

		public PathLaneModel(CarTimestamp timestamp, PathRoadModel.LaneEstimate lane) {
			this.path = lane.path;
			int dotIndex = lane.paritionID.LastIndexOf('.');
			if (dotIndex == -1) {
				this.laneID = lane.paritionID + ".not a lane";
			}
			else {
				this.laneID = lane.paritionID.Substring(0, dotIndex);
			}
			this.width = lane.width;

			if (this.width < TahoeParams.T + 2) {
				this.width = TahoeParams.T + 2;
			}

			this.timestamp = timestamp;
		}

		public LinePath LinearizeCenterLine(LinearizationOptions options) {
			LinePath transformedPath = path;
			if (options.Timestamp.IsValid) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(timestamp, options.Timestamp);
				OperationalTrace.WriteVerbose("in LinearizeCenterLine, tried to find {0}->{1}, got {2}->{3}", timestamp, options.Timestamp, relTransform.OriginTimestamp, relTransform.EndTimestamp);
				transformedPath = path.Transform(relTransform);
			}

			LinePath.PointOnPath startPoint = transformedPath.AdvancePoint(path.ZeroPoint, options.StartDistance);

			LinePath subPath = new LinePath(); ;
			if (options.EndDistance > options.StartDistance) {
				subPath = transformedPath.SubPath(startPoint, options.EndDistance-options.StartDistance);
			}

			if (subPath.Count < 2) {
				subPath.Clear();
				Coordinates startPt = path.GetPoint(startPoint);

				subPath.Add(startPt);
				subPath.Add(path.GetSegment(startPoint.Index).UnitVector*Math.Max(options.EndDistance-options.StartDistance, 0.1)+startPt);
			}

			return subPath;
		}

		public LinePath LinearizeLeftBound(LinearizationOptions options) {
			LinePath bound = FindBoundary(this.width/2 + options.LaneShiftDistance + 0.3, path);

			if (options.Timestamp.IsValid) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(timestamp, options.Timestamp);
				bound.TransformInPlace(relTransform);
			}

			if (options.EndDistance > options.StartDistance) {
				LinePath.PointOnPath startPoint = bound.AdvancePoint(bound.ZeroPoint, options.StartDistance);
				return bound.SubPath(startPoint, options.EndDistance-options.StartDistance);
			}
			else {
				return new LinePath();
			}
		}

		public LinePath LinearizeRightBound(LinearizationOptions options) {
			LinePath bound = FindBoundary(-this.width/2 + options.LaneShiftDistance, path);

			if (options.Timestamp.IsValid) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(timestamp, options.Timestamp);
				bound.TransformInPlace(relTransform);
			}

			if (options.EndDistance > options.StartDistance) {
				LinePath.PointOnPath startPoint = bound.AdvancePoint(bound.ZeroPoint, options.StartDistance);
				return bound.SubPath(startPoint, options.EndDistance-options.StartDistance);
			}
			else {
				return new LinePath();
			}
		}

		public string LaneID {
			get { return laneID; }
		}

		public CarTimestamp Timestamp {
			get { return timestamp; }
		}

		public static LinePath FindBoundary(double offset, LinePath path) {
			// create a list of shift line segments
			List<LineSegment> segs = new List<LineSegment>();

			foreach (LineSegment ls in path.GetSegmentEnumerator()) {
				segs.Add(ls.ShiftLateral(offset));
			}

			// find the intersection points between all of the segments
			LinePath boundPoints = new LinePath();
			// add the first point
			boundPoints.Add(segs[0].P0);

			// loop through the stuff
			for (int i = 0; i < segs.Count-1; i++) {
				// find the intersection
				Line l0 = (Line)segs[i];
				Line l1 = (Line)segs[i+1];

				Coordinates pt;
				if (l0.Intersect(l1, out pt)) {
					// figure out the angle of the intersection
					double angle = Math.Acos(l0.UnitVector.Dot(l1.UnitVector));
					double angle2 = Math.Sin(l0.UnitVector.Cross(l1.UnitVector));

					// determine if it's a left or right turn
					bool leftTurn = angle2 > 0;
					double f = 2.5;
					if ((leftTurn && offset > 0) || (!leftTurn && offset < 0)) {
						// left turn and looking for left bound or right turn and looking for right bound
						f = 0.5;
					}

					// calculate the width expansion factor
					// 90 deg, 3x width
					// 45 deg, 1.5x width
					// 0 deg, 1x width
					double widthFactor = Math.Pow(angle/(Math.PI/2.0),1.25)*f + 1;

					// get the line formed by pt and the corresponding path point
					Coordinates boundLine = pt - path[i+1];

					boundPoints.Add(widthFactor*Math.Abs(offset)*boundLine.Normalize() + path[i+1]);
				}
				else {
					boundPoints.Add(segs[i].P1);
				}
			}

			// add the last point
			boundPoints.Add(segs[segs.Count-1].P1);

			return boundPoints;
		}

		public double Width {
			get { return width; }
		}

		public LinePath Path {
			get { return path; }
		}
	}
}
