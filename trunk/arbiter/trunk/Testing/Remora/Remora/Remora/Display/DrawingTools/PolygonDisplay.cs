using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using System.Drawing;

namespace Remora.Display
{
	/// <summary>
	/// Displays a polygon
	/// </summary>
	public class PolygonDisplay : IDisplayObject
	{
		public Polygon Poly;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="poly"></param>
		public PolygonDisplay(Polygon poly)
		{
			this.Poly = poly;
		}

		#region IDisplayObject Members

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			for (int i = 0; i < Poly.points.Count; i++)
			{
				if (i == Poly.points.Count - 1)
				{
					DrawingUtility.DrawColoredControlLine(Color.Orange, Poly.points[i], Poly.points[0], g, t);
				}
				else
				{
					DrawingUtility.DrawColoredControlLine(Color.Orange, Poly.points[i], Poly.points[i + 1], g, t);
				}
			}
		}

		#endregion
	}
}
