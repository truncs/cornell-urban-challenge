using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	public interface IVehicleArea
	{
		double DistanceTo(Coordinates loc);

		bool ContainsVehicle(Coordinates center, double length, double width, Coordinates heading);

		string DefaultAreaId();
	}
}
