using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Route;

namespace Remora.Display
{
	public class RouteDisplay : IDisplayObject
	{
		RndfNetwork rndf;
		FullRoute route;

		public RouteDisplay(RndfNetwork rndf, FullRoute route)
		{
			this.rndf = rndf;
			this.route = route;
		}

		#region IDisplayObject Members

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			if (DrawingUtility.DisplayFullRoute)
			{
				for (int i = 0; i < route.RouteNodes.Count - 1; i++)
				{
					RndfWayPoint initial = rndf.Waypoints[route.RouteNodes[i]];
					RndfWayPoint final = rndf.Waypoints[route.RouteNodes[i + 1]];
					DrawingUtility.DrawControlLine(initial.Position, final.Position, DrawingUtility.RouteColor, g, t);
				}
			}
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (obj is IDisplayObject)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
