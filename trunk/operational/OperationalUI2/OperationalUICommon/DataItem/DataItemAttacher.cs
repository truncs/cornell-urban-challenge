using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Client;

namespace UrbanChallenge.OperationalUI.Common.DataItem {
	public class DataItemAttacher<T> : IDisposable {
		private DataItemClient<T> source;
		private AttachBuffer<T> buffer;

		public DataItemAttacher(IAttachable<T> target, IDataItemClient source) 
			: this(target, (DataItemClient<T>)source) { }

		public DataItemAttacher(IAttachable<T> target, DataItemClient<T> source) {
			this.source = source;
			this.buffer = new AttachBuffer<T>(target, source.Name);

			source.DataValueAdded += source_DataValueAdded;
		}

		void source_DataValueAdded(object sender, ClientDataValueAddedEventArgs e) {
			buffer.OnItemReceived((T)e.Value);
		}

		#region IDisposable Members

		public void Dispose() {
			source.DataValueAdded -= source_DataValueAdded;
			buffer.Dispose();
		}

		#endregion
	}

	public static class DataItemAttacher {
		public static DataItemAttacher<T> Attach<T>(IAttachable<T> target, DataItemClient<T> source) {
			return new DataItemAttacher<T>(target, source);
		}
	}
}
