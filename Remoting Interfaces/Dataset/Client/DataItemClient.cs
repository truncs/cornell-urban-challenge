using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace Dataset.Client {
	public class DataItemClient<T> : IDataItemClient {
		protected T currentValue;
		protected CarTimestamp currentValueTime;
		private string name;
		private string units;

		public DataItemClient(string name) {
			this.name = name;
		}

		public DataItemClient(DataItemDescriptor desc) {
			this.name = desc.Name;
			this.units = desc.Units;
		}

		protected virtual void OnDataValueAdded(object val, CarTimestamp t) {
			try {
				currentValue = (T)val;
				currentValueTime = t;
				if (DataValueAdded != null) {
					DataValueAdded(this, new ClientDataValueAddedEventArgs<T>(currentValue, t, this));
				}
			}
			catch (Exception) {
				// don't do anything
			}
		}

		public T CurrentValue {
			get { return currentValue; }
		}

		#region IDataItemClient Members

		public event EventHandler<ClientDataValueAddedEventArgs> DataValueAdded;

		public Type DataType {
			get { return typeof(T); }
		}

		public string Name {
			get { return name; ; }
		}

		object IDataItemClient.CurrentValue {
			get { return currentValue; }
		}

		public CarTimestamp CurrentValueTime {
			get { return currentValueTime; }
		}

		public void AddDataItem(object val, CarTimestamp t) {
			OnDataValueAdded(val, t);
		}

		public string Units {
			get { return units; }
		}

		#endregion
	}
}
