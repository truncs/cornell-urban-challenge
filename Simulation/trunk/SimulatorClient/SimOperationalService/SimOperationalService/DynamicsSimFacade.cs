using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;

namespace SimOperationalService {
	[Serializable]
	public abstract class DynamicsSimFacade : MarshalByRefObject {
		public const string ServiceName = "DynamicsSim";

		/// <summary>
		/// Called to set the current steering, brake and throttle values from the operational layer to a sim
		/// </summary>
		/// <param name="commanded_steer">Commanded steering wheel angle in radians</param>
		/// <param name="commanded_throttle">Commanded torque in N-m</param>
		/// <param name="commanded_brake">Commanded brake pressure in GM arbitrary units (22-50 basically)</param>
		public abstract void SetSteeringBrakeThrottle(double? commanded_steer, double? commanded_throttle, double? commanded_brake);

		public abstract void SetTransmissionGear(TransmissionGear gear);
	}
}
