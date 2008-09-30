using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Dataset.Source {
	[Serializable]
	public class DatasetSourceFacade : MarshalByRefObject {
		private DatasetSource ds;

		public DatasetSourceFacade(DatasetSource ds) {
			this.ds = ds;
		}

		public DataItemDescriptor[] GetDataItems() {
			List<DataItemDescriptor> dataItems = new List<DataItemDescriptor>();
			foreach (IDataItemSource di in ds.Values) {
				dataItems.Add(di.Descriptor);
			}

			return dataItems.ToArray();
		}

		public DataItemDescriptor GetDataItem(string name) {
			IDataItemSource item;
			if (ds.TryGetValue(name, out item)) {
				return item.Descriptor;
			}
			else {
				return null;
			}
		}

		public EndPoint GetSourceEndPoint() {
			return ds.Sender.LocalEndpoint;
		}

		public void RegisterListener(IPEndPoint endpoint) {
			ds.Sender.RegisterListener(endpoint);
		}

		public void RemoveListener(IPEndPoint endpoint) {
			ds.Sender.RemoveListener(endpoint);
		}

		public void ClearListeners() {
			ds.Sender.ClearListeners();
		}

		public IPEndPoint[] GetListeners() {
			return ds.Sender.GetListeners();
		}

	}
}
