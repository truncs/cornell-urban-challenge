using System;
using System.IO;
using System.ComponentModel;
using System.ServiceProcess;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Diagnostics;


namespace Watchdog
{

	public class WindowsService : ServiceBase
	{

#if SERVICE
		static void Main() {
			// Create one WindowsService and run it.
			ServiceBase[] servicesToRun = new ServiceBase[] { new WindowsService() };
			ServiceBase.Run(servicesToRun);
			
		}
#endif

		public WindowsService()
		{
			InitializeComponent();
			// Set the ServiceName in constructor so the ServiceInstaller can find it.
			this.ServiceName = "Watchdog";
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				Program.Init();
			}
			catch
			{ }
			NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(NetworkChange_NetworkAvailabilityChanged);
			Program.MainLoop();
		}

		void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			if (e.IsAvailable)
			{
				Program.Init();
				Trace.WriteLine("Network became available"); 
			}
			else
			{
				Program.KillSocket();
				Trace.WriteLine("Network went down"); 
			}
		}

		protected override void OnStop()
		{
			Program.KillApp();	
		}
		
		
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.ServiceName = "Service1";
		}

		#endregion

		private IContainer components = null;

	}

}
