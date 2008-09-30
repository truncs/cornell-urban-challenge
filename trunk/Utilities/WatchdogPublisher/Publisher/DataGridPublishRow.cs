using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WatchdogCommunication;

namespace Publisher
{
	public class DataGridPublishRow : DataGridViewRow
	{
		public DataGridPublishRow()
		{
		}

		public static void PopulateRow(DataGridView dgPublishes, int rownumber, ComputerStatus compstatus, ContextMenuStrip ctx)
		{
			string machineName = compstatus.msg.machineName;
			List<string> publishes = compstatus.msg.availablePublishes;
			string status = compstatus.msg.statusText;
			WatchdogStatusMessage.StatusLevel statusType = compstatus.msg.statusLevel;
			string curPublishName = compstatus.msg.curPublishName;

			DataGridViewTextBoxCell cellMachineName = (DataGridViewTextBoxCell)dgPublishes["MachineName", rownumber];
			DataGridViewComboBoxCell cellAvailablePublishes = (DataGridViewComboBoxCell)dgPublishes["AvailablePublishes", rownumber];
			DataGridViewImageCell cellStatus = (DataGridViewImageCell)dgPublishes["Status", rownumber];
			DataGridViewTextBoxCell cellStatusDetail = (DataGridViewTextBoxCell)dgPublishes["StatusDetail", rownumber];
			DataGridViewButtonCell cellPublish = (DataGridViewButtonCell)dgPublishes["RePublish", rownumber];
			DataGridViewButtonCell cellExecute = (DataGridViewButtonCell)dgPublishes["Execute", rownumber];
			DataGridViewButtonCell cellStop = (DataGridViewButtonCell)dgPublishes["Stop", rownumber];

			if (cellPublish.FlatStyle != FlatStyle.Flat)
			{
				cellPublish.Value = "Publish"; cellPublish.ReadOnly = true;
				cellPublish.FlatStyle = FlatStyle.Flat;
				cellExecute.Value = "Execute"; cellExecute.ReadOnly = true;
				cellExecute.FlatStyle = FlatStyle.Flat;
				cellStop.Value = "Stop"; cellStop.ReadOnly = true;
				cellStop.FlatStyle = FlatStyle.Flat;
				cellStatus.ContextMenuStrip = ctx;
				cellStatusDetail.ContextMenuStrip = ctx;
				cellMachineName.ContextMenuStrip = ctx;
				cellPublish.ContextMenuStrip = ctx;
			}
			
			if (((string)cellMachineName.Value) != machineName)
			{
				cellMachineName.Value = machineName;
				cellMachineName.ReadOnly = true;
			}
			//remember the selected publish
			object o = null;
			if (cellAvailablePublishes.Value != null)
				o = cellAvailablePublishes.Value;
			if (cellAvailablePublishes.FlatStyle != FlatStyle.Flat)
			{
				cellAvailablePublishes.FlatStyle = FlatStyle.Flat;
			}

			if (publishes != null && publishes.Count > 0)
			{
				foreach (string p in publishes)
				{
					if (cellAvailablePublishes.Items.Contains(p) == false)
						cellAvailablePublishes.Items.Add(p);
				}
				List<string> duds = new List<string>();
				foreach (string p in cellAvailablePublishes.Items)
				{
					if (publishes.Contains(p) == false)
						duds.Add(p);
				}
				foreach (string p in duds)
					cellAvailablePublishes.Items.Remove(p);
			}
			string pref = null;
			foreach (PreferredLocation ploc in PublishManager.settings.PreferredRemotePublish)
			{
				if (ploc.computername.ToLower().Equals(machineName.ToLower())) pref = ploc.publishname;
			}

			if (publishes.Count == 0)
				cellAvailablePublishes.Value = "";
			//show the last selected one...
			else if ((o != null) && (publishes.Contains((string)o)))
				cellAvailablePublishes.Value = o;
			//show the one that is running
			else if (((o == null) || (((string)o) == "")) && publishes.Contains(curPublishName))
				cellAvailablePublishes.Value = curPublishName;
			//show the preffered			
			else if (pref != null && publishes.Contains(pref))
				cellAvailablePublishes.Value = pref;
			//show the first
			else if (cellAvailablePublishes.Items.Count > 0)
				cellAvailablePublishes.Value = cellAvailablePublishes.Items[0];

			string statusDetail = curPublishName + ": " + status;
			if (curPublishName == "") statusDetail = status;
			if (compstatus.isPublishing) statusDetail = "Publishing....";
			
			if (((string)cellStatusDetail.Value) != statusDetail)
			{
				cellStatusDetail.Value = statusDetail;
				if (compstatus.isPublishing == false)
				{
					switch (statusType)
					{
						case WatchdogStatusMessage.StatusLevel.Error: cellStatus.Value = Properties.Resources.yellowlight; break;
						case WatchdogStatusMessage.StatusLevel.NotRunning: cellStatus.Value = Properties.Resources.redlight; break;
						case WatchdogStatusMessage.StatusLevel.Running: cellStatus.Value = Properties.Resources.greenlight; break;
						case WatchdogStatusMessage.StatusLevel.NoConnection: cellStatus.Value = Properties.Resources.noconnect; break;
					}
				}
				else
				{
					cellStatus.Value = Properties.Resources.publishing;
				}
			}
		}
	}
}
