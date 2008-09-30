using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Common;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

namespace UrbanChallenge.OperationalUI.Common.Map.Tools {
	public class SelectTool : ITool {
		private IDrawingSurface drawingSurface;
		private DragScreenHelper dragHelper;
		
		public void OnActivate(IDrawingSurface drawingService) {
			drawingService.GetControl().Cursor = Cursors.Arrow;
			this.drawingSurface = drawingService;
			this.dragHelper = null;
		}

		public void OnDeactivate(IDrawingSurface drawingService) {
			this.drawingSurface = null;
			this.dragHelper = null;
		}

		public void OnMouseDown(MouseEventArgs e) {
			// get the world point of the mouse hit
			Coordinates worldPoint = drawingSurface.Transform.GetWorldPoint(e.Location);

			// do a hit test
			// calculate hit tolerance in world units
			double tolerance = MapSettings.PixelHitTolerance / drawingSurface.Transform.Scale;

			// check for only selectable objects
			HitTestResult hitResult = drawingSurface.HitTest(worldPoint, tolerance, HitTestFilters.SelectableOnly);

			if (hitResult.Hit) {
				// we had a hit
				// NOTE: we could add dragging support here
				// check if the selected object has changed
				object selected = drawingSurface.SelectedObject = (ISelectable)hitResult.Target;

				// check if the selected object is IInteract
				if (selected is IMouseInteract) {
					((IMouseInteract)selected).OnMouseDown(new MouseEvent(worldPoint, e.Clicks, e.Button));
				}
			}
			else {
				// there was no hit
				// deselect the old object if one was selected
				drawingSurface.SelectedObject = null;

				// do a screen drag if this is a left mouse click
				if (e.Button == MouseButtons.Left) {
					dragHelper = new DragScreenHelper(drawingSurface, worldPoint);
				}
			}
		}

		public void OnMouseMove(MouseEventArgs e) {
			if (dragHelper != null) {
				dragHelper.InMove(e.Location);
			}
			else if (drawingSurface.SelectedObject != null) {
				// get the world point of the mouse hit
				Coordinates worldPoint = drawingSurface.Transform.GetWorldPoint(e.Location);

				// check if the selected object is IInteract
				if (drawingSurface.SelectedObject is IMouseInteract) {
					((IMouseInteract)drawingSurface.SelectedObject).OnMouseMove(new MouseEvent(worldPoint, e.Clicks, e.Button));
				}
			}
		}

		public void OnMouseUp(MouseEventArgs e) {
			if (dragHelper != null) {
				// apply the final point of the move
				dragHelper.InMove(e.Location);
				// kill it
				dragHelper = null;
			}
			else if (drawingSurface.SelectedObject != null) {
				// get the world point of the mouse hit
				Coordinates worldPoint = drawingSurface.Transform.GetWorldPoint(e.Location);

				// check if the selected object is IInteract
				if (drawingSurface.SelectedObject is IMouseInteract) {
					((IMouseInteract)drawingSurface.SelectedObject).OnMouseUp(new MouseEvent(worldPoint, e.Clicks, e.Button));
				}
			}
		}

		public void OnKeyDown(KeyEventArgs e) {
			if (e.KeyCode == Keys.Escape && dragHelper != null) {
				dragHelper.CancelMove();
			}
			else if (e.KeyCode == Keys.PageUp) {
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

		}

		public void OnPreRender(IGraphics g, WorldTransform transform) {
			
		}

		public void OnPostRender(IGraphics g, WorldTransform transform) {
			
		}
	}
}
