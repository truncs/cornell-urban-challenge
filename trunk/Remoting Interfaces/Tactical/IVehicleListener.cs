using System;
using UrbanChallenge.Common;

namespace UrbanChallenge.Tactical {

	public interface IVehicleListener {

		void StatusUpdate(IVehicle vehicle, VehicleStatus status, DateTime timeStamp);

	}

}
