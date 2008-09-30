using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.Shapes {
	[Serializable]
	public struct Circle : IEquatable<Circle> {
		public static readonly Circle Infinite = new Circle(double.PositiveInfinity, new Coordinates(0, 0));
		private static readonly Coordinates[] emptyCoords = new Coordinates[0];

		public double r;
		public Coordinates center;

		public Circle(double r, Coordinates center) {
			this.r = r;
			this.center = center;
		}

		public static double GetCurvature(Coordinates p0, Coordinates p1, Coordinates p2) {
			Coordinates d01 = p0 - p1;
			Coordinates d21 = p2 - p1;
			Coordinates d20 = p2 - p0;

			return 2*d01.Cross(d21)/(d01.Length*d21.Length*d20.Length);
		}

		public static Circle FromPoints(Coordinates p0, Coordinates p1, Coordinates p2) {
			Coordinates t1 = p1 - p0;
			Coordinates t2 = p1 - p2;

			Coordinates c1 = (p0 + p1) / 2.0;
			Coordinates c2 = (p1 + p2) / 2.0;

			// check if the points are colinear
			if (Math.Abs(t1.Cross(t2)) < 1e-20) {
				return Infinite;
			}
			else {
				double k = t2.Dot(c2 - c1) / t1.Cross(t2);
				Coordinates center = c1 + t1.Rotate90() * k;
				double radius = p0.DistanceTo(center);
				return new Circle(radius, center);
			}
		}

		public static Circle FromPointSlopeRadius(Coordinates p0, Coordinates tangent, double r) {
			if (r <= 0)
				throw new ArgumentOutOfRangeException("r", "Radius must be positive");

			Coordinates center = p0 + tangent.Rotate90().Normalize(r);
			return new Circle(r, center);
		}

		public static Circle FromPointSlopePoint(Coordinates p0, Coordinates tangent, Coordinates p1) {
			Line l1 = new Line(p0, p0 + tangent.Rotate90());
			Line l2 = new Line((p0+p1)/2, (p0+p1)/2 + (p1 - p0).Rotate90());

			Coordinates pt;
			if (l1.Intersect(l2, out pt)) {
				return new Circle(pt.DistanceTo(p0), pt);
			}
			else {
				return Circle.Infinite;
			}
		}

		public static Circle FromLines(Line oncircle, Line toline, out Coordinates tolinePoint) {
			tolinePoint = default(Coordinates);

			Coordinates m = (oncircle.P1 - oncircle.P0).Normalize();
			Coordinates u = (toline.P1 - toline.P0).Normalize();

			if (Math.Abs(m.Cross(u)) < 1e-10) {
				return Circle.Infinite;
			}

			Coordinates c = m.Rotate90();
			Coordinates t = u.Rotate90();

			Coordinates t_c = t - c;

			Matrix2 A = new Matrix2(
				u.X, t_c.X,
				u.Y, t_c.Y);

			Coordinates d = oncircle.P0 - toline.P0;

			if (Math.Abs(A.Determinant()) < 1e-10) {
				return Circle.Infinite;
			}

			Coordinates pvec = A.Inverse() * d;

			Coordinates center = oncircle.P0 + pvec.Y*c;

			tolinePoint = toline.P0 + pvec.X*u;
			return new Circle(Math.Abs(pvec.Y), center);
		}

		public Coordinates GetPoint(double theta) {
			return center + new Coordinates(r, 0).Rotate(theta);
		}

		public bool Intersect(LineSegment line, out Coordinates[] pts) {
			double[] K;
			return Intersect(line, out pts, out K);
		}

		public bool Intersect(LineSegment line, out Coordinates[] pts, out double[] K) {
			Coordinates t = line.P1 - line.P0;
			Coordinates s = line.P0 - center;

			double t_dot_s = t.Dot(s);
			double tnorm2 = t.Dot(t);
			double snorm2 = s.Dot(s);

			// check the discriminant to see if there is an intersection or not
			double disc = t_dot_s*t_dot_s - tnorm2*(snorm2 - r*r);
			if (disc < 0) {
				// there are no intersecting points
				goto NoIntersection;
			}
			else if (Math.Abs(disc) < 1e-10) {
				// there is one intersecting point, trim to line segment
				double u = -t_dot_s/tnorm2;
				if (u >= 0 && u <= 1) {
					pts = new Coordinates[] { line.P0 + t * u };
					K = new double[] { u };
					return true;
				}
				else {
					goto NoIntersection;
				}
			}
			else {
				// there are two intersecting points on the line, trim to line segment
				double sqrt_d = Math.Sqrt(disc);
				double u1 = (-t_dot_s - sqrt_d) / tnorm2;
				double u2 = (-t_dot_s + sqrt_d) / tnorm2;

				if (u1 >= 0 && u1 <= 1 && u2 >= 0 && u2 <= 1) {
					pts = new Coordinates[] { line.P0 + t * u1, line.P0 + t * u2 };
					K = new double[] { u1, u2 };
					return true;
				}
				else if (u1 >= 0 && u1 <= 1) {
					pts = new Coordinates[] { line.P0 + t * u1 };
					K = new double[] { u1 };
					return true;
				}
				else if (u2 >= 0 && u2 <= 1) {
					pts = new Coordinates[] { line.P0 + t * u2 };
					K = new double[] { u2 };
					return true;
				}
				else {
					goto NoIntersection;
				}
			}

		NoIntersection:
			pts = default(Coordinates[]);
			K = default(double[]);
			return false;
		}

		public bool Intersect(Line line, out Coordinates[] pts) {
			double[] K;
			return Intersect(line, out pts, out K);
		}

		public bool Intersect(Line line, out Coordinates[] pts, out double[] K) {
			Coordinates t = line.P1 - line.P0;
			Coordinates s = line.P0 - center;

			double t_dot_s = t.Dot(s);
			double tnorm2 = t.Dot(t);
			double snorm2 = s.Dot(s);

			// check the discriminant to see if there is an intersection or not
			double disc = t_dot_s * t_dot_s - tnorm2 * (snorm2 - r * r);
			if (disc < 0) {
				// there are no intersecting points
				goto NoIntersection;
			}
			else if (Math.Abs(disc) < 1e-10) {
				// there is one intersecting point
				double u = -t_dot_s / tnorm2;
				
				pts = new Coordinates[] { line.P0 + t * u };
				K = new double[] { u };
				return true;
			}
			else {
				// there are two intersecting points
				double sqrt_d = Math.Sqrt(disc);
				double u1 = (-t_dot_s - sqrt_d) / tnorm2;
				double u2 = (-t_dot_s + sqrt_d) / tnorm2;

				pts = new Coordinates[] { line.P0 + t * u1, line.P0 + t * u2 };
				K = new double[] { u1, u2 };
				return true;
			}

		NoIntersection:
			pts = default(Coordinates[]);
			K = default(double[]);
			return false;
		}

		public Coordinates[] Intersect(Circle circle) {
			double d = center.DistanceTo(circle.center);
			// check if there is any possibility of intersection
			if (d > r + circle.r) {
				return emptyCoords;
			}
			// check if they are the same centers
			else if (center.Equals(circle.center)) {
				return emptyCoords;
			}
			// check if one is completely interned by the other
			else if (d + r < circle.r || d + circle.r < r) {
				return emptyCoords;
			}
			else {
				// there is at least one intersection
				// assume that the circle are both lying on the x-axis to simplify analysis
				double R2 = circle.r * circle.r;
				double r2 = r * r;
				double d2 = d * d;

				double x = (d2 - r2 + R2) / (2 * d);
				double y = Math.Sqrt(R2 - x * x);

				// check if y is close to 0, indicating that this is only one hit
				if (y < 1e-10) {
					return new Coordinates[] { circle.center + r * (center - circle.center).Normalize() };
				}
				else {
					Coordinates vec = (center - circle.center).Normalize();
					Coordinates nom_pt = circle.center + x * vec;

					Coordinates perp_vec = vec.Rotate90();
					return new Coordinates[] { nom_pt + y * perp_vec, nom_pt - y * perp_vec };
				}
			}
		}

		public Coordinates ClosestPoint(Coordinates pt) {
			Coordinates vec = (pt - center).Normalize();
			return vec*r;
		}

		public bool IsInside(Coordinates pt) {
			return pt.DistanceTo(center) <= r;
		}

		public Coordinates[] GetTangentPoints(Coordinates ptFrom) {
			if (IsInside(ptFrom))
				return default(Coordinates[]);

			double h = ptFrom.DistanceTo(center);
			double o = r;

			double theta = Math.PI/2.0 - Math.Asin(o/h);

			Coordinates closestPt = ClosestPoint(ptFrom);
			Coordinates[] pts = new Coordinates[2];
			pts[0] = closestPt.Rotate(theta) + center;
			pts[1] = closestPt.Rotate(-theta) + center;

			return pts;
		}

		public Rect GetBoundingRectangle() {
			return new Rect(center.X-r, center.Y-r, 2*r, 2*r);
		}

		public Circle Transform(IPointTransformer transform) {
			return new Circle(r, transform.TransformPoint(center));
		}

		public Polygon ToPolygon(int numPoints) {
			double angleSpacing = 2*Math.PI/numPoints;

			Polygon p = new Polygon(numPoints);

			for (int i = 0; i < numPoints; i++) {
				p.Add(center + Coordinates.FromAngle(angleSpacing*i)*r);
			}

			return p;
		}

		#region IEquatable<Circle> Members

		public bool Equals(Circle c) {
			// check if both have the same radius and the centers match, or both radii are infinite
			return (c.r == r && c.center.Equals(center)) || (double.IsInfinity(c.r) && double.IsInfinity(r));
		}

		#endregion

		public override bool Equals(object obj) {
			if (obj is Circle) {
				return Equals((Circle)obj);
			}
			else {
				return false;
			}
		}

		public override int GetHashCode() {
			return r.GetHashCode() ^ center.GetHashCode();
		}

		public override string ToString() {
			return "(" + center.ToString() + "),r:" + r.ToString();
		}
	}
}
