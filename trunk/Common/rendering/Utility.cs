using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using UrbanChallenge.Common;

namespace Rendering {
	static class Utility {
		public static RectangleF CalcBoundingBox(params PointF[] pts) {
			float minx = float.MaxValue, miny = float.MaxValue;
			float maxx = float.MinValue, maxy = float.MinValue;

			for (int i = 0; i < pts.Length; i++) {
				if (pts[i].X < minx)
					minx = pts[i].X;

				if (pts[i].X > maxx)
					maxx = pts[i].X;

				if (pts[i].Y < miny)
					miny = pts[i].Y;

				if (pts[i].Y > maxy)
					maxy = pts[i].Y;
			}

			return new RectangleF(minx, miny, maxx - minx, maxy - miny);
		}

		public static RectangleF CalcBoundingBox(params Coordinates[] coords) {
			PointF[] pts = new PointF[coords.Length];
			for (int i = 0; i < pts.Length; i++)
				pts[i] = new PointF((float)coords[i].X, (float)coords[i].Y);

			return CalcBoundingBox(pts);
		}

		public static PointF ToPointF(Coordinates c) {
			return new PointF((float)c.X, (float)c.Y);
		}

		public static Coordinates ToCoord(PointF pt) {
			return new Coordinates(pt.X, pt.Y);	
		}

		public static Coordinates[] ToCoord(PointF[] pts) {
			Coordinates[] ret = new Coordinates[pts.Length];

			for (int i = 0; i < pts.Length; i++) {
				ret[i] = ToCoord(pts[i]);
			}

			return ret;
		}
	}
}
