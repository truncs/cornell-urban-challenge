using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Net;
using UrbanChallenge.MessagingService;
using System.Net.Sockets;
using CarBrowser.Config;

namespace CarBrowser.Channels {
	public partial class ChannelListView : ListView {
		private class ChannelListViewItem : ListViewItem {
			private string name;
			private IPEndPoint endpoint;
			private ChannelListener listener;
			private bool dynamicChannel;

			public ChannelListViewItem(IChannel channel, ChannelListView listView) : base(channel.Name, "dynamic channel", listView.Groups["dynamic"]) {
				this.name = channel.Name;
				this.dynamicChannel = true;

				endpoint = null;
				if (channel is UDPChannel) {
					UDPChannel udpChannel = (UDPChannel)channel;
					endpoint = new IPEndPoint(udpChannel.IP, udpChannel.Port);

					listener = new ChannelListener(endpoint, ChannelType.UDPChannel);
				}

				this.SubItems.Add(endpoint == null ? channel.GetType().Name : endpoint.ToString());
				this.SubItems.Add("");
				this.SubItems.Add("");
			}

			public ChannelListViewItem(string name, IPEndPoint endpoint, ChannelType channelType, ChannelListView listView)
				: base(name, "static channel", listView.Groups["static"]) {
				this.name = name;
				this.dynamicChannel = false;

				this.endpoint = endpoint;
				this.listener = new ChannelListener(endpoint, channelType);

				this.SubItems.Add(endpoint == null ? "unknown" : endpoint.ToString());
				this.SubItems.Add("");
				this.SubItems.Add("");
			}

			public bool DynamicChannel {
				get { return dynamicChannel; }
			}

			public void StartListening() {
				if (listener != null) {
					listener.StartListening();
				}
			}

			public void StopListening() {
				if (listener != null) {
					listener.StopListening();
				}
			}

			public void ResetData() {
				if (listener != null) {
					listener.ResetStats();
				}
			}

			public void UpdateListenerData() {
				if (this.ListView != null && listener != null) {
					ChannelListView channelListView = (ChannelListView)this.ListView;

					if (listener.HasData) {
						double packetRate = listener.PacketRate;
						double byteRate = listener.ByteRate;
						string senders = listener.GetSendersString();

						this.SubItems[channelListView.columnPacketRate.DisplayIndex].Text = string.Format("{0:F1}/{1:F1}", packetRate, byteRate);
						this.SubItems[channelListView.columnSenders.DisplayIndex].Text = senders;
					}
					else {
						this.SubItems[channelListView.columnPacketRate.DisplayIndex].Text = string.Empty;
						this.SubItems[channelListView.columnSenders.DisplayIndex].Text = string.Empty;
					}
				}
			}

			public bool IsListening {
				get { return listener != null && listener.Listening; }
			}
		}

		public ChannelListView() {
			InitializeComponent();
			this.DoubleBuffered = true;
		}

		public void RefreshList(IChannelFactory factory) {
			List<ListViewItem> removeItems = new List<ListViewItem>();
			foreach (ChannelListViewItem item in Items) {
				if (item.DynamicChannel) {
					if (item.IsListening) {
						item.StopListening();
					}

					removeItems.Add(item);
				}
			}

			foreach (ListViewItem item in removeItems) {
				Items.Remove(item);
			}

			ICollection<string> channels = null;
			try {
				channels = factory.Channels;
			}
			catch (Exception ex) {
				MessageBox.Show(this.FindForm(), "Could not get channels from channel factory:\n" + ex.Message, "Car Browser", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			List<ChannelListViewItem> items = new List<ChannelListViewItem>();
			foreach (string name in channels) {
				try {
					IChannel channel = factory.GetChannel(name, ChannelMode.Bytestream);
					ChannelListViewItem item = new ChannelListViewItem(channel, this);

					items.Add(item);
				}
				catch (Exception) {
				}
			}

			Items.AddRange(items.ToArray());
		}

		public void SetStaticChannels(List<ChannelConfig> staticChannels) {
			List<ListViewItem> removeItems = new List<ListViewItem>();
			foreach (ChannelListViewItem item in Items) {
				if (!item.DynamicChannel) {
					if (item.IsListening) {
						item.StopListening();
					}

					removeItems.Add(item);
				}
			}

			foreach (ListViewItem item in removeItems) {
				Items.Remove(item);
			}

			List<ChannelListViewItem> items = new List<ChannelListViewItem>();
			foreach (ChannelConfig channel in staticChannels) {
				try {
					IPAddress channelAddress = IPAddress.Parse(channel.address);
					IPEndPoint endpoint = new IPEndPoint(channelAddress, channel.port);
					ChannelListViewItem item = new ChannelListViewItem(channel.name, endpoint, channel.channelType, this);

					items.Add(item);
				}
				catch (Exception) {
				}
			}

			Items.AddRange(items.ToArray());
		}

		private void timerUpdateTimer_Tick(object sender, EventArgs e) {
			bool didBeginUpdate = false;
			foreach (ChannelListViewItem item in Items) {
				if (!didBeginUpdate) {
					this.BeginUpdate();
					didBeginUpdate = true;
				}
				item.UpdateListenerData();
			}

			if (didBeginUpdate) {
				this.EndUpdate();
			}
		}

		private void contextMenu_Opening(object sender, CancelEventArgs e) {
			bool anyListening = false;
			bool allListening = true;

			foreach (ChannelListViewItem item in SelectedItems) {
				if (item.IsListening) {
					anyListening = true;
				}
				else {
					allListening = false;
				}
			}

			menuStartListening.Enabled = !allListening;
			menuStopListening.Enabled = anyListening;
		}

		private void menuStartListening_Click(object sender, EventArgs e) {
			foreach (ChannelListViewItem item in SelectedItems) {
				if (!item.IsListening) {
					item.StartListening();
				}
			}
		}

		private void menuStopListening_Click(object sender, EventArgs e) {
			foreach (ChannelListViewItem item in SelectedItems) {
				if (item.IsListening) {
					item.StopListening();
				}
			}
		}

		private void menuResetStats_Click(object sender, EventArgs e) {
			this.BeginUpdate();
			try {
				foreach (ChannelListViewItem item in SelectedItems) {
					item.ResetData();
					item.UpdateListenerData();
				}
			}
			finally {
				this.EndUpdate();
			}
		}

		protected override void OnColumnClick(ColumnClickEventArgs e) {
			IColumnSorter columnSort = ListViewItemSorter as IColumnSorter;
			if (columnSort == null || columnSort.ColumnIndex != e.Column) {
				if (SetupSort(e.Column)) {
					this.Sort();
				}
			}
			else {
				if (columnSort.SortOrder == SortOrder.Ascending) {
					columnSort.SortOrder = SortOrder.Descending;
				}
				else {
					columnSort.SortOrder = SortOrder.Ascending;
				}

				this.Sort();
			}

			base.OnColumnClick(e);
		}

		private bool SetupSort(int col) {
			if (col == columnName.Index) {
				ListViewItemSorter = new StringColumnSorter(col, SortOrder.Ascending);
				return true;
			}
			else if (col == columnMulticast.Index) {
				ListViewItemSorter = new IPAddressColumnSorter(col, SortOrder.Ascending);
				return true;
			}

			return false;
		}
	}
}
