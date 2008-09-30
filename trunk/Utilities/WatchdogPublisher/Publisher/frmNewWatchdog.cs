using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Publisher
{
	public partial class frmNewWatchdog : Form
	{
		public frmNewWatchdog()
		{
			InitializeComponent();
		}

		private void btnAddFiles_Click(object sender, EventArgs e)
		{
			openFD.Filter = "Publishable Files (*.exe;*.bat;*.config;*.xml;*.dll;*.txt)|*.exe;*.bat;*.config;*.xml;*.dll;*.txt|All Files (*.*)|*.*";
			if (Directory.Exists(PublishManager.settings.RepoRoot + @"\trunk\Utilities\WatchdogPublisher\Watchdog\bin\Debug"))
				openFD.InitialDirectory = PublishManager.settings.RepoRoot + @"\trunk\Utilities\WatchdogPublisher\Watchdog\bin\Debug";
			else
				openFD.InitialDirectory = PublishManager.settings.RepoRoot;
			openFD.ShowDialog();
			
			foreach (string s in openFD.FileNames)
			{
				string reporoot = PublishManager.settings.RepoRoot;
					lstPublishFiles.Items.Add(s);								
			}			
		}

		private void frmNewWatchdog_Load(object sender, EventArgs e)
		{

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

		private void btnCancel_Click(object sender, EventArgs e)
		{

		}

		public List<string> SelectedFiles
		{
			get
			{
				List<string> l = new List<string>();
				foreach (object o in lstPublishFiles.Items)
				{
					l.Add ((string)o);
				}
				return l;
			}
		}
		private void btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}
	}
}