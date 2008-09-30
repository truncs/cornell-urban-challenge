using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Client;
using UrbanChallenge.OperationalUI.Common.RunControl;

namespace UrbanChallenge.OperationalUI.Common.DataItem {
	public class DataItemBuffer<T> : IDisposable {
		private DataItemClient<T> dataItem;
		private T lastValue;

		private bool disposed;

		public DataItemBuffer(IDataItemClient dataItem) : this((DataItemClient<T>)dataItem) { }

		public DataItemBuffer(DataItemClient<T> dataItem) {
			this.dataItem = dataItem;

			if (Services.RunControlService != null) {
				Services.RunControlService.RenderCycle += RunControlService_RenderCycle;
			}

			lastValue = dataItem.CurrentValue;
		}

		void RunControlService_RenderCycle(object sender, EventArgs e) {
			if (Services.RunControlService.RunMode == RunMode.Realtime) {
				lastValue = dataItem.CurrentValue;
			}
		}

		public T CurrentValue {
			get {
				if (disposed)
					throw new ObjectDisposedException("DataItemBuffer");

				return lastValue; 
			}
		}

		#region IDisposable Members

		public void Dispose() {
			Services.RunControlService.RenderCycle -= RunControlService_RenderCycle;
			disposed = true;
		}

		#endregion
	}
}
