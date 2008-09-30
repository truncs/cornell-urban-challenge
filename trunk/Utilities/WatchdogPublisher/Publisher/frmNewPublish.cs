using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PublishCommon;
using System.IO;

namespace Publisher
{
	public partial class frmNewPublish : Form
	{
		string pubRoot;
		public Publish newPublish = new Publish();
		public frmNewPublish(string pubRoot)
		{
			this.pubRoot = pubRoot;
			InitializeComponent();
		}

		private void frmNewPublish_Load(object sender, EventArgs e)
		{

		}

		private void btnAddFiles_Click(object sender, EventArgs e)
		{
			openFD.InitialDirectory = PublishManager.settings.RepoRoot;
			openFD.Filter = "Publishable Files (*.exe;*.bat;*.config;*.xml;*.dll;*.txt)|*.exe;*.bat;*.config;*.xml;*.dll;*.txt|All Files (*.*)|*.*";
			openFD.ShowDialog();
			string ignored = "";
			foreach (string s in openFD.FileNames)
			{
				string reporoot = PublishManager.settings.RepoRoot;
				if (s.ToLower().StartsWith(reporoot.ToLower()) == false)
					ignored += ("\n" + s);
				else
				{
					lstPublishFiles.Items.Add(s.ToLower().Replace(reporoot.ToLower(), ""));
					if ((Path.GetExtension(s).ToLower().Contains(".exe") || Path.GetExtension(s).ToLower().Contains(".bat")) && (lstCommands.Items.Count == 0))
						lstCommands.Items.Add (Path.GetFileName(s));
				}
			}
			if (ignored != "")
				MessageBox.Show("Warning: " + ignored + "\n is not in the Repository Root. The files will be ignored.");
			ResizeWindow();
		}

		private void ResizeWindow()
		{
			//make the window the proper size

			Graphics g = Graphics.FromImage(new Bitmap(1, 1));
			float maxSize = 0;
			foreach (string s in lstPublishFiles.Items)
			{
				if (maxSize < g.MeasureString(s, this.Font).Width)
					maxSize = g.MeasureString(s, this.Font).Width;
			}
			if (this.Width < maxSize + 40) this.Width = (int)maxSize + 40;
		}

		private void btnRemoveFiles_Click(object sender, EventArgs e)
		{
			if (lstPublishFiles.SelectedItems != null)
			{
				List<object> temp = new List<object>(lstPublishFiles.SelectedItems.Count);
				foreach (object o in lstPublishFiles.SelectedItems)
				{
					temp.Add(o);
				}
				foreach (object o in temp)
				{
					lstPublishFiles.Items.Remove(o);
				}
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			bool exists= false;
			foreach (string s in Publish.GetAllPublishNames(pubRoot))
			{
				if (s.ToLower().Equals(txtPublishName.Text)) exists = true;
			}
			if (exists)
			{
				if(MessageBox.Show(this,"A publish with that name already exists. Would you like to overwrite it?","Publish Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;
			}
		
			//now make the publish 			
			newPublish.name = txtPublishName.Text;
			newPublish.lastPublish = DateTime.MinValue;
			newPublish.files = new List<string>();
			foreach (string s in lstPublishFiles.Items)
				newPublish.files.Add(s);
			newPublish.relativeLocation = txtRelativeLocation.Text;
			newPublish.commands = new List<string>();
			foreach (string s in lstCommands.Items)
				newPublish.commands.Add(s);
			newPublish.watchdogAutoRestart = chkEnableWatchdog.Checked;
			newPublish.watchdogNames = new List<string>();
			foreach (string s in lstWatchdogs.Items)
				newPublish.watchdogNames.Add(s);
			int t;
			if (Int32.TryParse(cmbWatchdogPeriod.Text, out t))
				newPublish.watchdogPeriodms = t;
			else
			{
				MessageBox.Show("Invalid Watchdog Period.");
				return;
			}
			this.DialogResult = DialogResult.OK;
		}

		private void lstPublishFiles_DoubleClick(object sender, EventArgs e)
		{
			if (lstPublishFiles.SelectedItem == null) return;
			lstCommands.Items.Add (Path.GetFileName((string)lstPublishFiles.SelectedItem));
		}

		private void txtPublishName_TextChanged(object sender, EventArgs e)
		{
			txtRelativeLocation.Text = "\\" + txtPublishName.Text;
		}

		internal void PopulateWithExistingPublish(Publish p)
		{
			//now make the publish 
			newPublish = new Publish();
			txtPublishName.Text = p.name;
			lstPublishFiles.Items.Clear();
			if (p.files != null)
			{
				foreach (string s in p.files)
					lstPublishFiles.Items.Add(s);
			}
			txtRelativeLocation.Text = p.relativeLocation;
			if (p.commands != null)
			{
				lstCommands.Items.Clear();
				foreach (string s in p.commands)
					lstCommands.Items.Add(s);
			}
			if (p.watchdogNames != null)
			{
				lstWatchdogs.Items.Clear();
				foreach (string s in p.watchdogNames)
					lstWatchdogs.Items.Add(s);
			}
			cmbWatchdogPeriod.Text = p.watchdogPeriodms.ToString();
			chkEnableWatchdog.Checked = p.watchdogAutoRestart;
			ResizeWindow();
		}

		private void btnAddCommand_Click(object sender, EventArgs e)
		{
			lstCommands.Items.Add(txtNewCommand.Text);
		}

		private void btnRemoveCommand_Click(object sender, EventArgs e)
		{
			if ((lstCommands.SelectedItems == null) || (lstCommands.SelectedItems.Count == 0)) return;
			List<string> newItems = new List<string>();
			foreach (string s in lstCommands.Items)			
				newItems.Add(s);
			foreach (string s in lstCommands.SelectedItems)
				newItems.Remove(s);
			lstCommands.Items.Clear();
			foreach (string s in newItems)			
				lstCommands.Items.Add(s);			
		}

		private void lstCommands_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstCommands.SelectedItem != null)
			txtNewCommand.Text = lstCommands.SelectedItem.ToString();
		}

		private void lstPublishFiles_SelectedIndexChanged(object sender, EventArgs e)
		{

		}

		private void txtNewCommand_TextChanged(object sender, EventArgs e)
		{
			
			
		}

		private void btnEditCommand_Click(object sender, EventArgs e)
		{
			if (lstCommands.SelectedItem == null) return;
			lstCommands.Items.Remove(lstCommands.SelectedItem);
			lstCommands.Items.Add(txtNewCommand.Text);
		}

		private void txtNewCommand_KeyPress(object sender, KeyPressEventArgs e)
		{
			
		}

		private void txtNewCommand_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				btnEditCommand_Click(this, null);
		}

		private void btnEditWatchdog_Click(object sender, EventArgs e)
		{
			if (lstWatchdogs .SelectedItem == null) return;
			lstWatchdogs.Items.Remove(lstWatchdogs.SelectedItem);
			lstWatchdogs.Items.Add(txtWatchdogName.Text);
		}

		private void btnRemoveWatchdog_Click(object sender, EventArgs e)
		{
			if (lstWatchdogs.SelectedItem == null) return;
			lstWatchdogs.Items.Remove(lstWatchdogs.SelectedItem);
		}

		private void btnAddWatchdog_Click(object sender, EventArgs e)
		{
			lstWatchdogs.Items.Add(txtWatchdogName.Text);
		}

		private void lstWatchdogs_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lstWatchdogs.SelectedItem != null)
				txtWatchdogName.Text = lstWatchdogs.SelectedItem.ToString();
		}
	}
}