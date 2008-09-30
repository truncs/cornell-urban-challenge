using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;
using UrbanChallenge.Common;

namespace UrbanChallenge.OperationalUI.Common.Map.Tools {
	public class ZoomTool : ITool {
		private Coordinates startPoint, endPoint;
		private bool inZoom;

		private DragScreenHelper dragHelper;
		private IDrawingSurface drawingSurface;

		public void OnActivate(IDrawingSurface drawingService) {
			this.drawingSurface = drawingService;
			this.dragHelper = null;
			this.inZoom = false;
		}

		public void OnDeactivate(IDrawingSurface drawingService) {
			this.drawingSurface = null;
			this.dragHelper = null;
			this.inZoom = false;
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
			else {
				endPoint = startPoint = worldPoint;
				inZoom = true;
			}
		}

		public void OnMouseMove(MouseEventArgs e) {
			if (dragHelper != null) {
				dragHelper.InMove(e.Location);
			}
			else if (inZoom) {
				// set the end point
				endPoint = drawingSurface.Transform.GetWorldPoint(e.Location);
			}
		}

		public void OnMouseUp(MouseEventArgs e) {
			if (dragHelper != null) {
				dragHelper.InMove(e.Location);
				dragHelper = null;
			}
			else if (inZoom && e.Button == MouseButtons.Left) {
				SetZoom(startPoint, drawingSurface.Transform.GetWorldPoint(e.Location));
				inZoom = false;
			}
		}

		public void OnKeyDown(KeyEventArgs e) {
			if (e.KeyCode == Keys.Escape) {
				if (dragHelper != null) {
					dragHelper.CancelMove();
				}
				else if (inZoom) {
					inZoom = false;
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
			if (inZoom) {
				DrawZoomBox(g, transform, startPoint, endPoint);
			}
		}

		private void SetZoom(Coordinates start, Coordinates end) {
			// get the current window size
			SizeF windowSize = drawingSurface.Transform.ScreenSize;

			// figure out the tighter zoom
			double deltaX = Math.Abs(end.X - start.X);
			double deltaY = Math.Abs(end.Y - start.Y);

			// zoom is pixels per meter, so get the pixels per meter that we want
			double zoomHeight = windowSize.Height/deltaY;
			double zoomWidth = windowSize.Width /deltaX;

			// take the smaller of the two zooms
			float newZoom = (float)Math.Min(zoomHeight, zoomWidth);

			if (newZoom > 10000)
				return;

			// calculate the new center point
			Coordinates centerPoint = (start+end)/2.0;

			// update the transform
			drawingSurface.Transform.CenterPoint = centerPoint;
			drawingSurface.Transform.Scale = newZoom;
		}

		private void DrawZoomBox(IGraphics g, WorldTransform transform, Coordinates start, Coordinates end) {
			float x, y;
			float width, height;

			if (start.X < end.X) {
				x = (float)start.X;
				width = (float)(end.X - start.X);
			}
			else {
				x = (float)end.X;
				width = (float)(start.X - end.X);
			}

			if (start.Y < end.Y) {
				y = (float)start.Y;
				height = (float)(end.Y - start.Y);
			}
			else {
				y = (float)end.Y;
				height = (float)(start.Y - end.Y);
			}

			// create the rectangle
			RectangleF rect = new RectangleF(x, y, width, height);

			// draw the transparent background
			g.FillRectangle(Color.FromArgb(50, Color.Purple), rect);

			IPen pen = g.CreatePen();
			pen.Width = 1.0f/transform.Scale;
			pen.Color = Color.Purple;

			g.DrawRectangle(pen, rect);

			pen.Dispose();
		}
	}
}
