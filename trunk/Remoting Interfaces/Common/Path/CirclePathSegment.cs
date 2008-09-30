using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Path {
	[Serializable]
	public class CirclePathSegment : IPathSegment {
		private double startAngle, endAngle;
		private bool ccw;
		private Coordinates center;
		private double radius;

		public CirclePathSegment(Coordinates center, double radius, double startAngle, double endAngle, bool ccw) {
			startAngle = Math.IEEERemainder(startAngle, 2*Math.PI);
			this.center = center;
			this.radius = radius;
			this.startAngle = startAngle;
			this.endAngle = endAngle;
			this.ccw = ccw;

			if (!ccw) {
				throw new NotImplementedException("clockwise circle-segments are not implemented yet");
			}

			if (ccw) {
				// make sure endangle is bigger than start angle
				while (endAngle < startAngle)
					endAngle += 2*Math.PI;
			}
			else {
				// make sure endangle is smaller than start angle
				while (endAngle > startAngle)
					endAngle -= 2*Math.PI;
			}
		}

		#region IPathSegment Members

		public Coordinates Start {
			get {
				return new Coordinates(center.X + radius*Math.Cos(startAngle), center.Y + radius*Math.Sin(startAngle));
			}
			set {
				throw new NotSupportedException();
			}
		}

		public PointOnPath StartPoint {
			get { return new PointOnPath(this, 0, Start); }
		}

		public Coordinates End {
			get {
				return new Coordinates(center.X + radius*Math.Cos(endAngle), center.Y + radius*Math.Sin(endAngle)); 
			}
			set {
				throw new NotSupportedException();
			}
		}

		public PointOnPath EndPoint {
			get { return new PointOnPath(this, Length, End); }
		}

		public double Length {
			get { return Math.Abs(endAngle-startAngle)*radius; }
		}

		public double DistanceToGo(PointOnPath pt) {
			return DistanceToGo(pt.pt);
		}

		public double DistanceToGo(Coordinates pt) {
			// get the angle of the point
			double angle = (pt - center).ArcTan;

			// figure out where this sits in the scheme of things
			if (ccw) {
				// get it larger than start angle
				while (angle < startAngle)
					angle += 2*Math.PI;

				// make sure it's within 2pi
				while ((angle - startAngle) > 2*Math.PI) {
					angle -= 2*Math.PI;
				}

				// figure out if it's less than the end angle
				if (angle > endAngle) {
					// figure out the half-angle between end and start
					double halfAngle = Math.PI + (startAngle + endAngle) / 2.0;
					// check if we're closer to the start or end
					if (angle > halfAngle)
						return Length;
					else
						return 0;
				}

				// it's in the bounds, so calculate distance to end angle
				return radius*(endAngle-angle);
			}
			else {
				// get it smaller than the start angle
				if (angle > startAngle)
					angle -= 2*Math.PI;

				// figure out oif it's smaller than the end angle
				if (angle < endAngle)
					return 0;

				// it's in the bounds, so calculate distance to end angle
				return radius*(angle-endAngle);
			}
		}

		public double DistanceOffPath(Coordinates pt) {
			return Math.Abs(pt.DistanceTo(center) - radius);
		}

		public PointOnPath ClosestPoint(Coordinates pt) {
			Coordinates ptOnCircle = center + (pt - center).Normalize(radius); 
			// figure out if it's between the start and end point
			double angle = (ptOnCircle - center).ArcTan;

			if (ccw) {
				// add the start angle if needed
				while (angle < startAngle) {
					angle += 2*Math.PI;
				}

				// check if we're past the end angle
				if (angle > endAngle) {
					// figure out the half-angle between end and start
					double halfAngle = Math.PI + (startAngle + endAngle) / 2.0;
					if (angle > halfAngle)
						ptOnCircle = Start;
					else
						ptOnCircle = End;
				}
			}
			else {
				// get it smaller than the start angle
				while (angle > startAngle) {
					angle -= 2*Math.PI;
				}

				if (angle < endAngle) {
					// figure out the half-angle between end and start
					double halfAngle = endAngle - (2*Math.PI - (startAngle - endAngle))/2;

					if (angle > halfAngle)
						ptOnCircle = Start;
					else
						ptOnCircle = End;
				}
			}

			return new PointOnPath(this, Length - DistanceToGo(ptOnCircle), ptOnCircle);
		}

		public PointOnPath AdvancePoint(PointOnPath pt, ref double dist) {
			// get the angle of the point
			double angle = (pt.pt - center).ArcTan;

			// figure out where this sits in the scheme of things
			if (ccw) {
				// get it larger than start angle
				if (angle < startAngle)
					angle += 2*Math.PI;

				// figure out if it's less than the end angle
				if (angle > endAngle)
					throw new InvalidOperationException();

				// it's in the bounds, so calculate distance to end angle
				double distToGo = radius*(endAngle-angle);
				if (dist < distToGo) {
					double newAngle = angle + dist / radius;
					Coordinates pt2 = new Coordinates(center.X + radius*Math.Cos(newAngle), center.Y + radius*Math.Sin(newAngle));
					double dist2 = radius*(newAngle - startAngle);
					dist = 0;
					return new PointOnPath(this, dist2, pt2);
				}
				else {
					dist -= distToGo;
					return EndPoint;
				}
			}
			else {
				// get it smaller than the start angle
				if (angle > startAngle)
					angle -= 2*Math.PI;

				// figure out oif it's smaller than the end angle
				if (angle < endAngle)
					throw new InvalidOperationException();

				// it's in the bounds, so calculate distance to end angle
				double distToGo = radius*(angle-endAngle);
				if (dist < distToGo) {
					double newAngle = angle - dist/radius;
					Coordinates pt2 = new Coordinates(center.X + radius*Math.Cos(newAngle), center.Y + radius*Math.Sin(newAngle));
					double dist2 = radius*(startAngle - newAngle);
					dist = 0;
					return new PointOnPath(this, dist2, pt2);
				}
				else {
					dist -= distToGo;
					return EndPoint;
				}
			} 
		}

		public Coordinates Tangent(PointOnPath pt) {
			double angle = (pt.pt - center).ArcTan;
			if (ccw) {
				return new Coordinates(1,0).Rotate(angle + Math.PI/2);
			}
			else {
				return new Coordinates(1,0).Rotate(angle - Math.PI/2);
			}
		}

		public double Curvature(PointOnPath pt) {
			return 1/radius;
		}

		public IPathSegment Clone() {
			return new CirclePathSegment(center, radius, startAngle, endAngle, ccw);
		}

		public void Transform(UrbanChallenge.Common.Mapack.Matrix3 m) {
			throw new NotSupportedException();
		}

		#endregion

		#region IEquatable<IPathSegment> Members

		public bool Equals(IPathSegment other) {
			if (other is CirclePathSegment) {
				CirclePathSegment cp = (CirclePathSegment)other;
				return cp.radius == radius && cp.ccw == ccw && cp.startAngle == startAngle && cp.endAngle == endAngle && cp.center == center;
			}
			else {
				return false;
			}
		}

		#endregion
	}
}
