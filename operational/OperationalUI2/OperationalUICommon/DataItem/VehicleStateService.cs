using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using Dataset.Client;

namespace UrbanChallenge.OperationalUI.Common.DataItem {
	public class VehicleStateService {
		private DataItemClient<Coordinates> locationDataItem;
		private DataItemBuffer<Coordinates> locationBuffer;

		private DataItemClient<double> headingDataItem;
		private DataItemBuffer<double> headingBuffer;

		private string name;

		public VehicleStateService() {
			name = "";
		}

		public VehicleStateService(string name, DataItemClient<double> headingDataItem, DataItemClient<Coordinates> locationDataItem) {
			this.name = name;
			this.HeadingDataItem = headingDataItem;
			this.LocationDataItem = locationDataItem;
		}

		public string Name {
			get { return name; }
		}

		public Coordinates Location {
			get { return locationBuffer.CurrentValue; }
		}

		public double Heading {
			get { return headingBuffer.CurrentValue; }
		}

		public DataItemClient<Coordinates> LocationDataItem {
			get { return locationDataItem; }
			set {
				if (locationBuffer != null) {
					locationBuffer.Dispose();
					locationBuffer = null;
				}

				locationDataItem = value;

				if (locationDataItem != null) {
					locationBuffer = new DataItemBuffer<Coordinates>(locationDataItem);
				}
			}
		}

		public DataItemClient<double> HeadingDataItem {
			get { return headingDataItem; }
			set {
				if (headingBuffer != null) {
					headingBuffer.Dispose();
					headingBuffer = null;
				}

				headingDataItem = value;

				if (headingDataItem != null) {
					headingBuffer = new DataItemBuffer<double>(headingDataItem);
				}
			}
		}
	}
}
