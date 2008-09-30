using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using UrbanChallenge.Common;

using UrbanChallenge.Operational.Common;

using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.Map;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

namespace UrbanChallenge.OperationalUI.DisplayObjects {
	class SmoothingResultsDisplayObject : IRenderable, IClearable, IProvideContextMenu {
		private AvoidanceDetails avoidanceDetails;
		private string name;

		private bool drawBoundPoints = true;

		private ToolStripMenuItem[] menuItems;

		public SmoothingResultsDisplayObject(string name) {
			this.name = name;

			try {
				OperationalInterface.OperationalUIFacade.DebuggingFacade.GenerateAvoidanceDetails = true;
			}
			catch (Exception) {
			}

			ToolStripMenuItem menuGetDetails = new ToolStripMenuItem("Get Details", null, menuGetDetails_Click);
			menuItems = new ToolStripMenuItem[] { menuGetDetails };
		}

		private void menuGetDetails_Click(object sender, EventArgs e) {
			try {
				avoidanceDetails = OperationalInterface.OperationalUIFacade.DebuggingFacade.GetAvoidanceDetails();
				if (avoidanceDetails == null) {
					MessageBox.Show("Operational is not generating avoidance details");
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Error getting avoidance details:\n" + ex.Message);
			}
		}

		#region IRenderable Members

		public string Name {
			get { return name; }
		}

		public void Render(IGraphics g, WorldTransform wt) {
			if (avoidanceDetails == null)
				return;

			if (drawBoundPoints) {
				PointF vehicleLoc = Utility.ToPointF(Services.VehicleStateService.Location);
				g.GoToVehicleCoordinates(vehicleLoc, (float)Services.VehicleStateService.Heading + (float)Math.PI/2.0f);
				for (int i = 0; i < avoidanceDetails.smoothingDetails.leftBounds.Length; i++) {
					BoundInformation b = avoidanceDetails.smoothingDetails.leftBounds[i];
					DrawingUtility.DrawControlPoint(g, b.point, Color.Blue, null, ContentAlignment.MiddleRight, ControlPointStyle.LargeX, wt);
				}

				for (int i = 0; i < avoidanceDetails.smoothingDetails.rightBounds.Length; i++) {
					BoundInformation b = avoidanceDetails.smoothingDetails.rightBounds[i];
					DrawingUtility.DrawControlPoint(g, b.point, Color.Red, null, ContentAlignment.MiddleRight, ControlPointStyle.LargeX, wt);
				}

				g.ComeBackFromVehicleCoordinates();
			}
		}

		#endregion

		#region IClearable Members

		public void Clear() {
			avoidanceDetails = null;
		}

		#endregion

		#region IProvideContextMenu Members

		public ICollection<ToolStripMenuItem> GetMenuItems() {
			return menuItems;
		}

		#endregion

		#region IProvideContextMenu Members


		public void OnMenuOpening() {
			
		}

		#endregion
	}
}
