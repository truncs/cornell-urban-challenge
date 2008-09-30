using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Engine
{
	/// <summary>
	/// State of the simulator engine
	/// </summary>
	[Serializable]
	public enum SimEngineState
	{
		Stopped,
		Running
	}
}
