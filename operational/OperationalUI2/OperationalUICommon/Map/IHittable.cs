using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Drawing;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public interface IHittable : IRenderable {
		RectangleF GetBoundingBox();
		HitTestResult HitTest(Coordinates loc, float tol);

		IHittable Parent { get; }
	}
}
