using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using UrbanChallenge.Common;
using System.Diagnostics;
using UrbanChallenge.PathSmoothing;
using UrbanChallenge.Common.Shapes;

namespace PathSmootherTest {
	public partial class Form1 : Form {
		private Matrix transform, inv;

		private List<Coordinates> pathPoints = new List<Coordinates>();
		private List<Coordinates> ubPoints = new List<Coordinates>();
		private List<Coordinates> lbPoints = new List<Coordinates>();
		private List<Coordinates> smoothedPoints = new List<Coordinates>();

		private Pen pathPen;
		private Pen ubPen;
		private Pen lbPen;
		private Pen smoothedPen;

		public Form1() {
			InitializeComponent();

			SetupTransform();
			InitPens();

			labelw.Text = "w: " + getW().ToString("F4");
			labelV.Text = "v: " + getV().ToString("F1");
			labelD1.Text = "d1: " + getD1().ToString("F0");
		}

		private void InitPens() {
			pathPen = new Pen(Color.DarkGreen, 1);
			ubPen = new Pen(Color.Blue, 1);
			lbPen = new Pen(Color.Red, 1);
			smoothedPen = new Pen(Color.Black, 2);
		}

		private void SetupTransform() {
			transform = new Matrix();
			transform.Scale(20, -20);
			transform.Translate(picEntry.Width/2.0f, picEntry.Height/2.0f, MatrixOrder.Append);

			inv = transform.Clone();
			inv.Invert();
		}

		private void picEntry_MouseDown(object sender, MouseEventArgs e) {
			Coordinates worldPoint = WorldPoint(e.Location);
			if (optPath.Checked) {
				pathPoints.Add(worldPoint);
			}
			else if (optUB.Checked) {
				ubPoints.Add(worldPoint);
			}
			else if (optLB.Checked) {
				lbPoints.Add(worldPoint);
			}

			picEntry.Invalidate();
		}

		private void picEntry_Paint(object sender, PaintEventArgs e) {
			GraphicsState gs = e.Graphics.Save();
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;

			if (pathPoints.Count >= 2) {
				e.Graphics.DrawLines(pathPen, ToScreenPoints(pathPoints));
			}

			if (ubPoints.Count >= 2) {
				e.Graphics.DrawLines(ubPen, ToScreenPoints(ubPoints));
			}

			if (lbPoints.Count >= 2) {
				e.Graphics.DrawLines(lbPen, ToScreenPoints(lbPoints));
			}

			if (smoothedPoints.Count >= 2) {
				e.Graphics.DrawLines(smoothedPen, ToScreenPoints(smoothedPoints));
			}

			e.Graphics.Restore(gs);
		}

		private void picEntry_MouseMove(object sender, MouseEventArgs e) {
			PointF[] pt = new PointF[] { e.Location };
			inv.TransformPoints(pt);
			labelLoc.Text = string.Format("{0:F2},{1:F2}", pt[0].X, pt[0].Y);
		}

		private void picEntry_Resize(object sender, EventArgs e) {
			SetupTransform();
			picEntry.Invalidate();
		}

		private void buttonClear_Click(object sender, EventArgs e) {
			pathPoints.Clear();
			ubPoints.Clear();
			lbPoints.Clear();
			smoothedPoints.Clear();
			picEntry.Invalidate();
		}

		private PointF ToPointF(Coordinates c) {
			return new PointF((float)c.X, (float)c.Y);
		}

		private PointF[] ToPointF(ICollection<Coordinates> c) {
			PointF[] ret = new PointF[c.Count];
			int ind = 0;
			foreach (Coordinates cp in c) {
				ret[ind++] = ToPointF(cp);
			}

			return ret;
		}

		private PointF[] ToScreenPoints(ICollection<Coordinates> c) {
			PointF[] pts = ToPointF(c);
			transform.TransformPoints(pts);
			return pts;
		}

		private Coordinates ToCoord(PointF p) {
			return new Coordinates(p.X, p.Y);
		}

		private Coordinates WorldPoint(PointF p) {
			PointF[] pt = new PointF[] { p };
			inv.TransformPoints(pt);
			return ToCoord(pt[0]);
		}

		private void buttonSmooth_Click(object sender, EventArgs e) {
			Debug.WriteLine("stuff");
			SmoothIt();
		}

		private void SmoothIt() {
			Stopwatch s = Stopwatch.StartNew();

			SmootherOptions opt = PathSmoother.GetDefaultOptions();

			if (optIPOPT.Checked) {
				opt.alg = SmoothingAlgorithm.Ipopt;
			}
			else {
				opt.alg = SmoothingAlgorithm.Loqo;
			}

			opt.alpha_w = getW();
			opt.alpha_d = getD1();
			opt.set_init_velocity = true;
			opt.init_velocity = getV();

			opt.set_init_heading = true;
			opt.init_heading = Math.Atan2(pathPoints[1].Y-pathPoints[0].Y, pathPoints[1].X-pathPoints[0].X);

			opt.set_final_heading = true;
			opt.final_heading = Math.Atan2(pathPoints[pathPoints.Count-1].Y-pathPoints[pathPoints.Count-2].Y, pathPoints[pathPoints.Count-1].X-pathPoints[pathPoints.Count-2].X);

			opt.set_final_offset = true;

			List<PathPoint> path = new List<PathPoint>();

			Boundary ub = new Boundary();
			ub.Coords = ubPoints;
			ub.DesiredSpacing = 1;
			ub.MinSpacing = 0.25;
			ub.Polygon = false;
			List<Boundary> ub_bounds = new List<Boundary>();
			ub_bounds.Add(ub);

			Boundary lb = new Boundary();
			lb.Coords = lbPoints;
			lb.DesiredSpacing = 0.5;
			lb.MinSpacing = 0;
			lb.Polygon = false;
			List<Boundary> lb_bounds = new List<Boundary>();
			lb_bounds.Add(lb);

			LineList basePath = new LineList();
			basePath.AddRange(pathPoints);

			SmoothResult sr = SmoothResult.Error;
			try {
				sr = PathSmoother.SmoothPath(basePath, ub_bounds, lb_bounds, opt, path);
			}
			catch (Exception ex) {
				MessageBox.Show("exception: " + ex.Message);
				return;
			}

			smoothedPoints = path.ConvertAll<Coordinates>(delegate(PathPoint p) { return new Coordinates(p.x, p.y); });

			s.Stop();

			long ms = s.ElapsedMilliseconds;

			if (sr != SmoothResult.Sucess) {
				MessageBox.Show("Path smooth result: " + sr.ToString());
			}

			MessageBox.Show("Elapsed MS: " + ms);

			picEntry.Invalidate();
		}

		private void trackW_Scroll(object sender, EventArgs e) {
			labelw.Text = "w: " + getW().ToString("F4");
		}

		private double getW() {
			int val = 100 - trackW.Value;
			return Math.Pow(0.0001, val/100.0);
		}

		private double getV() {
			return trackV.Value / 10.0;
		}

		private double getD1() {
			return trackD1.Value;
		}

		private void trackV_Scroll(object sender, EventArgs e) {
			labelV.Text = "v: " + getV().ToString("F1");
		}

		private void trackD1_Scroll(object sender, EventArgs e) {
			labelD1.Text = "d1: " + getD1().ToString("F0");
		}
	}
}
