using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;
using UrbanChallenge.Operational.Common;
using UrbanChallenge.Common.Pose;
using OperationalLayer.Tracing;

namespace OperationalLayer.RoadModel {
	class CombinedLaneModel : ILaneModel {
		private LinePath centerlinePath;
		private LinePath leftBound;
		private LinePath rightBound;
		private CarTimestamp timestamp;
		private double nominalWidth;

		public CombinedLaneModel(LinePath centerlinePath, LinePath leftBound, LinePath rightBound, double nominalWidth, CarTimestamp timestamp) {
			this.centerlinePath = centerlinePath;
			this.leftBound = leftBound;
			this.rightBound = rightBound;
			this.timestamp = timestamp;
			this.nominalWidth = nominalWidth;
		}

		#region ILaneModel Members

		public LinePath LinearizeCenterLine(LinearizationOptions options) {
			LinePath transformedPath = centerlinePath;
			if (options.Timestamp.IsValid) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(timestamp, options.Timestamp);
				OperationalTrace.WriteVerbose("in LinearizeCenterLine, tried to find {0}->{1}, got {2}->{3}", timestamp, options.Timestamp, relTransform.OriginTimestamp, relTransform.EndTimestamp);
				transformedPath = centerlinePath.Transform(relTransform);
			}

			LinePath.PointOnPath startPoint = transformedPath.AdvancePoint(centerlinePath.ZeroPoint, options.StartDistance);

			LinePath subPath = new LinePath(); ;
			if (options.EndDistance > options.StartDistance) {
				subPath = transformedPath.SubPath(startPoint, options.EndDistance-options.StartDistance);
			}

			if (subPath.Count < 2) {
				subPath.Clear();
				Coordinates startPt = startPoint.Location;

				subPath.Add(startPt);
				subPath.Add(centerlinePath.GetSegment(startPoint.Index).UnitVector*Math.Max(options.EndDistance-options.StartDistance, 0.1)+startPt);
			}

			return subPath;
		}

		public LinePath LinearizeLeftBound(LinearizationOptions options) {
			LinePath bound = leftBound;
			if (options.Timestamp.IsValid) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(timestamp, options.Timestamp);
				bound = bound.Transform(relTransform);
			}

			if (options.EndDistance > options.StartDistance) {
				// TODO: work off centerline path
				LinePath.PointOnPath startPoint = bound.AdvancePoint(bound.ZeroPoint, options.StartDistance);
				return bound.SubPath(startPoint, options.EndDistance-options.StartDistance).ShiftLateral(options.LaneShiftDistance);
			}
			else {
				return new LinePath();
			}
		}

		public LinePath LinearizeRightBound(LinearizationOptions options) {
			LinePath bound = rightBound;
			if (options.Timestamp.IsValid) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(timestamp, options.Timestamp);
				bound = bound.Transform(relTransform);
			}

			if (options.EndDistance > options.StartDistance) {
				// TODO: work off centerline path
				LinePath.PointOnPath startPoint = bound.AdvancePoint(bound.ZeroPoint, options.StartDistance);
				return bound.SubPath(startPoint, options.EndDistance-options.StartDistance).ShiftLateral(options.LaneShiftDistance);
			}
			else {
				return new LinePath();
			}
		}

		public string LaneID {
			get { return "not specified"; }
		}

		public CarTimestamp Timestamp {
			get { return timestamp; }
		}

		public double Width {
			get { return nominalWidth; }
		}

		#endregion
	}
}
