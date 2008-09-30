using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Remora
{
	/// <summary>
	/// Remora provides remote viewing and control of the Arbiter.
	/// It starts up and can bind an Arbiter Remote Facade for control of the Arbiter
	/// It also Listens on the message service with an AiVeiwerListener for updates about what the Arbiter did
	/// </summary>
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new RemoraDisplay());
		}
	}
}