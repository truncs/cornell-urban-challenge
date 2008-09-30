using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using CarBrowser.Config;
using System.Net;
using System.Net.NetworkInformation;
using System.Drawing;
using System.Threading;

namespace CarBrowser.Micros {
	public partial class MicrocontrollerListView : ListView {
		class MicrocontrollerListViewItem : ListViewItem {
			public IPAddress address;
			public int powerPort;
			public bool? synced = null;
			public bool? pulsed = null;

			public bool supportsTiming;

			public Ping ping;

			public MicrocontrollerListViewItem(MicroConfig config, MicrocontrollerListView listView) : base(config.name) {
				if (IPAddress.TryParse(config.address, out address)) {
					this.Group = listView.Groups["groupMicros"];
					this.ImageKey = "micro";

					if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
						ping = new Ping();
						ping.PingCompleted += new PingCompletedEventHandler(ping_PingCompleted);
					}
				}
				else {
					this.Group = listView.Groups["groupPower"];
					this.ImageKey = "power";
				}

				this.powerPort = config.powerPort;
				this.supportsTiming = config.supportsTiming;

				// add the subitems
				for (int i = 0; i < listView.Columns.Count-1; i++) {
					this.SubItems.Add("");
				}

				if (address != null) {
					this.SubItems[listView.columnAddress.DisplayIndex].Text = address.ToString();
				}

				if (supportsTiming) {
					this.SubItems[listView.columnSyncPulse.DisplayIndex].Text = "?/?";
				}
				else {
					this.SubItems[listView.columnSyncPulse.DisplayIndex].Text = " - ";
				}

				if (powerPort != -1) {
					this.SubItems[listView.columnPowerStatus.DisplayIndex].Text = powerPort.ToString() + "/?";
				}
				else {
					this.SubItems[listView.columnPowerStatus.DisplayIndex].Text = " - ";
				}
			}

			void ping_PingCompleted(object sender, PingCompletedEventArgs e) {
				string message;
				if (e.Cancelled) {
					message = "cancelled";
				}
				else if (e.Error != null) {
					message = "failed: " + e.Error.Message;
				}
				else if (e.Reply.Status == IPStatus.TimedOut) {
					message = "timed out";
				}
				else if (e.Reply.Status != IPStatus.Success) {
					message = "failed: " + e.Reply.Status;
				}
				else {
					if (e.Reply.RoundtripTime == 0) {
						message = "< 1 ms";
					}
					else {
						message = string.Format("{0} ms", e.Reply.RoundtripTime);
					}
				}

				if (this.ListView != null){
					this.ListView.BeginInvoke(new MethodInvoker(delegate() {
						OnPingReceived(message);
					}));
				}
			}

			public void OnSyncReceived(bool synced) {
				this.synced = synced;
				UpdateSyncPulse();
			}

			public void OnPulseReceived(bool pulse) {
				this.pulsed = pulse;
				UpdateSyncPulse();
			}

			private void UpdateSyncPulse() {
				if (this.ListView != null) {
					MicrocontrollerListView listView = (MicrocontrollerListView)this.ListView;
					bool bad = !(synced.GetValueOrDefault(true) && pulsed.GetValueOrDefault(true));
					string syncText;
					if (synced.HasValue) {
						syncText = synced.Value ? "sync" : "no sync";
					}
					else {
						syncText = "?";
					}
					string pulseText;
					if (pulsed.HasValue){
						pulseText = pulsed.Value ? "pulse" : "no pulse";
					}
					else {
						pulseText = "?";
					}
					this.SubItems[listView.columnSyncPulse.DisplayIndex].Text = syncText + "/" + pulseText;
					if (bad) {
						this.SubItems[listView.columnSyncPulse.DisplayIndex].ForeColor = Color.Red;
					}
					else {
						this.SubItems[listView.columnSyncPulse.DisplayIndex].ForeColor = Color.Black;
					}
				}
			}

			public void OnPingReceived(string message) {
				if (this.ListView != null) {
					MicrocontrollerListView listView = (MicrocontrollerListView)this.ListView;
					this.SubItems[listView.columnPingTime.DisplayIndex].Text = message;
				}
			}

			public void OnPowerMessage(bool enabled, PowerState state) {
				if (this.ListView != null) {
					MicrocontrollerListView listView = (MicrocontrollerListView)this.ListView;
					string stateString = "unknown";

					switch (state) {
						case PowerState.On:
							stateString = "on";
							break;

						case PowerState.Off:
							stateString = "off";
							break;

						case PowerState.FuseBlown:
							stateString = "fuse blown";
							break;

						case PowerState.OpenLoad:
							stateString = "open load";
							break;

						case PowerState.Resetting1:
							stateString = "resetting 1";
							break;

						case PowerState.Resetting2:
							stateString = "resetting 2";
							break;
					}

					string text = string.Format("{0}/{1}/{2}", powerPort, stateString, enabled ? "enabled" : "disabled");

					this.SubItems[listView.columnPowerStatus.DisplayIndex].Text = text;

					if (state == PowerState.FuseBlown) {
						this.BackColor = Color.LightCoral;
					}
					else if (state == PowerState.Off) {
						this.BackColor = Color.LightGreen;
					}
					else if (state == PowerState.Resetting1 || state == PowerState.Resetting2) {
						this.BackColor = Color.Thistle;
					}
					else if (state == PowerState.OpenLoad) {
						this.BackColor = Color.Wheat;
					}
					else {
						this.BackColor = this.ListView.BackColor;
					}
				}
			}

			public void Close() {
				if (ping != null) {
					ping.Dispose();
					ping = null;
				}
			}
		}

		private MicroTimingInterface timingInterface;
		private MicroPowerInterface powerInterface;

		public MicrocontrollerListView() {
			InitializeComponent();
			this.DoubleBuffered = true;

			if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
				timingInterface = new MicroTimingInterface(this);
				timingInterface.PulseReceived += new EventHandler<MicroEventArgs>(timingInterface_PulseReceived);
				timingInterface.SyncReceived += new EventHandler<MicroEventArgs>(timingInterface_SyncReceived);

				powerInterface = new MicroPowerInterface(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 20), this);
				powerInterface.MicroPowerStatusReceived += new EventHandler<MicroPowerStatusEventArgs>(powerInterface_MicroPowerStatusReceived);
			}
		}

		protected override void OnColumnClick(ColumnClickEventArgs e) {
			base.OnColumnClick(e);

			IColumnSorter sorter = this.ListViewItemSorter as IColumnSorter;
			if (sorter == null || sorter.ColumnIndex != e.Column) {
				sorter = GetSorter(e.Column, SortOrder.Ascending);
				if (sorter != null) {
					this.ListViewItemSorter = sorter;
					this.Sort();
				}
			}
			else {
				sorter = GetSorter(e.Column, sorter.SortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending);
				this.ListViewItemSorter = sorter;
				this.Sort();
			}
		}

		private IColumnSorter GetSorter(int col, SortOrder sortOrder) {
			if (col == columnName.Index) {
				return new StringColumnSorter(col, sortOrder);
			}
			else if (col == columnAddress.Index) {
				return new IPAddressColumnSorter(col, sortOrder);
			}
			else if (col == columnPowerStatus.Index) {
				return new PowerPortSorter(col, sortOrder);
			}
			else {
				return null;
			}
		}

		void powerInterface_MicroPowerStatusReceived(object sender, MicroPowerStatusEventArgs e) {
			foreach (MicrocontrollerListViewItem item in Items) {
				if (item.powerPort >= 1 && item.powerPort <= e.Count) {
					item.OnPowerMessage(e.GetEnabled(item.powerPort), e.GetPowerState(item.powerPort));
				}
			}
		}

		void timingInterface_SyncReceived(object sender, MicroEventArgs e) {
			if (!e.TimedOut) {
				foreach (MicrocontrollerListViewItem item in Items) {
					if (object.Equals(e.Address, item.address)) {
						item.OnSyncReceived(e.BoolResult);
					}
				}
			}
		}

		void timingInterface_PulseReceived(object sender, MicroEventArgs e) {
			if (!e.TimedOut) {
				foreach (MicrocontrollerListViewItem item in Items) {
					if (object.Equals(e.Address, item.address)) {
						item.OnPulseReceived(e.BoolResult);
					}
				}
			}
		}

		public void SetConfig(List<MicroConfig> config) {
			Items.Clear();

			List<ListViewItem> items = new List<ListViewItem>();
			foreach (MicroConfig micro in config) {
				items.Add(new MicrocontrollerListViewItem(micro, this));
			}

			Items.AddRange(items.ToArray());
		}

		private void StartPing(MicrocontrollerListViewItem item) {
			if (item.address != null) {
				try {
					item.ping.SendAsync(item.address, 1000, null);
					Thread.Sleep(5);
				}
				catch (Exception) {
				}
			}
		}

		public void PingSelected() {
			foreach (MicrocontrollerListViewItem item in SelectedItems) {
				StartPing(item);
				Thread.Sleep(5);
			}
		}

		public void ResetSelected() {
			foreach (MicrocontrollerListViewItem item in SelectedItems) {
				if (item.powerPort != -1) {
					powerInterface.ResetPort(item.powerPort);
					Thread.Sleep(5);
				}
			}
		}

		public void EnableSelected(bool enabled) {
			foreach (MicrocontrollerListViewItem item in SelectedItems) {
				if (item.powerPort != -1) {
					powerInterface.SetPortEnabled(item.powerPort, enabled);
					Thread.Sleep(5);
				}
			}
		}

		public void EnableAll(bool enabled) {
			powerInterface.SetEnabledAll(enabled);
		}

		internal MicroPowerInterface PowerInterface {
			get { return powerInterface; }
		}

		public void RefreshSyncSelected() {
			foreach (MicrocontrollerListViewItem item in SelectedItems) {
				if (item.address != null && item.supportsTiming && !timingInterface.AnyPendingOps(item.address)) {
					timingInterface.BeginGetSync(item.address);
					Thread.Sleep(5);
				}
			}
		}

		public void RefreshPulseSelected() {
			foreach (MicrocontrollerListViewItem item in SelectedItems) {
				if (item.address != null && item.supportsTiming && !timingInterface.AnyPendingOps(item.address)) {
					timingInterface.BeginGetPulse(item.address);
					Thread.Sleep(5);
				}
			}
		}

		public void ResyncSelected() {
			foreach (MicrocontrollerListViewItem item in SelectedItems) {
				if (item.address != null && item.supportsTiming && !timingInterface.AnyPendingOps(item.address)) {
					timingInterface.BeginCommandResync(item.address);
					Thread.Sleep(5);
				}
			}
		}

		public void RefreshSyncAll() {
			foreach (MicrocontrollerListViewItem item in Items) {
				if (item.address != null && item.supportsTiming && !timingInterface.AnyPendingOps(item.address)) {
					timingInterface.BeginGetSync(item.address);
					Thread.Sleep(5);
				}
			}
		}

		public void RefreshPulseAll() {
			foreach (MicrocontrollerListViewItem item in Items) {
				if (item.address != null && item.supportsTiming && !timingInterface.AnyPendingOps(item.address)) {
					timingInterface.BeginGetPulse(item.address);
					Thread.Sleep(5);
				}
			}
		}

		public bool AnyPendingTimingOps {
			get {
				return timingInterface.AnyPendingOps();
			}
		}

		private void menuRefreshSync_Click(object sender, EventArgs e) {
			RefreshSyncSelected();
		}

		private void menuRefreshPulse_Click(object sender, EventArgs e) {
			RefreshPulseSelected();
		}

		private void menuPing_Click(object sender, EventArgs e) {
			PingSelected();
		}

		private void menuReset_Click(object sender, EventArgs e) {
			ResetSelected();
		}

		private void menuEnable_Click(object sender, EventArgs e) {
			EnableSelected(true);
		}

		private void menuDisable_Click(object sender, EventArgs e) {
			EnableSelected(false);
		}

		private void menuResync_Click(object sender, EventArgs e) {
			ResyncSelected();
		}

		public bool PingContinuously {
			get { return timerPing.Enabled; }
			set { timerPing.Enabled = value; }
		}

		private void timerPing_Tick(object sender, EventArgs e) {
			foreach (MicrocontrollerListViewItem item in Items) {
				StartPing(item);
			}
		}

		private void contextMenuMicros_Opening(object sender, CancelEventArgs e) {
			
		}
	}
}
