using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using PublishCommon;

namespace Publisher
{
	public partial class frmNewPublishLocation : Form
	{
		public RemotePublishLocation newPublishLocation;
		public frmNewPublishLocation()
		{
			InitializeComponent();
		}

		private void frmNewPublishLocation_Load(object sender, EventArgs e)
		{
			PopulateCmbNetworkDrives();
		}

		private void PopulateCmbNetworkDrives()
		{
			cmbLocalDrive.Items.Clear();
			for (int i = 0; i < 26; i++) 
			{
				byte[] tmp = new byte[1];
				tmp[0] = (byte)(i + 65);
				string drv = Encoding.ASCII.GetString(tmp) + ":\\";
				cmbLocalDrive.Items.Add(drv);
			}
			string next = NetworkDrive.GetNextAvailableDrive();
			foreach (string s in cmbLocalDrive.Items)
			{
				if (s.Equals(next)) cmbLocalDrive.SelectedItem = s;
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			newPublishLocation = new RemotePublishLocation();
			newPublishLocation.localDrive = cmbLocalDrive.Text;
			newPublishLocation.name = txtPublishLocationName.Text;
			newPublishLocation.password = txtPassword.Text;
			newPublishLocation.remoteShare = txtRemoteShare.Text;
			newPublishLocation.username = txtUsername.Text;
			newPublishLocation.removeShare = chkRemoveShare.Checked;
			this.DialogResult = DialogResult.OK;
		}

		private void txtPublishLocationName_TextChanged(object sender, EventArgs e)
		{
			txtRemoteShare.Text = "\\\\" + txtPublishLocationName.Text + "\\publish";
		}
	}
}