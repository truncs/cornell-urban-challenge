using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;
using UrbanChallenge.NameService;
using UrbanChallenge.MessagingService;
using UrbanChallenge.Simulator.Client;
using System.Windows.Forms;
using UrbanChallenge.Common.Utility;
using System.Threading;
using Simulator.Message;
using System.Runtime.Remoting.Lifetime;
using System.Collections;

namespace Simulator.Communications
{
	/// <summary>
	/// Handles communications for the simulator
	/// </summary>
	[Serializable]
	public class Communicator : SimulatorFacade
	{
		#region Private members

		/// <summary>
		/// Link to the upper level sim
		/// </summary>		
		private Simulation simulation;

		/// <summary>
		/// Remoting object directory
		/// </summary>
		private ObjectDirectory objectDirectory;

		/// <summary>
		/// Services
		/// </summary>
		private WellKnownServiceTypeEntry[] wkst;

		#region Channels

		// simulation broadcast alive
		private IChannel simulationMessageChannel;

		#endregion

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="simulation"></param>
		public Communicator(Simulation simulation)
		{
			// set sim
			this.simulation = simulation;
		}

		/// <summary>
		/// Configures remoting
		/// </summary>
		public void Configure()
		{
			try
			{
				// configure
				RemotingConfiguration.Configure("Simulator.exe.config", false);
				wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();

				// notfy
				SimulatorOutput.WriteLine("Remoting Configured Successfully");
			}
			catch (Exception e)
			{
				SimulatorOutput.WriteLine("Error Configuring Remoting: " + e.ToString());
			}
		}

		/// <summary>
		/// Registers with the correct services
		/// </summary>
		public void Register()
		{
			try
			{
				// "Activate" the NameService singleton.
				objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

				// Retreive the directory of messaging channels
				IChannelFactory channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");

				// Retreive the Messaging Service channels we want to push to
				simulationMessageChannel = channelFactory.GetChannel("SimulationMessageChannel", ChannelMode.UdpMulticast);

				// Rebind the Simulator as the simulator remote facade server
				objectDirectory.Rebind(this, "SimulationServer");

				// Notify user of success
				SimulatorOutput.WriteLine("Connection to Name Service and Registration of Simulation Server as Simulator Facade Successful");

				// let clients know the sim is alive
				simulationMessageChannel.PublishUnreliably(SimulationMessage.Alive);
			}
			catch (Exception e)
			{
				SimulatorOutput.WriteLine("Error Registering With Remoting Services: " + e.ToString());
			}
		}

		/// <summary>
		/// Allows for other objects to ping the simulation
		/// </summary>
		/// <returns></returns>
		public override bool Ping()
		{
			return true;
		}

		/// <summary>
		/// Register a client with the simulation
		/// </summary>
		/// <param name="clientName"></param>
		/// <returns></returns>
		public override bool Register(string clientName)
		{
			try
			{
				// resolve the client
				SimulatorClientFacade scf = (SimulatorClientFacade)objectDirectory.Resolve(clientName);

				// client name
				string name = scf.Name();

				// invoke changes
				this.simulation.BeginInvoke(new MethodInvoker(delegate()
				{
					// notify					
					this.simulation.clientHandler.AddClient(name, scf, this.simulation.simEngine.Vehicles);
					this.simulation.OnClientsChanged();					
				}));

				// return success
				return true;
			}
			catch (Exception e)
			{
				this.simulation.BeginInvoke(new MethodInvoker(delegate()
				{
					// notify
					SimulatorOutput.WriteLine("Error Registering Client: " + clientName + ". \n" + e.ToString());
				}));				

				// return error
				return false;
			}
		}

		/// <summary>
		/// Runs to maintain the simulation and clients
		/// </summary>
		private void Maintenance()
		{
			while (true)
			{
				// to remove
				List<string> toRemove = new List<string>();

				lock (((ICollection)this.simulation.clientHandler.AvailableClients).SyncRoot)
				{
					// attempt to ping all clients
					foreach (KeyValuePair<string, SimulatorClientFacade> client in this.simulation.clientHandler.AvailableClients)
					{
						try
						{
							client.Value.Ping();
						}
						catch (Exception e)
						{
							toRemove.Add(client.Key);
							Console.WriteLine(e.ToString());
						}
					}
				}

				foreach (string s in toRemove)
				{
					this.simulation.BeginInvoke(new MethodInvoker(delegate()
					{
						// notify
						SimulatorOutput.WriteLine("Error Maintaining Client: " + s + ", Removed");

						// remove client
						this.simulation.clientHandler.Remove(s);

						this.simulation.OnClientsChanged();
					}));	
				}

				Thread.Sleep(2000);
			}
		}

		/// <summary>
		/// run maintanance over the clients
		/// </summary>
		public void RunMaintenance()
		{
			Thread t = new Thread(Maintenance);
			t.IsBackground = true;
			t.Priority = ThreadPriority.Lowest;
			t.Start();
		}

		/// <summary>
		/// Wraps up loose ends
		/// </summary>
		public void ShutDown()
		{
			try
			{
				this.simulationMessageChannel.PublishUnreliably(SimulationMessage.Dead);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// Search for clients
		/// </summary>
		public void SearchForClients()
		{
			this.simulationMessageChannel.PublishUnreliably(SimulationMessage.Searching);
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
		/// Gets list of current clients
		/// </summary>
		/// <returns></returns>
		public override Dictionary<string, int> GetClientMachines()
		{
			try
			{
				Dictionary<string, int> clients = new Dictionary<string, int>();
				foreach (SimulatorClientFacade scf in this.simulation.clientHandler.AvailableClients.Values)
				{
					ListViewItem lvi = scf.ViewableItem();
					int num = int.Parse(lvi.SubItems[0].Text);
					string comp = lvi.SubItems[1].Text;
					clients.Add(comp, num);
				}

				return clients;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return new Dictionary<string, int>();
			}
			/*foreach (string name in this.simulation.clientHandler.AvailableClients.Keys)
			{
				if (this.simulation.clientHandler.ClientToVehicleMap.ContainsKey(name))
					clients.Add(, this.simulation.clientHandler.ClientToVehicleMap[name].Number);
				else
					clients.Add(name, -1);
			}
			return clients;*/
		}


		public override bool StepMode {
			get { return simulation.simEngine.StepMode; }
			set { simulation.simEngine.StepMode = value; }
		}

		public override double RealtimeFactor {
			get { return 1; }
			set { }
		}

		public override void Step() {
			simulation.simEngine.Step();
		}
	}
}
