using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Any object that implements this can be used in a priorityQueue of any implementation
	/// </summary>
	public interface IPriorityNode
	{
		string Name
		{
			get;
		}

		double Value
		{
			get;
		}
	}
}
