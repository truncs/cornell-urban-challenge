using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Dataset.Client;
using UrbanChallenge.OperationalUI.Graphing;

namespace UrbanChallenge.OperationalUI {
	public partial class formDataset : Form {
		public formDataset() {
			InitializeComponent();

			OperationalInterface.Dataset.DataItemAdded += Dataset_DataItemAdded;

			lock (listViewDataset) {
				foreach (IDataItemClient dataitem in OperationalInterface.Dataset.GetDataItems()) {
					ListViewItem item = GetListViewItem(dataitem);
					if (item != null) {
						listViewDataset.Items.Add(item);
					}
				}
			}

			listViewDataset.Sorting = SortOrder.Ascending;
		}

		private void formDataset_FormClosing(object sender, FormClosingEventArgs e) {
			OperationalInterface.Dataset.DataItemAdded -= Dataset_DataItemAdded;
		}

		void Dataset_DataItemAdded(object sender, ClientDataItemAddedEventArgs e) {
			MethodInvoker method = new MethodInvoker(delegate() {
				ListViewItem item = GetListViewItem(e.DataItem);
				if (item != null) {
					lock (listViewDataset) {
						listViewDataset.Items.Add(item);
					}
				}
			});

			if (this.IsHandleCreated && this.InvokeRequired) {
				this.BeginInvoke(method);
			}
			else {
				method();
			}
		}

		private ListViewItem GetListViewItem(IDataItemClient dataItem) {
			if (!DataItemAdapter.HasDefaultAdapter(dataItem)) {
				return null;
			}

			ListViewItem item = new ListViewItem(dataItem.Name, "item");
			item.Tag = dataItem;

			return item;
		}

		private void listViewDataset_ItemActivate(object sender, EventArgs e) {
			formGraph graph = new formGraph();

			foreach (ListViewItem item in listViewDataset.SelectedItems) {
				graph.AddGraphItem(((IDataItemClient)item.Tag).Name);
			}

			graph.Show();
		}

		private void listViewDataset_ItemDrag(object sender, ItemDragEventArgs e) {
			string[] names = new string[listViewDataset.SelectedItems.Count];
			for (int i = 0; i < listViewDataset.SelectedItems.Count; i++) {
				names[i] = ((IDataItemClient)listViewDataset.SelectedItems[i].Tag).Name;
			}

			DataObject dataObject = new DataObject("dataitem", names);

			listViewDataset.DoDragDrop(dataObject, DragDropEffects.Copy);
		}
	}
}