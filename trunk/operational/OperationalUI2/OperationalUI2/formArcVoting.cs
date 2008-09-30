using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Operational.Common;

namespace UrbanChallenge.OperationalUI {
	public partial class formArcVoting : Form {
		public formArcVoting() {
			InitializeComponent();

			string[] dataSources = arcVotingDisplay1.GetDataSources();
			comboField.Items.AddRange(dataSources);
			comboField.SelectedItem = arcVotingDisplay1.DataSource;

			OperationalInterface.Dataset.ItemAs<ArcVotingResults>("arc voting results").DataValueAdded += formArcVoting_DataValueAdded;
		}

		void formArcVoting_DataValueAdded(object sender, Dataset.Client.ClientDataValueAddedEventArgs e) {
			arcVotingDisplay1.SetArcVotingResults((ArcVotingResults)e.Value);
		}

		private void comboField_SelectedIndexChanged(object sender, EventArgs e) {
			if (comboField.SelectedItem != null) {
				arcVotingDisplay1.DataSource = (string)comboField.SelectedItem;
			}
		}

		private void formArcVoting_FormClosing(object sender, FormClosingEventArgs e) {
			OperationalInterface.Dataset.ItemAs<ArcVotingResults>("arc voting results").DataValueAdded -= formArcVoting_DataValueAdded;
		}
	}
}