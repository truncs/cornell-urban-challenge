using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UrbanChallenge.OperationalUI.Graphing {
	internal partial class formGraphProperties : Form {
		public formGraphProperties() {
			InitializeComponent();
		}

		public void SetGraphItems(IList<GraphItemAdapter> items) {
			// find the longest time window
			double maxTimeWindow = 0;

			tabControl1.SuspendLayout();
			foreach (GraphItemAdapter item in items) {
				if (item.WindowSize > maxTimeWindow) {
					maxTimeWindow = item.WindowSize;
				}

				TabPage page = new TabPage(item.Name);
				GraphItemProperties props = new GraphItemProperties();
				props.Dock = DockStyle.Fill;
				props.GraphItem = item;

				page.Controls.Add(props);
				// store the reference to the property page in the tag
				page.Tag = props;

				tabControl1.TabPages.Add(page);
			}

			tabControl1.ResumeLayout(true);

			numTimeWindow.Value = (decimal)maxTimeWindow;
		}

		private void ApplyChanges() {
			double timeWindow = (double)numTimeWindow.Value;

			foreach (TabPage page in tabControl1.TabPages) {
				if (page.Tag is GraphItemProperties) {
					GraphItemProperties props = (GraphItemProperties)page.Tag;
					props.ApplyProperties();
					props.GraphItem.WindowSize = timeWindow;
				}
			}
		}

		private void buttonOK_Click(object sender, EventArgs e) {
			ApplyChanges();
			this.DialogResult = DialogResult.OK;
		}
	}
}