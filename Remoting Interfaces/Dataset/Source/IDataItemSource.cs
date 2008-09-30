using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace Dataset.Source {
	public interface IDataItemSource {
		Type DataType { get; }
		string Name { get; }
		DataTypeCode TypeCode { get; }
		DataItemDescriptor Descriptor { get; }

		DatasetSource Parent { get; set; }

		void Add(object value, CarTimestamp t);
	}
}
