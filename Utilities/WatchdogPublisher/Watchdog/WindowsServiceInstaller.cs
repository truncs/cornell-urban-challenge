using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Watchdog
{

	[RunInstallerAttribute(true)]
	public class WindowsServiceInstaller : Installer
	{
		
		public WindowsServiceInstaller()
		{


			// Initialize process installer.
			ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
			processInstaller.Account = ServiceAccount.LocalSystem;
			processInstaller.Username = "labuser";
			processInstaller.Password = "dgcee05";
			
			// Initialize service installer.
			ServiceInstaller serviceInstaller = new ServiceInstaller();
			serviceInstaller.StartType = ServiceStartMode.Automatic;
			serviceInstaller.DisplayName = "DUC Watchdog";
			serviceInstaller.ServiceName = "Watchdog";
			

			// Add private installers to collection.
			this.Installers.Add(processInstaller);
			this.Installers.Add(serviceInstaller);

			
		}
		public override void Commit(System.Collections.IDictionary savedState)
		{
			
			base.Commit(savedState);
			// Here is where we set the bit on the value in the registry.
			// Grab the subkey to our service
			RegistryKey ckey = Registry.LocalMachine.OpenSubKey(
				@"SYSTEM\CurrentControlSet\Services\Watchdog", true);
			// Good to always do error checking!
			if (ckey != null)
			{
				// Ok now lets make sure the "Type" value is there, 
				//and then do our bitwise operation on it.
				if (ckey.GetValue("Type") != null)
				{
					ckey.SetValue("Type", ((int)ckey.GetValue("Type") | 256));
				}
			}
		}

	}

}