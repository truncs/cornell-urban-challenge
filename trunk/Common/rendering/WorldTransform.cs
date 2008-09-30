using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using UrbanChallenge.Common;
using Rendering;

namespace Rendering {
	public class WorldTransform {
		private float scale = 4.0f;
		private float rotation = 0.0f;
		private Coordinates centerPoint = new Coordinates(0, 0);
		private SizeF screenSize;
		private bool flipY = true;

		// transformation matrix
		private Matrix m;
		// inverse transformation matrix
		private Matrix mi;

		public float Scale {
			get { return scale; }
			set {
				scale = value;
				//if (m != null) m.Dispose();
				//if (mi != null) mi.Dispose();
				m = mi = null;
			}
		}

		public float Rotation {
			get { return rotation; }
			set {
				rotation = value;
				//if (m != null) m.Dispose();
				//if (mi != null) mi.Dispose();
				m = mi = null;
			}
		}

		public Coordinates CenterPoint {
			get { return centerPoint; }
			set {
				centerPoint = value;
				//if (m != null) m.Dispose();
				//if (mi != null) mi.Dispose();
				m = mi = null;
			}
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

		public SizeF ScreenSize {
			get { return screenSize; }
			set {
				screenSize = value;
				//if (m != null) m.Dispose();
				//if (mi != null) mi.Dispose();
				m = mi = null;
			}
		}

		public bool FlipY {
			get { return flipY; }
			set {
				flipY = value;
				//if (m != null) m.Dispose();
				//if (mi != null) mi.Dispose();
				m = mi = null;
			}
		}

		public Matrix GetTransform() {
			// apply in this order: 
			// 1) translate
			// 2) scale
			// 3) rotate
			// 4) flip
			// 5) center (translate)

			if (m != null) return m;

			Matrix result = new Matrix();

			result.Translate((float)-centerPoint.X, (float)-centerPoint.Y, MatrixOrder.Append);
			result.Scale(this.scale, this.scale, MatrixOrder.Append);
			result.Rotate(rotation, MatrixOrder.Append);

			if (flipY) {
				result.Scale(1, -1, MatrixOrder.Append);
			}

			result.Translate(screenSize.Width / 2.0f, screenSize.Height / 2.0f, MatrixOrder.Append);

			m = result;

			return result;
		}

		public Matrix GetInverseTransform() {
			if (mi != null) return mi;

			mi = GetTransform().Clone();

			mi.Invert();

			return mi;
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
	}
}
