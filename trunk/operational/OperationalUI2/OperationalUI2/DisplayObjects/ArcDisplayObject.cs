using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUI.Controls.DisplayObjects;
using System.Drawing;
using UrbanChallenge.OperationalUIService.Debugging;
using UrbanChallenge.OperationalUI.Common.Map;
using System.Windows.Forms;
using UrbanChallenge.Operational.Common;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;

namespace UrbanChallenge.OperationalUI.DisplayObjects {
	
	public class ArcDisplayObject : IRenderable, IAttachable<ArcVotingResults>, IClearable, IProvideContextMenu {
		private const float nomPixelWidth = 1;
		private const float dist = 20;

		private Color color = Color.BlueViolet;

		private ToolStripMenuItem[] menuItems;

		private ArcVotingResults results;

		#region IRenderable Members

		public void Render(IGraphics g, WorldTransform wt) {
			ArcVotingResults results = this.results;

			if (results == null)
				return;

			// get the current vehicle position/heading
			float vehicleHeading = (float)Services.VehicleStateService.Heading;
			PointF vehiclePos = Utility.ToPointF(Services.VehicleStateService.Location);

			g.GoToVehicleCoordinates(vehiclePos, vehicleHeading + (float)Math.PI/2.0f);

			IPen p = g.CreatePen();
			p.Color = color;
			p.Width = nomPixelWidth / wt.Scale;

			foreach (ArcResults result in results.arcResults) {
				// generate the arc
				if (Math.Abs(result.curvature) < 1e-4) {
					g.DrawLine(p, new PointF(0, 0), new PointF(dist, 0));
				}
				else {
					double curvature = result.curvature;
					bool leftTurn = curvature > 0;
					double radius = Math.Abs(1/curvature);
					double frontRadius = Math.Sqrt(TahoeParams.FL*TahoeParams.FL + radius*radius);

					CircleSegment rearSegment;

					if (leftTurn) {
						Coordinates center = new Coordinates(0, radius);
						rearSegment = new CircleSegment(radius, center, Coordinates.Zero, dist, true);
					}
					else {
						Coordinates center = new Coordinates(0, -radius);
						rearSegment = new CircleSegment(radius, center, Coordinates.Zero, dist, false);
					}

					PointF[] points = Utility.ToPointF(rearSegment.ToPoints(10));
					g.DrawLines(p, points);
				}
			}

			g.ComeBackFromVehicleCoordinates();

			p.Dispose();
		}

		public string Name {
			get { return "planning/burm sparcs"; }
		}

		#endregion

		#region IAttachable<ArcVotingResults> Members

		public void SetCurrentValue(ArcVotingResults value, string label) {
			results = value;
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			results = null;
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
