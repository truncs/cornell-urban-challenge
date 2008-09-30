using System;
using System.Collections.Generic;
using System.Text;

namespace OperationalLayer.Tracking {
	class NullCommandTransport : ICommandTransport {
		#region ICommandTransport Members

		public event EventHandler<CarModeChangedEventArgs> CarModeChanged;

		public UrbanChallenge.Common.CarMode CarMode {
			get { return UrbanChallenge.Common.CarMode.Unknown; }
		}

		public void SetCommand(double? engineTorque, double? brakePressue, double? steering, UrbanChallenge.Common.Vehicle.TransmissionGear? gear) {
			
		}

		public void SetTurnSignal(UrbanChallenge.Behaviors.TurnSignal signal) {
			
		}

		public void Flush() {
			
		}

		#endregion
	}
}
