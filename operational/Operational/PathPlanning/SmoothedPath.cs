using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracking.Steering;
using UrbanChallenge.Common;
using OperationalLayer.Tracking;
using UrbanChallenge.Common.Pose;
using OperationalLayer.Tracking.SpeedControl;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.PathPlanning {
	class SmoothedPath : LinePath, IRelativePath, ISpeedGenerator {
		private CarTimestamp startingTimestamp;
		private CarTimestamp curTimestamp;

		private double[] speeds;

		public SmoothedPath(CarTimestamp timestamp, IList<Coordinates> points, IList<double> speeds)
			: base(points) {

			this.curTimestamp = this.startingTimestamp = timestamp;

			if (speeds != null) {
				this.speeds = new double[speeds.Count];
				for (int i = 0; i < speeds.Count; i++) {
					this.speeds[i] = speeds[i];
				}
			}
		}

		public bool HasSpeeds {
			get { return speeds != null; }
		}

		private double CalculateAlongTrackDist(PointOnPath pt, Coordinates loc) {
			return pt.AlongtrackDistance(loc);
		}

		#region IRelativePath Members

		public SteeringControlData GetSteeringControlData(SteeringControlDataOptions opts) {
			double alongTrackDist = CalculateAlongTrackDist(ZeroPoint, Coordinates.Zero);
			double lookaheadDist = opts.PathLookahead+alongTrackDist;

			PointOnPath pathPoint = AdvancePoint(ZeroPoint, lookaheadDist);

			// send the path point
			Services.UIService.PushPoint(pathPoint.Location, curTimestamp, "path point", true);

			return new SteeringControlData(GetCurvature(pathPoint), 0, 0);
		}

		public void TransformPath(CarTimestamp timestamp) {
			// if the current and new timestamp match, ignore the request
			if (timestamp != curTimestamp) {
				RelativeTransform relTransform = Services.RelativePose.GetTransform(curTimestamp, timestamp);
				// transform the path in place
				TransformInPlace(relTransform);

				// update the current timestamp
				curTimestamp = timestamp;
			}
		}

		public CarTimestamp CurrentTimestamp {
			get { return curTimestamp; }
		}

		public CarTimestamp StartingTimestamp {
			get { return startingTimestamp; }
		}

		public double DistanceTo(LinePath.PointOnPath endPoint) {
			return DistanceBetween(ZeroPoint, endPoint);
		}

		public bool IsPastEnd {
			get { return ZeroPoint == EndPoint; }
		}

		public double GetCurvature(PointOnPath pt) {
			if (pt.Index >= Count-1 || !pt.Valid) {
				throw new ArgumentOutOfRangeException();
			}
			else if (pt.Index == Count-2) {
				return GetCurvature(pt.Index);
			}
			else {
				return (1-pt.DistFraction)*GetCurvature(pt.Index)+pt.DistFraction*GetCurvature(pt.Index+1);
			}
		}

		public double GetCurvature(int index) {
			if (index == 0) {
				return GetCurvature(1);
			}
			else if (index == Count-1) {
				// return 0 steering for final point
				return 0;
			}
			else {
				// calculate the cuvature using the points centered at index
				Coordinates d01 = this[index-1]-this[index];
				Coordinates d21 = this[index+1]-this[index];
				Coordinates d20 = this[index+1]-this[index-1];

				return -(2*d01.Cross(d21))/(d01.Length*d21.Length*d20.Length);
			}
		}

		#endregion

		#region ISpeedGenerator Members

		public SpeedControlData GetCommandedSpeed() {
			double alongTrackDist = CalculateAlongTrackDist(ZeroPoint, Coordinates.Zero);
			double lookaheadDist = Math.Max(Services.StateProvider.GetVehicleState().speed*(TahoeParams.actuation_delay + 0.75),1) + alongTrackDist;

			PointOnPath pathPoint = AdvancePoint(ZeroPoint, lookaheadDist);
			return new SpeedControlData(GetSpeed(pathPoint), null);
		}

		public void SetSpeed(int i, double speed) {
			speeds[i] = speed;
		}

		public double GetSpeed(int i) {
			return speeds[i];
		}

		public double GetSpeed(PointOnPath pt) {
			return (1-pt.DistFraction)*speeds[pt.Index]+pt.DistFraction*speeds[pt.Index+1];
		}

		public double GetAcceleration(PointOnPath pt) {
			return (speeds[pt.Index+1]*speeds[pt.Index+1]-speeds[pt.Index]*speeds[pt.Index])/(2*this[pt.Index].DistanceTo(this[pt.Index+1]));
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get {
				if (IsPastEnd) {
					return CompletionResult.Completed;
				}
				else {
					return CompletionResult.Working;
				}
			}
		}

		public object FailureData {
			get {
				return null;
			}
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			// transform the path
			TransformPath(timestamp);

			// reset to normal speed controller config
			if (SpeedController.config != SpeedControllerConfig.Normal) {
				SpeedController.config = SpeedControllerConfig.Normal;
			}
		}

		#endregion
	}

	/*class SmoothedPath2 : IRelativePath, ISpeedGenerator, IList<PathPoint> {
		private class PointData {
			public enum DistFlag {
				Beginning,
				Middle,
				End
			}

			public int index_lower, index_upper;
			public double dist;
			public Coordinates pt;
			public DistFlag df;

			public PointData(int index_lower, int index_upper, double dist, Coordinates pt, DistFlag df) {
				this.index_lower = index_lower;
				this.index_upper = index_upper;
				this.dist = dist;
				this.pt = pt;
				this.df = df;
			}
		}

		private Coordinates[] origPoints, points;
		private double[] speeds;
		private CarTimestamp startTimestamp;
		private CarTimestamp curTimestamp;
		private PointData curClosestPoint;

		public SmoothedPath2(CarTimestamp startTimestamp) {
			this.points = new Coordinates[0];
			this.speeds = new double[0];
			this.startTimestamp = this.curTimestamp = startTimestamp;
		}

		public SmoothedPath2(ICollection<PathPoint> points, CarTimestamp startTimestamp) {
			this.points = new Coordinates[points.Count];

			this.speeds = new double[points.Count];

			int i = 0;
			foreach (PathPoint p in points) {
				this.points[i] = new Coordinates(p.X, p.Y);
				this.speeds[i] = p.speed;

				i++;
			}

			this.origPoints = new Coordinates[this.points.Length];
			Array.Copy(this.points, this.origPoints, this.points.Length);

			this.startTimestamp = this.curTimestamp = startTimestamp;
		}

		public SmoothedPath2(ICollection<Coordinates> points, CarTimestamp startTimestamp) {
			this.points = new Coordinates[points.Count];
			int ind = 0;
			foreach (Coordinates c in points) {
				this.points[ind++] = c;
			}

			this.origPoints = new Coordinates[this.points.Length];
			Array.Copy(this.points, this.origPoints, this.points.Length);

			this.speeds = null;
			this.startTimestamp = this.curTimestamp = startTimestamp;
		}

		public SmoothedPath2(Coordinates[] points, CarTimestamp startTimestamp) {
			this.points = points;
			this.origPoints = new Coordinates[this.points.Length];
			Array.Copy(this.points, this.origPoints, this.points.Length);

			this.startTimestamp = this.curTimestamp = startTimestamp;
		}

		public SmoothedPath2(Coordinates[] points, double[] speeds, CarTimestamp startTimestamp) {
			this.points = points;
			this.origPoints = new Coordinates[this.points.Length];
			Array.Copy(this.points, this.origPoints, this.points.Length);

			this.speeds = speeds;
			this.startTimestamp = this.curTimestamp = startTimestamp;
		}

		private double CalculateCurvature(int index) {
			// TODO: figure out what we want to put for the curvature at index 0
			if (index < 1)
				return 0;
			else if (index >= points.Length-1)
				return 0;

			// calculate the cuvature using the points centered at index
			Coordinates d01 = points[index-1]-points[index];
			Coordinates d21 = points[index+1]-points[index];
			Coordinates d20 = points[index+1]-points[index-1];

			return -(2*d01.Cross(d21))/(d01.Length*d21.Length*d20.Length);
		}

		private PointData GetPointData(int index, double dist) {
			Coordinates s = points[index];
			Coordinates v = points[index+1]-s;
			double segDist = v.Length;
			v /= segDist;

			// determine the segment distance flag
			PointData.DistFlag df;
			if (dist <= 0) {
				df = PointData.DistFlag.Beginning;
				dist = 0;
			}
			else if (dist >= segDist) {
				df = PointData.DistFlag.End;
				dist = segDist;
			}
			else {
				df = PointData.DistFlag.Middle;
			}

			// calculate the point
			Coordinates pt = v * dist;

			return new PointData(index, index+1, dist, pt, df);
		}

		private PointOnPath GetPointOnPath(PointData pd) {
			return new PointOnPath(pd.pt, this, curTimestamp, pd);
		}

		private PointOnPath GetPointOnPath(int index, double dist) {
			return GetPointOnPath(GetPointData(index, dist));
		}

		private PointData GetClosestPoint() {
			if (curClosestPoint == null)
				curClosestPoint = GetClosestPoint(new Coordinates(0, 0));

			return curClosestPoint;
		}

		private PointData GetClosestPoint(Coordinates loc) {
			double minDist = double.MaxValue;
			PointData pointMin = null;

			// iterate throug each line segment and find the closest point
			for (int i = 0; i < points.Length-1; i++) {
				Coordinates s = points[i];
				Coordinates v = points[i+1] - s;

				double len = v.Length;
				v /= len;

				double t = v * (loc - s);
				Coordinates pt;
				PointData.DistFlag df;
				if (t < 0) {
					pt = s;
					df = PointData.DistFlag.Beginning;
				}
				else if (t > len) {
					pt = points[i+1];
					df = PointData.DistFlag.End;
				}
				else {
					pt = s + t*v;
					df = PointData.DistFlag.Middle;
				}

				// figure out the distance
				double dist = (pt-loc).Length;
				if (dist < minDist) {
					minDist = dist;
					pointMin = new PointData(i, i+1, t, pt, df);
				}
			}

			return pointMin;
		}

		public SmoothedPath SubPath(PointOnPath start, PointOnPath end) {
			PointData pdStart = (PointData)start.data;
			PointData pdEnd = (PointData)end.data;

			if (pdStart.index_lower < pdEnd.index_lower) {
				// this is the normal case
				List<PathPoint> points = new List<PathPoint>();
				points.Add(GetPathPoint(pdStart.index_lower, pdStart.dist));

				int ind = pdStart.index_lower+1;
				while (ind <= pdEnd.index_lower) {
					points.Add(this[ind++]);
				}

				points.Add(GetPathPoint(pdEnd.index_lower, pdEnd.dist));

				return new SmoothedPath(points, startTimestamp);
			}
			else if (pdStart.index_lower == pdEnd.index_lower && pdStart.dist < pdEnd.dist) {
				PathPoint[] points = new PathPoint[2];
				points[0] = GetPathPoint(pdStart.index_lower, pdStart.dist);
				points[1] = GetPathPoint(pdEnd.index_lower, pdEnd.dist);

				return new SmoothedPath(points, startTimestamp);
			}
			else {
				throw new ArgumentOutOfRangeException("End point is behind the start point. This operation is not supported at the current time");
			}
		}

		public bool HasSpeeds {
			get { return speeds != null; }
		}

		public LineList ToLineList() {
			LineList l = new LineList();
			l.AddRange(points);
			return l;
		}

		private PathPoint GetPathPoint(int index, double dist) {
			Coordinates s = points[index];
			Coordinates v = points[index+1]-s;
			double len = v.Length;
			double frac = dist/len;

			Coordinates pt = s + v*frac;
			double speed = -1;
			if (speeds != null) {
				speed = speeds[index]*(1-frac) + speeds[index+1]*frac;
			}

			return new PathPoint(pt.X, pt.Y, speed);
		}

		public SmoothedPath Clone() {
			Coordinates[] pointsCopy = new Coordinates[points.Length];
			Array.Copy(points, pointsCopy, points.Length);

			double[] speedCopy = null;
			if (speeds != null) {
				speedCopy = new double[speeds.Length];
				Array.Copy(speeds, speedCopy, speeds.Length);
			}

			return new SmoothedPath(pointsCopy, speedCopy, curTimestamp);
		}

		#region IRelativePath Members

		public SteeringControlData GetSteeringControlData(SteeringControlDataOptions opts) {
			PointData pd = GetClosestPoint();
			pd = AdvancePoint(pd, opts.PathLookahead);

			Coordinates p0 = points[pd.index_lower];
			Coordinates p1 = points[pd.index_upper];

			double segmentDist = (p1-p0).Length;

			double curvature = 0;
			double headingError = 0;
			if (pd.index_lower == 0) {
				curvature = CalculateCurvature(1);
			}
			else {
				double f = pd.dist/segmentDist;

				curvature = (1-f)*CalculateCurvature(pd.index_lower) + f*CalculateCurvature(pd.index_upper);
			}

			if (pd.index_lower == 0) {
				headingError = Math.Atan2(p1.Y-p0.Y, p1.X-p0.X);
			}
			else {
				double f = pd.dist/segmentDist;

				// TODO: consider if we want to do a weighted average for heading error as well
				headingError = Math.Atan2(p1.Y-p0.Y, p1.X-p0.X);
			}

			// vehicle vector with respect to segment closest point
			Coordinates carVec  = -pd.pt;
			// segment tangent vector
			Coordinates pathVec = p1-p0;

			// compute offtrack error
			double offtrackError = Math.Sign(carVec.Cross(pathVec)) * carVec.Length;

			// send the path point
			Services.UIService.PushPoint(pd.pt, curTimestamp, "path point", true);

			return new SteeringControlData(curvature, offtrackError, headingError);
		}

		public void Transform(IPointTransformer transform) {
			points = transform.TransformPoints(origPoints);

			curClosestPoint = null;
		}

		public void Transform(RelativeTransform transform) {
			points = transform.TransformPoints(origPoints);

			// update the current timestamp
			curTimestamp = transform.EndTimestamp;

			// invalidate the current closest point
			curClosestPoint = null;
		}

		public void TransformPath(CarTimestamp timestamp) {
			// check if we need to transform
			if (timestamp != curTimestamp) {
				// get the relative transform
				RelativeTransform rt = Services.RelativePose.GetTransform(startTimestamp, timestamp);
				points = rt.TransformPoints(origPoints);

				// update the current timestamp
				curTimestamp = timestamp;

				// invalidate the current closest point
				curClosestPoint = null;
			}

			// set out the stuff
			LineList list = new LineList(points);
			Services.UIService.PushLineList(list, curTimestamp, "tracking path", true);
		}

		public CarTimestamp CurrentTimestamp {
			get { return curTimestamp; }
		}

		public CarTimestamp StartingTimestamp {
			get { return startTimestamp; }
			set { startTimestamp = value; }
		}

		public PointOnPath ZeroPoint {
			get {
				PointData pd = GetClosestPoint();
				return new PointOnPath(pd.pt, this, curTimestamp, pd);
			}
		}

		private PointOnPath EndPoint {
			get {
				return GetPointOnPath(points.Length-2, (points[points.Length-1]-points[points.Length-2]).Length);
			}
		}

		private PointOnPath StartPoint {
			get {
				return GetPointOnPath(0, 0);
			}
		}

		private PointData AdvancePoint(PointData startPoint, double dist) {
			PointOnPath pt = AdvancePoint(GetPointOnPath(startPoint), dist);
			return (PointData)pt.data;
		}

		public PointOnPath AdvancePoint(PointOnPath startPoint, double dist) {
			return AdvancePoint(startPoint, ref dist);
		}

		public PointOnPath AdvancePoint(PointOnPath startPoint, ref double dist) {
			PointData pdStart = (PointData)startPoint.data;

			if (dist > 0) {
				// check if we can satisfy the distance on the current segment
				double segDist = (points[pdStart.index_upper]-points[pdStart.index_lower]).Length;
				if (pdStart.dist+dist <= segDist) {
					// we can satisfy staying on this segment
					PointOnPath ret = GetPointOnPath(pdStart.index_lower, pdStart.dist + dist);
					dist = 0;
					return ret;
				}
				else {
					// can't satisfy so, start looping through
					// subtract off the remaining distance on this segment
					dist -= (segDist-pdStart.dist);

					// loop that stuff
					for (int i = pdStart.index_lower+1; i < points.Length-1; i++) {
						segDist = (points[i+1]-points[i]).Length;

						// check if we can satisfy the remaining distance
						if (segDist >= dist) {
							PointOnPath ret = GetPointOnPath(i, dist);
							dist = 0;
							return ret;
						}
						else {
							dist -= segDist;
						}
					}
					// couldn't satisfy the distance, return the last point
					return EndPoint;
				}
			}
			else {
				// check if we can satisfy the distance on the current segment
				if (pdStart.dist+dist >= 0) {
					// we can satisfy staying on this segment
					PointOnPath ret = GetPointOnPath(pdStart.index_lower, pdStart.dist+dist);
					dist = 0;
					return ret;
				}
				else {
					// can't satisfy so, start looping through
					// subtract off the remaining distance on this segment
					dist += pdStart.dist;

					// loop that stuff
					for (int i = pdStart.index_lower+1; i < points.Length-1; i++) {
						double segDist = (points[i+1]-points[i]).Length;

						// check if we can satisfy the remaining distance
						if (segDist+dist >= 0) {
							PointOnPath ret = GetPointOnPath(i, segDist+dist);
							dist = 0;
							return ret;
						}
						else {
							dist += segDist;
						}
					}

					// couldn't satisfy the distance, return the last point
					return StartPoint;
				}
			}
		}

		public double DistanceBetween(PointOnPath startPoint, PointOnPath endPoint) {
			// calculate the distance to the start point and end point
			PointData pdStart = (PointData)startPoint.data;
			PointData pdEnd = (PointData)endPoint.data;

			double startDist = pdStart.dist;
			for (int i = 0; i < pdStart.index_lower; i++) {
				startDist += (points[i+1]-points[i]).Length;
			}

			double endDist = pdEnd.dist;
			for (int i = 0; i < pdEnd.index_lower; i++) {
				endDist += (points[i+1]-points[i]).Length;
			}

			return endDist - startDist;
		}

		public double DistanceTo(PointOnPath endPoint) {
			return DistanceBetween(ZeroPoint, endPoint);
		}

		public bool IsPastEnd {
			get {
				PointData pd = GetClosestPoint();
				return (pd.index_upper == points.Length-1) && (pd.df == PointData.DistFlag.End);
			}
		}

		#endregion

		#region ISpeedGenerator Members

		public SpeedControlData GetCommandedSpeed() {
			if (speeds == null)
				throw new InvalidOperationException("speeds are not specified for this path");

			// get the closest point
			PointData pd = GetClosestPoint();

			// calculate the total distance between points
			double segDist = (points[pd.index_upper]-points[pd.index_lower]).Length;
			// calculate the weighting factor
			double f = pd.dist/segDist;

			// determine the speed
			double curSpeed = (1-f)*speeds[pd.index_lower] + f*speeds[pd.index_upper];

			if (curSpeed <= 0) {
				if (speeds[pd.index_upper] > 0) {
					curSpeed = Math.Min(0.1, speeds[pd.index_upper]);
				}
			}

			// determine the acceleration
			double accel = (Math.Pow(speeds[pd.index_upper], 2)-Math.Pow(speeds[pd.index_lower], 2))/(2*segDist);

			if (Math.Abs(accel) < 0.75) {
				// de-rate the acceleration
				accel *= Math.Pow(accel/0.75, 4);
			}

			return new SpeedControlData(curSpeed, accel);
		}

		#endregion

		#region ITrackingCommandBase Members

		public CompletionResult CompletionStatus {
			get {
				if (IsPastEnd) {
					return CompletionResult.Completed;
				}
				else {
					return CompletionResult.Working;
				}
			}
		}

		public object FailureData {
			get {
				return null;
			}
		}

		public void BeginTrackingCycle(CarTimestamp timestamp) {
			// transform the path
			TransformPath(timestamp);

			// reset to normal speed controller config
			if (SpeedController.config != SpeedControllerConfig.Normal) {
				SpeedController.config = SpeedControllerConfig.Normal;
			}
		}

		#endregion

		#region IList<PathPoint> Members

		int IList<PathPoint>.IndexOf(PathPoint item) {
			throw new NotImplementedException();
		}

		public void Insert(int index, PathPoint item) {
			// allocate new space for the points and speeds
			Coordinates[] newPoints = new Coordinates[points.Length+1];
			for (int i = index; i < points.Length; i++) {
				newPoints[i+1] = points[i];
			}

			newPoints[index] = item.Point;
			points = newPoints;

			if (speeds != null) {
				double[] newSpeeds = new double[speeds.Length+1];
				for (int i = index; i < speeds.Length; i++) {
					newSpeeds[i+1] = speeds[i];
				}

				newSpeeds[index] = item.speed;
				speeds = newSpeeds;
			}
		}

		public void RemoveAt(int index) {
			throw new NotImplementedException();
		}

		public PathPoint this[int index] {
			get {
				if (speeds != null) {
					return new PathPoint(points[index].X, points[index].Y, speeds[index]);
				}
				else {
					return new PathPoint(points[index].X, points[index].Y, -1);
				}
			}
			set {
				points[index].X = value.X;
				points[index].Y = value.Y;
				if (speeds != null) {
					speeds[index] = value.speed;
				}
			}
		}

		#endregion

		#region ICollection<PathPoint> Members

		public void Add(Coordinates point) {
			Add(new PathPoint(point, -1));
		}

		public void Add(PathPoint item) {
			int i = points.Length;
			Array.Resize(ref points, points.Length + 1);
			points[i].X = item.X;
			points[i].Y = item.Y;

			if (speeds != null) {
				Array.Resize(ref speeds, speeds.Length + 1);
				speeds[i] = item.speed;
			}
		}

		public void Clear() {
			points = new Coordinates[0];
			speeds = new double[0];
		}

		public bool Contains(PathPoint item) {
			throw new NotImplementedException();
		}

		public void CopyTo(PathPoint[] array, int arrayIndex) {
			throw new NotImplementedException();
		}

		public int Count {
			get { return points.Length; }
		}

		bool ICollection<PathPoint>.IsReadOnly {
			get { return false; }
		}

		bool ICollection<PathPoint>.Remove(PathPoint item) {
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable<PathPoint> Members

		public IEnumerator<PathPoint> GetEnumerator() {
			return InternalEnumerator().GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		private IEnumerable<PathPoint> InternalEnumerator() {
			for (int i = 0; i < points.Length; i++) {
				yield return this[i];
			}
		}

		public IEnumerable<Coordinates> GetPointEnumerator() {
			for (int i = 0; i < points.Length; i++) {
				yield return points[i];
			}
		}
	}*/
}
