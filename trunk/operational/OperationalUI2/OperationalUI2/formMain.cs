using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUI.Common;

namespace UrbanChallenge.OperationalUI {
	public partial class formMain : Form {
		private formMap mapInstance;
		private OperationalFinder finder;

		public formMain() {
			InitializeComponent();

			timerRender.Interval = (int)Services.RunControlService.RenderPeriod.TotalMilliseconds;
			Services.RunControlService.RenderPeriodChanged += new EventHandler(RunControlService_RenderPeriodChanged);

			finder = new OperationalFinder();
			finder.StatusChanged += new EventHandler(finder_StatusChanged);
		}

		void finder_StatusChanged(object sender, EventArgs e) {
			ICollection<OperationalFinder.OperationalStatus> statusCollection = finder.GetStatus();

			ListViewItem[] items = new ListViewItem[statusCollection.Count];
			int i = 0;
			foreach (OperationalFinder.OperationalStatus status in statusCollection) {
				ListViewItem item = new ListViewItem();
				item.Name = item.Text = status.name;
				switch (status.responseStatus) {
					case OperationalFinder.ResponseStatus.Alive:
						item.ImageKey = "active";
						break;

					case OperationalFinder.ResponseStatus.Down:
						item.ImageKey = "dead";
						break;

					case OperationalFinder.ResponseStatus.Unknown:
						item.ImageKey = "unknown";
						break;
				}

				item.SubItems.Add("status: " + status.responseStatus.ToString().ToLower());

				items[i++] = item;
			}

			this.Invoke(new MethodInvoker(delegate() {
				listViewInstances.BeginUpdate();
				listViewInstances.Items.Clear();
				listViewInstances.Items.AddRange(items);
				listViewInstances.EndUpdate();
			}));
		}

		private formMap MapInstance {
			get {
				if (mapInstance == null || mapInstance.IsDisposed) {
					mapInstance = new formMap();
				}

				return mapInstance;
			}
		}

		private void buttonMap_Click(object sender, EventArgs e) {
			formMap map = MapInstance;
			map.Show();
		}

		private void formMain_Load(object sender, EventArgs e) {
			try {
				finder.RefreshInstances(true);
			}
			catch (Exception ex) {
				MessageBox.Show(this, "Could not refresh operational instances:\n" + ex.Message, "OperationalUI");
			}

			timerRender.Enabled = true;
		}

		private void formMain_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.F5) {
				try {
					this.UseWaitCursor = true;
					finder.RefreshInstances(true);
				}
				catch (Exception ex) {
					MessageBox.Show(this, "Could not refresh operational instances:\n" + ex.Message, "OperationalUI");
				}
				finally {
					this.UseWaitCursor = false;
				}
			}
		}

		private void listViewInstances_ItemActivate(object sender, EventArgs e) {
			if (listViewInstances.SelectedItems.Count == 1) {
				string name = listViewInstances.SelectedItems[0].Name;

				try {
					this.UseWaitCursor = true;
					OperationalInterface.Attach(name.Substring(OperationalUIService.OperationalUIFacade.ServiceName.Length), true);
				}
				catch (Exception ex) {
					MessageBox.Show(this, "Error connecting to operational instance:\n" + ex.Message, "OperationalUI");
				}
				finally {
					this.UseWaitCursor = false;
				}
			}
		}

		private void buttonConnectToSim_Click(object sender, EventArgs e) {
			try {
				SimulatorInterface.Attach();
			}
			catch (Exception ex) {
				MessageBox.Show(this, "Could not connect to simulation server:\n" + ex.Message, "OperationalUI");
			}
		}

		private void buttonDataset_Click(object sender, EventArgs e) {
			formDataset dataset = new formDataset();
			dataset.Show();
		}

		private void timerRender_Tick(object sender, EventArgs e) {
			Services.RunControlService.OnRenderCycle();
		}

		void RunControlService_RenderPeriodChanged(object sender, EventArgs e) {
			timerRender.Interval = (int)Services.RunControlService.RenderPeriod.TotalMilliseconds;
		}
	}
}