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
using UrbanChallenge.Operational.Common;

namespace UrbanChallenge.OperationalUI.Controls.DisplayObjects {
	public class PlanningGridDisplayObject : IRenderable, IAttachable<PlanningGrid>, IClearable {
		protected PlanningGrid grid;
		protected Color maxColor;
		protected Color minColor;

		protected string name;

		public PlanningGridDisplayObject(string name, Color maxColor, Color minColor) {
			this.name = name;
			this.maxColor = maxColor;
			this.minColor = minColor;
		}

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			if (grid == null) {
				return;
			}
			// go through and draw each rectangle
			float min = grid.MinValue;
			float max = grid.MaxValue;

			float a0 = minColor.A;
			float a1 = maxColor.A-minColor.A;
			float r0 = minColor.R;
			float r1 = maxColor.R-minColor.R;
			float g0 = minColor.G;
			float g1 = maxColor.G-minColor.G;
			float b0 = minColor.B;
			float b1 = maxColor.B-minColor.B;

			float offsetX = grid.OffsetX;
			float offsetY = grid.OffsetY;
			float spacing = grid.Spacing;

			SizeF boxSize = new SizeF(spacing, spacing);

			// go to the vehicle position
			PointF vehicleLoc = Utility.ToPointF(Services.VehicleStateService.Location);
			g.GoToVehicleCoordinates(vehicleLoc, (float)Services.VehicleStateService.Heading + (float)Math.PI/2.0f);


			if (max == min) {
				max = min + 1;
			}

			for (int x = 0; x < grid.SizeX; x++) {
				float xStart = offsetX + x*spacing;

				for (int y = 0; y < grid.SizeY; y++) {
					float val = grid[x, y];
					float frac = (val - min)/(max-min);
					Color c = Color.FromArgb((int)(a0 + a1*frac), (int)(r0 + r1*frac), (int)(g0 + g1*frac), (int)(b0 + b1*frac));

					float yStart = offsetY + y*spacing;

					g.FillRectangle(c, new RectangleF(new PointF(xStart, yStart), boxSize));
				}
			}

			g.ComeBackFromVehicleCoordinates();
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}

		#endregion

		#region IAttachable<PlanningGrid> Members

		public void SetCurrentValue(PlanningGrid value, string label) {
			grid = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			grid = null;
		}

		#endregion
	}
}
