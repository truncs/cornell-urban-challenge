using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Route
{
	/// <summary>
	/// Represents a point of interest
	/// </summary>
	public struct PointOfInterest
	{
		/// <summary>
		/// Position of the point
		/// </summary>
		public Coordinates Position;

		/// <summary>
		/// Message associated with this point
		/// </summary>
		public string Message;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Position"></param>
		/// <param name="Message"></param>
		public PointOfInterest(Coordinates Position, string Message)
		{
			this.Position = Position;
			this.Message = Message;
		}
	}
}
