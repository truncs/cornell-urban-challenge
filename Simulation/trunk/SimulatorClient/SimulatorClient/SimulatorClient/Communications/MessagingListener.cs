using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.MessagingService;
using Simulator.Message;

namespace UrbanChallenge.Simulator.Client.Communications
{
	/// <summary>
	/// Listens to messages from the messaging service
	/// </summary>
	[Serializable]
	public class MessagingListener : MarshalByRefObject, IChannelListener
	{
		#region Private Members

		private Communicator communicator;

		#endregion

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <param name="c"></param>
		public MessagingListener(Communicator c)
		{
			this.communicator = c;
		}

		#region IChannelListener Members

		public void MessageArrived(string channelName, object message)
		{
			if (channelName == "SimulationMessageChannel")
			{
				if (message is SimulationMessage)
				{
					SimulationMessage simMessage = (SimulationMessage)message;

					if (simMessage == SimulationMessage.Alive)
					{
						this.communicator.AttemptSimulationConnection();
					}
					else if (simMessage == SimulationMessage.Dead)
					{
						this.communicator.SimulatorLost();
					}
					else if (simMessage == SimulationMessage.Searching)
					{
						try
						{
							// notify
							Console.WriteLine(DateTime.Now.ToString() + ": Received Simulation Search Message");

							// ping sim
							bool b = this.communicator.PingSimulation();

							if (!b)
							{
								// notify
								Console.WriteLine(DateTime.Now.ToString() + ": Not connected to sim, reconnecting");

								// attempt sim connect
								this.communicator.AttemptSimulationConnection();
							}
						}
						catch
						{
							// notify
							Console.WriteLine(DateTime.Now.ToString() + ": Not connected to sim, reconnecting");

							// attempt sim connect
							this.communicator.AttemptSimulationConnection();
						}
					}
				}
			}
		}

		#endregion
	}
}
