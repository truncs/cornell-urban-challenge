using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace operwatchdog {
	class Program {
		static void Main(string[] args) {
			while (true) {
				Process operProcess = new Process();
				operProcess.StartInfo.FileName = "operational.exe";
				operProcess.StartInfo.Arguments = "/f";
				operProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				operProcess.StartInfo.ErrorDialog = false;
				operProcess.StartInfo.UseShellExecute = false;

				Console.WriteLine("starting operational");

				if (operProcess.Start()) {
					operProcess.WaitForExit();
					operProcess.Dispose();
				}
				else {
					Console.WriteLine("could not start process");
					Console.ReadLine();
				}
			}
		}
	}
}
