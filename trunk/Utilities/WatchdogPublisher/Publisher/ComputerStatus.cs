using System;
using System.Collections.Generic;
using System.Text;
using WatchdogCommunication;

namespace Publisher
{
	public class ComputerStatus
	{
		public WatchdogStatusMessage msg;
		public bool valid;
		public bool isPublishing;
		public ComputerStatus(WatchdogStatusMessage msg, bool valid)
		{
			this.msg = msg; this.valid = valid; isPublishing = false;
		}
	}
}
