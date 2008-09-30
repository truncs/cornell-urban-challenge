using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Dataset.Client;
using UrbanChallenge.OperationalUI.Utilities;
using Dataset.Units;
using UrbanChallenge.OperationalUI.Common;
using NPlot;

namespace UrbanChallenge.OperationalUI.Graphing {
	public partial class formGraph : Form {
		private const double min_grace = 0.1;
		private const double max_grace = 0.1;
		private const double zero_lever = 0.25;

		private static readonly Color[] colors = new Color[] { Color.DarkRed, Color.DarkBlue, Color.DarkGreen, Color.DarkViolet, Color.DarkOrange, Color.Goldenrod, Color.Gray, Color.DarkCyan };

		private int colorInd = 0;
		private double windowSize = 30;
		private List<GraphItemAdapter> graphItems = new List<GraphItemAdapter>();

		private LinearAxis yaxis;
		private LinearAxis xaxis;

		public formGraph() {
			InitializeComponent();

			menuWindow15.Tag = 15.0;
			menuWindow30.Tag = 30.0;
			menuWindow1Min.Tag = 60.0;
			menuWindow2Min.Tag = 120.0;
			menuWindow5Min.Tag = 300.0;

			// set up the axes
			xaxis = new LinearAxis(0, windowSize);
			yaxis = new LinearAxis(-1, 1);

			plotSurface.XAxis1 = xaxis;
			plotSurface.YAxis1 = yaxis;

			// register for the draw cycle event
			Services.RunControlService.DrawCycle += new EventHandler(RunControlService_DrawCycle);
		}

		private void formGraph_FormClosed(object sender, FormClosedEventArgs e) {
			Services.RunControlService.DrawCycle -= RunControlService_DrawCycle;

			foreach (GraphItemAdapter graphItem in graphItems) {
				graphItem.Dispose();
			}
		}

		public void AddGraphItem(string item) {
			if (CanAddItem(item)) {
				IDataItemClient dataItem = OperationalInterface.Dataset[item];
				DataItemAdapter dataAdapter = DataItemAdapter.GetDefaultAdapter(dataItem);

				Color itemColor = colors[colorInd++];
				GraphItemAdapter graphItem = new GraphItemAdapter(item, itemColor, windowSize, dataAdapter);

				// add tool bar button 
				// create the image for the button
				Bitmap image = (Bitmap)Properties.Resources.GraphIcon.Clone();
				ImageColorizer.Colorize(image, itemColor);
				ToolStripDropDownButton button = new ToolStripDropDownButton(item, image);
				button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
				button.Tag = graphItem;
				
				// add the drop down items
				ToolStripMenuItem menuSourceUnitSet = new ToolStripMenuItem("Item Units");
				foreach (Unit unit in UnitConverter.GetUnitsEnumerator()) {
					ToolStripMenuItem menuSourceUnit = new ToolStripMenuItem(unit.Abbreviation ?? unit.Name, null, menuSourceUnits_Click);
					menuSourceUnit.Tag = graphItem;
					if (object.Equals(unit, graphItem.SourceUnits)) {
						menuSourceUnit.Checked = true;
					}

					menuSourceUnitSet.DropDownItems.Add(menuSourceUnit);
				}

				ToolStripMenuItem menuDestUnitSet = new ToolStripMenuItem("Dest Units");
				menuDestUnitSet.Name = "menuDestUnitCollection";
				if (graphItem.SourceUnits != null) {
					graphItem.Conversion = UnitConverter.GetConversion(graphItem.SourceUnits, graphItem.SourceUnits);
					PopulateDestUnits(menuDestUnitSet, graphItem.SourceUnits, graphItem);
				}
				else {
					menuDestUnitSet.Enabled = false;
				}

				ToolStripMenuItem menuRemoveItem = new ToolStripMenuItem("Remove", Properties.Resources.Delete, menuRemoveItem_Click);
				menuRemoveItem.Tag = graphItem;

				// add the menu items to the button
				button.DropDownItems.Add(menuSourceUnitSet);
				button.DropDownItems.Add(menuDestUnitSet);
				button.DropDownItems.Add(new ToolStripSeparator());
				button.DropDownItems.Add(menuRemoveItem);

				toolStripItems.Items.Add(button);

				// add to the plot
				plotSurface.Add(graphItem.LinePlot);

				graphItems.Add(graphItem);
			}
		}

		public bool CanAddItem(string item) {
			// cause i'm stuff
			if (colorInd == colors.Length)
				return false;
			// get the data item
			IDataItemClient dataItem = null;
			try {
				dataItem = OperationalInterface.Dataset[item];
			}
			catch (Exception) {
				return false;
			}

			if (dataItem == null)
				return false;

			// check if there is a converter
			return DataItemAdapter.HasDefaultAdapter(dataItem);
		}

		private void PopulateDestUnits(ToolStripMenuItem parent, Unit source, GraphItemAdapter graphItem) {
			if (source == null) {
				parent.Enabled = false;
			}
			else {
				parent.Enabled = true;
				parent.DropDownItems.Clear();

				List<string> items = new List<string>();

				foreach (UnitConversion conversion in UnitConverter.GetConversions(source)) {
					items.Add(conversion.To.Abbreviation ?? conversion.To.Name);
				}

				items.Sort();

				string targetConversionName = null;
				if (graphItem.Conversion != null) {
					targetConversionName = graphItem.Conversion.To.Abbreviation ?? graphItem.Conversion.To.Name;
				}

				foreach (string name in items) {
					ToolStripMenuItem item = new ToolStripMenuItem(name, null, menuDestUnits_Click);
					item.Tag = graphItem;
					if (name == targetConversionName) {
						item.Checked = true;
					}

					parent.DropDownItems.Add(item);
				}
			}
		}

		private void menuSourceUnits_Click(object sender, EventArgs e) {
			ToolStripMenuItem menuitem = sender as ToolStripMenuItem;
			GraphItemAdapter graphItem = menuitem.Tag as GraphItemAdapter;

			Unit sourceUnit = UnitConverter.GetUnit(menuitem.Text);
			if (sourceUnit != null) {
				graphItem.SourceUnits = sourceUnit;
				// start with identity conversion
				graphItem.Conversion = UnitConverter.GetConversion(sourceUnit, sourceUnit);

				// find the menuDestUnitCollection menu item two levels up
				ToolStripDropDownButton parent2 = (ToolStripDropDownButton)menuitem.OwnerItem.OwnerItem;
				ToolStripMenuItem destUnitMenuItem = (ToolStripMenuItem)parent2.DropDownItems["menuDestUnitCollection"];
				PopulateDestUnits(destUnitMenuItem, sourceUnit, graphItem);

				menuitem.Checked = true;
				ToolStripMenuItem parent1 = (ToolStripMenuItem)menuitem.OwnerItem;
				foreach (ToolStripMenuItem child in parent1.DropDownItems) {
					if (child != menuitem) {
						child.Checked = false;
					}
				}
			}
		}

		private void menuDestUnits_Click(object sender, EventArgs e) {
			ToolStripMenuItem menuitem = sender as ToolStripMenuItem;
			GraphItemAdapter graphItem = menuitem.Tag as GraphItemAdapter;

			Unit destUnit = UnitConverter.GetUnit(menuitem.Text);
			graphItem.Conversion = UnitConverter.GetConversion(graphItem.SourceUnits, destUnit);

			menuitem.Checked = true;
			ToolStripMenuItem parent = (ToolStripMenuItem)menuitem.OwnerItem;
			foreach (ToolStripMenuItem item in parent.DropDownItems) {
				if (item != menuitem) {
					item.Checked = false;
				}
			}
		}

		private void menuRemoveItem_Click(object sender, EventArgs e) {
			ToolStripMenuItem source = sender as ToolStripMenuItem;
			GraphItemAdapter graphItem = source.Tag as GraphItemAdapter;

			plotSurface.Remove(graphItem.LinePlot, false);
			graphItems.Remove(graphItem);

			ToolStripItem removeItem = null;
			foreach (ToolStripItem button in toolStripItems.Items) {
				if (object.Equals(button.Tag, graphItem)) {
					removeItem = button;
					break;
				}
			}

			if (removeItem != null) {
				toolStripItems.Items.Remove(removeItem);
			}

			graphItem.Dispose();
		}

		private void menuWindow_Click(object sender, EventArgs e) {
			ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
			double windowSize = (double)menuItem.Tag;

			menuItem.Checked = true;
			foreach (ToolStripMenuItem otherItem in buttonWindowSize.DropDownItems) {
				if (menuItem != otherItem) {
					otherItem.Checked = false;
				}
			}

			this.windowSize = windowSize;
			foreach (GraphItemAdapter item in graphItems) {
				item.WindowSize = windowSize;
			}
		}

		private void plotSurface_DragEnter(object sender, DragEventArgs e) {
			if (!e.Data.GetDataPresent("dataitem")) {
				e.Effect = DragDropEffects.None;
			}
			else {
				string[] dataItemNames = (string[])e.Data.GetData("dataitem");
				foreach (string dataItemName in dataItemNames) {
					if (CanAddItem(dataItemName)) {
						e.Effect = DragDropEffects.Copy;
					}
					else {
						e.Effect = DragDropEffects.None;
					}
				}
			}
		}

		private void plotSurface_DragDrop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent("dataitem")) {
				string[] dataItemNames = (string[])e.Data.GetData("dataitem");
				foreach (string dataItemName in dataItemNames) {
					AddGraphItem(dataItemName);
				}
			}
		}

		void RunControlService_DrawCycle(object sender, EventArgs e) {
			// figure out min and max values
			double minValue = double.MaxValue;
			double maxValue = double.MinValue;
			double maxTimestamp = double.MinValue;
			bool anyData = false;
			foreach (GraphItemAdapter graphItem in graphItems) {
				if (graphItem.HasData) {
					if (graphItem.MinValue < minValue) minValue = graphItem.MinValue;
					if (graphItem.MaxValue > maxValue) maxValue = graphItem.MaxValue;
					if (graphItem.MaxTimestamp > maxTimestamp) maxTimestamp = graphItem.MaxTimestamp;

					anyData = true;
				}
			}

			double yAxisMin, yAxisMax;
			double xAxisMin, xAxisMax;
			if (anyData) {
				ComputeAxisLimits(minValue, maxValue, out yAxisMin, out yAxisMax);
				xAxisMax = maxTimestamp;
				xAxisMin = maxTimestamp - windowSize;
			}
			else {
				yAxisMin = -1;
				yAxisMax = 1;

				xAxisMax = windowSize;
				xAxisMin = 0;
			}

			yaxis.WorldMax = yAxisMax;
			yaxis.WorldMin = yAxisMin;

			xaxis.WorldMax = xAxisMax;
			xaxis.WorldMin = xAxisMin;

			plotSurface.Invalidate();
		}

		private void ComputeAxisLimits(double minValue, double maxValue, out double axisMin, out double axisMax) {
			double minVal = minValue;
			double maxVal = maxValue;

			// Make sure that minVal and maxVal are legitimate values
			if (Double.IsInfinity(minVal) || Double.IsNaN(minVal) || minVal == Double.MaxValue)
				minVal = 0.0;
			if (Double.IsInfinity(maxVal) || Double.IsNaN(maxVal) || maxVal == Double.MaxValue)
				maxVal = 0.0;

			double _min, _max;

			// if the scales are autoranged, use the actual data values for the range
			double range = maxVal - minVal;

			// For autoranged values, assign the value.  If appropriate, adjust the value by the
			// "Grace" value.
			_min = minVal;
			// Do not let the grace value extend the axis below zero when all the values were positive
			if (_min < 0 || minVal - min_grace * range >= 0.0)
				_min = minVal - min_grace * range;

			_max = maxVal;
			// Do not let the grace value extend the axis above zero when all the values were negative
			if (_max > 0 || maxVal + max_grace * range <= 0.0)
				_max = maxVal + max_grace * range;

			if (_max == _min) {
				if (Math.Abs(_max) > 1e-100) {
					_max *= (_min < 0 ? 0.95 : 1.05);
					_min *= (_min < 0 ? 1.05 : 0.95);
				}
				else {
					_max = 1.0;
					_min = -1.0;
				}
			}

			if (_max <= _min) {
				_max = _min + 1.0;
			}

			// Test for trivial condition of range = 0 and pick a suitable default
			if (_max - _min < 1.0e-30) {
				_max = _max + 0.2 * (_max == 0 ? 1.0 : Math.Abs(_max));
				_min = _min - 0.2 * (_min == 0 ? 1.0 : Math.Abs(_min));
			}

			// This is the zero-lever test.  If minVal is within the zero lever fraction
			// of the data range, then use zero.

			if (_min > 0 && _min / (_max - _min) < zero_lever) {
				_min = 0;
			}

			// Repeat the zero-lever test for cases where the maxVal is less than zero
			if (_max < 0 && Math.Abs(_max / (_max - _min)) < zero_lever) {
				_max = 0;
			}

			axisMin = _min;
			axisMax = _max;
		}
	}
}
