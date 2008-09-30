using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using System.Diagnostics;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Splines;

namespace UrbanChallenge.Common.Path {
	[Serializable]
	public class LinePath : LineList {

		#region PointOnPath class

		[Serializable]
		public struct PointOnPath : IEquatable<PointOnPath>, IComparable, IComparable<PointOnPath> {
			public static PointOnPath Invalid { get { return new PointOnPath(); } }

			public readonly int Index;
			public readonly double Dist;
			public readonly double DistFraction;
			public readonly bool Valid;
			public readonly LinePath Path;

			public PointOnPath(int index, double dist, double distFrac, LinePath path) {
				this.Index = index;
				this.Dist = dist;
				this.DistFraction = distFrac;
				this.Valid = true;
				this.Path = path;
			}

			#region Utility methods

			/// <summary>
			/// Returns the physical location of the point referenced to the parent path
			/// </summary>
			public Coordinates Location {
				get { return Path.GetPoint(this); }
			}

			public double OfftrackDistance(Coordinates loc) {
				// get the unit vector of the segment on the path rotated by 90
				Coordinates segmentVector = Path.GetSegment(Index).UnitVector.Rotate90();
				return segmentVector.Dot(loc-Location);
			}

			public double AlongtrackDistance(Coordinates loc) {
				Coordinates segmentVector = Path.GetSegment(Index).UnitVector;
				return segmentVector.Dot(loc-Location);
			}

			#endregion

			#region standard overrides

			public override bool Equals(object obj) {
				if (obj is PointOnPath) {
					return Equals((PointOnPath)obj);
				}
				else {
					return false;
				}
			}

			public override int GetHashCode() {
				return Index << 16 ^ Dist.GetHashCode();
			}

			#endregion

			#region IEquatable<PointOnPath> Members

			public bool Equals(PointOnPath other) {
				return other.Index == Index && other.Dist == Dist;
			}

			#endregion

			#region IComparable Members

			public int CompareTo(object obj) {
				if (obj is PointOnPath) {
					return CompareTo((PointOnPath)obj);
				}
				else {
					throw new ArgumentException("Invalid comparison target");
				}
			}

			#endregion

			#region IComparable<PointOnPath> Members

			public int CompareTo(PointOnPath other) {
				if (Index < other.Index)
					return -1;
				else if (Index > other.Index)
					return 1;
				else {
					return Dist.CompareTo(other.Dist);
				}
			}

			#endregion

			#region Operators

			public static bool operator==(PointOnPath lhs, PointOnPath rhs) {
				return lhs.Equals(rhs);
			}

			public static bool operator!=(PointOnPath lhs, PointOnPath rhs) {
				return !lhs.Equals(rhs);
			}

			public static bool operator<(PointOnPath lhs, PointOnPath rhs) {
				return lhs.CompareTo(rhs) == -1;
			}

			public static bool operator<=(PointOnPath lhs, PointOnPath rhs) {
				return lhs.CompareTo(rhs) <= 0;
			}

			public static bool operator>(PointOnPath lhs, PointOnPath rhs) {
				return lhs.CompareTo(rhs) == 1;
			}

			public static bool operator>=(PointOnPath lhs, PointOnPath rhs) {
				return lhs.CompareTo(rhs) >= 0;
			}

			#endregion
		}

		#endregion

		#region Constructors

		public LinePath() {
		}

		public LinePath(IEnumerable<Coordinates> points)
			: base(points) {
		}

		public LinePath(int capacity)
			: base(capacity) {
		}

		public static LinePath FromPath(IPath path) {
			if (path.Count == 0) {
				return new LinePath();
			}
			
			LinePath ret = new LinePath(path.Count + 1);

			ret.Add(path[0].Start);

			for (int i = 0; i < path.Count; i++) {
				ret.Add(path[i].End);
			}

			return ret;
		}

		public Path ToPath() {
			Path p = new Path();
			foreach (LineSegment seg in GetSegmentEnumerator()) {
				p.Add(new LinePathSegment(seg.P0, seg.P1));
			}

			return p;
		}

		public LinePath Clone() {
			LinePath clone = new LinePath(this.Count);
			clone.AddRange(this);
			return clone;
		}

		#endregion

		#region Misc Accessors

		public double PathLength {
			get {
				double len = 0;
				for (int i = 0; i < Count-1; i++) {
					len += SegmentLength(i);
				}

				return len;
			}
		}

		public Rect GetBoundingBox() {
			double minX = double.MaxValue, minY = double.MaxValue;
			double maxX = double.MinValue, maxY = double.MinValue;

			for (int i = 0; i < Count; i++) {
				Coordinates pt = this[i];

				if (pt.X < minX) minX = pt.X;
				if (pt.X > maxX) maxX = pt.X;

				if (pt.Y < minY) minY = pt.Y;
				if (pt.Y > maxY) maxY = pt.Y;
			}

			return new Rect(minX, minY, (maxX-minX), (maxY-minY));
		}

		#endregion

		#region Transforms

		public void TransformInPlace(IPointTransformer transformer) {
			transformer.TransformPointsInPlace(this);
		}

		public new LinePath Transform(IPointTransformer transformer) {
			return new LinePath(transformer.TransformPoints(this));
		}

		#endregion

		#region PointOnPath getters/manipulators

		public PointOnPath StartPoint {
			get { return new PointOnPath(0, 0, 0, this); }
		}

		public PointOnPath EndPoint {
			get { return new PointOnPath(Count-2, SegmentLength(Count-2), 1, this); }
		}

		public PointOnPath ZeroPoint {
			get { return GetClosestPoint(new Coordinates(0, 0)); }
		}

		public LineSegment EndSegment {
			get { return GetSegment(Count-2); }
		}

		public LineSegment GetSegment(int index) {
			return new LineSegment(this[index], this[index+1]);
		}

		public double SegmentLength(int index) {
			return (this[index+1]-this[index]).Length;
		}

		public Coordinates GetPoint(PointOnPath pt) {
			// check the first point specially
			Coordinates s = this[pt.Index];
			Coordinates e = this[pt.Index+1];
			Coordinates v = e-s;
			return s + pt.DistFraction*v;
		}

		public PointOnPath GetPointOnPath(int pointIndex) {
			if (pointIndex < 0 || pointIndex >= Count) {
				throw new ArgumentOutOfRangeException();
			}
			if (pointIndex == Count-1) {
				// don't make the point index the last point on the path, return the end point
				return EndPoint;
			}
			else {
				return new PointOnPath(pointIndex, 0, 0, this);
			}
		} 

		public PointOnPath GetClosestPoint(Coordinates loc) {
			double minDist = double.MaxValue;
			PointOnPath pointMin = new PointOnPath();

			// iterate throug each line segment and find the closest point
			for (int i = 0; i < Count-1; i++) {
				Coordinates s = this[i];
				Coordinates v = this[i+1] - s;

				double len = v.Length;
				v /= len;

				double t = v * (loc - s);
				Coordinates pt;
				if (t < 0) {
					pt = s;
					t = 0;
				}
				else if (t > len) {
					pt = this[i+1];
					t = len;
				}
				else {
					pt = s + t*v;
				}

				// figure out the distance
				double dist = (pt-loc).Length;
				if (dist < minDist) {
					minDist = dist;
					pointMin = new PointOnPath(i, t, t/len, this);
				}
			}

			return pointMin;
		}

		public PointOnPath GetForwardClosestPoint(PointOnPath startPoint, Coordinates loc) {
			double minDist = double.MaxValue;
			PointOnPath minPoint = new PointOnPath();

			Debug.Assert(startPoint.Valid);
			
			// check the first point specially
			Coordinates s = this[startPoint.Index];
			Coordinates e = this[startPoint.Index+1];
			Coordinates v = e-s;

			double len = v.Length;
			v /= len;

			double t = v*(loc-s);
			if (t > len) {
				minDist = (loc - e).Length;
				minPoint = new PointOnPath(startPoint.Index, len, 1, this);
			}
			else if (t < startPoint.Dist) {
				Coordinates pt = s + v*startPoint.Dist;
				minDist = (loc-pt).Length;
				minPoint = startPoint;
			}
			else {
				Coordinates pt = s + v*t;
				minDist = (loc-pt).Length;
				minPoint = new PointOnPath(startPoint.Index, t, t/len, this);
			}

			for (int i = startPoint.Index + 1; i < Count-1; i++) {
				s = this[i];
				e = this[i+1];
				v = e - s;

				len = v.Length;
				v /= len;

				t = v*(loc-s);
				Coordinates pt;
				if (t < 0) {
					pt = s;
					t = 0;
				}
				else if (t > len) {
					pt = e;
					t = len;
				}
				else {
					pt = s + t*v;
				}

				// figure out the distance
				double dist = (pt-loc).Length;
				if (dist < minDist) {
					minDist = dist;
					minPoint = new PointOnPath(i, t, t/len, this);
				}
			}

			return minPoint;
		}

		public PointOnPath GetReverseClosestPoint(PointOnPath startPoint, Coordinates loc) {
			double minDist = double.MaxValue;
			PointOnPath minPoint = new PointOnPath();

			Debug.Assert(startPoint.Valid);

			// check the first point specially
			Coordinates s = this[startPoint.Index];
			Coordinates e = this[startPoint.Index+1];
			Coordinates v = e-s;

			double len = v.Length;
			v /= len;

			double t = v*(loc-s);
			if (t > startPoint.Dist) {
				Coordinates pt = s + v*startPoint.Dist;
				minDist = (loc - pt).Length;
				minPoint = startPoint;
			}
			else if (t <= 0) {
				minDist = (loc-s).Length;
				minPoint = new PointOnPath(startPoint.Index, 0, 0, this);
			}
			else {
				Coordinates pt = s + v*t;
				minDist = (loc-pt).Length;
				minPoint = new PointOnPath(startPoint.Index, t, t/len, this);
			}

			for (int i = startPoint.Index-1; i >= 0; i--) {
				s = this[i];
				e = this[i+1];
				v = e - s;

				len = v.Length;
				v /= len;

				t = v*(loc-s);
				Coordinates pt;
				if (t < 0) {
					pt = s;
					t = 0;
				}
				else if (t > len) {
					pt = e;
					t = len;
				}
				else {
					pt = s + t*v;
				}

				// figure out the distance
				double dist = (pt-loc).Length;
				if (dist < minDist) {
					minDist = dist;
					minPoint = new PointOnPath(i, t, t/len, this);
				}
			}

			return minPoint;
		}

		public PointOnPath GetIntersectionPoint(Line l) {
			// find if there is a line segment that intersects with the specified line
			for (int i = 0; i < this.Count-1; i++) {
				LineSegment seg = GetSegment(i);
				Coordinates pt, k;
				if (seg.Intersect(l, out pt, out k)) {
					return new PointOnPath(i, k.X*seg.Length, k.X, this);
				}
			}

			return PointOnPath.Invalid;
		}

		public PointOnPath AdvancePoint(PointOnPath startPoint, double dist) {
			return AdvancePoint(startPoint, ref dist);
		}

		public PointOnPath AdvancePoint(PointOnPath startPoint, ref double dist) {
			Debug.Assert(startPoint.Valid);

			if (dist > 0) {
				// check if we can satisfy fith the first point
				double len = SegmentLength(startPoint.Index);
				if (startPoint.Dist + dist < len) {
					double totDist = startPoint.Dist + dist;
					PointOnPath ret = new PointOnPath(startPoint.Index, totDist, totDist/len, this);
					dist = 0;
					return ret;
				}
				else {
					// we need to use multiple segments
					dist -= (len-startPoint.Dist);

					for (int i = startPoint.Index+1; i < Count-1; i++) {
						len = SegmentLength(i);
						if (dist < len) {
							// this segment will satisfy the remaining distance
							PointOnPath ret = new PointOnPath(i, dist, dist/len, this);
							dist = 0;
							return ret;
						}
						else {
							dist -= len;
						}
					}

					// we couldn't satisfy the distance, return the end point
					return EndPoint;
				}
			}
			else {
				// distance < 0, go backwards
				if (startPoint.Dist + dist >= 0) {
					double len = SegmentLength(startPoint.Index);
					double totDist = startPoint.Dist + dist;
					PointOnPath ret = new PointOnPath(startPoint.Index, totDist, totDist/len, this);
					dist = 0;
					return ret;
				}
				else {
					dist += startPoint.Dist;

					for (int i = startPoint.Index-1; i >= 0; i--) {
						double len = SegmentLength(i);
						if (len + dist >= 0) {
							// this segment will satisfy the remaining distance
							PointOnPath ret = new PointOnPath(i, len+dist, (len+dist)/len, this);
							dist = 0;
							return ret;
						}
						else {
							dist += len;
						}
					}

					return StartPoint;
				}
			}
		}

		public double DistanceBetween(PointOnPath start, PointOnPath end) {
			if (start.Index == end.Index) {
				return end.Dist - start.Dist;
			}
			else if (start.Index < end.Index) {
				double dist = SegmentLength(start.Index)-start.Dist;
				for (int i = start.Index+1; i < end.Index; i++) {
					dist += SegmentLength(i);
				}
				dist += end.Dist;

				return dist;
			}
			else {
				// start.Index > end.Index
				double dist = start.Dist;
				for (int i = end.Index + 1; i < start.Index; i++) {
					dist += SegmentLength(i);
				}
				dist += (SegmentLength(end.Index) - end.Dist);
				return -dist;
			}
			// compute the total distance to start, end
			//double startDist = start.Dist;
			//for (int i = 0; i < start.Index; i++) {
			//  startDist += SegmentLength(i);
			//}

			//double endDist = end.Dist;
			//for (int i = 0; i < end.Index; i++) {
			//  endDist += SegmentLength(i);
			//}

			//return endDist - startDist;
		}

		#endregion

		#region Path manipulation

		public LinePath SubPath(int start, int end) {
			LinePath ret = new LinePath(end-start+1);

			for (int i = start; i <= end; i++) {
				ret.Add(this[i]);
			}

			return ret;
		}

		public LinePath SubPath(PointOnPath start, PointOnPath end) {
			Debug.Assert(start.Valid);
			Debug.Assert(end.Valid);

			LinePath ret = new LinePath();

			if (start.Index < end.Index || ((start.Index == end.Index) && (start.Dist < end.Dist))) {
				if (start.DistFraction < 0.999999999) {
					ret.Add(GetPoint(start));
				}

				// iterate through and add all the end point
				for (int i = start.Index+1; i <= end.Index; i++) {
					ret.Add(this[i]);
				}

				if (end.DistFraction > 0) {
					ret.Add(GetPoint(end));
				}

				return ret;
			}
			else {
				// end is first
				if (start.DistFraction > 0) {
					ret.Add(GetPoint(start));
				}

				for (int i = start.Index; i > end.Index; i--) {
					ret.Add(this[i]);
				}

				if (end.DistFraction < 1) {
					ret.Add(GetPoint(end));
				}

				return ret;
			}
		}

		public LinePath SubPath(PointOnPath start, double dist) {
			return SubPath(start, ref dist);
		}

		public LinePath SubPath(PointOnPath start, ref double dist) {
			PointOnPath end = AdvancePoint(start, ref dist);
			return SubPath(start, end);
		}

		/// <summary>
		/// Returns a line path with all points before pt removed
		/// </summary>
		public LinePath RemoveBefore(PointOnPath pt) {
			LinePath ret = new LinePath(Count-pt.Index);
			if (pt.DistFraction < 1) {
				ret.Add(pt.Location);
			}

			for (int i = pt.Index + 1; i < Count; i++) {
				ret.Add(this[i]);
			}

			return ret;
		}

		public LinePath RemoveAfter(PointOnPath pt) {
			LinePath ret = new LinePath(pt.Index+2);
			for (int i = 0; i < pt.Index; i++) {
				ret.Add(this[i]);
			}

			if (pt.DistFraction > 0) {
				ret.Add(pt.Location);
			}

			return ret;
		}

		public LinePath RemoveBetween(PointOnPath start, PointOnPath end) {
			// remove all the points between start and end (non-inclusive)

			// nothing to remove if they are the same index
			if (start.Index == end.Index) {
				return new LinePath(this);
			}

			int endIndex = end.DistFraction == 1 ? end.Index+2 : end.Index+1;

			LinePath ret = new LinePath();
			for (int i = 0; i <= start.Index; i++) {
				ret.Add(this[i]);
			}

			// if the start is not at a point, add it
			if (start.DistFraction != 0) {
				ret.Add(start.Location);
			}

			// add the end point
			ret.Add(end.Location);

			for (int i = endIndex; i < this.Count; i++) {
				ret.Add(this[i]);
			}

			return ret;
		}

		public LinePath RemoveZeroLengthSegments() {
			LinePath ret = new LinePath(this.Count);
			ret.Add(this[0]);
			for (int i = 1; i < this.Count; i++) {
				if (!this[i].ApproxEquals(this[i-1], 1e-10)) {
					ret.Add(this[i]);
				}
			}

			return ret;
		}

		public LinePath Resample(PointOnPath start, PointOnPath end, double spacing) {
			LinePath ret = new LinePath();
			PointOnPath pt = start;

			while (pt < end) {
				ret.Add(GetPoint(pt));
				pt = AdvancePoint(pt, spacing);
			}

			ret.Add(GetPoint(end));

			return ret;
		}

		public LinePath Resample(double spacing) {
			LinePath ret = new LinePath();
			PointOnPath pt = StartPoint;

			while (pt != EndPoint) {
				ret.Add(GetPoint(pt));
				pt = AdvancePoint(pt, spacing);
			}

			ret.Add(GetPoint(EndPoint));

			return ret;
		}

		[Obsolete]
		public LinePath ApplyMovingAverageSmooth(int iterations, int k_max) {
			LinePath smoothed = new LinePath(this.Count);
			LinePath source = this;
			for (int iter = 0; iter < iterations; iter++) {
				for (int i = 0; i < Count; i++) {
					int k_lower = Math.Min(k_max, i);
					int k_upper = Math.Min(k_max, Count-i-1);
					int k = Math.Min(k_lower, k_upper);

					Coordinates sum = Coordinates.Zero;
					for (int j = i-k; j <= i+k; j++) {
						sum += source[j];
					}

					smoothed.Add(sum/(2.0*k + 1.0));
				}

				source = smoothed;
				smoothed = new LinePath(source.Count);
			}

			return source;
		}

		public LinePath SplineInterpolate(double desiredSpacing) {
			CubicBezier[] beziers = SmoothingSpline.BuildCatmullRomSpline(this.ToArray(), null, null);
			LinePath newPath = new LinePath();
			// insert the first point
			newPath.Add(this[0]);
			for (int i = 0; i < beziers.Length; i++) {
				double splineLength = beziers[i].ArcLength;
				int numT = (int)(splineLength/desiredSpacing);
				double tspacing = 1.0/numT;

				double t;
				for (t = tspacing; t < 1; t += tspacing) {
					newPath.Add(beziers[i].Bt(t));
				}

				newPath.Add(this[i+1]);
			}

			return newPath;
		}

		public LinePath ShiftLateral(double dist) {
			if (this.Count < 2) {
				throw new InvalidOperationException("Cannot shift a line with less than two points.");
			}
			// create a list of shift line segments
			List<LineSegment> segs = new List<LineSegment>();

			foreach (LineSegment ls in GetSegmentEnumerator()) {
				segs.Add(ls.ShiftLateral(dist));
			}

			// find the intersection points between all of the segments
			LinePath boundPoints = new LinePath(this.Count);
			// add the first point
			boundPoints.Add(segs[0].P0);

			// loop through the stuff
			for (int i = 0; i < segs.Count-1; i++) {
				// find the intersection
				Line l0 = (Line)segs[i];
				Line l1 = (Line)segs[i+1];

				Coordinates pt;
				if (l0.Intersect(l1, out pt)) {
					boundPoints.Add(pt);
				}
				else {
					boundPoints.Add(segs[i].P1);
				}
			}

			// add the last point
			boundPoints.Add(segs[segs.Count-1].P1);

			return boundPoints;
		}

		public LinePath ShiftLateral(double[] dists) {
			if (dists == null)
				throw new ArgumentNullException();
			if (dists.Length != Count)
				throw new ArgumentOutOfRangeException();

			LinePath path = new LinePath(this.Count);

			if (this.Count == 0) {
				return path;
			}
			else if (this.Count == 1) {
				path.Add(this[0]);
				return path;
			}

			// get the normal of the first point
			Coordinates v0 = GetSegment(0).UnitVector.Rotate90();
			// offset and add it
			path.Add(this[0] + v0*dists[0]);

			// handle all the intermediate points
			for (int i = 1; i < Count-1; i++) {
				Coordinates vi = (this[i+1]-this[i-1]).Normalize().Rotate90();
				path.Add(this[i] + vi*dists[i]);
			}

			// get the normal of the last point
			Coordinates vn = EndSegment.UnitVector.Rotate90();
			path.Add(this[this.Count-1] + vn*dists[this.Count-1]);

			return path;
		}


		#endregion

		#region Enumerators

		public void ForEach(int start, int end, Action<Coordinates> action) {
			for (int i = start; i < end; i++)
				action(this[i]);
		}

		public void ForEach(Action<LineSegment> action) {
			ForEach(0, Count, action);
		}

		public void ForEach(int start, int end, Action<LineSegment> action) {
			for (int i = start; i < end; i++) {
				action(GetSegment(i));
			}
		}

		public List<Pair<int, double>> GetIntersectionAngles(int start, int end) {
			List<Pair<int, double>> ret = new List<Pair<int, double>>();
			for (int i = start; i < end-1; i++) {
				LineSegment l0 = GetSegment(i);
				LineSegment l1 = GetSegment(i+1);
				double angle = Math.Acos(l0.UnitVector.Dot(l1.UnitVector));

				ret.Add(new Pair<int, double>(i+1, angle));
			}

			return ret;
		}

		public IEnumerable<LineSegment> GetSegmentEnumerator() {
			for (int i = 0; i < Count-1; i++) {
				yield return GetSegment(i);
			}
		}

		public IEnumerable<Coordinates> GetSubpathEnumerator(int startingIndex, int endIndex) {
			if (startingIndex < endIndex) {
				for (int i = startingIndex; i <= endIndex; i++) {
					yield return this[i];
				}
			}
			else {
				for (int i = startingIndex; i >= endIndex; i--) {
					yield return this[i];
				}
			}
		}

		public IEnumerable<Coordinates> GetSubpathEnumerator(PointOnPath start, PointOnPath end) {
			Debug.Assert(start.Valid);
			Debug.Assert(end.Valid);

			LinePath ret = new LinePath();

			if (start.Index < end.Index || ((start.Index == end.Index) && (start.Dist < end.Dist))) {
				if (start.DistFraction < 1) {
					yield return start.Location;
				}

				// iterate through and add all the end point
				for (int i = start.Index+1; i <= end.Index; i++) {
					yield return this[i];
				}

				if (end.DistFraction > 0) {
					yield return end.Location;
				}
			}
			else {
				// end is first
				if (start.DistFraction > 0) {
					yield return start.Location;
				}

				for (int i = start.Index; i > end.Index; i--) {
					yield return this[i];
				}

				if (end.DistFraction < 1) {
					yield return end.Location;
				}
			}
		}

		#endregion

	}
}
