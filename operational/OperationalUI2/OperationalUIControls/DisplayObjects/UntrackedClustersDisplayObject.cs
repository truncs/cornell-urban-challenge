using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class UntrackedClustersDisplayObject : IHittable, IAttachable<SceneEstimatorUntrackedClusterCollection>, IClearable, ISimpleColored {
		private const float box_size = 0.05f;
		private const float nominal_pixel_size = 1.0f;

		private PointF[][] clusterPoints;
		private RectangleF[] boundingRects;
		private Color color;
		private bool hittable;

		private string name;

		public UntrackedClustersDisplayObject(string name, bool hittable, Color color) {
			this.name = name;
			this.hittable = hittable;
			this.color = color;
		}

		public Color Color {
			get { return color; }
			set { color = value; }
		}

		public bool Hittable {
			get { return hittable; }
			set { hittable = value; }
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			if (!hittable || clusterPoints == null) {
				return HitTestResult.NoHit;
			}

			// test all points
			double minDist = tol;
			Coordinates closestPoint = Coordinates.NaN;

			// construct a transform off the current vehicle state
			Coordinates vehicleLoc = Services.VehicleStateService.Location;
			Matrix3 transform = Matrix3.Rotation(-Services.VehicleStateService.Heading)*Matrix3.Translation(-vehicleLoc.X, -vehicleLoc.Y);
			loc = transform.TransformPoint(loc);

			PointF[][] pts = clusterPoints;
			for (int i = 0; i < pts.Length; i++) {
				// check if the bounding rect for this cluster is empty (if so, there are no point)
				if (boundingRects[i].IsEmpty)
					continue;

				// inflate the bounding rect by tol 
				RectangleF infBoundingRect = boundingRects[i];
				infBoundingRect.Inflate(tol, tol);
				// check if the point we're looking for is in the bounds of this cluster
				if (!infBoundingRect.Contains(Utility.ToPointF(loc)))
					continue;

				for (int j = 0; j < pts[i].Length; j++) {
					Coordinates pt = Utility.ToCoord(pts[i][j]);
					double dist = pt.DistanceTo(loc);
					if (dist < minDist) {
						minDist = dist;
						closestPoint = pt;
					}
				}
			}

			if (!closestPoint.IsNaN) {
				transform = transform.Inverse();
				closestPoint = transform.TransformPoint(closestPoint);
				return new HitTestResult(this, true, closestPoint, null);
			}
			else {
				return HitTestResult.NoHit;
			}
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			PointF[][] clusters = clusterPoints;
			if (clusters == null)
				return;

			IPen pen = g.CreatePen();
			pen.Width = nominal_pixel_size/wt.Scale;
			pen.Color = color;
			
			// get the current vehicle position/heading
			float vehicleHeading = (float)Services.VehicleStateService.Heading;
			PointF vehiclePos = Utility.ToPointF(Services.VehicleStateService.Location);

			g.GoToVehicleCoordinates(vehiclePos, vehicleHeading + (float)Math.PI/2.0f);

			for (int i = 0; i < clusters.Length; i++) {
				PointF[] pts = clusters[i];
				for (int j = 0; j < pts.Length; j++) {
					g.DrawRectangle(pen, new RectangleF(pts[j].X - box_size, pts[j].Y - box_size, 2*box_size, 2*box_size));
				}
			}

			g.ComeBackFromVehicleCoordinates();
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<SceneEstimatorUntrackedClusterCollection> Members

		public void SetCurrentValue(SceneEstimatorUntrackedClusterCollection value, string label) {
			if (value == null || value.clusters == null) {
				clusterPoints = null;
				return;
			}

			PointF[][] newPoints = new PointF[value.clusters.Length][];
			boundingRects = new RectangleF[value.clusters.Length];

			for (int i = 0; i < value.clusters.Length; i++) {
				SceneEstimatorUntrackedCluster cluster = value.clusters[i];
				if (cluster.points == null || cluster.points.Length == 0) {
					newPoints[i] = new PointF[0];
					boundingRects[i] = RectangleF.Empty;
				}

				float minX = float.MaxValue, minY = float.MaxValue;
				float maxX = float.MinValue, maxY = float.MinValue;

				newPoints[i] = new PointF[cluster.points.Length];

				for (int j = 0; j < cluster.points.Length; j++){
					PointF pt = Utility.ToPointF(cluster.points[j]);
					newPoints[i][j] = pt;

					if (pt.X < minX) minX = pt.X;
					if (pt.X > maxX) maxX = pt.X;

					if (pt.Y < minY) minY = pt.Y;
					if (pt.Y > maxY) maxY = pt.Y;
				}

				// prevent the rectangle from being "Empty" because it has zero length/width
				if (maxX == minX) maxX = minX + box_size;
				if (maxY == minY) maxY = minY + box_size;
				boundingRects[i] = new RectangleF(minX, minY, maxX-minX, maxY-minY);
			}

			clusterPoints = newPoints;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			clusterPoints = null;
		}

		#endregion
	}
}
