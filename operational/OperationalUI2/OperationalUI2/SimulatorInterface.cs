using System;
using System.Collections.Generic;
using System.Text;
using Simulator;

namespace UrbanChallenge.OperationalUI {
	static class SimulatorInterface {
		private static SimulatorFacade simFacade;

		public static void Attach() {
			simFacade = (SimulatorFacade)OperationalInterface.ObjectDirectory.Resolve("SimulationServer");
		}

		public static SimulatorFacade SimulatorFacade {
			get { return simFacade; }
		}
	}
}
