using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common;

namespace OperationalLayer.Tracking {
	interface ICommandTransport {
		event EventHandler<CarModeChangedEventArgs> CarModeChanged;

		CarMode CarMode { get; }

		void SetCommand(double? engineTorque, double? brakePressue, double? steering, TransmissionGear? gear);
		void SetTurnSignal(TurnSignal signal);
		void Flush();
	}
}
