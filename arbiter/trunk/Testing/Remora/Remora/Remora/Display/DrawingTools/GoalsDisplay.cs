using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace Remora.Display
{
	/// <summary>
	/// displays goals
	/// </summary>
	public class GoalsDisplay : IDisplayObject
	{
		private RndfNetwork rndf;
		public RndfWaypointID Current;
		public Queue<RndfWaypointID> GoalsRemaining;

		public GoalsDisplay(RndfNetwork rndf, RndfWaypointID current, Queue<RndfWaypointID> remaining)
		{
			this.rndf = rndf;
			this.Current = current;
			this.GoalsRemaining = remaining;
		}

		#region IDisplayObject Members

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			if(Current != null && rndf != null && DrawingUtility.DisplayCurrentGoal)
			{
				// draw goal
				DrawingUtility.DrawControlPoint(rndf.Waypoints[Current].Position, DrawingUtility.CurrentGoalColor, 
					"Goal", System.Drawing.ContentAlignment.TopCenter, ControlPointStyle.SmallCircle, g, t);
			}
		}

		#endregion
	}
}
