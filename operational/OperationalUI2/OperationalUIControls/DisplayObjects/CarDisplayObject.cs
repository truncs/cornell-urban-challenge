using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Mapack;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class CarDisplayObject : IHittable, IAttachable<double>, IAttachable<Coordinates>, ISimpleColored {
		private const float length = (float)TahoeParams.VL;
		private const float width = (float)TahoeParams.T;
		private const float wheelbase = (float)TahoeParams.L;
		private const float rearOffset = (float)TahoeParams.RL;
		private const float tireDiameter = 0.7874f;
		private const float tireWidth = 0.28f;
		private const float wheelOffset = width / 2 - tireWidth;
		private const float nomPixelWidth = 1;

		private float heading;
		private PointF location;

		private RectangleF bodyRect;
		private RectangleF wheelRectR, wheelRectL; // left and right wheel rectangle

		private Color color;

		private bool attachToVehicleState;

		private string name = "Tahoe";

		public CarDisplayObject(Color color) : this() {
			this.color = color;
		}

		public CarDisplayObject(string name, Color color) : this() {
			this.name = name;
			this.color = color;
		}

		public CarDisplayObject() {
			bodyRect = new RectangleF(-width / 2, -rearOffset, width, length);
			wheelRectL = RectangleF.FromLTRB(-tireWidth, -tireDiameter / 2, 0, tireDiameter / 2);
			wheelRectR = RectangleF.FromLTRB(0, -tireDiameter / 2, tireWidth, tireDiameter / 2);
			attachToVehicleState = true;
		}

		public bool AttachToVehicleState {
			get { return attachToVehicleState; }
			set { attachToVehicleState = value; }
		}

		public Color Color {
			get { return color; }
			set { color = value; }
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			// calculate the bounding polygon
			Matrix3 trans = Matrix3.Translation(location.X, location.Y)*Matrix3.Rotation(heading - Math.PI/2.0);
			RectangleF bodyRectInflated = bodyRect;
			bodyRectInflated.Inflate(tol, tol);
			// create a polygon for the body rect
			Polygon poly = new Polygon(4);
			poly.Add(new Coordinates(bodyRectInflated.Left, bodyRectInflated.Bottom));
			poly.Add(new Coordinates(bodyRectInflated.Right, bodyRectInflated.Bottom));
			poly.Add(new Coordinates(bodyRectInflated.Right, bodyRectInflated.Top));
			poly.Add(new Coordinates(bodyRectInflated.Left, bodyRectInflated.Top));
			// transform it
			trans.TransformPointsInPlace(poly);

			// check if we're inside the polygon
			bool hit = poly.IsInside(loc);

			// if it's a hit, find the closest point
			if (hit) {
				// re-create the polygon as not inflated
				poly.Clear();
				poly.Add(new Coordinates(bodyRect.Left, bodyRect.Bottom));
				poly.Add(new Coordinates(bodyRect.Right, bodyRect.Bottom));
				poly.Add(new Coordinates(bodyRect.Right, bodyRect.Top));
				poly.Add(new Coordinates(bodyRect.Left, bodyRect.Top));
				// transform it
				trans.TransformPointsInPlace(poly);

				// get the distance to the rear-axle point
				Coordinates closestPoint = Utility.ToCoord(location);
				double minDist = loc.DistanceTo(closestPoint);

				// iterate through the segments and get the closest point
				foreach (LineSegment ls in poly.GetSegmentEnumerator()) {
					Coordinates testPoint = ls.ClosestPoint(loc);
					double dist = testPoint.DistanceTo(loc);
					if (dist < minDist) {
						minDist = dist;
						closestPoint = testPoint;
					}
				}

				return new HitTestResult(this, true, closestPoint, null);
			}

			return HitTestResult.NoHit;
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public string Name {
			get { return name; }
			set { name = value; }
		}

		public void Render(IGraphics g, WorldTransform wt) {
			// if we're attached to the vehicle state, update our shits
			if (attachToVehicleState) {
				location = Utility.ToPointF(Services.VehicleStateService.Location);
				heading = (float)Services.VehicleStateService.Heading;
			}

			g.GoToVehicleCoordinates(location, heading);

			float penWidth = nomPixelWidth / wt.Scale;
			//if (penWidth > 0.01f) penWidth = 0.01f;
			IPen p = g.CreatePen();
			p.Width = penWidth;
			p.Color = color;

			g.DrawRectangle(p, bodyRect);

			// build the transform for the rear wheels
			// do the left wheel
			g.PushMatrix();
			g.Translate(-wheelOffset, 0);
			g.FillRectangle(Color.White, wheelRectL);
			g.DrawRectangle(p, wheelRectL);
			g.PopMatrix();

			// do the right wheel
			g.PushMatrix();
			g.Translate(wheelOffset, 0);
			g.FillRectangle(Color.White, wheelRectR);
			g.DrawRectangle(p, wheelRectR);
			g.PopMatrix();

			// do the front wheels
			// do the left wheel
			g.PushMatrix();
			g.Translate(-wheelOffset, wheelbase);
			g.FillRectangle(Color.White, wheelRectL);
			g.DrawRectangle(p, wheelRectL);
			g.PopMatrix();

			// do the right wheel
			g.PushMatrix();
			g.Translate(wheelOffset, wheelbase);
			g.FillRectangle(Color.White, wheelRectR);
			g.DrawRectangle(p, wheelRectR);
			g.PopMatrix();

			// draw the forward arrow
			g.DrawLine(p, new PointF(bodyRect.Left + 0.5f, bodyRect.Bottom - bodyRect.Width / 2), new PointF(bodyRect.Left + bodyRect.Width / 2, bodyRect.Bottom - .5f));
			g.DrawLine(p, new PointF(bodyRect.Right - 0.5f, bodyRect.Bottom - bodyRect.Width / 2), new PointF(bodyRect.Right - bodyRect.Width / 2, bodyRect.Bottom - .5f));

			g.DrawCross(p, new PointF(0, 0), 8/wt.Scale);

			g.ComeBackFromVehicleCoordinates();

			p.Dispose();
		}

		#endregion

		#region IAttachable<double> Members

		public void SetCurrentValue(double value, string label) {
			if (!attachToVehicleState) {
				heading = (float)value;
			}
		}

		#endregion

		#region IAttachable<Coordinates> Members

		public void SetCurrentValue(Coordinates value, string label) {
			if (!attachToVehicleState) {
				location = Utility.ToPointF(value);
			}
		}

		#endregion
	}
}
