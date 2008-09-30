using System;
using System.Collections.Generic;
using System.Text;
using NPlot;
using Dataset.Client;
using UrbanChallenge.OperationalUI.Common;
using UrbanChallenge.OperationalUI.Common.RunControl;
using System.Drawing;
using Dataset.Units;

namespace UrbanChallenge.OperationalUI.Graphing {
	class GraphItemAdapter : IDisposable {
		private string name;
		private LinePlot plot;
		private DataItemAdapter dataItemAdapter;

		private TimeWindowQueue plotQueue;
		private TimeWindowQueue recvQueue;

		private Unit sourceUnits;
		private UnitConversion conversion;
		private ConversionListWrapper conversionWrapper;
		
		private object lockobj = new object();

		public GraphItemAdapter(string name, Color color, double windowSize, DataItemAdapter dataItemAdapter) {
			// store values
			this.dataItemAdapter = dataItemAdapter;
			this.name = name;

			// get the source units
			this.sourceUnits = UnitConverter.GetUnit(dataItemAdapter.DataItemUnits);

			// create the queues
			plotQueue = new TimeWindowQueue(windowSize);
			recvQueue = new TimeWindowQueue(windowSize);

			// create the plot object
			plot = new LinePlot(plotQueue.ValueList, plotQueue.TimestampList);
			plot.Color = color;
			plot.Label = name;

			// subscribe to relevant events
			Services.RunControlService.RenderCycle += RunControlService_RenderCycle;
			dataItemAdapter.DataValueReceived += dataItemAdapter_DataValueReceived;
		}

		#region Properties

		public bool HasData {
			get { return plotQueue.Count > 0; }
		}

		public double MinValue {
			get {
				if (conversion != null) {
					return conversion.Convert(plotQueue.MinValue);
				}
				else {
					return plotQueue.MinValue;
				}
			}
		}

		public double MaxValue {
			get {
				if (conversion != null) {
					return conversion.Convert(plotQueue.MaxValue);
				}
				else {
					return plotQueue.MaxValue;
				}
			}
		}

		public double MaxTimestamp {
			get { return plotQueue.MaxTimestamp; }
		}

		public double MinTimestamp {
			get { return plotQueue.MinTimestamp; }
		}

		public double WindowSize {
			get { return plotQueue.WindowSize; }
			set {
				plotQueue.WindowSize = value;
				recvQueue.WindowSize = value;
			}
		}

		public LinePlot LinePlot {
			get { return plot; }
		}

		public string Name {
			get { return name; }
			set {
				name = value;
				plot.Label = name;
			}
		}

		public UnitConversion Conversion {
			get { return conversion; }
			set {
				conversion = value;
				if (conversion != null && !object.Equals(conversion.To, conversion.From)) {
					conversionWrapper = new ConversionListWrapper(conversion, plotQueue.ValueList);
					plot.OrdinateData = conversionWrapper;
				}
				else {
					plot.OrdinateData = plotQueue.ValueList;
					conversionWrapper = null;
				}
			}
		}

		public DataItemAdapter DataItemAdapter {
			get { return dataItemAdapter; }
		}

		public Unit SourceUnits {
			get { return sourceUnits; }
			set { sourceUnits = value; }
		}

		#endregion 

		#region Utility Methods

		public void Clear() {
			plotQueue.Clear();
		}

		#endregion

		#region Event Handlers

		void dataItemAdapter_DataValueReceived(double value, double timestamp) {
			lock (lockobj) {
				// add the new value/timestamp to the queue
				recvQueue.Add(value, timestamp);
			}
		}

		void RunControlService_RenderCycle(object sender, EventArgs e) {
			lock (lockobj) {
				if (Services.RunControlService.RunMode != RunMode.Paused && recvQueue.Count > 0) {
					// add the newly received data to the plot queue
					plotQueue.AddRange(recvQueue);
					// clear the receive queue
					recvQueue.Clear();
				}
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			dataItemAdapter.Dispose();
		}

		#endregion
	}
}
