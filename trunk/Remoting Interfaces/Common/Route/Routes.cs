using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Route
{
	/// <summary>
	/// Holds localRoutes that we can possibly take from our current position
	/// </summary>
	[Serializable]
	public class Routes
	{
		private List<LocalRoute> localRoutes;

		/// <summary>
		/// Default Constructor
		/// </summary>
		public Routes()
		{
			this.localRoutes = new List<LocalRoute>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="localRoutes">Options describing where we can go for routes</param>
		public Routes(List<LocalRoute> localRoutes)
		{
			this.localRoutes = localRoutes;
		}

		/// <summary>
		/// Options describing where we can go for routes
		/// </summary>
		public List<LocalRoute> LocalRoutes
		{
			get { return localRoutes; }
			set { localRoutes = value; }
		}
	}
}
