using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Common.Shapes {
	[Serializable]
	public class Polygon : IList<Coordinates> {
		public List<Coordinates> points;
		private CoordinateMode coordMode;

		private Circle? boundingCircle = null;

		public Polygon() {
			points = new List<Coordinates>();
			coordMode = CoordinateMode.AbsoluteProjected;
		}

		public Polygon(int capacity) {
			points = new List<Coordinates>(capacity);
			coordMode = CoordinateMode.AbsoluteProjected;
		}

		public Polygon(CoordinateMode coordMode) {
			points = new List<Coordinates>();
			this.coordMode = coordMode;
		}

		public Polygon(IEnumerable<Coordinates> points) {
			this.points = new List<Coordinates>(points);
			coordMode = CoordinateMode.AbsoluteProjected;
		}

		public Polygon(IEnumerable<Coordinates> points, CoordinateMode coordMode) {
			this.points = new List<Coordinates>(points);
			this.coordMode = coordMode;
		}

		public CoordinateMode CoordinateMode {
			get { return coordMode; }
			set { coordMode = value; }
		}

		public Circle BoundingCircle {
			get {
				if (boundingCircle == null) {
					boundingCircle = CalculateBoundingCircle();
				}

				return boundingCircle.Value;
			}
		}

		/// <summary>
		/// Center of the polygon
		/// </summary>
		public Coordinates Center
		{
			get
			{
				return  this.GetCentroid();
				/*
				double x = 0;
				double y = 0;

				foreach (Coordinates c in this.points)
				{
					x += c.X;
					y += c.Y;
				}

				x = x / (double)this.points.Count;
				y = y / (double)this.points.Count;

				return new Coordinates(x, y);*/
			}
		}

		/// <summary>
		/// Inflates the polygon from the center
		/// </summary>
		/// <param name="amt">amt to expand by</param>
		public Polygon Inflate(double amt)
		{
			Coordinates center = this.Center;

			Polygon ePoints = new Polygon(points.Count);

			foreach (Coordinates c in this.points)
			{
				Coordinates vec = c - center;
				vec = vec.Normalize(vec.Length + amt);
				Coordinates nVec = center + vec;
				ePoints.Add(nVec);
			}

			return ePoints;
		}

		public double GetArea() {
			double area = 0;
			for (int i = 0, j = points.Count-1; i < points.Count; j = i++) {
				area += points[j].Cross(points[i]);
			}
			return 0.5*area;
		}

		public Coordinates GetCentroid() {
			if (points.Count == 0) {
				throw new InvalidOperationException("There are no points in the polygon");
			}
			else if (points.Count == 1) {
				return points[0];
			}
			else if (points.Count == 2) {
				return (points[0]+points[1])*0.5;
			}

			double area = 0;
			Coordinates c = Coordinates.Zero;
			for (int i = 0, j = points.Count-1; i < points.Count; j = i++) {
				double area_factor = points[j].Cross(points[i]);
				area += area_factor;
				c += (points[i]+points[j])*area_factor;
			}

			return c/(3*area);
		}

		public void GetCentroidAndArea(out Coordinates centroid, out double area) {
			if (points.Count == 0) {
				throw new InvalidOperationException("There are no points in the polygon");
			}
			else if (points.Count == 1) {
				centroid = points[0];
				area = 0;
				return;
			}
			else if (points.Count == 2) {
				centroid = (points[0]+points[1])*0.5;
				area = 0;
				return;
			}

			double a = 0;
			Coordinates c = Coordinates.Zero;

			for (int i = 0, j = points.Count-1; i < points.Count; j = i++) {
				double area_factor = points[j].Cross(points[i]);
				a += area_factor;
				c += (points[i]+points[j])*area_factor;
			}

			area = 0.5*a;
			centroid = c/(3*a);
		}

		public bool IsCounterClockwise {
			get { return GetArea() >= 0; }
		}

		/// <summary>
		/// Checks if polygon is inside this polygon
		/// </summary>
		/// <param name="p">Polygon to test</param>
		/// <returns></returns>
		public bool IsInside(Polygon p)
		{
			foreach (Coordinates c in p)
			{
				if (!this.IsInside(c))
					return false;
			}

			return true;
		}

		public bool IsInside(Coordinates pt) {
			// intersect the polygon with a line going along the x-axis
			// count the number of intersection with positive K
			// if it's odd, then we're inside

			//Line l = new Line(pt, pt + new Coordinates(0, 1));
			//Coordinates[] pts;
			//double[] K;

			//int inter_count = 0;
			//if (Intersect(l, out pts, out K)) {
			//  for (int i = 0; i < K.Length; i++) {
			//    if (K[i] > 0)
			//      inter_count++;
			//  }
			//}

			//return (inter_count % 2) == 1;

			// possible other code, not tested (see http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html)
			int i, j;
			bool c = false;
			// yes, this next line is correct, this is j = i++ (j always follows i by 1, except on the first iteration)
			for (i = 0, j = points.Count-1; i < points.Count; j = i++) {
				Coordinates ptj = points[j];
			  Coordinates pti = points[i];

			  // check if the polygon points y values span the test point y value and then 
			  // see if its an intersection
				if ((((pti.Y <= pt.Y) && (pt.Y < ptj.Y)) || 
						 ((ptj.Y <= pt.Y) && (pt.Y < pti.Y))) &&
					  (pt.X < (ptj.X-pti.X)*(pt.Y-pti.Y)/(ptj.Y-pti.Y) + pti.X)) {
					c = !c;
				}
			}

			return c;
		}

		public enum VectorDirection {
			Inside,
			Outside,
			AlongEdge
		}

		public VectorDirection GetVectorDirection(int boundPointIndex, Coordinates vector) {
			// assume the points go counter-clockwise
			Coordinates pt1 = this[boundPointIndex];
			Coordinates pt2 = this[(boundPointIndex+1)%Count];

			// if the vector formed by the points cross the test vector is < 0, then the vector is pointing out (to the right)
			// if > 0, then the vector is pointing in (to the left)
			// if == 0, then the vector is parallel to the edge vector
			double cross = (pt2-pt1).Cross(vector);
			if (Math.Abs(cross) < 1e-20) {
				return VectorDirection.AlongEdge;
			}
			else if (cross < 0) {
				return VectorDirection.Outside;
			}
			else {
				return VectorDirection.Inside;
			}
		}

    public bool DoesIntersect(LineSegment ls)
    {
      Coordinates[] tmp;
      return Intersect(ls, out tmp);
    }

		public bool Intersect(Line l, out Coordinates[] pts) {
			double[] K;
			return Intersect(l, out pts, out K);
		}

		public bool Intersect(Line l, out Coordinates[] pts, out double[] K) {
			// go through and label each point on the "left" or "right" of the line
			bool[] pt_side = new bool[points.Count];
			Coordinates s = l.P1 - l.P0;

			for (int i = 0; i < points.Count; i++) {
				Coordinates t = points[i] - l.P0;
				pt_side[i] = s.Cross(t) < 0;
			}

			// figure out any pairs spanning the line
			List<Coordinates> pt_list = new List<Coordinates>();
			List<double> K_list = new List<double>();
			for (int i = 0; i < points.Count - 1; i++) {
				if (pt_side[i] != pt_side[i+1]) {
					// this is a cross, check for intersection
					LineSegment ls = new LineSegment(points[i], points[i+1]);
					Coordinates pt_temp, K_temp;
					if (ls.Intersect(l, out pt_temp, out K_temp)) {
						pt_list.Add(pt_temp);
						K_list.Add(K_temp.Y);
					}
				}
			}

			// check the start/end pair
			if (pt_side[0] != pt_side[pt_side.Length-1]) {
				LineSegment ls = new LineSegment(points[0], points[points.Count-1]);
				Coordinates pt_temp, K_temp;
				if (ls.Intersect(l, out pt_temp, out K_temp)) {
					pt_list.Add(pt_temp);
					K_list.Add(K_temp.Y);
				}
			}

			pts = pt_list.ToArray();
			K = K_list.ToArray();

			return pts.Length > 0;
		}

		public bool Intersect(LineSegment ls, out Coordinates[] pts) {
			double[] K;
			return Intersect(ls, out pts, out K);
		}

		public bool Intersect(LineSegment l, out Coordinates[] pts, out double[] K) {
			// go through and label each point on the "left" or "right" of the line
			bool[] pt_side = new bool[points.Count];
			Coordinates s = l.P1 - l.P0;

			for (int i = 0; i < points.Count; i++) {
				Coordinates t = points[i] - l.P0;
				pt_side[i] = s.Cross(t) < 0;
			}

			// figure out any pairs spanning the line
			List<Coordinates> pt_list = new List<Coordinates>();
			List<double> K_list = new List<double>();
			for (int i = 0, j = points.Count-1; i < points.Count; j = i++) {
				if (pt_side[i] != pt_side[j]) {
					// this is a cross, check for intersection
					LineSegment ls = new LineSegment(points[i], points[j]);
					Coordinates pt_temp, K_temp;
					if (ls.Intersect(l, out pt_temp, out K_temp)) {
						pt_list.Add(pt_temp);
						K_list.Add(K_temp.Y);
					}
				}
			}

			pts = pt_list.ToArray();
			K = K_list.ToArray();

			return pts.Length > 0;
		}

		public bool Intersect(Circle c, out Coordinates[] pts) {
			// iterate through the points and find all intersections
			Coordinates[] pt_temp;
			List<Coordinates> pt_list = new List<Coordinates>();
			for (int i = 0; i < points.Count - 1; i++) {
				LineSegment ls = new LineSegment(points[i], points[i+1]);
				if (ls.Intersect(c, out pt_temp)) {
					pt_list.AddRange(pt_temp);
				}
			}

			LineSegment ls1 = new LineSegment(points[0], points[points.Count-1]);
			if (ls1.Intersect(c, out pt_temp)) {
				pt_list.AddRange(pt_temp);
			}

			pts = pt_list.ToArray();
			return pts.Length > 0;
		}

		public bool Intersect(CircleSegment c, out Coordinates[] pts) {
			// iterate through the points and find all intersections
			Coordinates[] pt_temp;
			List<Coordinates> pt_list = new List<Coordinates>();
			foreach (LineSegment ls in GetSegmentEnumerator()) {
				if (c.Intersect(ls, out pt_temp)) {
					pt_list.AddRange(pt_temp);
				}
			}

			pts = pt_list.ToArray();
			return pts.Length > 0;
		}

		public bool DoesIntersect(LinePath path) {
			foreach (LineSegment ls in path.GetSegmentEnumerator()) {
				if (DoesIntersect(ls)) return true;
			}

			return false;
		}

		public Polygon Transform(IPointTransformer transformer) {
			return new Polygon(transformer.TransformPoints(this), coordMode);
		}

		public Circle CalculateBoundingCircle() {
			// calculate the average point location
			Coordinates center = GetCentroid();

			// calculate the minimum radius required
			double rad = 0;
			for (int i = 0; i < points.Count; i++) {
				double dist = center.DistanceTo(points[i]);

				if (rad < dist) {
					rad = dist;
				}
			}

			return new Circle(rad, center);
		}

		public Rect CalculateBoundingRectangle() {
			double minX = double.MaxValue, minY = double.MaxValue;
			double maxX = double.MinValue, maxY = double.MinValue;

			for (int i = 0; i < points.Count; i++) {
				Coordinates pt = points[i];
				if (pt.X < minX) minX = pt.X;
				if (pt.X > maxX) maxX = pt.X;

				if (pt.Y < minY) minY = pt.Y;
				if (pt.Y > maxY) maxY = pt.Y;
			}

			if (minX == double.MinValue) {
				return new Rect(0, 0, 0, 0);
			}
			else {
				return new Rect(minX, minY, maxX-minX, maxY-minY);
			}
		}

		/// <summary>
		/// Finds the point at the extreme along search vector u
		/// </summary>
		/// <param name="u">Unit vector to search along</param>
		/// <returns>Extreme point on polygon</returns>
		public Coordinates ExtremePoint(Coordinates u) {
			double maxDist = double.MinValue;
			Coordinates maxPt = Coordinates.NaN;
			for (int i = 0; i < points.Count; i++) {
				Coordinates pt = points[i];
				double dist = u.Dot(pt);

				if (dist > maxDist) {
					maxDist = dist;
					maxPt = pt;
				}
			}

			return maxPt;
		}

		public Polygon Reverse() {
			Polygon rev = new Polygon(this.Count);

			for (int i = this.Count-1; i >= 0; i--) {
				rev.Add(this[i]);
			}

			return rev;
		}

		#region IList<Coordinates> Members

		public int IndexOf(Coordinates item) {
			return points.IndexOf(item);
		}

		public void Insert(int index, Coordinates item) {
			points.Insert(index, item);
			boundingCircle = null;
		}

		public void RemoveAt(int index) {
			points.RemoveAt(index);
			boundingCircle = null;
		}

		public Coordinates this[int index] {
			get { return points[index]; }
			set {	
				points[index] = value;
				boundingCircle = null;
			}
		}

		public Coordinates[] ToArray() {
			return points.ToArray();
		}

		#endregion

		#region ICollection<Coordinates> Members

		public void Add(Coordinates item) {
			points.Add(item);
			boundingCircle = null;
		}

		public void AddRange(IEnumerable<Coordinates> range) {
			points.AddRange(range);
			boundingCircle = null;
		}

		public void Clear() {
			points.Clear();
			boundingCircle = null;
		}

		public bool Contains(Coordinates item) {
			return points.Contains(item);
		}

    public bool IsInside(LineSegment ls)
    {
      return (IsInside(ls.P0) && IsInside(ls.P1) && !DoesIntersect(ls));
    }

		public void CopyTo(Coordinates[] array) {
			points.CopyTo(array, 0);
		}

		public void CopyTo(Coordinates[] array, int arrayIndex) {
			points.CopyTo(array, arrayIndex);
		}

		public int Count {
			get { return points.Count; }
		}

		bool ICollection<Coordinates>.IsReadOnly {
			get { return false; }
		}

		public bool Remove(Coordinates item) {
			boundingCircle = null;
			return points.Remove(item);
		}

		#endregion

		#region IEnumerable<Coordinates> Members

		public IEnumerator<Coordinates> GetEnumerator() {
			return points.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		public IEnumerable<LineSegment> GetSegmentEnumerator() {
			for (int i = 0; i < points.Count-1; i++) {
				yield return new LineSegment(points[i], points[i+1]);
			}

			yield return new LineSegment(points[points.Count-1], points[0]);
		}

		public static bool TestConvexIntersection(Polygon p1, Polygon p2) {
			// check the bounding circle first
			Circle bc_p1 = p1.BoundingCircle;
			Circle bc_p2 = p2.BoundingCircle;

			// check the bounding circle first -- if the distance between the center of the bounding circles is greater than the sum of radii, then they can't intersect
			if (bc_p1.center.DistanceTo(bc_p2.center) > bc_p1.r + bc_p2.r) {
				return false;
			}

			bool anyInside = false;
			foreach (Coordinates pt in p1) {
				if (p2.IsInside(pt)) {
					anyInside = true;
					break;
				}
			}

			if (anyInside)
				return true;

			foreach (Coordinates pt in p2) {
				if (p1.IsInside(pt)) {
					anyInside = true;
					break;
				}
			}

			return anyInside;

			// if there is overlap on all segments, then there is an intersection
			// if we can find a segment where the projections do not overlap, then the polygons do not intersect
			
			/*foreach (LineSegment seg in p1.GetSegmentEnumerator()) {
				if (!TestAxisOverlap(seg, p1, p2)) {
					return false;
				}
			}

			return true;*/
		}

		// return true if the projected intervals overlapped, returns false otherwise
		private static bool TestAxisOverlap(LineSegment axis, Polygon p1, Polygon p2) {
			// project each point onto the axis
			Coordinates u = axis.UnitVector.Rotate90();
			double min_p1 = double.MaxValue, min_p2 = double.MaxValue;
			double max_p1 = double.MinValue, max_p2 = double.MinValue;

			for (int i = 0; i < p1.points.Count; i++) {
				double v = u.Dot(p1.points[i]);
				if (v < min_p1) min_p1 = v;
				if (v > max_p1) max_p1 = v;
			}

			for (int i = 0; i < p2.points.Count; i++) {
				double v = u.Dot(p1.points[i]);
				if (v < min_p2) min_p2 = v;
				if (v > max_p2) max_p2 = v;
			}

			if (min_p2 >= min_p1 && min_p2 <= max_p1) {
				// overlap guaranteed
				return true;
			}
			else if (max_p2 >= min_p1 && max_p2 <= max_p1) {
				return true;
			}
			else if (min_p2 < min_p1 && max_p2 > max_p1) {
				// p2 interval completely uncompases p1 interval
				return true;
			}

			return false;
		}

		private struct GrahamScanPoint {
			public Coordinates point;
			public double angle;

			public GrahamScanPoint(Coordinates pt, Coordinates basePoint) {
				this.point = pt;
				this.angle = (pt-basePoint).ArcTan;
			}
		}

		private class GrahamScanComparer : IComparer<GrahamScanPoint> {
			private Coordinates P;

			public GrahamScanComparer(Coordinates P) {
				this.P = P;
			}

			#region IComparer<GrahamScanPoint> Members

			public int Compare(GrahamScanPoint x, GrahamScanPoint y) {
				if (x.angle == y.angle) {
					double xDist = (x.point - P).Length;
					double yDist = (y.point - P).Length;

					if (xDist == yDist) {
						return 0;
					}
					else if (xDist < yDist) {
						return -1;
					}
					else {
						return 1;
					}
				}
				else if (x.angle < y.angle) {
					return -1;
				}
				else {
					return 1;
				}
			}

			#endregion
		}

		public static Polygon GrahamScan(IList<Coordinates> points) {
			if (points == null)
			{
				return null;
			}

			if (points.Count <= 1)
			{
				throw new ArgumentOutOfRangeException("Cannot find convex hull of a single point");
			}
			else if (points.Count == 2)
			{
				return new Polygon(points);
			}

			List<GrahamScanPoint> gsPoints = new List<GrahamScanPoint>(points.Count);

			// find the minimum point
			Coordinates minPoint = new Coordinates(double.MaxValue, double.MaxValue);
			int minPointInd = -1;
			for (int i = 0; i < points.Count; i++) {
				Coordinates pt = points[i];

				if (pt.Y < minPoint.Y || (pt.Y == minPoint.Y && pt.X < minPoint.X)) {
					minPoint = pt;
					minPointInd = i;
				}
			}

			for (int i = 0; i < points.Count; i++) {
				if (i != minPointInd) {
					gsPoints.Add(new GrahamScanPoint(points[i], minPoint));
				}
			}

			gsPoints.Sort(new GrahamScanComparer(minPoint));

			Coordinates[] stack = new Coordinates[points.Count];
			int stackInd = 0;
			stack[stackInd++] = minPoint;
			stack[stackInd++] = gsPoints[0].point;
			for (int i = 1; i < gsPoints.Count; i++) {
				Coordinates p2 = gsPoints[i].point;
				while (stackInd >= 2) {
					Coordinates p0 = stack[stackInd-2];
					Coordinates p1 = stack[stackInd-1];
					
					if ((p1-p0).Cross(p2-p0) <= 0) {
						stackInd--;
					}
					else {
						break;
					}
				}
				stack[stackInd++] = p2; 
			}

			Polygon poly = new Polygon(stackInd);
			for (int i = 0; i < stackInd; i++) {
				poly.Add(stack[i]);
			}

			return poly;
		}

		public static Polygon ConvexMinkowskiConvolution(Polygon r, Polygon p) {
			Coordinates pLowerLeftPoint = new Coordinates(double.MaxValue, double.MaxValue);
			int pLowerLeftInd = -1;

			for (int i = 0; i < p.Count; i++) {
				Coordinates pt = p[i];
				if (pt.Y < pLowerLeftPoint.Y || (pt.Y == pLowerLeftPoint.Y && pt.X < pLowerLeftPoint.X)) {
					pLowerLeftPoint = pt;
					pLowerLeftInd = i;
				}
			}

			Coordinates rLowerLeftPoint = new Coordinates(double.MaxValue, double.MaxValue);
			int rLowerLeftInd = -1;

			for (int i = 0; i < r.Count; i++) {
				Coordinates pt = r[i];
				if (pt.Y < rLowerLeftPoint.Y || (pt.Y == rLowerLeftPoint.Y && pt.X < rLowerLeftPoint.X)) {
					rLowerLeftPoint = pt;
					rLowerLeftInd = i;
				}
			}

			int n = p.Count;
			int m = r.Count;
			int vi = pLowerLeftInd;
			int vj = rLowerLeftInd;

			int orig_count = p.Count + r.Count;
			Polygon conv = new Polygon(p.Count+r.Count);

			bool inc_i = false;
			bool inc_j = false;

			do {
				conv.Add(p[vi] + r[vj]);

				if (conv.Count > orig_count) {
					throw new ApplicationException("Error performing minkowski convolution");
				}

				int vi1 = (vi+1)%n;
				int vj1 = (vj+1)%m;
				double theta_i = (p[vi1]-p[vi]).ArcTan;
				if (theta_i < 0) theta_i += 2*Math.PI;
				double theta_j = (r[vj1]-r[vj]).ArcTan;
				if (theta_j < 0) theta_j += 2*Math.PI;
				if (theta_i <= theta_j) {
					inc_i = true;
					vi = vi1;
				}
				if (theta_i >= theta_j) {
					inc_j = true;
					vj = vj1;
				}
			} while ((vi != pLowerLeftInd && vj != rLowerLeftInd) || !inc_i || !inc_j);

			// make sure we add the remainder of the points
			while (vi != pLowerLeftInd) {
				conv.Add(p[vi] + r[vj]);
				vi = (vi+1)%n;
			}

			while (vj != rLowerLeftInd) {
				conv.Add(p[vi] + r[vj]);
				vj = (vj+1)%m;
			}

			return conv;
		}

		/// <summary>
		/// Checks if two segments intersect
		/// </summary>
		/// <param name="seg"></param>
		/// <param name="otherEdge"></param>
		/// <param name="inclusive">flag if should include start and end points</param>
		/// <returns></returns>
		private bool LineSegmentInstersectsLineSegment(LinePath seg, LinePath otherEdge, bool inclusive, out Coordinates? intersection)
		{
			intersection = null;

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

			if ((x1 == x2 && y1 == y2) ||
				(x2 == x3 && y2 == y3) ||
				(x3 == x4 && y3 == y4) ||
				(x4 == x1 && y4 == y1))
			{
				return false;
			}

			if (inclusive)
			{
				if (0.0 <= ua && ua <= 1.0 && 0.0 <= ub && ub <= 1.0)
				{
					double x = x1 + ua * (x2 - x1);
					double y = y1 + ua * (y2 - y1);
					intersection = new Coordinates(x, y);
					return true;
				}
			}
			else
			{
				if (0.0 < ua && ua < 1.0 && 0.0 < ub && ub < 1.0)
				{
					double x = x1 + ua * (x2 - x1);
					double y = y1 + ua * (y2 - y1);
					intersection = new Coordinates(x, y);
					return true;
				}
			}

			return false;
		}

		public bool IsComplex
		{
			get
			{
				if (this.Count < 2)
					return false;

				for (int i = 0; i < this.Count; i++)
				{
					// get current segment
					LinePath currentSeg =  i < this.Count - 1 ? 
						new LinePath(new Coordinates[]{this[i], this[i+1]}) : 
						new LinePath(new Coordinates[]{this[i], this[0]});
					
					// check other segs
					for (int j = 0; j < this.Count; j++)
					{
						// check waypoints equal
						if (j != i && this[i] == this[j])
							return true;

						// check not same
						if (j != i)
						{
							// get current segment
							LinePath testSeg = j < this.Count - 1 ?
								new LinePath(new Coordinates[] { this[j], this[j + 1] }) :
								new LinePath(new Coordinates[] { this[j], this[0] });

							// check intersection
							Coordinates? intersection;
							if (this.LineSegmentInstersectsLineSegment(currentSeg, testSeg, false, out intersection))
								return true;
						}
					}
				}

				// no outer edges intersect, return true
				return false;
			}
		}
	}
}
