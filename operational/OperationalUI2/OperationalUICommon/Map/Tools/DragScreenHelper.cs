using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Drawing;

namespace UrbanChallenge.OperationalUI.Common.Map.Tools {
	public class DragScreenHelper {
		private IDrawingSurface drawingSurface;
		private Coordinates moveOrigin;
		private Coordinates originalCenter;
		private WorldTransform originalTransform;

		public DragScreenHelper(IDrawingSurface drawingSurface, Coordinates worldPoint) {
			this.drawingSurface = drawingSurface;
			this.originalTransform = drawingSurface.Transform.Clone();
			this.originalCenter = originalTransform.CenterPoint;
			this.moveOrigin = worldPoint + originalCenter;
		}

		public void InMove(PointF screenPoint) {
			// get the world point in the original transformation
			Coordinates origWorldPoint = originalTransform.GetWorldPoint(screenPoint);
			// adjust the current transformation 
			drawingSurface.Transform.CenterPoint = moveOrigin - origWorldPoint;
		}

		public void CancelMove() {
			drawingSurface.Transform.CenterPoint = originalCenter;
		}
	}
}
