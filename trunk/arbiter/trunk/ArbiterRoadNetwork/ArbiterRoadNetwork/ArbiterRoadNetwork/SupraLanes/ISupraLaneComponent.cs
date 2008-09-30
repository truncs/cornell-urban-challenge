using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	public interface ISupraLaneComponent
	{
		ISupraLaneComponent NextComponent
		{
			get;
			set;
		}

		ISupraLaneComponent PreviousComponent
		{
			get;
			set;
		}
	}
}
