using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.TestDataServer
{
	public abstract class OperationalTestFacade : MarshalByRefObject
	{
		/// <summary>
		/// Execute the Behavior
		/// </summary>
		/// <param name="behavior"></param>
		public abstract void ExecuteBehavior(Behavior behavior, Common.Coordinates location, RndfWaypointID lowerBound, RndfWaypointID upperBound);

		public abstract void Echo(string echoString);
	}
}
