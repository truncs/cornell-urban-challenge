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
using UrbanChallenge.Common.Path;
using System.Windows.Forms;


namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class LineListDisplayObject : IHittable, IAttachable<LineList>, IClearable, ISimpleColored, IProvideContextMenu {
		private float nom_pixel_width = 1.0f;

		private LinePath line;
		private Color color;

		private bool labelPoints;

		private string name;

		private ToolStripMenuItem[] menuItems;

		public LineListDisplayObject(string name, Color color) {
			this.name = name;
			this.color = color;

			ToolStripMenuItem menuLabelPoints = new ToolStripMenuItem("Label Points");
			menuLabelPoints.Click += new EventHandler(menuLabelPoints_Click);

			menuItems = new ToolStripMenuItem[] { menuLabelPoints };
		}

		void menuLabelPoints_Click(object sender, EventArgs e) {
			labelPoints = !labelPoints;
			((ToolStripMenuItem)sender).Checked = labelPoints;
		}
		
		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			if (line == null) {
				return HitTestResult.NoHit;
			}

			LinePath.PointOnPath pt = line.GetClosestPoint(loc);
			if (!pt.Valid) {
				return HitTestResult.NoHit;
			}
			Coordinates closestPoint = pt.Location;

			if (closestPoint.DistanceTo(loc) < tol) {
				return new HitTestResult(this, true, closestPoint, null);
			}

			return HitTestResult.NoHit;
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			LinePath path = line;

			if (path == null) {
				return;
			}

			PointF[] pts = Utility.ToPointF(path);
			IPen p = g.CreatePen();
			p.Color = color;
			p.Width = nom_pixel_width/wt.Scale;
			g.DrawLines(p, pts);
			p.Dispose();

			if (labelPoints) {
				for (int i = 0; i < path.Count; i++) {
					DrawingUtility.DrawControlPoint(g, path[i], Color.Black, i.ToString(), ContentAlignment.MiddleRight, ControlPointStyle.SmallX, wt);
				}
			}
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<LineList> Members

		public void SetCurrentValue(LineList value, string label) {
			line = new LinePath(value);
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			line = null;
		}

		#endregion

		#region ISimpleColored Members

		public Color Color {
			get { return color; }
			set { color = value; }
		}

		#endregion

		#region IProvideContextMenu Members

		public ICollection<ToolStripMenuItem> GetMenuItems() {
			return menuItems;
		}

		public void OnMenuOpening() {
			
		}

		#endregion
	}
}
