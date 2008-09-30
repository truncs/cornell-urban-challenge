using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.OperationalUI.Common.Map.Tools;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public interface IDrawingSurface {
		ISelectable SelectedObject { get; set; }

		HitTestResult HitTest(Coordinates point, double tolerance, HitTestFilter filter);
		ITool CurrentTool { get; set; }

		WorldTransform Transform { get; }

		Control GetControl();
		IGraphics GetGraphics();
		void Draw();

		void ZoomDelta(int steps);

		void Clear();
	}
}
