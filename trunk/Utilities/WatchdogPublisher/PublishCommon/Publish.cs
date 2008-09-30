using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using System.Diagnostics;

namespace PublishCommon
{	
	

	[Serializable]
	public class RemotePublishLocation
	{		
		public string name;
		public string localDrive;
		public string remoteShare;
		public string username="labuser";
		public string password="dgcee05";
		public bool removeShare = false;

		public override string ToString()
		{
			return name.ToString();
		}
	}
	
	public class PublishRunStatus
	{
		public bool ok;
		public string text;
		public PublishRunStatus(string text, bool ok)
		{
			this.ok = ok; this.text = text;
		}
	}

	[Serializable]	
	public class Publish : IDisposable 
	{
		[NonSerialized]
		List<Process> processes = new List<Process>();		
		[NonSerialized]
		private static XmlSerializer pubSerializer = new XmlSerializer(typeof(Publish));

		[NonSerialized]
		public static string pubExt = ".pbl";

		[NonSerialized]
		Thread watchdogThread;
		[NonSerialized]
		bool runWatchdogThread=true;
		[NonSerialized]
		string pubRoot="";

		public bool watchdogAutoRestart=false;
		public List<string> watchdogNames = new List<string>();
		public int watchdogPeriodms = 2000;
		public DateTime lastWatchdogIntervention = DateTime.MinValue;		
		public int version = 1;
		public string name;
		public List<string> files;
		public string relativeLocation;
		public DateTime lastPublish;
		public List<string> commands;
		

		[NonSerialized]
		public bool active;

		public static string FilenameFromPublishname(string publishName, string pubRoot)
		{
			if (pubRoot.EndsWith ("\\"))
				return pubRoot  + publishName + pubExt;
			else
				return pubRoot + "\\"  + publishName + pubExt;
		}

		public static List<string> GetAllPublishNames(string pubRoot)
		{
			List<string> ret = new List<string>();
			if (Directory.Exists(pubRoot))
			{
				string[] files = Directory.GetFiles(pubRoot, "*" + pubExt, SearchOption.TopDirectoryOnly);
				foreach (string s in files)
				{
					ret.Add(Path.GetFileNameWithoutExtension(s));
				}
			}			
			return ret;
		}

		public static bool PublishExists(string publishName, string pubRoot)
		{
			return (GetAllPublishNames(pubRoot).Exists(delegate(string s)
			{
				return s.Equals(FilenameFromPublishname(publishName, pubRoot)); 			
			})) ;
		}

		public static Publish Load(string publishName, string pubRoot)
		{
			Trace.WriteLine("Loading " + FilenameFromPublishname(publishName, pubRoot));
			if (File.Exists(FilenameFromPublishname(publishName, pubRoot)) == false)
			{
				if (Directory.Exists(pubRoot) == false) Directory.CreateDirectory(pubRoot);
				Publish p = new Publish();
				p.name = publishName;
				return p;
			}
			else
			{
				TextReader r;
				r = new StreamReader(FilenameFromPublishname(publishName, pubRoot));				
				Publish p = (Publish)pubSerializer.Deserialize(r);
				r.Close();
				return p;
			}
		}

		public void Delete(string pubRoot)
		{
			if ((this.name == "") || (this.name == null)) throw new InvalidDataException("The name cannot be null of the publish");
			if (File.Exists(FilenameFromPublishname(this.name, pubRoot)))
				File.Delete(FilenameFromPublishname(this.name, pubRoot));
		}

		public void Save(string pubRoot)
		{
			if (pubRoot!="" && Directory.Exists(pubRoot) == false) Directory.CreateDirectory(pubRoot);
			if ((this.name == "")  || (this.name == null))throw new InvalidDataException("The name cannot be null of the publish");
			TextWriter w = new StreamWriter(FilenameFromPublishname(this.name, pubRoot));
			pubSerializer.Serialize(w,this);
			w.Close();
		}

		private PublishRunStatus StartCommands(string pubRoot)
		{
			foreach (string cmd in commands)
			{
				//unfortunately this is a little ghetto...
				string filename = cmd;
				string args = "";
				if (cmd.Contains(".exe"))
				{
					filename = cmd.Substring(0, cmd.IndexOf(".exe") + 4);
					if (cmd.Length > cmd.IndexOf(".exe") + 4)
						args = cmd.Substring(cmd.IndexOf(".exe") + 5);
				}
				Process p = new Process();

				if (this.relativeLocation.EndsWith("\\"))
					p.StartInfo.FileName = pubRoot + this.relativeLocation + filename;
				else
					p.StartInfo.FileName = pubRoot + this.relativeLocation + "\\" + filename;
				p.StartInfo.Arguments = args;
				//p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
				p.StartInfo.WorkingDirectory = pubRoot + this.relativeLocation;

				try
				{
					if (p.Start() != true)
					{
						//failed
						//kill the old processes
						foreach (Process oldp in processes) oldp.Kill();
						Trace.WriteLine("Could not start " + p.StartInfo.FileName + ". Cancelled Start of Publish.");
						return new PublishRunStatus("Could not start " + p.StartInfo.FileName + ". Cancelled Start of Publish.", false);
					}
				}
				catch (Exception ex)
				{
					foreach (Process oldp in processes) oldp.Kill();
					Trace.WriteLine("Could not start the publish exec " + p.StartInfo.FileName + ". Unhandled Exception: " + ex.Message);
					return new PublishRunStatus("Could not start the publish exec " + p.StartInfo.FileName + ". Unhandled Exception: " + ex.Message, false);
				}
				this.processes.Add(p);
			}
			return new PublishRunStatus("", true);
		}
		
		/// <summary>
		/// starts the associated publish
		/// </summary>
		/// <returns></returns>
		public PublishRunStatus Start(string pubRoot)
		{			
			this.pubRoot = pubRoot;
			if (this.commands == null) return new PublishRunStatus ("No commands exist.",false);
			bool wasRunning = IsCommandRunning();
			if (wasRunning == false)
			{
				PublishRunStatus stat = StartCommands(pubRoot);
				if (stat.ok == false) return stat;
			}
			
			StartWatchdog();
			Trace.WriteLine("Started Publish OK " + DateTime.Now + " AutoReset: " + watchdogAutoRestart);
			string ret = "Publish Started OK.";

			if (wasRunning)
				ret += " Publish was Already Running.";
			if (watchdogAutoRestart)
				ret += " WD Active.";
			active = true;	
			return new PublishRunStatus(ret, true);
		}

		private bool StopCommands()
		{
			//do the easy ones first
			foreach (Process p in processes)
			{
				try
				{ p.CloseMainWindow(); }
				catch
				{ }
			}
			Thread.Sleep(1000);
			List<string> filenames = new List<string>();
			foreach (string cmd in commands)
			{
				string filename = cmd;
				string args = "";
				if (cmd.Contains(".exe"))
				{
					filename = cmd.Substring(0, cmd.IndexOf(".exe"));
					if (filename.Length > (cmd.IndexOf(".exe") + 4))
						args = cmd.Substring(cmd.IndexOf(".exe") + 5);
				}
				filenames.Add(filename);
			}

			Process[] parr = Process.GetProcesses();
			List<Process> runningProcesses = new List<Process>(parr);
			foreach (string filename in filenames)
			{
				Process p = runningProcesses.Find(new Predicate<Process>(delegate(Process proc)
				{
					return proc.ProcessName.ToLower().Equals(filename.ToLower());
				}));
				if (p != null)
				{
					try
					{
						p.Kill();
					}
					catch
					{

					}
				}
			}			
			return true;
		}
		public bool Stop()
		{
			active = false;
			bool ret = StopCommands();						
			return ret;
		}
		
		public bool IsCommandRunning()
		{			
			//so this also sucks.
			List<string> filenames = new List<string>();
			foreach (string cmd in commands)
			{							
				string filename = cmd;
				string args = "";
				if (cmd.ToLower().Contains(".exe")) 
				{
					filename = cmd.Substring(0, cmd.IndexOf(".exe"));
					if (cmd.Length > cmd.IndexOf(".exe") + 4)
						args = cmd.Substring(cmd.IndexOf(".exe") + 5);
				}
				filenames.Add(filename);
			}

			Process[] parr = Process.GetProcesses();
			List<Process> runningProcesses = new List<Process>(parr);
			foreach (string filename in filenames)
			{
				if (runningProcesses.Find(new Predicate<Process>(delegate(Process proc)
				{
					return proc.ProcessName.ToLower().Equals(filename.ToLower());
				}))==null)
				{
					return false;
				}
			}	
			return true;
		}

		public override string ToString()
		{
			return this.name;
		}

		public string GetDebugString()
		{
			StringBuilder s = new StringBuilder();			
			s.AppendLine("Name: " + this.name);
			s.AppendLine("--------------------------------------------------");
			s.AppendLine("Files: ");			
			if (files != null)
			{
				foreach (string f in this.files)
					s.AppendLine(f);
			}
			s.AppendLine("----------------------------------------------------");
			s.AppendLine("Commands:");			
			if (this.commands != null)
			{
				foreach (string cmd in commands)
				{
					string filename = cmd;
					string args = "";
					if (cmd.Contains(".exe"))
					{
						filename = cmd.Substring(0, cmd.IndexOf(".exe") + 4);
						if (cmd.Length > (cmd.IndexOf(".exe") + 4))
							args = cmd.Substring(cmd.IndexOf(".exe") + 5);
					}
					s.AppendLine("Filename: " + filename + " Args: " + args);
				}
			}
			s.AppendLine("RelativeLocation " + this.relativeLocation);
			s.AppendLine("Watchdog Enabled: " + this.watchdogAutoRestart);
			foreach (string wd in this.watchdogNames)
				s.AppendLine("Watchdog Name: " + wd);
			s.AppendLine("Watchdog Period(ms): " + this.watchdogPeriodms);
			s.AppendLine("Last Publish " + this.lastPublish);
			return s.ToString();
		}

		public void PrintDebug()
		{
			Trace.WriteLine(GetDebugString());
		}

		public void StartWatchdog()
		{			
			if (watchdogThread == null)
			{
				watchdogThread = new Thread(MonitorFunction);
				watchdogThread.Name = "watchdog" + this.name;
				watchdogThread.Start(this);
			}
		}

		public event EventHandler WatchdogReset;
		void MonitorFunction(object state)
		{
			Trace.WriteLine("Monitor Started.");

			Publish p = (state as Publish);
			if (p.watchdogNames == null || p.watchdogNames.Count == 0)
			{
				Trace.WriteLine("Cannot Monitor becuase watchdog name is null or blank.");
				return; //we cant monitor without a watchdog name
			}
			WaitHandle[] handles = new WaitHandle[p.watchdogNames.Count];
			int i = 0;
			foreach (string s in p.watchdogNames)
			{
				handles[i++] = new EventWaitHandle(false, EventResetMode.AutoReset, s);
				Trace.WriteLine("Created WatchdogHandle : " + s);
			}
			

			if (!AutoResetEvent.WaitAll(handles, 30000,false))
				Trace.WriteLine("Timed out waiting for init signal (30 seconds).");
			Thread.Sleep(p.watchdogPeriodms);
			Trace.WriteLine("Monitor Initialized. Watchdog Active: " + watchdogAutoRestart.ToString());

			

			while (runWatchdogThread)
			{
				if (!AutoResetEvent.WaitAll (handles,p.watchdogPeriodms,false))
				//if (!AutoResetEvent.WaitOne(p.watchdogPeriodms, false))//check for timeout
				{
					if (watchdogAutoRestart && active) //we have timed out, see if we should reset
					{
						lastWatchdogIntervention = DateTime.Now;
						Trace.WriteLine("Beginning Watchdog Reset @ " + DateTime.Now);
						Trace.WriteLine("Watchdog Stopping Processs...");
						if (this.StopCommands())
						{
							Thread.Sleep(p.watchdogPeriodms * 2);
							Trace.WriteLine("Watchdog Starting Processs...");
							this.StartCommands(pubRoot);
							Trace.WriteLine("Watchdog Firing Event..");
							if (WatchdogReset != null) WatchdogReset(this, null);
							Trace.WriteLine("Reinitializing Wait Handles...");
							if (!AutoResetEvent.WaitAll(handles, 30000, false))
								Trace.WriteLine("Timed out waiting for init signal (30 seconds).");
							Thread.Sleep(p.watchdogPeriodms);
						}						
					}
				}
				else
					Trace.WriteLine("Got Signals OK");
			}
			Trace.WriteLine("Monitor Exited.");
		}

		public PublishRunStatus GetStatus()
		{
			if (IsCommandRunning())
				return new PublishRunStatus("Publish Started OK.", true);
			else
				return new PublishRunStatus("Publish Not Started.", false);
		}

		#region IDisposable Members

		public void Dispose()
		{
			runWatchdogThread = false;			
		}

		#endregion
	}
}
