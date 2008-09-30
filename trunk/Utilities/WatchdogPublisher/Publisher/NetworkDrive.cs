using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace Publisher
{
	//this was jacked from codeproject and fixed to not suck

	
	public static class NetworkDrive
	{
		public static string GetNextAvailableDrive ()
		{
			string[] drives = Directory.GetLogicalDrives();
			
			for (int i = 6; i < 26; i++) //start at G
			{
				string testDrive = Encoding.ASCII.GetString(new byte[] { (byte)(i + 65) }) + ":\\";
				string toRet = Encoding.ASCII.GetString(new byte[] { (byte)(i + 65) }) + ":";
				bool driveTaken = false;
				foreach (string existingDrive in drives) {
					if (string.Equals(testDrive, existingDrive, StringComparison.InvariantCultureIgnoreCase)) {
						driveTaken = true;
						break;
					}
				}

				if (!driveTaken) {
					return toRet;
				}
			}
			return "z:"; //out of drives!!!
		}

		
		#region API

		[DllImport("mpr.dll")]
		private static extern int WNetAddConnection2A(ref structNetResource pstNetRes, string psPassword, string psUsername, int piFlags);
		[DllImport("mpr.dll")]
		private static extern int WNetCancelConnection2A(string psName, int piFlags, int pfForce);
		[DllImport("mpr.dll")]
		private static extern int WNetConnectionDialog(int phWnd, int piType);
		[DllImport("mpr.dll")]
		private static extern int WNetDisconnectDialog(int phWnd, int piType);
		[DllImport("mpr.dll")]
		private static extern int WNetRestoreConnectionW(int phWnd, string psLocalDrive);

		[StructLayout(LayoutKind.Sequential)]
		private struct structNetResource
		{
			public int iScope;
			public int iType;
			public int iDisplayType;
			public int iUsage;
			public string sLocalName;
			public string sRemoteName;
			public string sComment;
			public string sProvider;
		}

		private const int RESOURCETYPE_DISK = 0x1;

		//Standard	
		private const int CONNECT_INTERACTIVE = 0x00000008;
		private const int CONNECT_PROMPT = 0x00000010;
		private const int CONNECT_UPDATE_PROFILE = 0x00000001;
		//IE4+
		private const int CONNECT_REDIRECT = 0x00000080;
		//NT5 only
		private const int CONNECT_COMMANDLINE = 0x00000800;
		private const int CONNECT_CMD_SAVECRED = 0x00001000;

		#endregion

		// Map network drive
		public static void zMapDrive(string psUsername, string psPassword, string localDrive, string sharename)
		{
			//create struct data
			structNetResource stNetRes = new structNetResource();
			stNetRes.iScope = 2;
			stNetRes.iType = RESOURCETYPE_DISK;
			stNetRes.iDisplayType = 3;
			stNetRes.iUsage = 1;
			stNetRes.sRemoteName = sharename;
			stNetRes.sLocalName = localDrive;
			//prepare params
			int iFlags = 0;
			iFlags += CONNECT_CMD_SAVECRED;
			iFlags += CONNECT_UPDATE_PROFILE;
			//iFlags += CONNECT_INTERACTIVE + CONNECT_PROMPT; do not prompt for credentials

			//if force, unmap ready for new connection
			try { zUnMapDrive(true, localDrive); }
			catch { }
			//call and return
			int i = WNetAddConnection2A(ref stNetRes, psPassword, psUsername, iFlags);
			if (i > 0) { throw new System.ComponentModel.Win32Exception(i); }
		}


		// Unmap network drive	
		public static void zUnMapDrive(bool pfForce, string localDrive)
		{
			//call unmap and return
			int iFlags = 0;
			iFlags += CONNECT_UPDATE_PROFILE; //if persistant only
			int i = WNetCancelConnection2A(localDrive, iFlags, Convert.ToInt32(pfForce));
			//if (i != 0) i = WNetCancelConnection2A(sharename, iFlags, Convert.ToInt32(pfForce));  //disconnect if localname was null
			if (i== 2250) //network connection doesnt exist
			{ }

			else if (i > 0) { 
				throw new System.ComponentModel.Win32Exception(i); 
			}
		}
	}
}
