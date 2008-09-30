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
	public partial class frmNewRepoRoot : Form
	{
		public frmNewRepoRoot()
		{
			InitializeComponent();
			txtRepoRoot.Text = PublishManager.settings.RepoRoot;
		}
		
		public string Result
		{
			get { return txtRepoRoot.Text; }
		}
		private void btnChangeRoot_Click(object sender, EventArgs e)
		{
			folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
			if (Directory.Exists (txtRepoRoot.Text))
				folderBrowserDialog1.SelectedPath = txtRepoRoot.Text;
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
				txtRepoRoot.Text = folderBrowserDialog1.SelectedPath;
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			if (txtRepoRoot.Text.Contains("trunk"))
			{
				MessageBox.Show("Do not include the trunk in the repo path. Just select the ROOT.");
			}
			else
			{
				this.DialogResult = DialogResult.OK;
			}
		}
	}
}