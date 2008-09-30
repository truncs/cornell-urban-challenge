using System;
using System.Collections.Generic;
using System.Text;
using Dataset.Client;
using System.Globalization;
using Dataset.Units;

namespace UrbanChallenge.OperationalUI.Graphing {
	delegate void DataValueHandler(double value, double timestamp);

	abstract class DataItemAdapter : IDisposable {
		public event DataValueHandler DataValueReceived;

		protected void OnValueReceived(double value, double timestamp) {
			if (DataValueReceived != null) {
				DataValueReceived(value, timestamp);
			}
		}

		public abstract string DataItemUnits { get; }

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing) { }

		#endregion

		#region Static builder methods

		private static Type convertGenericType = typeof(ConvertDataItemAdapter<>);
		private static Type convertibleType = typeof(IConvertible);

		public static bool HasDefaultAdapter(IDataItemClient dataItem) {
			return convertibleType.IsAssignableFrom(dataItem.DataType);
		}

		public static DataItemAdapter GetDefaultAdapter(IDataItemClient dataItem) {
			switch (Type.GetTypeCode(dataItem.DataType)) {
				case TypeCode.Double:
					return new DoubleDataItemAdapter((DataItemClient<double>)dataItem);

				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.DateTime:
				case TypeCode.Decimal:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return BuildConvertAdapter(dataItem);

				case TypeCode.Object:
					if (convertibleType.IsAssignableFrom(dataItem.DataType)) {
						return BuildConvertAdapter(dataItem);
					}
					break;
			}

			throw new ArgumentException("The supplied data item type cannot be converted to a double");
		}

		private static DataItemAdapter BuildConvertAdapter(IDataItemClient dataItem) {
			Type convertType = convertGenericType.MakeGenericType(dataItem.DataType);
			return (DataItemAdapter)Activator.CreateInstance(convertType, dataItem);
		}

		#endregion
	}

	class DoubleDataItemAdapter : DataItemAdapter {
		private DataItemClient<double> dataItem;

		public DoubleDataItemAdapter(DataItemClient<double> dataItem) {
			this.dataItem = dataItem;
			dataItem.DataValueAdded += dataItem_DataValueAdded;
		}

		void dataItem_DataValueAdded(object sender, ClientDataValueAddedEventArgs e) {
			ClientDataValueAddedEventArgs<double> ev = (ClientDataValueAddedEventArgs<double>)e;

			OnValueReceived(ev.GenericValue, ev.Time.ts);
		}

		public override string DataItemUnits {
			get { return dataItem.Units; }
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				dataItem.DataValueAdded -= dataItem_DataValueAdded;
			}
		}
	}

	class ConvertDataItemAdapter<T> : DataItemAdapter where T : IConvertible {
		private DataItemClient<T> dataItem;

		public ConvertDataItemAdapter(IDataItemClient dataItem)
			: this((DataItemClient<T>)dataItem) { }

		public ConvertDataItemAdapter(DataItemClient<T> dataItem) {
			this.dataItem = dataItem;
			dataItem.DataValueAdded += dataItem_DataValueAdded;
		}

		void dataItem_DataValueAdded(object sender, ClientDataValueAddedEventArgs e) {
			ClientDataValueAddedEventArgs<T> ev = (ClientDataValueAddedEventArgs<T>)e;

			OnValueReceived(Convert.ToDouble(ev.GenericValue), ev.Time.ts);
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				dataItem.DataValueAdded -= dataItem_DataValueAdded;
			}
		}

		public override string DataItemUnits {
			get { return dataItem.Units; }
		}
	}
}
