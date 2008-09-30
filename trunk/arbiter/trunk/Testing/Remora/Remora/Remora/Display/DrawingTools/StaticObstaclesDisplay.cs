using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Common.Sensors.Obstacle;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using System.Drawing;

namespace Remora.Display
{
	public class StaticObstaclesDisplay : IDisplayObject
	{
		private ObservedObstacles staticObstacles;
		private VehicleState vehicleState;

		public StaticObstaclesDisplay(ObservedObstacles staticObstacles, VehicleState vehicleState)
		{
			this.staticObstacles = staticObstacles;
			this.vehicleState = vehicleState;
		}

		#region IDisplayObject Members

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			if (staticObstacles.Obstacles != null)
			{
				foreach (ObservedObstacle obstacle in staticObstacles.Obstacles)
				{
					// get front of the vehicle
					Coordinates vPos = vehicleState.xyPosition;
					Coordinates vHead = vehicleState.heading;

					// get imu position of vehicle
					Coordinates imuPos = vPos + vHead.Normalize(0.4);

					// get obstacle relative vector
					Coordinates obstacleRelative = obstacle.ObstacleVector;

					// get final vector
					Coordinates final = imuPos + obstacleRelative.Rotate(vHead.ArcTan);
					
					// draw the obstacle
					DrawingUtility.DrawControlPoint(final, DrawingUtility.pointObstacleColor, null, System.Drawing.ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
					
				}
			}
		}

		#endregion
	}
}
