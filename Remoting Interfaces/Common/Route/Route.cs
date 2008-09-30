using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Route;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Route
{
	/// <summary>
	/// Route information
	/// </summary>
	public class Route
	{
		private FullRoute expandedRoute;
		private LocalRoute localRoute;
		private Goal goal;
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="expandedRoute"></param>
		/// <param name="localRoute"></param>
		public Route(FullRoute expandedRoute, LocalRoute localRoute, Goal goal)
		{
			this.expandedRoute = expandedRoute;
			this.localRoute = localRoute;
			this.goal = goal;
		}

		/// <summary>
		/// Goal we are heading towards
		/// </summary>
		public Goal Goal
		{
			get { return goal; }
			set { goal = value; }
		}

		/// <summary>
		/// Local Route to Follow
		/// </summary>
		public LocalRoute LocalRoute
		{
			get { return localRoute; }
			set { localRoute = value; }
		}

		/// <summary>
		/// Expanded Route for debugging information
		/// </summary>
		public FullRoute ExpandedRoute
		{
			get { return expandedRoute; }
			set { expandedRoute = value; }
		}
		
	}
}
