using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace Dataset.Client {
	public interface IDataItemClient {
		event EventHandler<ClientDataValueAddedEventArgs> DataValueAdded;

		void AddDataItem(object val, CarTimestamp t);

		Type DataType { get; }
		string Name { get; }
		object CurrentValue { get; }
		CarTimestamp CurrentValueTime { get; }
		string Units { get; }

		// miscellaneous useful things
	}
}
