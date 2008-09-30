using System;
using System.Collections.Generic;
using System.Text;
using PublishCommon;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Threading;
using System.ServiceProcess;
using CarBrowser.Config;

namespace Publisher
{
	/// <summary>
	/// Manages the local store of publishes
	/// </summary>
	public static class PublishManager
	{
		static PublishManager()
		{
		
		}
		public static void Initialize(string pubRoot)
		{
			settings = PublishSettings.Load (pubRoot);			
		}

		public static PublishSettings settings;

		public static List<string> mountedDrives;

		public static bool IsDrivesMapped()
		{
			return (mountedDrives != null);
		}
		public static bool DeployWatchdog(List<string> files)
		{
			Trace.WriteLine("Begin Deploying Watchdog----------------------");
			if (MapAllDrives() != true) return false;
			foreach (string drv in mountedDrives)
			{
				Trace.WriteLine("Copying to " + drv);
				foreach (string s in files)
				{
					string filename = Path.GetFileName(s);
					string dloc = drv + "\\deployment\\" + filename;
					if (Directory.Exists(drv + "\\deployment") == false)
						Directory.CreateDirectory(drv + "\\deployment");
					string sloc = s;
					try
					{
						File.Copy(sloc, dloc, true);
					}
					catch (Exception ex)
					{
						Trace.WriteLine("Could not copy file " + filename + " to " + drv + ". Exception:" + ex.Message);
					}
					Trace.Write(".");
				}

			}
			UnmapAllDrives();
			Trace.WriteLine("Done Deploying Watchdog.----------------------------");
			return true;
		}
		public static void JumpstartWatchdogs()
		{
			Thread t = new Thread(new ParameterizedThreadStart(JumpstartService));
			t.Start("Watchdog");
		}

		public static void JumpstartHealthmonitor()
		{
			Thread t = new Thread(new ParameterizedThreadStart(JumpstartService));
			t.Start("HealthMonitorService");
		}

		

		private static void StopService(object arg)
		{
			ServiceController serviceController = new ServiceController();
			string service = (string)arg;
			serviceController.ServiceName = service;
			Trace.WriteLine("Stopping All Services-------------------------");
			foreach (RemotePublishLocation rpl in PublishManager.settings.RemoteLocations)
			{
				string compname = rpl.remoteShare.Replace("\\\\", "");
				if (compname.Contains("\\"))
				{
					int bs = compname.IndexOf("\\");
					compname = compname.Substring(0, bs);
				}
				serviceController.MachineName = compname;
				try
				{
					serviceController.Stop();
				}
				catch (Exception ex)
				{
					Trace.WriteLine("Warning: Could not stop " + compname + ". Exception: " + ex.Message);
				}
			}
			Trace.WriteLine("Services Stopped----------------------------");			
		}

		
		private static void JumpstartService(object arg)
		{
			ServiceController serviceController = new ServiceController();
			string service = (string)arg;
			serviceController.ServiceName = service;
			
			Trace.WriteLine("Stopping All Services-----------------------");
			foreach (RemotePublishLocation rpl in PublishManager.settings.RemoteLocations)
			{
				string compname = rpl.remoteShare.Replace("\\\\", "");
				if (compname.Contains("\\"))
				{
					int bs = compname.IndexOf("\\");
					compname = compname.Substring(0, bs);
				}
				serviceController.MachineName = compname;
				try
				{
					serviceController.Stop();
				}
				catch
				{
					Trace.WriteLine("Warning: Could not stop " + compname + ".");
				}
			}
			Trace.WriteLine("Waiting for Services to Stop....");
			Thread.Sleep(3000);
			Trace.WriteLine("Restarting All Services....");
			foreach (RemotePublishLocation rpl in PublishManager.settings.RemoteLocations)
			{
				string compname = rpl.remoteShare.Replace("\\\\", "");
				if (compname.Contains("\\"))
				{
					int bs = compname.IndexOf("\\");
					compname = compname.Substring(0, bs);
				}
				serviceController.MachineName = compname;

				try
				{
					serviceController.Start();
				}
				catch
				{
					Trace.WriteLine("Failed to jumpstart " + compname);
				}
			}
			Trace.WriteLine("Jumpstart Complete----------------------");
		}
		public static void ClearAllPublishDirs()
		{
			Trace.WriteLine("Clearing all publishes!------------------");
			if (mountedDrives == null) return;
			foreach (string drv in mountedDrives)
			{
				string dloc = drv + "\\";
				if (Directory.Exists(dloc) == false) continue;
				string[] oldfiles = Directory.GetFiles(dloc);
				foreach (string s in oldfiles)
				{
					File.Delete(s); Debug.Write(".");
				}
			}
			Trace.WriteLine("Done Cleaning Publish Directories.");
		}
		public static bool UnmapAllDrives()
		{
			if (mountedDrives == null) return false;
			foreach (string s in mountedDrives)
			{
				try
				{
					NetworkDrive.zUnMapDrive(true, s);
					Debug.Write(".");
				}
				catch
				{
					Trace.WriteLine("Could not unmap network drive to " + s);
				}
			}
			mountedDrives = null;
			Trace.WriteLine("Done Unmapping Drives.");
			return true;
		}

		public static void LaunchVNCViewer(string server)
		{
			
			Process process = new Process();
			string execPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"RealVNC\VNC4\vncviewer.exe");
			File.WriteAllText(server + ".vnc", "[Connection]" +
																						Environment.NewLine +
																						"Host=" + server + 
																						Environment.NewLine +
																						"Password=d066f96b42c06a58");
			
			process.StartInfo.FileName = execPath;
			process.StartInfo.Arguments = "-config " + server + ".vnc";
			try
			{
				process.Start();
			}
			catch (Exception)
			{

			}
		}


		public static bool MapAllDrives()
		{
			Trace.WriteLine("Mapping all network drives-----------------------");
			if (mountedDrives != null) return false;

			mountedDrives = new List<string>();
			foreach (RemotePublishLocation rpl in PublishManager.settings.RemoteLocations)
			{
				try
				{
					string drv = NetworkDrive.GetNextAvailableDrive();
					NetworkDrive.zMapDrive(rpl.username, rpl.password, drv, rpl.remoteShare);
					mountedDrives.Add(drv);
					Debug.Write(".");
				}
				catch
				{
					Trace.WriteLine("Could not map network drive to " + rpl.remoteShare);
				}
			}
			Trace.WriteLine("Done Mapping Drives.");
			return true;
		}

		public static bool GetRemotePublishLocationByComputerName(string name, out RemotePublishLocation rpl)
		{			
			foreach (RemotePublishLocation r in PublishManager.settings.RemoteLocations)
			{
				if (r.remoteShare.ToLower().Contains("\\\\" + name.ToLower() + "\\"))
				{
					rpl = r;
					return true;
				}
			}			
			Trace.WriteLine("Could not find rpl named: " + name);
			rpl = null;
			return false;
		}


		public static void SyncPublishWithRemotePublishDefinition(string pubname, string pubRoot, RemotePublishLocation publoc)
		{								
			Trace.WriteLine("Attempting to sync publish " + pubname + " with " + publoc.name + ".");
			string netDrive = NetworkDrive.GetNextAvailableDrive();
			if (publoc.remoteShare.EndsWith("\\")) publoc.remoteShare = publoc.remoteShare.Remove(publoc.remoteShare.Length - 1);
			Trace.WriteLine("Mounting Drive " + netDrive + " to " + publoc.remoteShare + "...");
			try
			{
				NetworkDrive.zMapDrive(publoc.username, publoc.password, netDrive, publoc.remoteShare);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to map network drive! " + ex.Message);				
				return;
			}
			Trace.WriteLine("Copying files...");
			
			//the file SHOULD be located now on the drive with the pub name....so load it....			
			try
			{
				File.Copy(Publish.FilenameFromPublishname(pubname, netDrive), Publish.FilenameFromPublishname(pubname, pubRoot), true);
			}
			catch (IOException)
			{
				Trace.WriteLine("Could not sync the file. It may not exist on the remote machine or is locked for some other reason.");
				NetworkDrive.zUnMapDrive(true, netDrive);
				return;
			}
			
			Trace.WriteLine("Synced Published OK!");			
			return;
		}

		public static void SendPublishDefinitionOnly(Publish pub, RemotePublishLocation publoc, string pubRoot)
		{
			Trace.WriteLine("------------Attempting to send publish definition " + pub.name + " to " + publoc.name + ".---------------");
			string netDrive = NetworkDrive.GetNextAvailableDrive();
			if (publoc.remoteShare.EndsWith("\\")) publoc.remoteShare = publoc.remoteShare.Remove(publoc.remoteShare.Length - 1);
			Trace.WriteLine("Mounting Drive " + netDrive + " to " + publoc.remoteShare + "...");
			try
			{
				NetworkDrive.zMapDrive(publoc.username, publoc.password, netDrive, publoc.remoteShare);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to map network drive! " + ex.Message);
				return;
			}
			
			Trace.WriteLine("Copying files...");			
			try
			{
				File.Copy(Publish.FilenameFromPublishname(pub.name, pubRoot), netDrive + "\\" + pub.name + Publish.pubExt, true);
			}
			catch (IOException ex)
			{
				Trace.WriteLine("Error copying file " + Publish.FilenameFromPublishname(pub.name, pubRoot) + ".");
				Trace.WriteLine("Check the publish is stopped and that your definition of the publish is recent. Detail:" + ex.Message);
				NetworkDrive.zUnMapDrive(true, netDrive);
				Trace.WriteLine("Published Defition Failed!----------------------------------------");
				return;
			}
			pub.lastPublish = DateTime.Now;
			pub.Save(pubRoot);
			if (publoc.removeShare)
				NetworkDrive.zUnMapDrive(true, netDrive);
			Trace.WriteLine("Published Defition Sent OK!----------------------------------------------");			

			return;
		}

		public static void SendPublishToRemoteComputer(Publish pub, RemotePublishLocation publoc, string pubRoot, EventHandler<SendPublishEventArgs> completed, bool dobackup )
		{
			Object[] objects = new object[5];
			objects[0] = pub; objects[1] = publoc; objects[2] = pubRoot; objects[3] = completed; objects[4] = dobackup;
			
			Thread publishThread = new Thread(new ParameterizedThreadStart(SendPublish));
			publishThread.Start(objects);
		}

		public class SendPublishEventArgs : EventArgs
		{
			public bool ok;
			public SendPublishEventArgs(bool ok)
			{
				this.ok = ok;
			}
		}
		private static void SendPublish(Object o)
		{
			Object[] objects = (Object[])o;
			Publish pub = (Publish)objects[0];
			RemotePublishLocation publoc = (RemotePublishLocation)objects[1];
			string pubRoot = (string)objects[2];
			EventHandler<SendPublishEventArgs> completed = (EventHandler<SendPublishEventArgs>)objects[3];
			bool dobackup = (bool)objects[4];
			Trace.WriteLine("------------Attempting to publish " + pub.name + " to " + publoc.name + ".---------------");
			string netDrive = NetworkDrive.GetNextAvailableDrive();
			if (publoc.remoteShare.EndsWith("\\")) publoc.remoteShare = publoc.remoteShare.Remove(publoc.remoteShare.Length - 1);
			Trace.WriteLine("Mounting Drive " + netDrive + " to " + publoc.remoteShare + "...");
			try
			{
				NetworkDrive.zMapDrive(publoc.username, publoc.password, netDrive, publoc.remoteShare);
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Failed to map network drive! " + ex.Message);
				completed(pub, new SendPublishEventArgs(false));
				return;
			}
			if (dobackup)
			{
				string backupDir = DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year +  " " + DateTime.Now.Hour + "." + DateTime.Now.Minute + "." + DateTime.Now.Second;
				Trace.WriteLine("Backing up existing copy files to \\" + backupDir + "\\...");
				if (Directory.Exists (netDrive + pub.relativeLocation))
				{
					Directory.CreateDirectory (netDrive + pub.relativeLocation + "\\" +  backupDir);
					string[] files = Directory.GetFiles (netDrive + pub.relativeLocation,"*.*", SearchOption.TopDirectoryOnly);					
					foreach (string file in files)
					{
						if (file.ToLower().Contains ("log")) continue;
						try
						{
							File.Copy (file,netDrive + pub.relativeLocation + "\\" +  backupDir + "\\" + Path.GetFileName (file));
						}
						catch (Exception ex)
						{
							Trace.WriteLine ("Warning: Could not backup file " + Path.GetFileName (file) + "." + ex.Message);
						}
					}
				}
				else
					Trace.WriteLine ("Warning: No files found to backup!");
			}
			Trace.WriteLine("Copying files...");
			if (pub.files != null)
			{
				foreach (string pubFile in pub.files)
				{
					string lpath = settings.RepoRoot + pubFile;
					string filename = Path.GetFileName(pubFile);
					string rpath = netDrive + pub.relativeLocation + "\\" + filename;
					Directory.CreateDirectory(netDrive + pub.relativeLocation);
					Trace.WriteLine("copying " + filename);
					try
					{
						File.Copy(lpath, rpath, true);
					}
					catch (IOException ex)
					{
						Trace.WriteLine("Error Copying file: " + lpath + ". Detail: " + ex.Message);
						NetworkDrive.zUnMapDrive(true, netDrive);
						Trace.WriteLine("Published Failed!----------------------------------------");
						completed(pub, new SendPublishEventArgs(false));
						return;
					}
					catch (NotSupportedException ex)
					{
						Trace.WriteLine("Error Copying file: " + lpath + ". The file does not appear to be in the repository. Detail: " + ex.Message);
						NetworkDrive.zUnMapDrive(true, netDrive);
						Trace.WriteLine("Published Failed!----------------------------------------");
						completed(pub, new SendPublishEventArgs(false));
						return;
					}
				}
			}
			try
			{
				File.Copy(Publish.FilenameFromPublishname(pub.name, pubRoot), netDrive + "\\" + pub.name + Publish.pubExt, true);
			}
			catch (IOException ex)
			{
				Trace.WriteLine("Error copying file " + Publish.FilenameFromPublishname(pub.name, pubRoot) + ".");
				Trace.WriteLine("Check the publish is stopped and that your definition of the publish is recent. Detail:" + ex.Message);
				NetworkDrive.zUnMapDrive(true, netDrive);
				completed(pub, new SendPublishEventArgs(false));
				Trace.WriteLine("Published Failed!----------------------------------------");
				return;
			}
			pub.lastPublish = DateTime.Now;
			pub.Save(pubRoot);
			if (publoc.removeShare)
				NetworkDrive.zUnMapDrive(true, netDrive);
			Trace.WriteLine("Published OK!----------------------------------------------");
			completed(pub, new SendPublishEventArgs(true));

			return;
		}
		internal static void KillHealthMonitor()
		{
			Thread t = new Thread(new ParameterizedThreadStart(StopService));
			t.Start("HealthMonitorService");
		}

		internal static void KillAllWatchdogs()
		{
			Thread t = new Thread(new ParameterizedThreadStart(StopService));
			t.Start("Watchdog");
		}

		internal static void ImportSettingsFile(string settings, string pubRoot)
		{
			string oldroot = PublishManager.settings.RepoRoot; //save this always
			XmlSerializer pubSerializer = new XmlSerializer(typeof(PublishSettings));
			if (File.Exists(settings) == false) return;
			TextReader r = new StreamReader(settings);
			PublishSettings s = (PublishSettings)pubSerializer.Deserialize(r);
			r.Close();
			s.RepoRoot = oldroot;
			PublishManager.settings = s;
			s.Save(pubRoot);
		}

		
	}
	[Serializable]
	public class PreferredLocation
	{
		public string computername; public string publishname;
		public PreferredLocation(string computername, string publishname) { this.computername = computername; this.publishname = publishname; }
		public PreferredLocation()
		{ }
	}
	[Serializable]
	public class PublishSettings
	{
		[NonSerialized]
		private static XmlSerializer pubSerializer = new XmlSerializer(typeof(PublishSettings));
		[NonSerialized]
		private static string pubSettingFile = "settings.xml";

		public string RepoRoot = "";
		public List<RemotePublishLocation> RemoteLocations = new List<RemotePublishLocation>();
		public List<PreferredLocation> PreferredRemotePublish = new List<PreferredLocation>();
		[XmlArrayItem("microConfig")]
		public List<MicroConfig> microcontrollers = new List<MicroConfig>();
		
		public static PublishSettings Load(string pubRoot)
		{
			Trace.WriteLine("Loading " + pubSettingFile);
			if (File.Exists(pubRoot + "\\" + pubSettingFile) == false)
			{
				if (pubRoot != ""  && Directory.Exists(pubRoot) == false) Directory.CreateDirectory(pubRoot);
				Trace.WriteLine("Finished Loading Settings----------------------------------------");
				return new PublishSettings();
			}
			else
			{
				TextReader r = new StreamReader(pubRoot + "\\"+pubSettingFile);
				PublishSettings s = (PublishSettings)pubSerializer.Deserialize(r);
				r.Close();
				Trace.WriteLine("Finished Loading Settings----------------------------------------");
				return s;
			}
		}

		public void Save(string pubRoot)
		{
			if (pubRoot != "" &&  Directory.Exists(pubRoot) == false) Directory.CreateDirectory(pubRoot);
			TextWriter w = new StreamWriter(pubRoot + "\\" + pubSettingFile);
			pubSerializer.Serialize(w, this);
			w.Close();
		}

	}
}
