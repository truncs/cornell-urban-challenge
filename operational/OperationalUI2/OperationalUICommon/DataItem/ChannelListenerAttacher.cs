using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.MessagingService;

namespace UrbanChallenge.OperationalUI.Common.DataItem {
	public class ChannelListenerAttacher<T> : IDisposable, IChannelListener {
		private AttachBuffer<T> buffer;
		private string channelName;

		public ChannelListenerAttacher(IAttachable<T> target, string channelName) {
			this.buffer = new AttachBuffer<T>(target, channelName);
			this.channelName = channelName;
		}

		#region IChannelListener Members

		public void MessageArrived(string channelName, object message) {
			if ((string.IsNullOrEmpty(this.channelName) || channelName == this.channelName) && message is T) {
				buffer.OnItemReceived((T)message);
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			buffer.Dispose();
		}

		#endregion
	}
}
