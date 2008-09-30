using System;
using System.Collections.Generic;
using System.Text;
using OperationalLayer.Tracing;
using System.Diagnostics;
using System.Threading;
using UrbanChallenge.Common.Mapack;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer {
	class Program {
		static void Main(string[] args) {
			foreach (string arg in args) {
				string lowerArg = arg.ToLower();
				if (lowerArg == "/test" || lowerArg == "/t" || lowerArg == "-test" || lowerArg == "-t") {
					Settings.TestMode = true;
				}
			}

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			AsyncFlowControl flowControl = ExecutionContext.SuppressFlow();

			OperationalTrace.WriteInformation("starting up operational");

			Console.SetWindowSize(Math.Min(130, Console.LargestWindowWidth), Math.Min(50, Console.LargestWindowHeight));
			Console.SetBufferSize(Math.Min(130, Console.LargestWindowWidth), 999);

			OperationalBuilder.Build();

			Console.Write("operational> ");

			flowControl.Undo();

			while (Console.ReadLine() != "quit") {
			}
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			Console.WriteLine("unhandled stuff: " + e.ExceptionObject.ToString());
		}
	}
}
