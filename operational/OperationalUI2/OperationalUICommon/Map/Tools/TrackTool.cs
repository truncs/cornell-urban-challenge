using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.Common;

namespace UrbanChallenge.OperationalUI.Common.Map.Tools {
	public class TrackTool : ITool {
		private IDrawingSurface drawingSurface;

		private float offsetFrac;

		public float OffsetFraction {
			get { return offsetFrac; }
			set { offsetFrac = value; }
		}

		public void OnActivate(IDrawingSurface drawingService) {
			this.drawingSurface = drawingService;
		}

		public void OnDeactivate(IDrawingSurface drawingService) {
			this.drawingSurface = null;
		}

		public void OnMouseDown(MouseEventArgs e) {
			
		}

		public void OnMouseMove(MouseEventArgs e) {
			
		}

		public void OnMouseUp(MouseEventArgs e) {
			
		}

		public void OnKeyDown(KeyEventArgs e) {
			if (e.KeyCode == Keys.PageUp) {
				drawingSurface.ZoomDelta(1);
			}
			else if (e.KeyCode == Keys.PageDown) {
				drawingSurface.ZoomDelta(-1);
			}
		}

		public void OnKeyPress(KeyPressEventArgs e) {
			
		}

		public void OnKeyUp(KeyEventArgs e) {
			
		}

		public void SetupTransform(WorldTransform transform) {
			// figure out if the height or width is more constraining
			Coordinates lowerLeft = transform.WorldLowerLeft;
			Coordinates upperRight = transform.WorldUpperRight;

			double width = Math.Abs(upperRight.X - lowerLeft.X);
			double height = Math.Abs(upperRight.Y - lowerLeft.Y);

			double rad;
			if (width < height) {
				rad = width / 2;
			}
			else {
				rad = height / 2;
			}

			// get the heading
			double heading = Services.VehicleStateService.Heading;

			// calculate the offset vector
			Coordinates offsetVector = Coordinates.FromAngle(heading)*rad*offsetFrac;

			// calculate the vehicle center position
			transform.CenterPoint = Services.VehicleStateService.Location - offsetVector;
		}

		public void OnPreRender(IGraphics g, WorldTransform transform) {

		}

		public void OnPostRender(IGraphics g, WorldTransform transform) {
			
		}
	}
}
