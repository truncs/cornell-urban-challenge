using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.Core.Common.Common
{
	/// <summary>
	/// Reason to pass the vehicle
	/// </summary>
	public enum LaneChangeReason
	{
		NotApplicable,
		Navigation,
		FailedForwardVehicle,
		SlowForwardVehicle
	}
}
