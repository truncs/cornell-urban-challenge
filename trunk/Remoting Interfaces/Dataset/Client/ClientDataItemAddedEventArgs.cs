using System;
using System.Collections.Generic;
using System.Text;

namespace Dataset.Client {
	public class ClientDataItemAddedEventArgs : EventArgs {
		private IDataItemClient dataItem;

		public ClientDataItemAddedEventArgs(IDataItemClient item) {
			this.dataItem = item;
		}

		public IDataItemClient DataItem {
			get { return dataItem; }
		}
	}
}
