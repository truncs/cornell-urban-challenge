using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Remoting;
using UrbanChallenge.NameService;
using UrbanChallenge.RndfEditor.Remote;

using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Vehicle;

using System.Threading;

namespace UrbanChallenge.Arbiter.TestDataServer
{
	class Program
	{
		static void Main(string[] args)
		{
			// Initialize Communications
			Console.WriteLine("1. Initializing Communications...");
			Communicator communicator = new Communicator();
			Console.WriteLine("   > Communications Initialized\n");

			// Wait for Rndf,Mdf to continue
			Console.WriteLine("2. Waiting for Rndf, Mdf to continue...");
			communicator.InitializeTestServer();
			Console.WriteLine("   > Rndf, Mdf Received\n");

			// Wait for User continue
			Console.WriteLine("3. Ready To Start Testing, Press ENTER to Continue...");
			Console.ReadLine();

			// Wait for 1 second
			Thread.Sleep(1000);

			/*
			// Turn => Stopped at Stop
			// Create a fake vehicle state
			RndfWayPoint position = communicator.RndfNetwork.Goals[1].Waypoint;
			Console.WriteLine("  > Position: " + position.WaypointID.ToString());
			VehicleState vehicleState = new VehicleState();
			vehicleState.xyPosition = position.Position;
			LaneEstimate laneEstimate = new LaneEstimate(position.Lane.LaneID, position.Lane.LaneID, 1);
			List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
			laneEstimates.Add(laneEstimate);
			vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);			
			communicator.PublishVehicleState(vehicleState);
			Console.WriteLine("  > Published Position");*/

			/*
			// Road
			// Create a fake vehicle state
			RndfWaypointID tmpID = new RndfWaypointID(new LaneID(new WayID(new SegmentID(1), 1), 1), 1);
			RndfWayPoint position = communicator.RndfNetwork.Waypoints[tmpID];
			Console.WriteLine("  > Position: " + position.WaypointID.ToString());
			VehicleState vehicleState = new VehicleState();
			vehicleState.xyPosition = position.Position;
			LaneEstimate laneEstimate = new LaneEstimate(position.Lane.LaneID, position.Lane.LaneID, 1);
			List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
			laneEstimates.Add(laneEstimate);
			vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
			vehicleState.speed = 3;
			communicator.PublishVehicleState(vehicleState);
			Console.WriteLine("  > Published Position");*/

			/*
			// Intersection
			// Create a fake vehicle state
			RndfWaypointID tmpID = new RndfWaypointID(new LaneID(new WayID(new SegmentID(1), 1), 1), 2);
			RndfWaypointID tmpEndID = new RndfWaypointID(new LaneID(new WayID(new SegmentID(26), 2), 3), 1);
			InterconnectID testID = new InterconnectID(tmpID, tmpEndID);
			Interconnect testInter = communicator.RndfNetwork.Interconnects[testID];
			RndfWayPoint position = communicator.RndfNetwork.Waypoints[tmpID];
			VehicleState vehicleState = new VehicleState();
			vehicleState.xyPosition = position.Position + new UrbanChallenge.Common.Coordinates(4, 4);
			LaneEstimate laneEstimate = new LaneEstimate(tmpID.LaneID, tmpEndID.LaneID, 1);
			List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
			laneEstimates.Add(laneEstimate);
			vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
			vehicleState.speed = 3;
			communicator.PublishVehicleState(vehicleState);
			Console.WriteLine("  > Published Position");
			 */

			// In Lane
			// Create a fake vehicle state
			RndfWayPoint position = communicator.RndfNetwork.Goals[2].Waypoint;
			Console.WriteLine("  > Position: " + position.WaypointID.ToString());
			VehicleState vehicleState = new VehicleState();
			vehicleState.xyPosition = position.Position;
			LaneEstimate laneEstimate = new LaneEstimate(position.Lane.LaneID, position.Lane.LaneID, 1);
			List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
			laneEstimates.Add(laneEstimate);
			vehicleState.speed = 0;
			vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);			
			communicator.PublishVehicleState(vehicleState);
			//Console.WriteLine("  > Published Position");



			/*// Read the configuration file.
			RemotingConfiguration.Configure("..\\..\\App.config", false);
			WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();

			// "Activate" the NameService singleton.
			ObjectDirectory objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

			// Receive the facades of components we want to use
			RndfEditorFacade editorFacade = (RndfEditorFacade)objectDirectory.Resolve("RndfEditor");			
			
			// Get the RndfNetwork
			RndfNetwork rndfNetwork = editorFacade.RndfNetwork;

			// Create the Goals from the RndfNetwork
			Queue<Goal> goals = generateGoals(rndfNetwork);
			Console.WriteLine("Created Goals");

			// Create Speed Limits from the RndfNetwork
			List<SpeedInformation> speedLimits = generateSpeedLimits(rndfNetwork);
			Console.WriteLine("Created speed limits");

			// Create Mdf
			Mdf mdf = new Mdf(goals, speedLimits);
			Console.WriteLine("Created Mdf");

			// Create a fake vehicle state
			RndfWayPoint position = rndfNetwork.Goals[1].Waypoint;
			Console.WriteLine("Position: " + position.WaypointID.ToString());
			VehicleState vehicleState = new VehicleState();
			vehicleState.xyPosition = position.Position;
			LaneEstimate laneEstimate = new LaneEstimate(position.Lane.LaneID, position.Lane.LaneID, 1);
			List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
			laneEstimates.Add(laneEstimate);
			vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);

			// Test
			Console.WriteLine("Number of RndfNetwork Segments: " + rndfNetwork.Segments.Values.Count);

			// Bind the facades of components we implement.
			objectDirectory.Rebind(TestDataServerFacadeImpl.Instance(rndfNetwork, mdf, vehicleState), "TestServer");*/

			// Show that the navigation system is ready and waiting for input
			Console.WriteLine("");
			Console.WriteLine("4. Test Serve Complete, Waiting. Press ENTER to Shut Down");
			Console.ReadLine();
			Console.WriteLine("");
			communicator.ShutDown();
		}

		/// <summary>
		/// Generates some goals from an Rndf
		/// </summary>
		/// <param name="rndfNetwork"></param>
		/// <returns></returns>
		private static Queue<Goal> generateGoals(RndfNetwork rndfNetwork)
		{
			Queue<Goal> goals = new Queue<Goal>();

			try
			{
				// add goals in order of id
				for (int i = 1; i < 5; i++)
				{
					goals.Enqueue(rndfNetwork.Goals[i]);					
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				throw e;
			}

			return goals;
		}

		/// <summary>
		/// Generates speed limits for the rndf segments
		/// </summary>
		/// <param name="rndfNetwork"></param>
		private static List<SpeedInformation> generateSpeedLimits(RndfNetwork rndfNetwork)
		{
			List<SpeedInformation> speedLimits = new List<SpeedInformation>();

			foreach(Segment segment in rndfNetwork.Segments.Values)
			{
				SpeedInformation speedLimit = new SpeedInformation(segment.SegmentID, 0, 8.8);
				speedLimits.Add(speedLimit);
			}

			return speedLimits;
		}
	}
}
