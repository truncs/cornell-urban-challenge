using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Splines;
using System.Diagnostics;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Path {
	[Serializable]
	public class BezierPathSegment : ISpeedPathSegment {
		public CubicBezier cb;
		private double? endSpeed;
		private bool stopLine;

		public BezierPathSegment(Coordinates p0, Coordinates p1, Coordinates p2, Coordinates p3, double? endSpeed, bool stopLine) {
			cb = new CubicBezier(p0, p1, p2, p3);
			this.endSpeed = endSpeed;
			this.stopLine = stopLine;
		}

		public BezierPathSegment(CubicBezier bezier, double? endSpeed, bool stopLine) {
			cb = bezier;
			this.endSpeed = endSpeed;
			this.stopLine = stopLine;
		}

		public CubicBezier Bezier
		{
			get { return cb; }
		}

		#region IPathSegment Members

		public Coordinates Start {
			get {
				return cb.P0;
			}
			set {
				cb = new CubicBezier(value, cb.P1, cb.P2, cb.P3);
			}
		}

		public PointOnPath StartPoint {
			get { return new PointOnPath(this, 0, cb.P0); }
		}

		public Coordinates End {
			get {
				return cb.P3;
			}
			set {
				cb = new CubicBezier(cb.P0, cb.P1, cb.P2, value);
			}
		}

		public PointOnPath EndPoint {
			get { return new PointOnPath(this, cb.ArcLength, cb.P3); }
		}

		public double Length {
			get { return cb.ArcLength; }
		}

		public double DistanceToGo(PointOnPath pt) {
			// assert that we're looking at the same segment
			Debug.Assert(Equals(pt.segment));
			// calculate as the distance remaining
			return cb.ArcLength - pt.dist;
		}

		public double DistanceOffPath(Coordinates pt) {
			PointOnPath cp = ClosestPoint(pt);
			return cp.pt.DistanceTo(pt);
		}

		public PointOnPath ClosestPoint(Coordinates pt) {
			CubicBezier.NearestPointResult npr = cb.GetClosestPoint(pt);
			return new PointOnPath(this, cb.PartialArcLength(npr.tval), npr.NearestPoint);
		}

		public PointOnPath AdvancePoint(PointOnPath pt, ref double dist) {
			// assert that we're looking at the same segment
			Debug.Assert(Equals(pt.segment));

			if (pt.dist + dist <= 0) {
				// handle the case of negative distance going before start point
				dist += pt.dist;
				return StartPoint;
			}
			else if (pt.dist + dist >= cb.ArcLength) {
				// handle the case of positive distance going after end point
				dist -= cb.ArcLength - pt.dist;
				return EndPoint;
			}
			else {
				// we're in the range that we can achieve
				double targetDist = pt.dist + dist;
				double tValue = cb.FindT(targetDist);
				double actDist = cb.PartialArcLength(tValue);

				dist = 0;
				return new PointOnPath(this, targetDist, cb.Bt(tValue));
			}
		}

		public Coordinates Tangent(PointOnPath pt) {
			// assert that we're looking at the same segment
			Debug.Assert(Equals(pt.segment));

			// short circuit for near the start/end point
			if (pt.pt.ApproxEquals(cb.P0, 0.001)) {
				return cb.dBdt(0).Normalize();
			}
			else if (pt.pt.ApproxEquals(cb.P3, 0.001)) {
				return cb.dBdt(1).Normalize();
			}
			else {
				// find the t-value in the general case
				double tvalue = cb.FindT(pt.dist);
				return cb.dBdt(tvalue).Normalize();
			}
		}

		public double Curvature(PointOnPath pt) {
			// assert that we're looking at the same segment
			Debug.Assert(Equals(pt.segment));

			// short circuit for near the start/end point
			if (pt.pt.ApproxEquals(cb.P0, 0.001)) {
				return cb.Curvature(0);
			}
			else if (pt.pt.ApproxEquals(cb.P3, 0.001)) {
				return cb.Curvature(1);
			}
			else {
				// find the t-value in the general case
				double tvalue = cb.FindT(pt.dist);
				return cb.Curvature(tvalue);
			}
		}

		public IPathSegment Clone() {
			return new BezierPathSegment(cb.P0, cb.P1, cb.P2, cb.P3, endSpeed, stopLine);
		}

		public void Transform(Matrix3 m) {
			Vector3 p0 = new Vector3(cb.P0.X, cb.P0.Y, 1);
			Vector3 p1 = new Vector3(cb.P1.X, cb.P1.Y, 1);
			Vector3 p2 = new Vector3(cb.P2.X, cb.P2.Y, 1);
			Vector3 p3 = new Vector3(cb.P3.X, cb.P3.Y, 1);

			p0 = m * p0;
			p1 = m * p1;
			p2 = m * p2;
			p3 = m * p3;

			cb = new CubicBezier(new Coordinates(p0.X, p0.Y), new Coordinates(p1.X, p1.Y),
				new Coordinates(p2.X, p2.Y), new Coordinates(p3.X, p3.Y));
		}

		#endregion

		#region IEquatable<IPathSegment> Members

		public bool Equals(IPathSegment other) {
			if (other is BezierPathSegment) {
				BezierPathSegment bp = (BezierPathSegment)other;

				// check that all the point match up
				return bp.cb.P0 == cb.P0 && bp.cb.P1 == cb.P1 && bp.cb.P2 == cb.P2 && bp.cb.P3 == cb.P3;
			}
			else {
				return false;
			}
		}

		#endregion

		#region ISpeedPathSegment Members

		public bool EndSpeedSpecified {
			get { return endSpeed.HasValue; }
		}

		public double EndSpeed {
			get { return endSpeed.GetValueOrDefault(0); }
		}

		public bool StopLine {
			get { return stopLine; }
		}

		#endregion
	}
}
