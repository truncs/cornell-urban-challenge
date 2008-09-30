using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Simulator.Client;
using Simulator.Engine;
using System.Collections;

namespace Simulator.Communications
{
	/// <summary>
	/// Interfaces with clients
	/// </summary>
	[Serializable]
	public class ClientHandler
	{
		/// <summary>
		/// Clients of the simulation
		/// </summary>
		public Dictionary<string, SimulatorClientFacade> AvailableClients;

		/// <summary>
		/// Vehicles registered with clients
		/// </summary>
		public Dictionary<SimVehicleId, string> VehicleToClientMap;

		/// <summary>
		/// Clients registered with vehicles
		/// </summary>
		public Dictionary<string, SimVehicleId> ClientToVehicleMap;

		/// <summary>
		/// Constructor
		/// </summary>
		public ClientHandler()
		{
			this.AvailableClients = new Dictionary<string, SimulatorClientFacade>();
			this.VehicleToClientMap = new Dictionary<SimVehicleId, string>();
			this.ClientToVehicleMap = new Dictionary<string, SimVehicleId>();
		}

		/// <summary>
		/// Associate the vehicle with a enw client
		/// </summary>
		/// <param name="vehicleId"></param>
		/// <returns></returns>
		public bool ReBind(SimVehicleId vehicleId)
		{
			// make sure that the vehicle is not already associated with a client
			if (!VehicleToClientMap.ContainsKey(vehicleId))
			{
				// loop over all clients looking for an open client
				foreach (string s in this.AvailableClients.Keys)
				{
					// make sure the client is not associated with a vehicle
					if (!ClientToVehicleMap.ContainsKey(s))
					{
						try
						{
							// get the client
							SimulatorClientFacade scf = this.AvailableClients[s];

							// set the vehicle in the client
							scf.SetVehicle(vehicleId);

							// set vehicle as having client
							VehicleToClientMap.Add(vehicleId, s);

							// set client as having vehicle
							ClientToVehicleMap.Add(s, vehicleId);

							// success
							SimulatorOutput.WriteLine("Vehicle: " + vehicleId.ToString() + " Bound to client: " + s);

							// return that the rebind was successful
							return true;
						}
						catch (Exception e)
						{
							// there was an error
							SimulatorOutput.WriteLine("Error binding vehicle: " + vehicleId.ToString() + " to client: " + s + "\n" + e.ToString());

							// cleanup vehicle to client map
							if (this.VehicleToClientMap.ContainsKey(vehicleId))
								this.VehicleToClientMap.Remove(vehicleId);

							// cleanup client to vehicle map
							if (this.ClientToVehicleMap.ContainsKey(s))
								this.ClientToVehicleMap.Remove(s);

							// return that the attempt was unsuccessful
							return false;
						}
					}
				}

				// success
				SimulatorOutput.WriteLine("Vehicle: " + vehicleId.ToString() + " Could not be bound to any clients, lack of availability");

				// notify that we never got to this point
				return false;
			}
			else
				return true;
		}

		/// <summary>
		/// Remove a client
		/// </summary>
		/// <param name="vehicleId"></param>
		/// <returns></returns>
		public bool Remove(SimVehicleId vehicleId)
		{
			// check to see if this vehicle is associated with a client
			if (VehicleToClientMap.ContainsKey(vehicleId))
			{
				// get name of client vehicle associated with
				string client = VehicleToClientMap[vehicleId];

				try
				{	
					// remove vehicle from client to vehicle map
					this.ClientToVehicleMap.Remove(client);

					// remove client from vehicle to client map
					this.VehicleToClientMap.Remove(vehicleId);

					// remove vehicle from client
					this.AvailableClients[client].SetVehicle(null);

					// notify success
					SimulatorOutput.WriteLine("Successfully removed vehicle: " + vehicleId.ToString() + " from clients");

					// return success
					return true;
				}
				catch (Exception e)
				{
					// there was an error
					SimulatorOutput.WriteLine("Error removing vehicle: " + vehicleId.ToString() + " from clients: \n" + e.ToString());

					// cleanup vehicle to client map
					if (this.VehicleToClientMap.ContainsKey(vehicleId))
						this.VehicleToClientMap.Remove(vehicleId);

					// cleanup client to vehicle map
					if (this.ClientToVehicleMap.ContainsKey(client))
						this.ClientToVehicleMap.Remove(client);

					// return unsuccessful
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Attempt to rebind all vehicles
		/// </summary>
		/// <param name="vehicles"></param>
		public void ReBindAll(Dictionary<SimVehicleId, SimVehicle> vehicles)
		{
			// loop over all vehicles
			foreach (SimVehicleId svi in vehicles.Keys)
			{
				// attempt to bind the vehicle
				this.ReBind(svi);
			}
		}

		/// <summary>
		/// Add a client to the simulation
		/// </summary>
		/// <param name="client"></param>
		/// <param name="scf"></param>
		/// <param name="vehicles"></param>
		/// <returns></returns>
		public bool AddClient(string client, SimulatorClientFacade scf, Dictionary<SimVehicleId, SimVehicle> vehicles)
		{
			try
			{
				// check if already a client
				if (this.AvailableClients.ContainsKey(client))
				{
					// remove if already bound for some reason
					this.Remove(client);
				}

				// lock
				lock (((ICollection)this.AvailableClients).SyncRoot)
				{
					// add to available clients
					this.AvailableClients.Add(client, scf);
				}

				// notify success
				SimulatorOutput.WriteLine("Successfully added client: " + client);

				// attempt to rebind the vehicles
				this.ReBindAll(vehicles);

				// return success
				return true;
			}
			catch (Exception e)
			{
				// notify failure
				SimulatorOutput.WriteLine("Error adding client: " + client + ": \n" + e.ToString());

				// return false
				return false;
			}
		}

		/// <summary>
		/// Removes a client
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public bool Remove(string client)
		{
			// check if the client is associated with a vehicle
			if (this.ClientToVehicleMap.ContainsKey(client))
			{
				// get vehicle associated with teh client
				SimVehicleId svi = this.ClientToVehicleMap[client];
				
				try
				{
					// remove vehicle from client map
					if (this.ClientToVehicleMap.ContainsKey(client))
						this.ClientToVehicleMap.Remove(client);

					// remove client from vehicle map
					if (this.VehicleToClientMap.ContainsKey(svi))
						this.VehicleToClientMap.Remove(svi);

					// lock the clients
					lock(((ICollection)this.AvailableClients).SyncRoot)
					{
						// kill the client
						this.AvailableClients[client].Kill();

						// remove the client from the available clients
						this.AvailableClients.Remove(client);
					}
				}
				catch (Exception e)
				{
					// lock the clients
					lock (((ICollection)this.AvailableClients).SyncRoot)
					{
						// remove the client from available
						if (this.AvailableClients.ContainsKey(client))
							this.AvailableClients.Remove(client);
					}
					Console.WriteLine(e.ToString());
				}

				// notify success
				SimulatorOutput.WriteLine("Successfully removed client: " + client);

				// rebind teh vehicle associated with the client
				return this.ReBind(svi);
			}
			else
			{
				// lock the clients
				lock (((ICollection)this.AvailableClients).SyncRoot)
				{
					try
					{
						// kill the client
						this.AvailableClients[client].Kill();

						// remove the client from the available clients
						this.AvailableClients.Remove(client);
					}
					catch (Exception e)
					{
						// remove the client from available
						if (this.AvailableClients.ContainsKey(client))
							this.AvailableClients.Remove(client);
						Console.WriteLine(e.ToString());
					}
				}

				// notify success
				SimulatorOutput.WriteLine("Successfully removed client: " + client);

				// return success
				return true;
			}
		}
	}
}
