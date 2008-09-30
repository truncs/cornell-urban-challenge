using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WatchdogCommunication;
using System.Net;
using PublishCommon;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using System.Collections;
using CarBrowser.Config;
using System.Runtime.InteropServices;

namespace Publisher
{

	public partial class frmMain : Form
	{
		public Dictionary<string, ComputerStatus> computerStatus = new Dictionary<string, ComputerStatus>();
		string pubRoot = Application.StartupPath;
	

		public WatchdogComm comm = new WatchdogComm();
		public frmMain()
		{
			InitializeComponent();
			PublishManager.Initialize(pubRoot);
			UpdateLocalPublishes();
			UpdateLocalPublishLocations();
			//microcontrollerListView1.SetConfig(PublishManager.settings.microcontrollers);
			//AysncGetSyncPulse();

			try
			{
				comm.Init();
			}
			catch
			{
				MessageBox.Show(this, "No Network Detected!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			comm.GotWatchdogStatusMessage += new EventHandler<WatchdogMessageEventArgs<WatchdogStatusMessage>>(comm_GotWatchdogStatusMessage);
			comm.GotStartPublishMessageReply += new EventHandler<WatchdogMessageEventArgs<StartStopPublishMessageReply>>(comm_GotStartPublishMessageReply);
			Trace.Listeners.Add(new DebugTextObject(txtDebug));
		}

		void comm_GotStartPublishMessageReply(object sender, WatchdogMessageEventArgs<StartStopPublishMessageReply> e)
		{
			Trace.WriteLine(e.msg.senderName + ": Got Message On Start : OK?" + e.msg.ok + " " + e.msg.status + " for publish " + e.msg.publishName);
			
			if (e.msg.ok) 
				notifyIcon1.ShowBalloonTip(1000, "Publish " + e.msg.publishName + "  Started OK", "Status: " + e.msg.status, ToolTipIcon.Info);			
			else
				notifyIcon1.ShowBalloonTip(1000, "Publish " + e.msg.publishName + " FAILED ", "Status: " + e.msg.status, ToolTipIcon.Error);
		}

		void comm_GotWatchdogStatusMessage(object sender, WatchdogMessageEventArgs<WatchdogStatusMessage> e)
		{
			try
			{
				if (this.IsDisposed == false)
					this.BeginInvoke(new MethodInvoker(delegate() { UpdateWatchdogStatus(e.msg); }));
			}
			catch
			{ }
		}

		void UpdateWatchdogStatus(WatchdogStatusMessage msg)
		{

			if (computerStatus.ContainsKey(msg.machineName.ToLower()))
			{
				if ((msg.statusText == "Stopped.") && (computerStatus[msg.machineName.ToLower()].msg.statusText.Equals(msg.statusText)==false))
				{
					notifyIcon1.ShowBalloonTip(1000, "Publish " + msg.curPublishName + "  Stopped", "The publish stopped.", ToolTipIcon.Warning);
				}	
				computerStatus[msg.machineName.ToLower()].msg = msg;
				computerStatus[msg.machineName.ToLower()].valid = true;
			}
			else
			{
				computerStatus.Add(msg.machineName.ToLower(), new ComputerStatus(msg, true));

				ToolStripMenuItem tsb = new ToolStripMenuItem(msg.machineName.ToLower());
				GenerateTsbSubMenu(tsb);
				tsb.DropDownOpening += new EventHandler(tsb_DropDownOpening);
				ctxNotify.Items.Add(tsb);
			}			
		}

		void tsb_DropDownOpening(object sender, EventArgs e)
		{
			int rowIndex = 0;
			rowIndex = GetDGRowIndex((sender as ToolStripMenuItem).Text);
			if (rowIndex < 0) return;
			ctxPublishes.Tag = rowIndex;
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rowIndex, out m)) return;

			tsMenuTitle.Text = " --" + m[0] + "-- ";
			executeToolStripMenuItem.Text = "Execute " + m[1];
			stopToolStripMenuItem.Text = "Stop " + m[1];
			republishToolStripMenuItem.Text = "Publish " + m[1];
			tsEditPublish.Text = "Edit " + m[1];
		}

		void GenerateTsbSubMenu(ToolStripMenuItem tsb)
		{
			tsb.DropDown = ctxPublishes;
		}

		void StopPublish(string machineName, string publishName)
		{
			comm.SendMessage(new StopPublishMessage(machineName, publishName));
		}

		void ExecutePublish(string machineName, string publishName)
		{
			comm.SendMessage(new StartPublishMessage(machineName, publishName));
		}

		void DoRePublish(string machineName, string publishName)
		{
			RemotePublishLocation rpl;
			if (!PublishManager.GetRemotePublishLocationByComputerName(machineName, out rpl))
			{
				MessageBox.Show(this, "Could not find a remote publish location for " + machineName + ".", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (computerStatus[machineName.ToLower()].isPublishing == true)
			{
				MessageBox.Show(this, "A Publish is already in progress for this computer. Wait for it to complete...","Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			computerStatus[machineName.ToLower()].isPublishing = true;
			bool dobackup = false;
			if (Properties.Settings.Default.autoBackupPublishes)
				dobackup = true;
			else
			{
				DialogResult r= MessageBox.Show(this, "Create backup of publish " + publishName + " on computer " + machineName + "?", "Backup Exiting Publish", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				dobackup = (r == DialogResult.Yes);
				if (r == DialogResult.Cancel)
				{
					computerStatus[machineName.ToLower()].isPublishing = false;
					return;
				}
			}
			PublishManager.SendPublishToRemoteComputer(Publish.Load(publishName, pubRoot), rpl, pubRoot, new EventHandler<Publisher.PublishManager.SendPublishEventArgs>(delegate(object o, Publisher.PublishManager.SendPublishEventArgs args)
			{
				this.BeginInvoke(new MethodInvoker(delegate()
				{
					computerStatus[machineName.ToLower()].isPublishing = false;
					Publish p = o as Publish;
					if (args.ok)
						MessageBox.Show(this, "The Publish " + p.name + " Succeeded.", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Information);
					else
						MessageBox.Show(this, "The Publish Failed. Check Trace for more info.", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}));
			}),dobackup);		
		}

		void UpdateComputers()
		{
			bool didadd = false;
			foreach (KeyValuePair<string, ComputerStatus> status in computerStatus)
			{
				int rowIndex = GetDGRowIndex(status.Key);
				if (rowIndex < 0)
				{
					//add a new record
					rowIndex = dgPublishes.Rows.Add();
					didadd = true;
				}

				//update an existing one
				DataGridPublishRow.PopulateRow(this.dgPublishes, rowIndex,
						status.Value,
						ctxPublishes);

			}
			if (didadd) dgPublishes.Sort(dgSorter);
		}

		void SavePreferredRemotePublish()
		{
			PublishManager.settings.PreferredRemotePublish = new List<PreferredLocation>();

			foreach (DataGridViewRow row in dgPublishes.Rows)
			{
				string machine = ""; string publish = "";
				if ((string)dgPublishes["MachineName", row.Index].Value != null)
					machine = ((string)dgPublishes["MachineName", row.Index].Value).ToLower();
				if ((string)dgPublishes["AvailablePublishes", row.Index].Value != null)
					publish = ((string)dgPublishes["AvailablePublishes", row.Index].Value).ToLower();
				PreferredLocation pl = new PreferredLocation(machine, publish);
				PublishManager.settings.PreferredRemotePublish.Add(pl);
			}
			PublishManager.settings.Save(pubRoot);
		}

		int GetDGRowIndex(string machineName)
		{
			foreach (DataGridViewRow row in dgPublishes.Rows)
			{
				if (((string)dgPublishes["MachineName", row.Index].Value).ToLower().Equals(machineName.ToLower()))
					return row.Index;
			}
			return -1;
		}


		private void UpdateLocalPublishes()
		{
			object o = cmbPublishes.SelectedItem;
			foreach (Object tsmi in deployNewToolStripMenuItem.DropDownItems)
			{
				if (tsmi is ToolStripMenuItem)
					(tsmi as ToolStripMenuItem).Click -= new EventHandler(tsmi_Click);
			}
			deployNewToolStripMenuItem.DropDownItems.Clear();

			cmbPublishes.Items.Clear();
			foreach (string p in Publish.GetAllPublishNames(pubRoot))
			{
				cmbPublishes.Items.Add(p);
				ToolStripMenuItem tsmi = new ToolStripMenuItem(p);
				tsmi.Tag = p;
				tsmi.Click += new EventHandler(tsmi_Click);
				deployNewToolStripMenuItem.DropDownItems.Add(tsmi);
			}
			deployNewToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
			deployNewToolStripMenuItem.DropDownItems.Add(newPublishToolStripMenuItem);

			if (o != null) cmbPublishes.SelectedItem = o;
			else if (cmbPublishes.Items.Count > 0) cmbPublishes.SelectedIndex = 0;
		}


		private void UpdateLocalPublishLocations()
		{
			object o = cmbRemoteLocations.SelectedItem;
			cmbRemoteLocations.Items.Clear();
			foreach (RemotePublishLocation rpl in PublishManager.settings.RemoteLocations)
				cmbRemoteLocations.Items.Add(rpl);
			if (o != null) cmbRemoteLocations.SelectedItem = o;
			else if (cmbRemoteLocations.Items.Count > 0) cmbRemoteLocations.SelectedIndex = 0;
		}

		private void btnCreateNewPublish_Click(object sender, EventArgs e)
		{
			frmNewPublish newpub = new frmNewPublish(pubRoot);
			newpub.ShowDialog();
			if (newpub.DialogResult == DialogResult.OK)
			{
				Publish p = newpub.newPublish;
				p.Save(pubRoot);
				UpdateLocalPublishes();
				cmbPublishes.SelectedItem = p;
			}
		}

		private void btnDeletePublish_Click(object sender, EventArgs e)
		{
			if (cmbPublishes.SelectedItem == null) return;
			Publish p = Publish.Load((string)cmbPublishes.SelectedItem, pubRoot);
			p.Delete(pubRoot);
			txtPublish.Text = "";
			UpdateLocalPublishes();
		}

		private void btnCreateNewRemoteLocation_Click(object sender, EventArgs e)
		{
			frmNewPublishLocation newPubLoc = new frmNewPublishLocation();
			newPubLoc.ShowDialog();
			if (newPubLoc.DialogResult == DialogResult.OK)
			{
				RemotePublishLocation rpl = newPubLoc.newPublishLocation;
				if (PublishManager.settings == null) PublishManager.settings = new PublishSettings();
				if (PublishManager.settings.RemoteLocations == null) PublishManager.settings.RemoteLocations = new List<RemotePublishLocation>();
				PublishManager.settings.RemoteLocations.Add(rpl);
				PublishManager.settings.Save(pubRoot);
				UpdateLocalPublishLocations();
				cmbRemoteLocations.SelectedItem = rpl;
			}
		}

		private void btnDeleteRemoteLocation_Click(object sender, EventArgs e)
		{
			if (cmbRemoteLocations.SelectedItem is RemotePublishLocation == false) return;
			RemotePublishLocation rpl = cmbRemoteLocations.SelectedItem as RemotePublishLocation;
			PublishManager.settings.RemoteLocations.Remove(rpl);
			PublishManager.settings.Save(pubRoot);
			UpdateLocalPublishLocations();
		}

		private void tmrGrid_Tick(object sender, EventArgs e)
		{
			UpdateComputers();
		}

		private void dgPublishes_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			Trace.WriteLine("DGrid Exception! " + e.Exception.Message);
		}

		private void btnPublish_Click(object sender, EventArgs e)
		{

			if ((cmbPublishes.SelectedItem == null) || (cmbRemoteLocations.SelectedItem == null)) return;
			RemotePublishLocation rpl = (RemotePublishLocation)cmbRemoteLocations.SelectedItem;

			Trace.WriteLine("Starting to publish : " + (string)cmbPublishes.SelectedItem);
			bool dobackup = false;
			if (Properties.Settings.Default.autoBackupPublishes)
				dobackup = true;
			else
			{
				DialogResult r = MessageBox.Show(this, "Create backup of publish " + cmbPublishes.SelectedItem + " on computer " + rpl.ToString() + "?", "Backup Exiting Publish", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				dobackup = (r == DialogResult.Yes);
				if (r == DialogResult.Cancel)
				{					
					return;
				}
			}
			PublishManager.SendPublishToRemoteComputer(Publish.Load((string)cmbPublishes.SelectedItem, pubRoot), rpl, pubRoot, new EventHandler<Publisher.PublishManager.SendPublishEventArgs>(delegate(object o, Publisher.PublishManager.SendPublishEventArgs args)
			{
				this.BeginInvoke(new MethodInvoker(delegate()
				{
					Publish p = o as Publish;
					if (args.ok)
						MessageBox.Show(this, "The Publish " + p.name + " Succeeded.", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Information);
					else
						MessageBox.Show(this, "The Publish Failed. Check Trace for more info.", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}));
			}),dobackup);
			//			MessageBox.Show(this, "The Publish Failed. Check Trace for more info.", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void cmbPublishes_SelectedIndexChanged(object sender, EventArgs e)
		{
			string s = cmbPublishes.SelectedItem as string;
			if (s == null) return;
			txtPublish.Text = Publish.Load(s, pubRoot).GetDebugString();
		}

		private void frmMain_Load(object sender, EventArgs e)
		{
			if (PublishManager.settings.RepoRoot == "")
			{
				frmNewRepoRoot rr = new frmNewRepoRoot();
				rr.ShowDialog();
				PublishManager.settings.RepoRoot = rr.Result;
				PublishManager.settings.Save(pubRoot);
			}
			dgPublishes.Sort(dgSorter);
		}

		private void btnEditPublish_Click(object sender, EventArgs e)
		{
			if (cmbPublishes.SelectedItem == null) return;
			Publish pold = Publish.Load((string)cmbPublishes.SelectedItem, pubRoot);

			frmNewPublish newpub = new frmNewPublish(pubRoot);
			newpub.PopulateWithExistingPublish(pold);
			newpub.ShowDialog();
			if (newpub.DialogResult == DialogResult.OK)
			{
				Publish p = newpub.newPublish;
				p.Save(pubRoot);
				UpdateLocalPublishes();
				cmbPublishes.SelectedItem = p;
			}
		}

		private void tmrInvalidate_Tick(object sender, EventArgs e)
		{
			foreach (KeyValuePair<string, ComputerStatus> status in computerStatus)
			{
				if (status.Value.valid == false) //still invalidated!!
				{
					status.Value.msg.statusLevel = WatchdogStatusMessage.StatusLevel.NoConnection;
					status.Value.msg.statusText = "Lost Network Connection.";
				}
			}
			//now invalidate all
			foreach (KeyValuePair<string, ComputerStatus> status in computerStatus)
				status.Value.valid = false;
		}

		private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (PublishManager.IsDrivesMapped())
			{
				if (MessageBox.Show(this, "You still have network drives mapped by the Publisher. Would you like them disconnected?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					PublishManager.UnmapAllDrives();
			}
		}


		PublishSorter dgSorter = new PublishSorter();
		private void dgPublishes_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			dgPublishes.Sort(dgSorter);
		}

		public class PublishSorter : IComparer {
			static char[ ] numbers = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			// Calls CaseInsensitiveComparer.Compare with the parameters reversed.
			int IComparer.Compare(Object x, Object y)
			{
				DataGridViewRow dgr1 = x as DataGridViewRow;
				DataGridViewRow dgr2 = y as DataGridViewRow;
				if ((dgr1 == null) || (dgr2 == null)) return 1;
				//seperate the string into the alpha and the number
				string s1 = dgr1.Cells["MachineName"].Value as string;
				string s2 = dgr2.Cells["MachineName"].Value as string;
				if ((s1 == null) || (s2 == null)) return 1;

				int numberIndex1 = s1.IndexOfAny(numbers);
				int numberIndex2 = s2.IndexOfAny(numbers);

				if (numberIndex1 == -1 || numberIndex2 == -1)
					return (new CaseInsensitiveComparer().Compare(s1, s2));

				//so now they are both alpha numeric...
				string alpha1 = s1.Substring(0, numberIndex1);
				string alpha2 = s2.Substring(0, numberIndex2);
				if (alpha1 != alpha2)
					return (new CaseInsensitiveComparer().Compare(s1, s2));

				string number1 = s1.Substring(numberIndex1);
				string number2 = s2.Substring(numberIndex2);
				int int1 = int.Parse(number1);
				int int2 = int.Parse(number2);

				if (int1 > int2) return 1;
				else if (int1 == int2) return 0;
				else return -1;

			}
		}

		private void btnExecutePublishLocally_Click(object sender, EventArgs e)
		{
			Publish p = Publish.Load((string)cmbPublishes.SelectedItem, pubRoot);
			p.Start(pubRoot);
		}

		private void cmbRemoteLocations_SelectedIndexChanged(object sender, EventArgs e)
		{
		
		}

		private void dgPublishes_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{ }
			if (e.Button == MouseButtons.Left)
			{
				if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
				if (dgPublishes[e.ColumnIndex, e.RowIndex] is DataGridViewButtonCell)
				{
					if (((string)dgPublishes["AvailablePublishes", e.RowIndex].Value) == null || ((string)dgPublishes["AvailablePublishes", e.RowIndex].Value) == "")
					{
						MessageBox.Show("You must select a publish.");
						return;
					}
					if (e.ColumnIndex == dgPublishes.Columns["Execute"].Index)
						ExecutePublish((string)dgPublishes["MachineName", e.RowIndex].Value, (string)dgPublishes["AvailablePublishes", e.RowIndex].Value);
					if (e.ColumnIndex == dgPublishes.Columns["RePublish"].Index)
						DoRePublish((string)dgPublishes["MachineName", e.RowIndex].Value, (string)dgPublishes["AvailablePublishes", e.RowIndex].Value);
					if (e.ColumnIndex == dgPublishes.Columns["Stop"].Index)
						StopPublish((string)dgPublishes["MachineName", e.RowIndex].Value, (string)dgPublishes["AvailablePublishes", e.RowIndex].Value);
				}
			}
		}

		private void dgPublishes_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
		{
			ctxPublishes.Tag = e.RowIndex;
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(e.RowIndex, out m)) return;

			tsMenuTitle.Text = " --" + m[0] + "-- ";
			executeToolStripMenuItem.Text = "Execute " + m[1];
			stopToolStripMenuItem.Text = "Stop " + m[1];
			republishToolStripMenuItem.Text = "Publish " + m[1];
			tsEditPublish.Text = "Edit " + m[1];
		}

		private void mountNetworkDriveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!PublishManager.MapAllDrives())
				MessageBox.Show("You must unmap the existing drives first.");
		}

		private void deployToolStripMenuItem_Click(object sender, EventArgs e)
		{
			frmNewWatchdog wd = new frmNewWatchdog();
			if (wd.ShowDialog() != DialogResult.OK) return;
			PublishManager.DeployWatchdog(wd.SelectedFiles);
		}

		private void clearAllPublishesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PublishManager.ClearAllPublishDirs();
		}

		private void unmountNetworkDrivesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!PublishManager.UnmapAllDrives())
				MessageBox.Show("No Drives to unmap.");
		}

		private void killAllWatchdogsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PublishManager.KillAllWatchdogs();
		}

		private void jumpstartAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PublishManager.JumpstartWatchdogs();
		}

		private void setLocalSecurityPolicyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			comm.SendMessage(new CommandMessage(CommandMessage.WatchdogCommand.AddServiceRight, "", CommandMessage.ALLMACHINES));
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			openFileDialog1.Multiselect = true;
			openFileDialog1.InitialDirectory = PublishManager.settings.RepoRoot;
			openFileDialog1.Filter = "Publish Files (*.pbl)|*.pbl|All Files (*.*)|*.*";
			if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

			foreach (string s in openFileDialog1.FileNames)
			{
				string dst = pubRoot + Path.GetFileName(s);
				if (pubRoot.EndsWith("\\") == false)
					dst = pubRoot + "\\" + Path.GetFileName(s);
				File.Copy(s, dst,true);
			}
			UpdateLocalPublishes();
		}

		private void changeRepositoryLocationToolStripMenuItem_Click(object sender, EventArgs e)
		{

			frmNewRepoRoot rr = new frmNewRepoRoot();
			rr.ShowDialog();

			PublishManager.settings.RepoRoot = rr.Result;
			PublishManager.settings.Save(pubRoot);
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SavePreferredRemotePublish();
			PublishManager.settings.Save(pubRoot);
			MessageBox.Show("Saved settings to " + pubRoot + "\\settings.xml.");
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
			UpdateLocalPublishes();
			UpdateLocalPublishLocations();
		}

		private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		bool GetMachineNameAndSelectedPub(int rownum, out string[] ret)
		{
			ret = new string[2];
			if (rownum < 0) return false;
			string machineName = (string)dgPublishes["MachineName", rownum].Value;
			string selectedPub = (string)dgPublishes["AvailablePublishes", rownum].Value;
			if ((machineName == null) || (selectedPub == null))
				return false;
			ret[0] = machineName; ret[1] = selectedPub;
			return true;
		}
		private void executeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			ExecutePublish(m[0], m[1]);
		}

		private void stopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			StopPublish(m[0], m[1]);
		}

		private void republishToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			DoRePublish(m[0], m[1]);
		}

		private void publishDefitionOnlyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;

			RemotePublishLocation rpl;
			if (!PublishManager.GetRemotePublishLocationByComputerName(m[0], out rpl))
			{
				MessageBox.Show(this, "Could not find a remote publish location for " + m[0] + ".", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			PublishManager.SendPublishDefinitionOnly(Publish.Load(m[1], pubRoot),rpl,pubRoot);
		}


		private void startWatchdogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			comm.SendMessage(new CommandMessage(CommandMessage.WatchdogCommand.EnableWatchdogAutoReset, "", m[0]));
		}

		private void startRemoteDebuggerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			comm.SendMessage (new CommandMessage ( CommandMessage.WatchdogCommand.StartRemoteDebugger,"",m[0]));
		}

		private void refreshAvailablePublishesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			comm.SendMessage(new CommandMessage(CommandMessage.WatchdogCommand.RefreshPublishes, "", m[0]));
		}

		private void stopWatchdogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			comm.SendMessage(new CommandMessage(CommandMessage.WatchdogCommand.DisableWatchdogAutoReset, "", m[0]));
		}

		private void launchVNCToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			PublishManager.LaunchVNCViewer(m[0]);
		}


		void tsmi_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
			if (tsmi == null || tsmi.Tag == null) return;
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			string machineName = m[0];
			RemotePublishLocation rpl;
			if (!PublishManager.GetRemotePublishLocationByComputerName(machineName, out rpl))
			{
				MessageBox.Show(this,"Could not find a remote publish location for " + machineName + ".",  "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (computerStatus[machineName.ToLower()].isPublishing == true)
			{
				MessageBox.Show(this, "A Publish is already in progress for this computer. Wait for it to complete...", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			computerStatus[machineName.ToLower()].isPublishing = true;
			Trace.WriteLine("Starting to publish : " + (string)cmbPublishes.SelectedItem);
			bool dobackup = false;





			if (Properties.Settings.Default.autoBackupPublishes)
				dobackup = true;
			else
			{
				DialogResult r = MessageBox.Show(this, "Create backup of publish " + (string)tsmi.Tag + " on computer " + machineName + "?", "Backup Exiting Publish", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
				dobackup = (r == DialogResult.Yes);
				if (r == DialogResult.Cancel)
				{
					computerStatus[machineName.ToLower()].isPublishing = false;
					return;
				}
			}
			
			
			PublishManager.SendPublishToRemoteComputer(Publish.Load((string)tsmi.Tag, pubRoot), rpl, pubRoot, new EventHandler<Publisher.PublishManager.SendPublishEventArgs>(delegate(object o, Publisher.PublishManager.SendPublishEventArgs args)
			{
				this.BeginInvoke(new MethodInvoker(delegate()
					{
						computerStatus[machineName.ToLower()].isPublishing = false;
						Publish p = o as Publish;
						if (args.ok)
							MessageBox.Show(this, "The Publish " + p.name + " Succeeded.", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Information);
						else
							MessageBox.Show(this, "The Publish Failed. Check Trace for more info.", "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}));
			}),dobackup);

		}

		private void killHealthMonitorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PublishManager.KillHealthMonitor();
		}

		private void jumpstartHealthMonitorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PublishManager.JumpstartHealthmonitor();
		}

		private void newPublishToolStripMenuItem_Click(object sender, EventArgs e)
		{
			btnCreateNewPublish_Click(this, null);
		}
		

		private void syncWithRemoteDefinitionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;
			
			RemotePublishLocation rpl;
			if (!PublishManager.GetRemotePublishLocationByComputerName(m[0], out rpl))
			{
				MessageBox.Show(this,"Could not find a remote publish location for " + m[0] + ".",  "Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (MessageBox.Show(this, "Are you sure you want to replace your definition for Publish " + m[1] + " with the one from " + m[0] + "?", "Publisher", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				PublishManager.SyncPublishWithRemotePublishDefinition(m[1], pubRoot, rpl);
				UpdateLocalPublishes();
			}
		}


		private void tsEditPublish_Click_1(object sender, EventArgs e)
		{
			if (ctxPublishes.Tag == null) return;
			int rownum = (int)ctxPublishes.Tag;
			string[] m;
			if (!GetMachineNameAndSelectedPub(rownum, out m)) return;

			Publish pold = Publish.Load(m[1], pubRoot);

			frmNewPublish newpub = new frmNewPublish(pubRoot);
			newpub.PopulateWithExistingPublish(pold);
			newpub.ShowDialog();
			if (newpub.DialogResult == DialogResult.OK)
			{
				Publish p = newpub.newPublish;
				p.Save(pubRoot);
				UpdateLocalPublishes();
			}
		}

		private void dgPublishes_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			string[] m;
			if (!GetMachineNameAndSelectedPub(e.RowIndex, out m)) return;

			PublishManager.LaunchVNCViewer(m[0]);
		}

		private void importSettingsFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			openFileDialog1.Multiselect = false;
			openFileDialog1.InitialDirectory = PublishManager.settings.RepoRoot;
			openFileDialog1.Filter = "Publisher Settings Files (*.xml)|*.xml|All Files (*.*)|*.*";
			if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;
			string settings = openFileDialog1.FileName;
			PublishManager.ImportSettingsFile(settings, pubRoot);
			
			//microcontrollerListView1.SetConfig(PublishManager.settings.microcontrollers);
			//AysncGetSyncPulse();
			UpdateLocalPublishes();
		}

		private void btnClearConsole_Click(object sender, EventArgs e)
		{
			txtDebug.Text = "";
		}

		private void reconnectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				comm.Init();
			}
			catch
			{
				MessageBox.Show(this, "No Network Detected!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		private void notifyIcon1_DoubleClick(object sender, EventArgs e)
		{
			this.ShowInTaskbar = true;
			this.WindowState = FormWindowState.Normal;
			SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
			this.BringToFront();
		}

		#region MCU STUFF
		/*
		private bool getPulsePhase;

		private void AysncGetSyncPulse()
		{
			
			getPulsePhase = false;

			if (!microcontrollerListView1.AnyPendingTimingOps)
			{
				microcontrollerListView1.RefreshSyncAll();
				getPulsePhase = true;
			}

			timerResyncMicro.Enabled = true;
		}

		private void timerResyncMicro_Tick(object sender, EventArgs e)
		{
			if (microcontrollerListView1.AnyPendingTimingOps)
			{
				return;
			}

			if (!getPulsePhase)
			{
				microcontrollerListView1.RefreshSyncAll();
				getPulsePhase = true;
			}
			else
			{
				microcontrollerListView1.RefreshPulseAll();
				timerResyncMicro.Enabled = false;
			}
		}
		*/
		#endregion

		private void ctxNotify_Opening(object sender, CancelEventArgs e)
		{

		}

		private void hideToolStripMenuItem_Click(object sender, EventArgs e)
		{
			notifyIcon1.Visible = true;
			this.ShowInTaskbar = false;
			this.WindowState = FormWindowState.Minimized;
		}

		private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
		{

		}

		private void hideTrayIconToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (hideTrayIconToolStripMenuItem.Checked)
				notifyIcon1.Visible = false;
			else
				notifyIcon1.Visible = true;
		}

		private void autoBackupPublishesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Properties.Settings.Default.Save();
		}
	}
}
