using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.OperationalUI.Common.Map;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	class TrackedClustersDisplayObject : IHittable, IAttachable<SceneEstimatorTrackedClusterCollection>, IClearable, IProvideContextMenu {
		private class InternalTrackedCluster {
			public PointF[] points;
			public Polygon polygon;
			public RectangleF boundingBox;
			public PointF centerPoint;

			public SceneEstimatorTargetClass targetClass;
			public SceneEstimatorTargetStatusFlag targetStatus;
			public int id;
			public double speed;
			public bool isStopped;
			public double heading;
		}

		private const int deleted_alpha = 100;
		private const float point_box_size = 0.1f;
		private const float nominal_pixel_size = 1.0f;
		
		// colors for unknown, carlike, not-carlike
		// filters for those same categories
		// indicator for stopped, not stoppped
		// partially transparent or big x for deleted, normal for active
		// option to draw id, speed, class?, stopped?, status?
		// arrow for heading

		private Color unknownColor;
		private Color carlikeColor;
		private Color notcarColor;

		private bool drawID, drawSpeed;
		private bool drawPoints, drawPolygon;

		private string name;

		private List<InternalTrackedCluster> trackedClusters;

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			throw new Exception("The method or operation is not implemented.");
		}

		public IHittable Parent {
			get { throw new Exception("The method or operation is not implemented."); }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			List<InternalTrackedCluster> trackedClusters = this.trackedClusters;

			float vehicleHeading = (float)Services.VehicleStateService.Heading;
			PointF vehiclePos = Utility.ToPointF(Services.VehicleStateService.Location);

			g.GoToVehicleCoordinates(vehiclePos, vehicleHeading + (float)Math.PI/2.0f);

			SizeF boxSize = new SizeF(2*point_box_size, 2*point_box_size);
			foreach (InternalTrackedCluster cluster in trackedClusters) {
				// determine cluster color
				Color clusterColor;
				switch (cluster.targetClass) {
					case SceneEstimatorTargetClass.TARGET_CLASS_CARLIKE:
						clusterColor = carlikeColor;
						break;

					case SceneEstimatorTargetClass.TARGET_CLASS_NOTCARLIKE:
						clusterColor = notcarColor;
						break;

					case SceneEstimatorTargetClass.TARGET_CLASS_UNKNOWN:
					default:
						clusterColor = unknownColor;
						break;
				}

				if (cluster.targetStatus == SceneEstimatorTargetStatusFlag.TARGET_STATE_DELETED) {
					clusterColor = Color.FromArgb(deleted_alpha, clusterColor);
				}

				if (drawPoints) {
					// render the points
					IPen pen = g.CreatePen();
					pen.Color = clusterColor;
					pen.Width = nominal_pixel_size/wt.Scale;

					for (int j = 0; j < cluster.points.Length; j++) {
						g.DrawRectangle(pen, new RectangleF(cluster.points[j], boxSize));
					}
				}

				if (drawPolygon) {
					IPen pen = g.CreatePen();
					pen.Color = clusterColor;
					pen.Width = nominal_pixel_size/wt.Scale;

					g.DrawPolygon(pen, Utility.ToPointF(cluster.polygon));
				}

				// if we're stopped, draw an x at the center of the polygon
				if (cluster.isStopped) {
					DrawingUtility.DrawControlPoint(g, cluster.polygon.GetCentroid(), Color.Red, null, ContentAlignment.BottomCenter, ControlPointStyle.LargeX, wt);
				}
				else {
					// draw a vector pointing from the centroid of the polygon out

				}
			}

			g.ComeBackFromVehicleCoordinates();
		}

		public string Name {
			get { return name; }
		}

		#endregion

		#region IAttachable<SceneEstimatorTrackedClusterCollection> Members

		public void SetCurrentValue(SceneEstimatorTrackedClusterCollection value, string label) {
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion

		#region IProvideContextMenu Members

		public ICollection<ToolStripMenuItem> GetMenuItems() {
			throw new Exception("The method or operation is not implemented.");
		}

		public void OnMenuOpening() {
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}
}
