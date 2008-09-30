using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class ObstacleDisplayObject : IHittable, IAttachable<OperationalObstacle[]>, IClearable, IProvideContextMenu {
		private const float nom_pixel_width = 1.0f;

		private Font labelFont = new Font("Verdana", 8.25f, FontStyle.Regular);

		private Color[] classColors = new Color[] { Color.LightBlue, Color.DarkBlue, Color.GreenYellow, Color.Orange, Color.Red, Color.Plum };
		private string name;

		private bool[] classRenderFlags = new bool[] { true, true, true, true, true, true };
		private bool ageRenderFlag;

		private OperationalObstacle[] obstacles;

		private ToolStripMenuItem menuRenderAge;
		private ToolStripMenuItem[] menuRenderClass;

		public ObstacleDisplayObject(string name) {
			this.name = name;

			menuRenderAge = new ToolStripMenuItem("Render Age", null, menuRenderAge_Click);

			menuRenderClass = new ToolStripMenuItem[classRenderFlags.Length];
			for (int i = 0; i < classRenderFlags.Length; i++) {
				menuRenderClass[i] = new ToolStripMenuItem("Render " + Enum.GetName(typeof(ObstacleClass), (ObstacleClass)i), null, menuRenderClass_Click);
				menuRenderClass[i].Tag = i;
			}
		}

		#region IHittable Members

		public RectangleF GetBoundingBox() {
			return RectangleF.Empty;
		}

		public HitTestResult HitTest(Coordinates loc, float tol) {
			return HitTestResult.NoHit;
		}

		public IHittable Parent {
			get { return null; }
		}

		#endregion

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			OperationalObstacle[] obstacles = this.obstacles;

			if (obstacles == null || obstacles.Length == 0)
				return;

			foreach (OperationalObstacle obs in obstacles) {
				if (classRenderFlags[(int)obs.obstacleClass]) {
					DrawObstacle(g, wt, obs);
				}
			}
		}

		private void DrawObstacle(IGraphics g, WorldTransform wt, OperationalObstacle obs) {
			IPen pen = g.CreatePen();
			pen.Width = nom_pixel_width/wt.Scale;
			pen.Color = classColors[(int)obs.obstacleClass];

			g.DrawPolygon(pen, Utility.ToPointF(obs.poly));

			if (obs.headingValid) {
				DrawingUtility.DrawArrow(g, obs.poly.GetCentroid(), Coordinates.FromAngle(obs.heading), 4, 0.75, Color.Black, wt);
			}

			if (ageRenderFlag || obs.ignored) {
				// draw the model confidence 
				string labelString = "";
				if (ageRenderFlag) {
					labelString += obs.age.ToString();
				}
				if (obs.ignored) {
					if (labelString.Length != 0) 
						labelString += ", ";
					labelString += "IGN"; 
				}

				SizeF stringSize = g.MeasureString(labelString, labelFont);
				stringSize.Width /= wt.Scale;
				stringSize.Height /= wt.Scale;
				Coordinates labelPt = obs.poly.GetCentroid();
				RectangleF rect = new RectangleF(Utility.ToPointF(labelPt), stringSize);
				float inflateValue = 4/wt.Scale;
				rect.X -= inflateValue;
				rect.Y -= inflateValue;
				rect.Width += 2/wt.Scale;
				g.FillRectangle(Color.FromArgb(127, Color.White), rect);
				g.DrawString(labelString, labelFont, Color.Black, Utility.ToPointF(labelPt));
			}
		}

		public string Name {
			get { return name; }
		}

		#endregion

		#region IAttachable<OperationalObstacle[]> Members

		public void SetCurrentValue(OperationalObstacle[] value, string label) {
			this.obstacles = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			this.obstacles = null;
		}

		#endregion

		#region IProvideContextMenu Members

		private void menuRenderAge_Click(object sender, EventArgs e) {
			menuRenderAge.Checked = !menuRenderAge.Checked;
			this.ageRenderFlag = menuRenderAge.Checked;
		}

		private void menuRenderClass_Click(object sender, EventArgs e) {
			ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			classRenderFlags[(int)menuItem.Tag] = menuItem.Checked;
		}

		public ICollection<ToolStripMenuItem> GetMenuItems() {
			ToolStripMenuItem[] menuItems = new ToolStripMenuItem[1+menuRenderClass.Length];
			menuItems[0] = menuRenderAge;
			Array.Copy(menuRenderClass, 0, menuItems, 1, menuRenderClass.Length);

			return menuItems;
		}

		public void OnMenuOpening() {
			menuRenderAge.Checked = ageRenderFlag;

			for (int i = 0; i < menuRenderClass.Length; i++) {
				menuRenderClass[i].Checked = classRenderFlags[i];
			}
		}

		#endregion
	}
}
