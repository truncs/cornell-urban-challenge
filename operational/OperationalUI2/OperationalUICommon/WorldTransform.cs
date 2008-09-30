using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UrbanChallenge.OperationalUI.Common {
	public class WorldTransform {
		private float scale;
		private Coordinates centerPoint;
		private SizeF screenSize;

		private Matrix transform;
		private Matrix transformInv;

		private bool centerScreen;

		public WorldTransform() {
			this.scale = 10;
			this.centerPoint = Coordinates.Zero;
			screenSize = new SizeF(500, 500);
			centerScreen = true;
		}

		public WorldTransform(float scale, Coordinates centerPoint, SizeF screenSize) {
			this.scale = scale;
			this.centerPoint = centerPoint;
			this.screenSize = screenSize;
			centerScreen = true;
		}

		public bool CenterScreen {
			get { return centerScreen; }
			set { centerScreen = value; }
		}

		public float Scale {
			get { return scale; }
			set {
				scale = value;
				Invalidate();
			}
		}

		public Coordinates CenterPoint {
			get { return centerPoint; }
			set { 
				centerPoint = value;
				Invalidate();
			}
		}

		public SizeF ScreenSize {
			get { return screenSize; }
			set { 
				screenSize = value;
				Invalidate();
			}
		}

		private void Invalidate() {
			transform = null;
			transformInv = null;
		}

		public Matrix GetTransform() {
			if (transform != null) {
				return transform;
			}

			transform = new Matrix();
			transform.Translate((float)-centerPoint.X, (float)-centerPoint.Y, MatrixOrder.Append);
			transform.Scale(this.scale, this.scale, MatrixOrder.Append);
			transform.Scale(1, -1, MatrixOrder.Append);
			if (centerScreen) {
				transform.Translate(screenSize.Width / 2.0f, screenSize.Height / 2.0f, MatrixOrder.Append);
			}
			else {
				transform.Translate(0, screenSize.Height, MatrixOrder.Append);
			}

			return transform;
		}

		public Matrix GetInverseTransform() {
			if (transformInv != null) {
				return transformInv;
			}

			transformInv = GetTransform().Clone();
			transformInv.Invert();

			return transformInv;
		}

		public Coordinates WorldLowerLeft {
			get {
				Matrix inv = GetInverseTransform();
				PointF screenLL = new PointF(0, screenSize.Height);
				PointF[] pts = new PointF[] { screenLL };
				try {
					inv.TransformPoints(pts);
					return new Coordinates(pts[0].X, pts[0].Y);
				}
				catch (Exception) {
					return new Coordinates(0, 0);
				}
			}
		}

		public Coordinates WorldUpperRight {
			get {
				Matrix inv = GetInverseTransform();
				PointF screenUR = new PointF(screenSize.Width, 0);
				PointF[] pts = new PointF[] { screenUR };
				try {
					inv.TransformPoints(pts);
					return new Coordinates(pts[0].X, pts[0].Y);
				}
				catch (Exception) {
					return new Coordinates(0, 0);
				}
			}
		}

		public bool ShouldDraw(RectangleF bb) {
			// transform the bounding box
			// get the 4 corners
			PointF[] pts = new PointF[4];
			pts[0] = new PointF(bb.Left, bb.Top);
			pts[1] = new PointF(bb.Right, bb.Top);
			pts[2] = new PointF(bb.Left, bb.Bottom);
			pts[3] = new PointF(bb.Right, bb.Bottom);

			// transform the points
			Matrix trans = GetTransform();
			trans.TransformPoints(pts);

			// construct a new bounding box
			RectangleF rect = Utility.CalcBoundingBox(pts);

			return (rect.IntersectsWith(new RectangleF(new PointF(0, 0), screenSize)));
		}

		public Coordinates GetWorldPoint(PointF screenLoc) {
			Matrix inv = GetInverseTransform();
			PointF[] pts = new PointF[] { screenLoc };
			inv.TransformPoints(pts);
			return Utility.ToCoord(pts[0]);
		}

		public PointF GetScreenPoint(Coordinates loc) {
			Matrix t = GetTransform();
			PointF[] pts = new PointF[] { Utility.ToPointF(loc) };
			t.TransformPoints(pts);
			return pts[0];
		}

		public WorldTransform Clone() {
			WorldTransform wt = new WorldTransform(scale, centerPoint, screenSize);
			wt.centerScreen = centerScreen;
			return wt;
		}
	}
}
