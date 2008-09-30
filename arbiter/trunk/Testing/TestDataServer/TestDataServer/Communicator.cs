using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Remoting;
using UrbanChallenge.NameService;

using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Vehicle;


using UrbanChallenge.Behaviors;
using UrbanChallenge.MessagingService;
using System.Threading;

namespace UrbanChallenge.Arbiter.TestDataServer
{
	public class Communicator : OperationalTestFacade
	{
		// ********** Remoting Communications *********** //
		ObjectDirectory objectDirectory;
		MessagingListener channelListener;

		// ********** Messaging Channels **************** //
		private IChannel rndfChannel;		// Listen for the Rndf
		private IChannel mdfChannel;		// Listen for the Mdf
		private IChannel positionChannel;	// Listen for Position Updates
		private IChannel testStringChannel;
		uint rndfToken;		// rndf listener
		uint mdfToken;		// mdf listener
		uint positionToken; // position listener
		uint testStringToken;

		// *********** Operational Facade ************** //
		private VehicleState vehicleState;

		/// <summary>
		/// Constructor
		/// </summary>		
		public Communicator()
		{
			// Initialize Remoting Communications
			this.InitializeRemotingCommunications();
		}

		#region Initialization

		/// <summary>
		/// Initializes communications with the outside world by means of remoting
		/// </summary>
		private void InitializeRemotingCommunications()
		{
			// Read the configuration file.
			RemotingConfiguration.Configure("..\\..\\App.config", false);
			WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();

			// "Activate" the NameService singleton.
			objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

			// Bind the operational test faceade
			objectDirectory.Rebind(this, "OperationalTestFacade");			

			// Retreive the directory of messaging channels
			IChannelFactory channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");
			
			// Retreive the Messaging Service channels we want to listen to
			rndfChannel = channelFactory.GetChannel("RndfNetworkChannel", ChannelMode.Bytestream);
			mdfChannel = channelFactory.GetChannel("MdfChannel", ChannelMode.Bytestream);
			positionChannel = channelFactory.GetChannel("PositionChannel", ChannelMode.Bytestream);
			testStringChannel = channelFactory.GetChannel("TestStringChannel", ChannelMode.Vanilla);
			
			// Create a channel listeners and listen on wanted channels
			channelListener = new MessagingListener();
			rndfToken = rndfChannel.Subscribe(channelListener);
			mdfToken = mdfChannel.Subscribe(channelListener);
			//positionToken = positionChannel.Subscribe(channelListener);
			testStringToken = testStringChannel.Subscribe(new StringListener());
			
			// Show that the navigation system is ready and waiting for input
			Console.WriteLine("   > Remoting Communication Initialized");
		}	

		#endregion

		#region Publishing

		/// <summary>
		/// Publish the vehicle state to the service
		/// </summary>
		/// <param name="vehicleState"></param>
		public void PublishVehicleState(VehicleState vehicleState)
		{
			Console.WriteLine("   > Position Sent: " + vehicleState.ToString());

			// publish the serialized state
			positionChannel.PublishUnreliably(vehicleState);
		}

		public void PublishTestString(string message)
		{
			testStringChannel.PublishUnreliably(message);
		}

		#endregion

		#region Wait, Block for Data

		/// <summary>
		/// Makes sure rndf and mdf have been received before the arbiter is started
		/// </summary>
		public void InitializeTestServer()
		{
			// repetitively listen for the mdf and rndf to arrive, block until it does
			while ((channelListener).Mdf == null || (channelListener).RndfNetwork == null)
			{
				Thread.Sleep(1000);
			}
		}

		#endregion

		#region Fields

		/// <summary>
		/// Gets the most recently received rndf
		/// </summary>
		public Mdf Mdf
		{
			get { return channelListener.Mdf; }
		}

		/// <summary>
		/// Gets the most recently received mdf
		/// </summary>
		public RndfNetwork RndfNetwork
		{
			get { return channelListener.RndfNetwork; }
		}

		/// <summary>
		/// Gets the most recently received vehicle state
		/// </summary>
		public VehicleState VehicleState
		{
			get { return channelListener.Vehicle; }
		}

		#endregion

		public void ShutDown()
		{
			rndfChannel.Unsubscribe(rndfToken);
			mdfChannel.Unsubscribe(mdfToken);
			//positionChannel.Unsubscribe(positionToken);
			testStringChannel.Unsubscribe(testStringToken);
		}

		public override void Echo(string echoString)
		{
			Console.WriteLine("Echo: " + echoString);
		}

		public override void ExecuteBehavior(Behavior behavior, Common.Coordinates location, RndfWaypointID lowerBound, RndfWaypointID upperBound)
		{
			if(behavior is UTurnBehavior)
			{
				//
			}
			
			// check to see if we are at the upper bound
			RndfWayPoint upperWaypoint = channelListener.RndfNetwork.Waypoints[upperBound];
			if (!(behavior is TurnBehavior) && upperWaypoint.NextLanePartition != null && location.DistanceTo(upperWaypoint.Position) < 3)
			{
				lowerBound = upperWaypoint.WaypointID;
				upperBound = upperWaypoint.NextLanePartition.FinalWaypoint.WaypointID;
			}

			Console.WriteLine("   > Received Instruction to Execute Behavior: " + behavior.ToString());
			if (behavior is StayInLaneBehavior)
			{
				StayInLaneBehavior stayInLane = (StayInLaneBehavior)behavior;

				if (stayInLane.SpeedCommand is StopLineLaneSpeedCommand)
				{
					StopLineLaneSpeedCommand speedCommand = (StopLineLaneSpeedCommand)stayInLane.SpeedCommand;

					if (speedCommand.Distance < 2)
					{
						// Create a fake vehicle state
						VehicleState vehicleState = new VehicleState();
						vehicleState.xyPosition = location;
						LaneEstimate laneEstimate = new LaneEstimate(lowerBound.LaneID, lowerBound.LaneID, 1);
						List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
						laneEstimates.Add(laneEstimate);
						vehicleState.speed = 0;
						vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
						this.PublishVehicleState(vehicleState);
						///Console.WriteLine("  > Published Position");
					}
					else
					{
						// Create a fake vehicle state
						VehicleState vehicleState = new VehicleState();
						vehicleState.xyPosition = channelListener.RndfNetwork.Waypoints[upperBound].Position;
						LaneEstimate laneEstimate = new LaneEstimate(lowerBound.LaneID, lowerBound.LaneID, 1);
						List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
						laneEstimates.Add(laneEstimate);
						vehicleState.speed = 3;
						vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
						this.PublishVehicleState(vehicleState);
						///Console.WriteLine("  > Published Position");
					}
				}
				else if (stayInLane.SpeedCommand is StopLaneSpeedCommand)
				{
					// Create a fake vehicle state
					VehicleState vehicleState = new VehicleState();
					vehicleState.xyPosition = location;
					LaneEstimate laneEstimate = new LaneEstimate(lowerBound.LaneID, lowerBound.LaneID, 1);
					List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
					laneEstimates.Add(laneEstimate);
					vehicleState.speed = -5;
					vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
					this.PublishVehicleState(vehicleState);
					///Console.WriteLine("  > Published Position");
				}
				else if(stayInLane.SpeedCommand is DefaultLaneSpeedCommand)
				{
					// Create a fake vehicle state
					VehicleState vehicleState = new VehicleState();
					vehicleState.xyPosition = channelListener.RndfNetwork.Waypoints[upperBound].Position;
					LaneEstimate laneEstimate = new LaneEstimate(lowerBound.LaneID, lowerBound.LaneID, 1);
					List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
					laneEstimates.Add(laneEstimate);
					vehicleState.speed = 3;
					vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
					this.PublishVehicleState(vehicleState);
					//Console.WriteLine("  > Published Position");
				}
				else
				{
					throw new ArgumentException("Unknown Lane Speed Type", "stayInLane.SpeedCommand");
				}
			}
			// TODO: include midway point
			else if (behavior is TurnBehavior)
			{
				TurnBehavior currentBehavior = (TurnBehavior)behavior;


				RndfWayPoint exitWaypoint = channelListener.RndfNetwork.Waypoints[currentBehavior.ExitPoint];
				if (location.DistanceTo(exitWaypoint.Position) < 0.1)
				{
					// Create a fake vehicle state
					VehicleState vehicleState = new VehicleState();
					
					RndfWayPoint entryWaypoint = channelListener.RndfNetwork.Waypoints[currentBehavior.EntryPoint];
					Common.Coordinates change = entryWaypoint.Position - exitWaypoint.Position;
					Common.Coordinates midpoint = exitWaypoint.Position + change/2;

					LaneEstimate laneEstimate = new LaneEstimate(currentBehavior.ExitPoint.LaneID, currentBehavior.EntryPoint.LaneID, 1);

					vehicleState.xyPosition = midpoint;
					List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
					laneEstimates.Add(laneEstimate);
					vehicleState.speed = 3;
					vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
					this.PublishVehicleState(vehicleState);
				}
				else
				{
					// Create a fake vehicle state
					VehicleState vehicleState = new VehicleState();
					vehicleState.xyPosition = channelListener.RndfNetwork.Waypoints[currentBehavior.EntryPoint].Position;
					LaneEstimate laneEstimate = new LaneEstimate(currentBehavior.EntryPoint.LaneID, currentBehavior.EntryPoint.LaneID, 1);
					List<LaneEstimate> laneEstimates = new List<LaneEstimate>();
					laneEstimates.Add(laneEstimate);
					vehicleState.speed = 3;
					vehicleState.vehicleRndfState = new VehicleRndfState(laneEstimates);
					this.PublishVehicleState(vehicleState);
					//Console.WriteLine("  > Published Position");
				}
			}
			else
			{
				throw new ArgumentException("Unknown Behavior Type", "behavior");
			}

			//Console.WriteLine("Sent Back Position \n");
		}
	}
}
