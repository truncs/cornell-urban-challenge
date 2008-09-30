using System;
using System.Collections.Generic;
using System.Text;
// stuff shits
using UrbanChallenge.Simulator.Client.World;
using UrbanChallenge.Common.Shapes;
using Simulator.Engine.Obstacles;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Simulator.Client
{
	/// <summary>
	/// Entry point for the simulation client
	/// </summary>
	public class Program
	{
		static void Main(string[] args)
		{
			SimSensor sensor = new SimSensor(Math.PI/4, -Math.PI/4, 1*Math.PI/180, SensorType.Scan, 30);
			SimObstacleState obsState = new SimObstacleState();
			obsState.Position = new Coordinates(5, 5);
			obsState.Length = 4;
			obsState.Width = 2;
			obsState.Heading = new Coordinates(1, 0);
			Polygon[] poly = new Polygon[] { obsState.ToPolygon() };
			SceneEstimatorUntrackedClusterCollection clusters = new SceneEstimatorUntrackedClusterCollection();
			sensor.GetHits(poly, Coordinates.Zero, 0, clusters);

			SimulatorClient client = new SimulatorClient();
			client.BeginClient();
		}
	}
}
