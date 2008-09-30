using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Simulator.Client.World;
using Simulator;
using UrbanChallenge.Simulator.Client.Communications;
using System.Windows.Forms;
using Simulator.Engine;
using UrbanChallenge.Simulator.Client.Vehicles;
using System.Runtime.Remoting.Lifetime;
using UrbanChallenge.Arbiter.Core.Remote;
using System.Diagnostics;
using UrbanChallenge.Common.Utility;
using System.Threading;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Simulator.Client
{
	/// <summary>
	/// The client of the simulator
	/// </summary>
	public class SimulatorClient : SimulatorClientFacade
	{
		public static string MachineName = null;
		private static DateTime startTime = HighResDateTime.Now;
		private static double sumDt = 0;

		static SimulatorClient() {
			MachineName = Properties.Settings.Default.MachineName;

			if (string.IsNullOrEmpty(MachineName)) {
				MachineName = Environment.MachineName;
			}
		}

		#region Private Members

		private WorldService world;		
		private Communicator communicator;
		private ArbiterRoadNetwork roadNetwork;
		private ArbiterMissionDescription mission;
		private bool stepMode = false;
		private double continuousModeRate = 1;
		private SortedList<int, ClientRunControlFacade> runControlClients = new SortedList<int, ClientRunControlFacade>();

		#endregion

		#region Public Members

		/// <summary>
		/// Facade of the simulation
		/// </summary>
		public SimulatorFacade SimulationServer;

		/// <summary>
		/// Client vehicle we are simulating
		/// </summary>
		public ClientVehicle ClientVehicle;

		/// <summary>
		/// Dynamics sim vehicle
		/// </summary>
		public DynamicsSimVehicle DynamicsVehicle;

		/// <summary>
		/// Arbtier for this client
		/// </summary>
		public ArbiterAdvancedRemote ClientArbiter;

		#endregion
		
		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		public SimulatorClient()
		{
			this.communicator = new Communicator(this);			
		}

		#endregion

		#region Client Facade Members

		/// <summary>
		/// Sets the mode of the car
		/// </summary>
		/// <param name="mode"></param>
		public override void SetCarMode(CarMode mode)
		{
			if (this.ClientArbiter != null)
			{
				try
				{
					this.DynamicsVehicle.SetRunMode(mode);
					Console.WriteLine("Set dynamics vehicle run mode to: " + mode.ToString());
				}
				catch (Exception e)
				{
					Console.WriteLine("Error setting dynamics vehicle run mode to: " + mode.ToString() + "\n" + e.ToString());
				}
			}
		}

		/// <summary>
		/// Sets the road network and mission for the car
		/// </summary>
		/// <param name="roadNetwork"></param>
		/// <param name="mission"></param>
		public override void SetRoadNetworkAndMission(ArbiterRoadNetwork roadNetwork, ArbiterMissionDescription mission)
		{			
			this.roadNetwork = roadNetwork;
			this.mission = mission;
			this.roadNetwork.SetSpeedLimits(mission.SpeedLimits);
			this.world = new WorldService(this.roadNetwork);
		}

		/// <summary>
		/// Returns name of the client
		/// </summary>
		/// <returns></returns>
		public override string Name()
		{
			return "SimulationClient_" + SimulatorClient.MachineName;
		}

		#endregion

		#region Command Line

		public void Command()
		{
			string command = "";

			while (command != "exit")
			{
				Console.Write("SimClient > ");
				command = Console.ReadLine();
				command = command.ToLower();

				switch (command)
				{
					case "initremoting":
					case "init remoting":
						this.communicator.Configure();

						break;

					case "registerclient":
					case "register client":
						this.communicator.Register();

						break;

					case "connecttosim":
					case "connect to sim":
						this.communicator.AttemptSimulationConnection();

						break;

					case "simplesim":
					case "simple sim":
						this.RunSimpleSim();
						Console.WriteLine("sim started");
						break;

					case "resetsim":
					case "reset sim":
						if (this.DynamicsVehicle != null) {
							this.DynamicsVehicle.Reset();
						}
						break;

					case "man":

						Console.WriteLine("InitRemoting");
						Console.WriteLine("RegisterClient");
						Console.WriteLine("ConnectToSim");
						Console.WriteLine("SimpleSim - runs an operational only simulation");
						Console.WriteLine("ResetSim - resets the operational sim");
						Console.WriteLine("");

						break;

					default:
						Console.WriteLine("unknown command");
						break;
				}
			}
		}

		#endregion

		/// <summary>
		/// Start the client
		/// </summary>
		public void BeginClient()
		{
			// begin startup thread
			this.communicator.Configure();
			this.communicator.Register();
			this.communicator.AttemptSimulationConnection();

			// maintenance
			this.communicator.RunMaintenance();

			// go into command mode
			this.Command();
		}

		/// <summary>
		/// allows for ping to determine if alive
		/// </summary>
		/// <returns></returns>
		public override bool Ping()
		{
			return true;
		}

		/// <summary>
		/// list view item of the client name
		/// </summary>
		/// <returns></returns>
		public override ListViewItem ViewableItem()
		{
			if (this.ClientVehicle == null)
			{
				ListViewItem lvi = new ListViewItem(new string[] { "-1", SimulatorClient.MachineName, this.Name() });
				lvi.Name = this.Name();
				return lvi;
			}
			else
			{
				ListViewItem lvi = new ListViewItem(new string[] { this.ClientVehicle.VehicleId.ToString(), SimulatorClient.MachineName, this.Name() });
				lvi.Name = this.Name();
				return lvi;
			}
		}

		/// <summary>
		/// kill the sim
		/// </summary>
		public override void Kill()
		{
			this.communicator.SimulatorLost();
		}

		/// <summary>
		/// Update the state of the client vehicle
		/// </summary>
		/// <param name="worldState"></param>
		/// <param name="dt"></param>
		public override SimVehicleState Update(WorldState worldState, double dt)
		{
			try
			{
				sumDt += dt;
				// update world
				this.world.UpdateWorld(worldState);

				// update value held in sim client
				this.ClientVehicle.Update(worldState);

				// set vehicle speed check
				double speedLock = this.ClientVehicle.CurrentState.Speed;

				// vehicle state
				VehicleState vs = world.VehicleStateFromSim(this.ClientVehicle.CurrentState);
				vs.Timestamp = GetCurrentTimestamp.ts;

				// update in ai and all listeners
				this.communicator.Update(
					vs,
					world.VehiclesFromWorld(this.ClientVehicle.CurrentState.VehicleID, vs.Timestamp),
					world.ObstaclesFromWorld(this.ClientVehicle.CurrentState.VehicleID, this.ClientVehicle.CurrentState.Position, this.ClientVehicle.CurrentState.Heading.ArcTan, vs.Timestamp),
					this.ClientVehicle.CurrentState.Speed,
					world.GetLocalRoadEstimate(this.ClientVehicle.CurrentState),
					world.GetPathRoadEstimate(this.ClientVehicle.CurrentState));

				// check if we're in step mode
				lock (runControlClients) {
					if (stepMode) {
						foreach (ClientRunControlFacade stepClient in runControlClients.Values) {
							stepClient.Step();
						}
					}
				}

				// save persistent info
				double maxSpeed = this.ClientVehicle.CurrentState.MaximumSpeed;
				bool useMaxSpeed = this.ClientVehicle.CurrentState.UseMaximumSpeed;
				Coordinates pos = this.ClientVehicle.CurrentState.Position;
				Coordinates heading = this.ClientVehicle.CurrentState.Heading;
				bool canMove = this.ClientVehicle.CurrentState.canMove;

				// get next state
				DynamicsVehicle.VehicleState = this.ClientVehicle.CurrentState;
				SimVehicleState updatedState = DynamicsVehicle.Update(dt, this.world);

				// modify with persistent info
				if (useMaxSpeed) {
					updatedState.Speed = maxSpeed;
				}
				if (!canMove) {
					updatedState.Position = pos;
					updatedState.Heading = heading;
				}

				// set state
				this.ClientVehicle.CurrentState = updatedState;

				if(this.ClientVehicle.CurrentState.LockSpeed)
					this.ClientVehicle.CurrentState.Speed = speedLock;

				// return updated to sim
				return updatedState;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error updating: \n" + e.ToString());
				return null;
			}
		}

		/// <summary>
		/// Sets the vehicle
		/// </summary>
		/// <param name="vehicleId"></param>
		public override void SetVehicle(SimVehicleId vehicleId)
		{
			if (vehicleId != null)
			{
				this.ClientVehicle = new ClientVehicle();
				this.ClientVehicle.VehicleId = vehicleId;
				Console.WriteLine(DateTime.Now.ToString() + ": Registered Vehicle with Vehicle Id: " + vehicleId.ToString());
			}
			else
			{
				if(this.ClientVehicle != null)
					Console.WriteLine(DateTime.Now.ToString() + ": Deregistered Vehicle with Vehicle Id: " + this.ClientVehicle.VehicleId.ToString());
				
				this.ClientVehicle = null;
			}
		}

		/// <summary>
		/// Initialize lifetime service
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		/// <summary>
		/// startup the vehicle
		/// </summary>
		/// <returns></returns>
		public override bool StartupVehicle()
		{
			try
			{
				if (this.roadNetwork != null && this.mission != null && this.ClientVehicle != null && this.ClientVehicle.VehicleId != null)
				{
					return this.communicator.StartupVehicle(this.roadNetwork, this.mission);
				}
				else
				{
					Console.WriteLine("Some components not initialized");
					return false;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Startup of vehicle failed: \n" + e.ToString());
				return false;
			}
		}

		public static CarTimestamp GetCurrentTimestamp {
			get {
				return new CarTimestamp(sumDt);
			}
		}

    /// <summary>
    /// Resets the sim vehicle
    /// </summary>
    public override void ResetSim()
        {
            if (this.DynamicsVehicle != null)
            {
                this.DynamicsVehicle.Reset();
                Console.WriteLine("Reset Dynamics Vehicle Remotely");
            }
        }

		#region SimpleSim

		private volatile bool simRunning;
		private Thread simThread;

		private void RunSimpleSim() {
			// create the dynamics vehicle
			if (this.communicator.CreateSimpleVehicle()) {
				// mark the sim as running
				simRunning = true;
				
				// create the sim thread
				simThread = new Thread(SimProc);
				simThread.Priority = ThreadPriority.AboveNormal;
				simThread.IsBackground = true;
				simThread.Name = "Sim Thread";
				simThread.Start();
			}
		}

		private void SimProc() {
			using (MMWaitableTimer timer = new MMWaitableTimer(50)) {
				while (simRunning) {
					// wait on the timer
					timer.WaitEvent.WaitOne();

					sumDt += 0.05;

					// call update on the dynamics vehicle
					this.DynamicsVehicle.Update(0.05, null);
				}
			}
		}

		#endregion

		#region step mode handling

		public override void SetStepMode() {
			this.stepMode = true;
			this.continuousModeRate = double.NaN;

			List<int> removeList = new List<int>();

			lock (runControlClients) {
				foreach (KeyValuePair<int, ClientRunControlFacade> kvp in runControlClients) {
					try {
						// set the client to step mode
						kvp.Value.SetStepMode();
					}
					catch (Exception) {
						// remove the client
						removeList.Add(kvp.Key);
					}
				}

				foreach (int removeClient in removeList) {
					runControlClients.Remove(removeClient);
				}
			}
		}

		public override void SetContinuousMode(double realtimeFactor) {
			this.stepMode = false;
			this.continuousModeRate = realtimeFactor;

			List<int> removeList = new List<int>();

			lock (runControlClients) {
				foreach (KeyValuePair<int, ClientRunControlFacade> kvp in runControlClients) {
					try {
						// set the client to continuous
						kvp.Value.SetContinuousMode(realtimeFactor);
					}
					catch (Exception ex) {
						Console.WriteLine("Could not contact steppable client: " + ex.Message);
						// remove the client
						removeList.Add(kvp.Key);
					}
				}

				foreach (int removeClient in removeList) {
					runControlClients.Remove(removeClient);
				}
			}
		}

		public override void RegisterSteppableClient(ClientRunControlFacade client, int stepOrder) {
			lock (runControlClients) {
				try {
					// invoke the step mode and continuous mode stuff on the client
					if (stepMode) {
						client.SetStepMode();
					}
					else {
						client.SetContinuousMode(continuousModeRate);
					}
					runControlClients[stepOrder] = client;
				}
				catch (Exception ex) {
					Console.WriteLine("could not register steppable client: " + ex.Message);
				}
			}
		}

		#endregion
	}
}
