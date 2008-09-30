using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.Utility;
using System.Threading;
using UrbanChallenge.Simulator.Client.World;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Simulator.Client;

namespace Simulator.Engine
{
	/// <summary>
	/// State of the simulation
	/// </summary>
	public enum SimulationState
	{
		Stopped,
		Running
	}

	/// <summary>
	/// The engine behind the simulation
	/// </summary>
	[Serializable]
	public class SimEngine
	{
		#region Private Members

		/// <summary>
		/// Settings
		/// </summary>
		public SimEngineSettingsAccessor settings;

		/// <summary>
		/// Main gui of sim
		/// </summary>
		[NonSerialized]
		public Simulation simulationMain;

		/// <summary>
		/// indicates if the simulation is in step mode
		/// </summary>
		private volatile bool stepMode;

		/// <summary>
		/// Event used to halt the simulation thread
		/// </summary>
		[NonSerialized]
		private AutoResetEvent stepEvent;

		#endregion

		#region Public Members

		/// <summary>
		/// The property grid associated with the sim engine
		/// </summary>
		[NonSerialized]
		public PropertyGrid propertyGrid;

		/// <summary>
		/// Vehicles in the sim
		/// </summary>
		public Dictionary<SimVehicleId, SimVehicle> Vehicles;

		/// <summary>
		/// State of the engine as a whole
		/// </summary>
		public SimEngineState EngineState;

		/// <summary>
		/// World service
		/// </summary>
		public SimWorldService WorldService;

		/// <summary>
		/// State of the sim
		/// </summary>
		public SimulationState SimulationState = SimulationState.Stopped;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="pg"></param>
		public SimEngine(PropertyGrid pg, Simulation simulationMain)
		{
			// create settings file
			this.settings = new SimEngineSettingsAccessor();

			// set main
			this.simulationMain = simulationMain;

			// create property grid			
			this.propertyGrid = pg;
			this.propertyGrid.SelectedObject = this.settings;

			// create world service
			this.WorldService = new SimWorldService(this);

			// initialize held engine vehicles
			this.Vehicles = new Dictionary<SimVehicleId, SimVehicle>();

			this.stepMode = false;
			this.stepEvent = new AutoResetEvent(false);
		}

		#endregion

		#region Accessors

		/// <summary>
		/// Set property grid
		/// </summary>
		/// <param name="pg"></param>
		public void SetPropertyGrid(PropertyGrid pg)
		{
			this.propertyGrid = pg;
		}

		/// <summary>
		/// Sets the property grid to display defualt settings
		/// </summary>
		public void SetPropertyGridDefault()
		{
			this.propertyGrid.SelectedObject = this.settings;
		}

		#endregion

		#region Functions

		/// <summary>
		/// Obstacle
		/// </summary>
		/// <param name="screenCenter"></param>
		/// <returns></returns>
		public SimObstacle AddObstacle(Coordinates screenCenter)
		{
			// deafualt id is # of elts
			int idNumber = this.WorldService.Obstacles.Count + this.Vehicles.Count;

			// create array to hold used vehicle and obstacle id's
			bool[] usedIds = new bool[this.WorldService.Obstacles.Count + this.Vehicles.Count];

			// loop over obstacles and get ids
			foreach (SimObstacle ism in this.WorldService.Obstacles.Values)
			{
				// make sure num within # of obstacles and vehicles
				if (ism.ObstacleId.Number < this.WorldService.Obstacles.Count + this.Vehicles.Count)
				{
					// check off
					usedIds[ism.ObstacleId.Number] = true;
				}
			}

			// loop over vehicles and get ids
			foreach (SimVehicle ism in this.Vehicles.Values)
			{
				// make sure num within # of obstacles and vehicles
				if (ism.VehicleId.Number < this.WorldService.Obstacles.Count + this.Vehicles.Count)
				{
					// check off
					usedIds[ism.VehicleId.Number] = true;
				}
			}

			// loop over checked off id's
			for (int i = usedIds.Length - 1; i >= 0; i--)
			{
				// if find a false one set that id
				if (usedIds[i] == false)
				{
					// set id num
					idNumber = i;
				}
			}

			// create obstacle id
			SimObstacleId soi = new SimObstacleId(idNumber);

			// get position (center of screen)
			Coordinates position = screenCenter;

			// create obstacle
			SimObstacle so = new SimObstacle(soi, position, new Coordinates(1, 0));

			// add to obstacles
			this.WorldService.Obstacles.Add(so.ObstacleId, so);

			// return
			return so;
		}

		/// <summary>
		/// Adds a vehicle to the world
		/// </summary>
		/// <param name="screenCenter"></param>
		/// <returns></returns>
		public SimVehicle AddVehicle(Coordinates screenCenter)
		{
			// deafualt id is # of elts
			int idNumber = this.WorldService.Obstacles.Count + this.Vehicles.Count;

			// create array to hold used vehicle and obstacle id's
			bool[] usedIds = new bool[this.WorldService.Obstacles.Count + this.Vehicles.Count];

			// loop over obstacles and get ids
			foreach (SimObstacle ism in this.WorldService.Obstacles.Values)
			{
				// make sure num within # of obstacles and vehicles
				if (ism.ObstacleId.Number < this.WorldService.Obstacles.Count + this.Vehicles.Count)
				{
					// check off
					usedIds[ism.ObstacleId.Number] = true;
				}
			}

			// loop over vehicles and get ids
			foreach (SimVehicle ism in this.Vehicles.Values)
			{
				// make sure num within # of obstacles and vehicles
				if (ism.VehicleId.Number < this.WorldService.Obstacles.Count + this.Vehicles.Count)
				{
					// check off
					usedIds[ism.VehicleId.Number] = true;
				}
			}

			// loop over checked off id's
			for (int i = usedIds.Length - 1; i >= 0; i--)
			{
				// if find a false one set that id
				if (usedIds[i] == false)
				{
					// set id num
					idNumber = i;
				}
			}

			// create vehicle id
			SimVehicleId svi = new SimVehicleId(idNumber);

			// get position (center of screen)
			Coordinates position = screenCenter;

			// create state
			SimVehicleState svs = new SimVehicleState();
			svs.canMove = true;
			svs.Heading = new Coordinates(1,0);
			svs.IsBound = false;
			svs.Position = position;
			svs.Speed = 0;
			svs.VehicleID = svi;

			// create vehicle
			SimVehicle stv = new SimVehicle(svs, TahoeParams.VL, TahoeParams.T);

			// add to vehicles
			this.Vehicles.Add(stv.VehicleId, stv);

			// return
			return stv;			 
		}

		/// <summary>
		/// The simulation main thread
		/// </summary>
		public void Simulate()
		{
			// run sim at 10Hz
			MMWaitableTimer timer = new MMWaitableTimer((uint)this.settings.SimCycleTime);

			// notify
			this.simulationMain.SimulationModeLabel.Text = "Simulation Running";
			this.simulationMain.SimulationModeLabel.Image = global::Simulator.Properties.Resources.Light_Bulb_On_16_n_p;

			// run while on
			while (this.SimulationState == SimulationState.Running)
			{
				if (stepMode) {
					// if we're in step mode, then wait on the step event or time out
					bool gotEvent = stepEvent.WaitOne(250, false);
					// if we timed out, start the loop over
					if (!gotEvent) {
						continue;
					}
				}
				else {
					// wait for 10hz
					timer.WaitEvent.WaitOne();
				}

				try
				{
					// get world state
					WorldState ws = this.WorldService.GetWorldState();					
					lock (this.simulationMain.clientHandler)
					{
						// set world state to each client in the sim
						foreach (string client in this.simulationMain.clientHandler.VehicleToClientMap.Values)
						{
							try
							{
								// update client
								SimVehicleState nextState = this.simulationMain.clientHandler.AvailableClients[client].Update(ws, (double)this.settings.SimCycleTime/1000.0);

								if (nextState != null)
								{
									// update state
									this.Vehicles[this.simulationMain.clientHandler.ClientToVehicleMap[client]].SimVehicleState = nextState;
								}
								else
								{
									throw new Exception("Received null SimVehicleState from " + client);
								}
							}
							catch (Exception e)
							{
								if (!this.simulationMain.IsDisposed)
								{
									this.simulationMain.BeginInvoke(new MethodInvoker(delegate()
									{
										// notify
										SimulatorOutput.WriteLine("Error Updating Client: " + client + ", Simulation Stopped");

										// set state
										this.simulationMain.SimulationModeLabel.Text = "Simulation Stopped";
										this.simulationMain.SimulationModeLabel.Image = global::Simulator.Properties.Resources.Light_Bulb_Off_16_n_p;

										// stop
										this.EndSimulation();

									}));
									Console.WriteLine(e.ToString());
									// leave
									break;
								}
							}
						}
					}

					// redraw
					this.simulationMain.BeginInvoke(new MethodInvoker(delegate()
					{
						// notify
						this.simulationMain.roadDisplay1.Invalidate();
					}));
				}
				catch (Exception e)
				{
					if (!this.simulationMain.IsDisposed)
					{
						this.simulationMain.BeginInvoke(new MethodInvoker(delegate()
						{
							// notify
							SimulatorOutput.WriteLine("Error in outer sim loop:" + e.ToString());
						}));
					}
				}
			}
		}

		/// <summary>
		/// Begin simulation
		/// </summary>
		public void BeginSimulation(ArbiterRoadNetwork roadNetwork, ArbiterMissionDescription mission)
		{
			try
			{
				// startup clients
				foreach (KeyValuePair<SimVehicleId, SimVehicle> vhcs in this.Vehicles)
				{
					// set road and mission
					SimulatorClientFacade scf = this.simulationMain.clientHandler.AvailableClients[this.simulationMain.clientHandler.VehicleToClientMap[vhcs.Key]];                    

					// check if we need to randomize mission
					if (vhcs.Value.RandomMission)
					{
						// create random mission
						Queue<ArbiterCheckpoint> checks = new Queue<ArbiterCheckpoint>(60);
						int num = mission.MissionCheckpoints.Count - 1;
						ArbiterCheckpoint[] checkPointArray = mission.MissionCheckpoints.ToArray();
						Random r = new Random();						
						for (int i = 0; i < 60; i++)
						{
							checks.Enqueue(checkPointArray[r.Next(num)]);
						}
						ArbiterMissionDescription amd = new ArbiterMissionDescription(checks, mission.SpeedLimits);

						// set road , mission
						scf.SetRoadNetworkAndMission(roadNetwork, amd);
					}
					// otherwise no random mission
					else
					{
						// set road , mission
						scf.SetRoadNetworkAndMission(roadNetwork, mission);
					}

					// startup ai
					bool b = scf.StartupVehicle();

					// check for false
					if (!b)
					{
						Console.WriteLine("Error starting simulation for vehicle id: " + vhcs.Key.ToString());
						return;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error starting simulation: \n" + e.ToString());
				return;
			}

			// run sim
			this.RunSimulation();
		}

		/// <summary>
		/// Just run the sim
		/// </summary>
		public void RunSimulation()
		{
			// running
			this.SimulationState = SimulationState.Running;

			// notify
			SimulatorOutput.WriteLine("Simulation Started");

			// start thread
			Thread d = new Thread(Simulate);
			d.Priority = ThreadPriority.AboveNormal;
			d.IsBackground = true;
			d.Start();

			// state
			this.simulationMain.SimulationModeLabel.Text = "Simulation Running";
			this.simulationMain.SimulationModeLabel.Image = global::Simulator.Properties.Resources.Light_Bulb_On_16_n_p;
		}

		/// <summary>
		/// End simulation
		/// </summary>
		public void EndSimulation()
		{
			// stop sim
			this.SimulationState = SimulationState.Stopped;

			// notify
			SimulatorOutput.WriteLine("Simulation Stopped");

			// state
			this.simulationMain.SimulationModeLabel.Text = "Simulation Stopped";
			this.simulationMain.SimulationModeLabel.Image = global::Simulator.Properties.Resources.Light_Bulb_Off_16_n_p;
		}

		#endregion

		public bool StepMode {
			get { return stepMode; }
			set {
				stepMode = value;
				stepEvent.Reset();

				// iterate through each client and set it to sim mode
				lock (this.simulationMain.clientHandler) {
					// set world state to each client in the sim
					foreach (string client in this.simulationMain.clientHandler.VehicleToClientMap.Values) {
						try {
							// update the client
							if (stepMode) {
								this.simulationMain.clientHandler.AvailableClients[client].SetStepMode();
							}
							else {
								this.simulationMain.clientHandler.AvailableClients[client].SetContinuousMode(1);
							}
						}
						catch (Exception e) {
							if (!this.simulationMain.IsDisposed) {
								this.simulationMain.BeginInvoke(new MethodInvoker(delegate() {
									// notify
									SimulatorOutput.WriteLine("Error Updating Step Mode On Client: " + client + ", Simulation Stopped");

									// set state
									this.simulationMain.SimulationModeLabel.Text = "Simulation Stopped";
									this.simulationMain.SimulationModeLabel.Image = global::Simulator.Properties.Resources.Light_Bulb_Off_16_n_p;

									// stop
									this.EndSimulation();

								}));
								Console.WriteLine(e.ToString());
							}
						}
					}
				}

				if (!this.simulationMain.IsDisposed) {
					this.simulationMain.BeginInvoke(new MethodInvoker(delegate() {
						// notify
						if (stepMode) {
							SimulatorOutput.WriteLine("Setting sim to step mode");
						}
						else {
							SimulatorOutput.WriteLine("Setting sim to continuous mode");
						}

						// set state
						if (stepMode) {
							this.simulationMain.SimulationModeLabel.Text = "Simulation Running - Step";
						}
						else {
							this.simulationMain.SimulationModeLabel.Text = "Simulation Running";
						}
						this.simulationMain.SimulationModeLabel.Image = global::Simulator.Properties.Resources.Light_Bulb_On_16_n_p;
					}));
				}
			}
		}

		public void Step() {
			if (stepMode) {
				stepEvent.Set();

				if (!this.simulationMain.IsDisposed) {
					this.simulationMain.BeginInvoke(new MethodInvoker(delegate() {
						SimulatorOutput.WriteLine("Stepping Sim");
					}));
				}
			}
		}
	}
}
