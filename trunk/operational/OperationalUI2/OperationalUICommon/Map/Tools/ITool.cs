using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

namespace UrbanChallenge.OperationalUI.Common.Map.Tools {
	public interface ITool {
		void OnActivate(IDrawingSurface drawingService);
		void OnDeactivate(IDrawingSurface drawingService);

		void OnMouseDown(MouseEventArgs e);
		void OnMouseMove(MouseEventArgs e);
		void OnMouseUp(MouseEventArgs e);

		void OnKeyDown(KeyEventArgs e);
		void OnKeyPress(KeyPressEventArgs e);
		void OnKeyUp(KeyEventArgs e);

		void SetupTransform(WorldTransform transform);
		void OnPreRender(IGraphics g, WorldTransform transform);
		void OnPostRender(IGraphics g, WorldTransform transform);
	}
}
