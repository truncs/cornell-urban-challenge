using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace Dataset.Source {
	public class DataItemSource<T> : IDataItemSource {
		private T currentValue;
		private CarTimestamp currentValueTime;

		private DataTypeCode dtc;
		private string name;
		private DataItemDescriptor desc;

		protected DatasetSource parent;

		public DataItemSource(DataItemDescriptor ds) {
			this.desc = ds;
			this.dtc = ds.DataTypeCode;
			this.name = ds.Name;
		}

		public T CurrentValue {
			get { return currentValue; }
		}

		public CarTimestamp CurrentTime {
			get { return currentValueTime; }
		}

		public virtual void Add(T value, CarTimestamp t) {
			currentValue = value;
			currentValueTime = t;

			if (DataValueAdded != null) {
				DataValueAdded(this, new SourceDataValueAddedEventArgs<T>(value, t, this));
			}

			if (parent != null)
				parent.OnDataItemValueAdded(new SourceDataValueAddedEventArgs<T>(value, t, this));
		}

		public event EventHandler<SourceDataValueAddedEventArgs<T>> DataValueAdded;

		#region IDataItemSource Members

		void IDataItemSource.Add(object val, CarTimestamp t) {
			Add((T)val, t);
		}

		public virtual DatasetSource Parent {
			get { return parent; }
			set { parent = value; }
		}

		public Type DataType {
			get { return typeof(T); }
		}

		public string Name {
			get { return name; }
		}

		public DataTypeCode TypeCode {
			get { return dtc; }
		}

		public DataItemDescriptor Descriptor {
			get { return desc; }
		}

		#endregion
	}
}
