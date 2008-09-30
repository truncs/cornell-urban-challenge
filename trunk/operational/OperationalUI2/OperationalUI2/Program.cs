using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UrbanChallenge.OperationalUI {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) {
			if (args.Length > 0) {
				OperationalInterface.ConfigureRemoting(args[0]);
			}
			else {
				OperationalInterface.ConfigureRemoting(null);
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new formMain());
		}
	}
}