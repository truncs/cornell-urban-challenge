using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.DarpaRndf
{
	[Serializable]
	public class IMdf
	{
		// Holds the numbered, ordered checkpoints.
		public ICollection<string> CheckpointOrder;

		// Holds speed limits.
		public ICollection<SpeedLimit> SpeedLimits;

		// Other data.
		public string Name;
		public string RndfName;
		public string Version;
		public string CreationDate;
		public string NumberCheckpoints;
		public string NumberSpeedLimits;
	}
}
