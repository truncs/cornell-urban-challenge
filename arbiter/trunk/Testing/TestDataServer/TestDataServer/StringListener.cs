using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.MessagingService;

namespace UrbanChallenge.Arbiter.TestDataServer
{
	public class StringListener : MarshalByRefObject, IChannelListener
	{
		public void MessageArrived(string channelName, object message)
		{
			Console.WriteLine("{0}: {1}", channelName, (string)message);
		}
	}
}
