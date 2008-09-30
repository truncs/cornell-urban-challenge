using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Windows.Forms;
using System.Drawing;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

namespace UrbanChallenge.OperationalUI.Common.Map.Tools {
	public class RulerTool : ITool {
		private Coordinates startPoint, endPoint;
		private bool inRuler;

		private DragScreenHelper dragHelper;
		private IDrawingSurface drawingSurface;

		public void OnActivate(IDrawingSurface drawingService) {
			this.drawingSurface = drawingService;
			this.dragHelper = null;
			this.inRuler = false;
		}

		public void OnDeactivate(IDrawingSurface drawingService) {
			this.drawingSurface = null;
			this.dragHelper = null;
			this.inRuler = false;
		}

		public void OnMouseDown(MouseEventArgs e) {
			// get the world point of the mouse hit
			Coordinates worldPoint = drawingSurface.Transform.GetWorldPoint(e.Location);

			if (e.Button != MouseButtons.Left)
				return;

			// check if the control key is down
			if ((Control.ModifierKeys & Keys.Control) != Keys.None) {
				// user wants to do a drag
				dragHelper = new DragScreenHelper(drawingSurface, worldPoint);
			}
			else if ((Control.ModifierKeys & Keys.Shift) != Keys.None) {
				// do a hit test to get a snap point
				// calculate hit tolerance in world units
				double tolerance = MapSettings.PixelHitTolerance / drawingSurface.Transform.Scale;

				// check for only selectable objects
				HitTestResult hitResult = drawingSurface.HitTest(worldPoint, tolerance, HitTestFilters.HasSnap);

				if (hitResult.Hit) {
					// we have a snap point
					if (inRuler) {
						endPoint = hitResult.SnapPoint;
					}
					else {
						endPoint = startPoint = hitResult.SnapPoint;
					}
				}
				else {
					// no snap point
					if (inRuler) {
						endPoint = worldPoint;
					}
					else {
						endPoint = startPoint = worldPoint;
					}
				}
				inRuler = true;
			}
			else {
				if (inRuler) {
					endPoint = worldPoint;
				}
				else {
					endPoint = startPoint = worldPoint;
				}
				inRuler = true;
			}
		}

		public void OnMouseMove(MouseEventArgs e) {
			if (dragHelper != null) {
				dragHelper.InMove(e.Location);
			}
			else if (inRuler) {
				Coordinates worldPoint = drawingSurface.Transform.GetWorldPoint(e.Location);

				// check if the user is holding down the left mouse button
				if ((Control.ModifierKeys & Keys.Shift) != Keys.None) {
					// do a hit test to get a snap point
					// calculate hit tolerance in world units
					double tolerance = MapSettings.PixelHitTolerance / drawingSurface.Transform.Scale;

					// check for only selectable objects
					HitTestResult hitResult = drawingSurface.HitTest(worldPoint, tolerance, HitTestFilters.HasSnap);

					if (hitResult.Hit) {
						// we have a snap point
						endPoint = hitResult.SnapPoint;
					}
					else {
						// no snap, just use the world point
						endPoint = worldPoint;
					}
				}
				else {
					// not holding down the mouse button, just use the world point
					endPoint = worldPoint;
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
		}

		public void OnKeyDown(KeyEventArgs e) {
			if (e.KeyCode == Keys.Escape) {
				if (dragHelper != null) {
					dragHelper.CancelMove();
				}
				else if (inRuler) {
					inRuler = false;
				}
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
			if (inRuler) {
				DrawRuler(g, transform, startPoint, endPoint);
			}
		}

		private void DrawRuler(IGraphics g, WorldTransform transform, Coordinates start, Coordinates end) {
			// determine what alignment to use

			Coordinates diff = end - start;
			ContentAlignment align;
			if (diff.X > 0 && diff.Y > 0) {
				align = ContentAlignment.MiddleRight;
			}
			else if (diff.X < 0 && diff.Y > 0) {
				align = ContentAlignment.MiddleLeft;
			}
			else if (diff.X < 0 && diff.Y < 0) {
				align = ContentAlignment.MiddleLeft;
			}
			else if (diff.X > 0 && diff.Y < 0) {
				align = ContentAlignment.MiddleRight;
			}
			else if (diff.X == 0 && diff.Y > 0) {
				align = ContentAlignment.MiddleRight;
			}
			else if (diff.X == 0 && diff.Y < 0) {
				align = ContentAlignment.MiddleRight;
			}
			else if (diff.X < 0 && diff.Y == 0) {
				align = ContentAlignment.MiddleLeft;
			}
			else if (diff.X > 0 && diff.Y == 0) {
				align = ContentAlignment.MiddleRight;
			}
			else {
				align = ContentAlignment.MiddleRight;
			}

			double dist = diff.VectorLength;

			string label = dist.ToString("F2") + " m, (" + end.X.ToString("F1") + "," + end.Y.ToString("F1") + ")";

			using (IPen pen = g.CreatePen()) {
				pen.Color = Color.DarkRed;
				pen.Width = 1.5f / transform.Scale;
				g.DrawLine(pen, Utility.ToPointF(start), Utility.ToPointF(end));
			}
			
			DrawingUtility.DrawControlPoint(g, start, Color.DarkRed, null, ContentAlignment.BottomRight, ControlPointStyle.LargeCircle, transform);
			DrawingUtility.DrawControlPoint(g, end, Color.DarkRed, label, align, ControlPointStyle.LargeCircle, transform);
		}
	}
}
