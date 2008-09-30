using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace UrbanChallenge.OperationalUI.Common.GraphicsWrapper {
	internal class GLPen : IPen {
		private Color color;
		private float width;
		private DashStyle dashStyle;

		internal GLPen() {}

		#region IPen Members

		public Color Color {
			get { return color; }
			set { color = value; }
		}

		public float Width {
			get { return width; }
			set { width = value; }
		}

		public DashStyle DashStyle {
			get { return dashStyle; }
			set { dashStyle = value; }
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			// nothing to do
		}

		#endregion
	}
}
