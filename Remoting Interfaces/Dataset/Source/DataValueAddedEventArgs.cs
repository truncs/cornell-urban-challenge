using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace Dataset.Source {
	public abstract class SourceDataValueAddedEventArgs : EventArgs {
		public abstract IDataItemSource AbstractDataItem { get; }
		public abstract object ObjectValue { get; }
		public abstract CarTimestamp Time { get; }
	}

	public class SourceDataValueAddedEventArgs<T> : SourceDataValueAddedEventArgs {
		private T value;
		private CarTimestamp time;
		private DataItemSource<T> dataItem;

		public SourceDataValueAddedEventArgs(T value, CarTimestamp time, DataItemSource<T> dataItem) {
			this.value = value;
			this.time = time;
			this.dataItem = dataItem;
		}

		public override IDataItemSource AbstractDataItem  {
			get { return dataItem; }
		}

		public DataItemSource<T> DataItem {
			get { return dataItem; }
		}

		public override object ObjectValue {
			get { return value; }
		}

		public override CarTimestamp Time {
			get { return time; }
		}

		public T Value {
			get { return value; }
		}
	}
}
