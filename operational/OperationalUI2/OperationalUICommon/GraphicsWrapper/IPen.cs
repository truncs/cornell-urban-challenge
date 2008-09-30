using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UrbanChallenge.OperationalUI.Common.GraphicsWrapper {
	public interface IPen : IDisposable {
		Color Color { get; set; }
		float Width { get; set; }
		DashStyle DashStyle { get; set; }
	}
}
