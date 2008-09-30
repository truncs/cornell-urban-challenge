using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Dataset.Units;

namespace UrbanChallenge.OperationalUI.Graphing {
	internal partial class GraphItemProperties : UserControl {
		private GraphItemAdapter graphItem;

		public GraphItemProperties() {
			InitializeComponent();

			// initialize the unit lists
			foreach (Unit unit in UnitConverter.GetUnitsEnumerator()) {
				comboSourceUnits.Items.Add(unit.Abbreviation ?? unit.Name);
			}

			comboDestUnits.Enabled = false;
			comboAxis.Enabled = false;
		}

		private void comboSourceUnits_SelectedIndexChanged(object sender, EventArgs e) {
			// figure out what unit they selected
			Unit unit = UnitConverter.GetUnit((string)comboSourceUnits.SelectedItem);
			PopulateDestUnits(unit);
		}

		private void PopulateDestUnits(Unit source) {
			comboDestUnits.Items.Clear();
			if (source != null) {
				List<string> destUnits = new List<string>();

				foreach (UnitConversion conv in UnitConverter.GetConversions(source)) {
					destUnits.Add(conv.To.Abbreviation ?? conv.To.Name);
				}

				destUnits.Sort();

				// find the source name again
				string sourceName = source.Abbreviation ?? source.Name;
				int identityIndex = destUnits.FindIndex(delegate(string s) { return s == sourceName; });
				if (identityIndex < 0) identityIndex = 0;

				comboDestUnits.Items.AddRange(destUnits.ToArray());

				comboDestUnits.Enabled = true;
				comboDestUnits.SelectedIndex = identityIndex;
			}
			else {
				comboDestUnits.Enabled = false;
			}
		}

		private void SelectSourceUnit(Unit source) {
			SelectUnit(source, comboSourceUnits);
		}

		private void SelectDestUnit(Unit dest) {
			SelectUnit(dest, comboDestUnits);
		}

		private void SelectUnit(Unit unit, ComboBox combo) {
			string name = unit.Abbreviation ?? unit.Name;
			for (int i = 0; i < combo.Items.Count; i++) {
				if (object.Equals(name, combo.Items[i])) {
					combo.SelectedIndex = i;
					break;
				}
			}
		}

		public GraphItemAdapter GraphItem {
			get { return graphItem; }
			set {
				// apply the properties
				this.graphItem = value;

				if (graphItem.Conversion != null) {
					SelectSourceUnit(graphItem.Conversion.From);
					SelectDestUnit(graphItem.Conversion.To);
				}
				else {
					string unitName = graphItem.DataItemAdapter.DataItemUnits;
					if (unitName != null) {
						Unit sourceUnit = UnitConverter.GetUnit(unitName);
						SelectSourceUnit(sourceUnit);
					}
					else {
						comboSourceUnits.SelectedIndex = -1;
					}
				}
			}
		}

		public void ApplyProperties() {
			if (graphItem == null) {
				return;
			}

			if (comboSourceUnits.SelectedIndex != -1 && comboDestUnits.SelectedIndex != -1) {
				// get the units for each
				Unit source = UnitConverter.GetUnit((string)comboSourceUnits.SelectedItem);
				Unit dest = UnitConverter.GetUnit((string)comboDestUnits.SelectedItem);

				UnitConversion conv = UnitConverter.GetConversion(source, dest);

				graphItem.Conversion = conv;
			}
		}
	}
}
