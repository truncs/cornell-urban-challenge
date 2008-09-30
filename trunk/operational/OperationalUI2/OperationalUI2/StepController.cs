using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI {
	static class StepController {
		private static bool stepMode;

		public static bool StepMode {
			get { return stepMode; }
			set {
				// if we're connected to the sim, then 
				if (SimulatorInterface.SimulatorFacade != null) {
					SimulatorInterface.SimulatorFacade.StepMode = value;
				}
				else {
					if (value) {
						OperationalInterface.OperationalUIFacade.DebuggingFacade.SetStepMode();
					}
					else {
						OperationalInterface.OperationalUIFacade.DebuggingFacade.SetContinuousMode(1);
					}

					stepMode = value;
				}
			}
		}

		public static void RefreshStepMode() {
			try {
				// try to get from the operational layer first
				stepMode = OperationalInterface.OperationalUIFacade.DebuggingFacade.StepMode;
			}
			catch (Exception) {
				// if that fails, try the simulator server
				if (SimulatorInterface.SimulatorFacade != null) {
					stepMode = SimulatorInterface.SimulatorFacade.StepMode;
				}
				else {
					// rethrow the exception
					throw;
				}
			}
		}

		public static void Step() {
			if (!stepMode)
				return;

			if (SimulatorInterface.SimulatorFacade != null) {
				SimulatorInterface.SimulatorFacade.Step();
			}
			else {
				OperationalInterface.OperationalUIFacade.DebuggingFacade.Step();
			}
		}
	}
}
