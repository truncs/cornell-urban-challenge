using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace Dataset.Client {
	public abstract class ClientDataValueAddedEventArgs : EventArgs {
		private CarTimestamp time;

		public ClientDataValueAddedEventArgs(CarTimestamp time) {
			this.time = time;
		}

		public abstract object Value { get; }

		public CarTimestamp Time {
			get { return time; }
		}

		public abstract IDataItemClient DataItem { get; }
	}

	public class ClientDataValueAddedEventArgs<T> : ClientDataValueAddedEventArgs {
		private T value;
		private DataItemClient<T> dataItem;

		public ClientDataValueAddedEventArgs(T value, CarTimestamp time, DataItemClient<T> dataItem)
			: base(time) {
			this.value = value;
			this.dataItem = dataItem;
		}

		public override object Value {
			get { return value; }
		}

		public override IDataItemClient DataItem {
			get { return dataItem; }
		}

		public T GenericValue {
			get { return value; }
		}

		public DataItemClient<T> GenericDataItem {
			get { return dataItem; }
		}
	}
}
