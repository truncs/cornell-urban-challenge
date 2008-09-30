using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;

namespace Simulator.Engine.Obstacles
{
	/// <summary>
	/// Sim Obstacle State stuff
	/// </summary>
	[Serializable]
	public class SimObstacleState
	{
		/// <summary>
		/// WIdth of obstacle
		/// </summary>
		public double Width;

		/// <summary>
		/// Length of obstacle
		/// </summary>
		public double Length;

		/// <summary>
		/// Blockages
		/// </summary>
		public bool IsBlockage;

		/// <summary>
		/// Heading of obstacle
		/// </summary>
		public Coordinates Heading;

		/// <summary>
		/// Position of Obstacle
		/// </summary>
		public Coordinates Position;

		/// <summary>
		/// Id of the obstacle
		/// </summary>
		public SimObstacleId ObstacleId;

		public Polygon ToPolygon() {
			double halfWidth = Width/2;
			double halfLength = Length/2;

			Coordinates heading = Heading.Normalize();
			Coordinates heading90 = heading.Rotate90();

			Coordinates l = heading*halfLength;
			Coordinates w = heading90*halfWidth;

			Coordinates pt1 = Position - l - w;
			Coordinates pt2 = Position + l - w;
			Coordinates pt3 = Position + l + w;
			Coordinates pt4 = Position - l + w;

			Polygon poly = new Polygon(4);
			poly.Add(pt1);
			poly.Add(pt2);
			poly.Add(pt3);
			poly.Add(pt4);

			return poly;
		}
	}
}
