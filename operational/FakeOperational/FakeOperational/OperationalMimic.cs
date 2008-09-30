using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalService;
using UrbanChallenge.Common;
using UrbanChallenge.Behaviors;
using UrbanChallenge.NameService;
using System.Runtime.Remoting;

namespace FakeOperational
{
	public class OperationalMimic : OperationalFacade
	{
		private CarMode carMode = CarMode.Human;
		private bool displaySignals = false;

		public void TryConnect()
		{
			try
			{
				// configure
				RemotingConfiguration.Configure("FakeOperational.exe.config", false);
				WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();

				// "Activate" the NameService singleton.
				ObjectDirectory objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

				// register the core as implementing the arbiter advanced remote facade
				objectDirectory.Rebind(this, "OperationalService_" + Environment.MachineName);

				Console.WriteLine("Connection and Registration Success");
			}
			catch (Exception e)
			{
				Console.WriteLine("Connection error: \n" + e.ToString());
			}
		}

		public void Run()
		{
			this.TryConnect();
			this.Command();
		}

		public void Command()
		{
			while (true)
			{
				Console.Write("Mimic > ");
				string s = Console.ReadLine();

				switch (s)
				{
					case "exit":
						return;
					case "run":
						carMode = CarMode.Run;
						break;
					case "pause":
						carMode = CarMode.Pause;
						break;
					case "human":
						carMode = CarMode.Human;
						break;
					case "connect":
						this.TryConnect();
						break;
					case "signal":
						this.displaySignals = this.displaySignals ? false : true;
						break;
					case "man":
						Console.WriteLine("exit");
						Console.WriteLine("run");
						Console.WriteLine("pause");
						Console.WriteLine("human");
						Console.WriteLine("connect");
						Console.WriteLine("signal");
						break;
				}
			}
		}

		public override void ExecuteBehavior(UrbanChallenge.Behaviors.Behavior b)
		{
			if (displaySignals)
			{
				if (b.Decorators != null)
				{
					Console.Write("Decorators: ");

					foreach (BehaviorDecorator bd in b.Decorators)
					{
						if (bd is TurnSignalDecorator)
						{
							TurnSignalDecorator tsd = (TurnSignalDecorator)bd;
							Console.Write(tsd.Signal.ToString() + ", ");
						}
						else
						{
							Console.Write("Unrecognized, ");
						}
					}						

					Console.Write("\n");
				}
				else
				{
					Console.WriteLine("Decorators: null");
				}
			}
		}

		public override Type GetCurrentBehaviorType()
		{
			return (new StayInLaneBehavior(null, null, null)).GetType();
		}

		public override UrbanChallenge.Common.CarMode GetCarMode()
		{
			return carMode;
		}

		public override void SetProjection(UrbanChallenge.Common.EarthModel.PlanarProjection proj)
		{
			
		}

		public override void RegisterListener(OperationalListener listener)
		{
			
		}

		public override void UnregisterListener(OperationalListener listener)
		{
			
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}

		public override void Ping()
		{
			
		}

		public override void SetRoadNetwork(UrbanChallenge.Arbiter.ArbiterRoads.ArbiterRoadNetwork roadNetwork)
		{
			
		}
	}
}
