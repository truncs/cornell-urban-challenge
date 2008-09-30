using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Net;
using WatchdogCommunication;
using PublishCommon;
using System.Net.NetworkInformation;

namespace Watchdog
{
	public class Program
	{
		static WatchdogComm comm = new WatchdogComm ();
		static Publish curPublish = null;
		static bool running = true;
		public static string pubRoot = "C:\\publish";
		#if !SERVICE
		static void Main(string[] args)
		{
			Trace.WriteLine("Watchdog Started.");
			NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(NetworkChange_NetworkAvailabilityChanged);
			try
			{
				Init();
			}
			catch
			{ }
			MainLoop();			
		}

		static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			Trace.WriteLine("Got Network Change Event");
			if (e.IsAvailable)
			{
				try
				{
					Init();
				}
				catch { }
			}
			else
			{
				try
				{
					KillSocket();
				}
				catch {}
			}
		}
		#endif
		static TextWriterTraceListener traceText;

		public static void Init()
		{
			traceText = new TextWriterTraceListener("watchdog " + DateTime.Now.Hour + "." + DateTime.Now.Minute + " " + DateTime.Now.Day + "." + DateTime.Now.Month + "." + DateTime.Now.Year);
			ConsoleTraceListener consoleTrace = new ConsoleTraceListener(false);
			Trace.Listeners.Add(traceText);
			Trace.Listeners.Add(consoleTrace);
			Trace.WriteLine("Init Socket");
			comm.Init();
		}

		internal static void KillSocket()
		{
			Trace.WriteLine("Kill Socket");
			comm.KillSocket();
		}

		public static void MainLoop ()
		{
			comm.GotStartPublishMessage += new EventHandler<WatchdogMessageEventArgs<StartPublishMessage>>(comm_GotStartPublishMessage);
			comm.GotStopPublishMessage += new EventHandler<WatchdogMessageEventArgs<StopPublishMessage>>(comm_GotStopPublishMessage);
			comm.GotWatchdogTerminateMessage += new EventHandler<WatchdogMessageEventArgs<WatchdogTerminateMessage>>(comm_GotWatchdogTerminateMessage);
			comm.GotCommandMessage += new EventHandler<WatchdogMessageEventArgs<CommandMessage>>(comm_GotCommandMessage);
			
			Thread t = new Thread(new ThreadStart(StatusThread));
			t.Name = "Watchdog Status Thread";
			t.Start();
			Publish autoP = AutodetectActivePublish();
			if (autoP != null)
			{
				StartPublish(autoP.name);
			}
#if !SERVICE
				
			while(running)
			{
				string s = Console.ReadLine();
				if (s == null) continue;
				if (s.StartsWith("load") && s.Split(" ".ToCharArray(), StringSplitOptions.None).Length > 1) //load a publish
				{
					Trace.WriteLine("Loading " + s.Substring(s.IndexOf(" ") + 1));
					curPublish = Publish.Load(s.Substring(s.IndexOf(" ") + 1),pubRoot);
				}
				if (s.StartsWith("show all"))
				{
					foreach (string p in Publish.GetAllPublishNames(pubRoot))
					{
						Trace.WriteLine(p);
					}
				}
				if (s.StartsWith("curpub"))
				{
					if (curPublish == null)
						Trace.WriteLine("No Publish Loaded.");
					else
					{
						curPublish.PrintDebug();
						Trace.WriteLine("Running: " + curPublish.IsCommandRunning());
					}
				}
				if (s.StartsWith("start"))
				{
					if (curPublish == null) Trace.WriteLine("No publish loaded.");
					else curPublish.Start(pubRoot);
				}

				if (s.StartsWith("wdon"))
				{
					if (curPublish == null) Trace.WriteLine("No publish loaded.");
					else
					{
						Trace.WriteLine("Enabling Watchdog...");
						curPublish.watchdogAutoRestart = true;
					}
				}

				if (s.StartsWith("wdoff"))
				{
					if (curPublish == null) Trace.WriteLine("No publish loaded.");
					else
					{
						Trace.WriteLine("Disabling Watchdog...");
						curPublish.watchdogAutoRestart = false;
					}
				}
				if (s.StartsWith("list"))
				{
					Process[] parr = Process.GetProcesses();
					List<Process> runningProcesses = new List<Process>(parr);
					foreach (Process filename in runningProcesses)
					{
						Trace.WriteLine(filename.ProcessName);
					}
				}


			}
			t.Abort();
			traceText.Flush();
#endif
			
		}

		static void comm_GotCommandMessage(object sender, WatchdogMessageEventArgs<CommandMessage> e)
		{
			if (!((e.msg.machineName.ToLower() == WatchdogComm.GetMachineName().ToLower()) || (e.msg.machineName == CommandMessage.ALLMACHINES)))
				return;

			Trace.WriteLine("Executing Command: " + e.msg.command.ToString());
			switch (e.msg.command)
			{
				case CommandMessage.WatchdogCommand.AddServiceRight :
					LocalSecurityPolicyManager.SetRight("labuser", LocalSecurityPolicyManager.ServiceRight);
					break;
				case CommandMessage.WatchdogCommand.EnableWatchdogAutoReset:
					if (curPublish != null) { curPublish.watchdogAutoRestart = true; curPublish.Save(pubRoot); }
					break;
				case CommandMessage.WatchdogCommand.DisableWatchdogAutoReset:
					if (curPublish != null) { curPublish.watchdogAutoRestart = false; curPublish.Save(pubRoot); }
					break;
				case CommandMessage.WatchdogCommand.Quit:
					KillApp();
					break;
				case CommandMessage.WatchdogCommand.StartRemoteDebugger:
					try
					{
						Process[] p = Process.GetProcessesByName("msvsmon");
						Trace.WriteLine("MSVSMONS : " + p.Length);
						if (p.Length > 0)
						{
							foreach (Process mon in p)
							{
								mon.Kill();
							}
						}
						Trace.WriteLine("Starting : " + pubRoot + "\\deployment\\debugger.bat");
						ProcessStartInfo pi = new ProcessStartInfo(pubRoot + "\\deployment\\debugger.bat");
						pi.WorkingDirectory = pubRoot + "\\deployment";
						Process.Start(pi);
					}
					catch
					{ }			
					
					break;
				case CommandMessage.WatchdogCommand.RefreshPublishes:
						SendStatusMessage();
					break;
				case CommandMessage.WatchdogCommand.AutoDetectRunningPublish:
					if (curPublish == null)
					{
						Publish autoP = AutodetectActivePublish();
						if (autoP != null)
						{
							StartPublish(autoP.name);
						}
					}
					break;
			}			
		}

		public static void KillApp()
		{
			traceText.Flush();
			try
			{
				if (curPublish != null)
					curPublish.Stop();
			}
			catch (Exception ex)
			{
				if (curPublish != null)
					comm.SendMessage<StartStopPublishMessageReply>(new StartStopPublishMessageReply(WatchdogComm.GetMachineName(), curPublish.name, false, "Exception Stopping Publish" + ex.Message));
				Trace.WriteLine("Exception Stopping! " + ex.Message);
			}
			running = false;
			
			Thread.Sleep(3000);
			traceText.Flush();
			//Process.GetCurrentProcess().Kill();

		}

		static void comm_GotWatchdogTerminateMessage(object sender, WatchdogMessageEventArgs<WatchdogTerminateMessage> e)
		{
			KillApp();
		}

		static void comm_GotStopPublishMessage(object sender, WatchdogMessageEventArgs<StopPublishMessage> e)
		{
			if (curPublish == null) return;
			if (e.msg.publishName.ToLower() != curPublish.name.ToLower()) return;
			try
			{
				curPublish.Stop();
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Exception Stopping! " + ex.Message);
				comm.SendMessage<StartStopPublishMessageReply>(new StartStopPublishMessageReply(WatchdogComm.GetMachineName(), e.msg.publishName, false, "Exception Stopping Publish" + ex.Message));
			}
			curPublish.WatchdogReset -= new EventHandler(curPublish_WatchdogReset);
		}

		static void StatusThread()
		{			
			while (true)
			{
				SendStatusMessage();			
				Thread.Sleep(500);
			}			
		}

		static void SendStatusMessage()
		{
			WatchdogStatusMessage msg = new WatchdogStatusMessage(WatchdogComm.GetMachineName(), "", "No Publish Active", WatchdogStatusMessage.StatusLevel.NotRunning, Publish.GetAllPublishNames(pubRoot));
			if (curPublish == null)
			{
				msg.curPublishName = "";
				msg.statusText = "No Publish Active";
				msg.statusLevel = WatchdogStatusMessage.StatusLevel.NotRunning;
			}
			else
			{
				msg.curPublishName = curPublish.name;
				if (curPublish.IsCommandRunning())
				{
					if (curPublish.watchdogAutoRestart)
						msg.statusText = "Running. (WD)";
					else
						msg.statusText = "Running.";
				}
				else
					msg.statusText = "Stopped.";
				if (curPublish.IsCommandRunning())
					msg.statusLevel = WatchdogStatusMessage.StatusLevel.Running;
				else
					msg.statusLevel = WatchdogStatusMessage.StatusLevel.Error;
			}
			comm.SendMessage(msg);	
		}
		static void comm_GotStartPublishMessage(object sender, WatchdogMessageEventArgs<StartPublishMessage> e)
		{
			if (e.msg.machineName==null || e.msg.machineName.Equals(WatchdogComm.GetMachineName()) == false) return; //not for us
			Trace.WriteLine("Got Publish Start Command for: " + e.msg.publishName + " from " + e.msg.senderName);
			StartPublish(e.msg.publishName);
		}

		static void StartPublish(string name)
		{
			//if we had an old publish, kill it.
			if (curPublish != null)
			{
				curPublish.Stop();
				curPublish.Dispose();
			}
			
			try
			{
				curPublish = Publish.Load(name, pubRoot);
			}
			catch (Exception ex)
			{
				comm.SendMessage<StartStopPublishMessageReply>(new StartStopPublishMessageReply(WatchdogComm.GetMachineName(), name, false, "Exception Loading Publish" + ex.Message));
				return;
			}
			PublishRunStatus status = new PublishRunStatus("Unknown Publish Error", false);
			try
			{
				status = curPublish.Start(pubRoot);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Exception Starting! " + ex.Message);
				comm.SendMessage<StartStopPublishMessageReply>(new StartStopPublishMessageReply(WatchdogComm.GetMachineName(), name, false, "Exception Loading Publish" + ex.Message));
				return;
			}
			Trace.WriteLine("Pub Status: " + status.text);
			curPublish.WatchdogReset += new EventHandler(curPublish_WatchdogReset);
			comm.SendMessage<StartStopPublishMessageReply>(new StartStopPublishMessageReply(WatchdogComm.GetMachineName(), curPublish.name, status.ok, status.text));
		
		}
		static Publish AutodetectActivePublish()
		{
			try
			{
				foreach (string s in Publish.GetAllPublishNames(pubRoot))
				{
					Publish p = Publish.Load(s, pubRoot);
					if (p.IsCommandRunning())
					{
						return p;
					}
				}
			}
			catch
			{ }
			return null;
		}

		static void curPublish_WatchdogReset(object sender, EventArgs e)
		{
			comm.SendMessage<StartStopPublishMessageReply>(new StartStopPublishMessageReply(WatchdogComm.GetMachineName(), curPublish.name, false, "WARNING: Watchdog reset publish: " + curPublish.name + " @ " + curPublish.lastWatchdogIntervention.ToString()));
		}

		
	}
}
