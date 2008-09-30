using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common {
	[Serializable]
	public struct Rect {
		public double x;
		public double y;
		public double width;
		public double height;

		public Rect(double x, double y, double width, double height) {
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		public static Rect FromExtents(Coordinates lowerLeft, Coordinates upperRight) {
			return new Rect(lowerLeft.X, lowerLeft.Y, upperRight.X - lowerLeft.X, upperRight.Y - lowerLeft.Y);
		}

		public double Right {
			get { return x+width; }
		}

		public double Top {
			get { return y+height; }
		}

		public bool Contains(Coordinates coord) {
			return (coord.X >= x && coord.X <= x + width && coord.Y >= y && coord.Y <= y + height);
		}

		public bool IsInside(Coordinates coord) {
			return Contains(coord);
		}

		public void Inflate(double dx, double dy) {
			x -= dx;
			width += 2 * dx;
			y -= dy;
			height += 2 * dy;
		}

		public bool Overlaps(Rect r) {
			// pre-calculate left, bottom, right, top of this and right rect
			double tl = x, tb = y, tr = x+width, tt = y+height;
			double rl = r.x, rb = r.y, rr = r.x+r.width, rt = r.y+r.height;

			if (tl <= rl && tb <= rb && tr >= rr && tt >= rt) {
				// r is fully contained in this rectangle
				return true;
			}
			else if (tl >= rl && tb >= rb && tr <= rr && tt <= rt) {
				// this rectangle is fully contained in r
				return true;
			}
			else if (((rl >= tl && rl <= tr) || (rr >= tl && rr <= tr)) && ((rb >= tb && rb <= tt) || (rt >= tb && rt <= tt))) {
				return true;
			}

			return false;
		}

		public static Rect Union(Rect l, Rect r) {
			double left = Math.Min(l.x, r.x);
			double right = Math.Max(l.Right, r.Right);
			double bottom = Math.Min(l.y, r.y);
			double top = Math.Max(l.Top, r.Top);

			return new Rect(left, bottom, right-left, top-bottom);
		}
	}
}
