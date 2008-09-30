using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUI.Common.RunControl;

namespace UrbanChallenge.OperationalUI.Common.DataItem {
	public class AttachBuffer<T> : IDisposable {
		private IAttachable<T> target;
		private string label;

		private bool gotNewRenderValue;
		private T lastRenderValue;
		private bool gotNewRecvValue;
		private T lastRecvValue;

		private bool disposed;

		public AttachBuffer(IAttachable<T> target, string label) {
			this.target = target;
			this.label = label;

			Services.RunControlService.RenderCycle += runControl_RenderCycle;
		}

		void runControl_RenderCycle(object sender, EventArgs e) {
			if (gotNewRecvValue && Services.RunControlService.RunMode == RunMode.Realtime) {
				lastRenderValue = lastRecvValue;
				gotNewRecvValue = false;
				gotNewRenderValue = true;
			}

			if (gotNewRenderValue) {
				target.SetCurrentValue(lastRenderValue, label);
				gotNewRenderValue = false;
			}
		}

		public void OnItemReceived(T value) {
			if (disposed) {
				throw new ObjectDisposedException("AttachBuffer");
			}

			lastRecvValue = value;
			gotNewRecvValue = true;
		}

		#region IDisposable Members

		public void Dispose() {
			Services.RunControlService.RenderCycle -= runControl_RenderCycle;
			disposed = true;
		}

		#endregion
	}
}
