using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace RndfEditor.Display.Utilities
{
	/// <summary>
	/// Allows transformations to and from the world and display
	/// </summary>
	[Serializable]
	public class WorldTransform
	{
		private float scale = 6.0f;
		private float rotation = 0.0f;
		private Coordinates centerPoint = new Coordinates(0, 0);
		private SizeF screenSize;
		private bool flipY = true;

		// transformation matrix
		private Matrix m;
		// inverse transformation matrix
		private Matrix mi;

		public float Scale
		{
			get { return scale; }
			set
			{
				scale = value;
				m = mi = null;
			}
		}

		public float Rotation
		{
			get { return rotation; }
			set
			{
				rotation = value;
				m = mi = null;
			}
		}

		public Coordinates CenterPoint
		{
			get { return centerPoint; }
			set
			{
				centerPoint = value;
				m = mi = null;
			}
		}

		public Coordinates WorldLowerLeft
		{
			get
			{
				Matrix inv = GetInverseTransform();
				PointF screenLL = new PointF(0, screenSize.Height);

				Coordinates wll = this.GetWorldPoint(screenLL);
				return wll;
			}
		}

		public Coordinates WorldUpperRight
		{
			get
			{
				Matrix inv = GetInverseTransform();
				PointF screenUR = new PointF(screenSize.Width, 0);

				Coordinates wur = this.GetWorldPoint(screenUR);
				return wur;
			}
		}

		public SizeF ScreenSize
		{
			get { return screenSize; }
			set
			{
				screenSize = value;
				m = mi = null;
			}
		}

		public bool FlipY
		{
			get { return flipY; }
			set
			{
				flipY = value;
				m = mi = null;
			}
		}

		public Matrix GetTransform()
		{
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

			if (flipY)
			{
				result.Scale(1, -1, MatrixOrder.Append);
			}

			result.Translate(screenSize.Width / 2.0f, screenSize.Height / 2.0f, MatrixOrder.Append);

			m = result;

			return result;
		}

		public Matrix GetInverseTransform()
		{
			if (mi != null) return mi;

			mi = GetTransform().Clone();

			mi.Invert();

			return mi;
		}

		public Coordinates GetWorldPoint(PointF screenLoc)
		{
			PointF[] pts = new PointF[] { screenLoc };
			GetInverseTransform().TransformPoints(pts);
			return DrawingUtility.ToCoord(pts[0]);
		}

		public PointF GetScreenPoint(Coordinates worldLoc)
		{
			PointF[] pts = new PointF[] { DrawingUtility.ToPointF(worldLoc) };
			GetTransform().TransformPoints(pts);
			return pts[0];
		}

		public bool ShouldDraw(RectangleF bb)
		{
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
			RectangleF rect = DrawingUtility.CalcBoundingBox(pts);

			return (rect.IntersectsWith(new RectangleF(new PointF(0, 0), screenSize)));
		}
	}
}
