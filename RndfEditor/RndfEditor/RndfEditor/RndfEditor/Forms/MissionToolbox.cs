using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.Common;
using RndfEditor.Display.Utilities;
using System.Diagnostics;
using UrbanChallenge.Common.Utility;
using System.IO;
using RndfEditor.Common;
using System.Runtime.Serialization.Formatters.Binary;

namespace RndfEditor.Forms
{
	/// <summary>
	/// Edits missions
	/// </summary>
	public partial class MissionToolbox : Form
	{
		#region Members

		public ArbiterRoadNetwork RoadNetwork;
		public ArbiterMissionDescription Mission;
		public List<Object> CreateMissionCheckpoints;
		public Dictionary<string, ArbiterSpeedLimit> CreateMissionNonDefaultSpeeds;

		#endregion

		#region Construction

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arn"></param>
		/// <param name="mission"></param>
		/// <param name="currentCenter"></param>
		public MissionToolbox(ArbiterRoadNetwork arn, ArbiterMissionDescription mission, Coordinates currentCenter)
		{
			InitializeComponent();
			this.RoadNetwork = arn;
			this.Mission = mission;
			this.roadDisplay1.Center(currentCenter);
			this.CreateMissionCheckpoints = new List<object>();
			this.currentMissionComboBox.Click += new EventHandler(currentMissionComboBox_Click);
			this.selectMissionCheckpointComboBox.Click += new EventHandler(selectMissionCheckpointComboBox_Click);
			this.CreateMissionNonDefaultSpeeds = new Dictionary<string, ArbiterSpeedLimit>();
		}

		/// <summary>
		/// Things to do on loading
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			this.roadDisplay1.AddDisplayObjectRange(RoadNetwork.DisplayObjects);
			this.selectRoadNetworkCheckpointComboBox.Items.AddRange(this.GetRoadCheckpointNames().ToArray());
			this.selectAreaComboBox.Items.AddRange(this.GetAreaNames().ToArray());
			this.PopulateModifyTab();
		}

		/// <summary>
		/// What to do when form closes
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosing(CancelEventArgs e)
		{
			DrawingUtility.DisplayArbiterWaypointCheckpointId = false;
		}

		#endregion

		#region Create New Mission

		/// <summary>
		/// Repopulate mission checkpoints box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void selectMissionCheckpointComboBox_Click(object sender, EventArgs e)
		{
			this.selectMissionCheckpointComboBox.Items.Clear();
			this.selectMissionCheckpointComboBox.Items.AddRange(this.CreateMissionCheckpoints.ToArray());
		}
		
		/// <summary>
		/// populates current misison box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void currentMissionComboBox_Click(object sender, EventArgs e)
		{
			this.currentMissionComboBox.Items.Clear();
			this.currentMissionComboBox.Items.AddRange(this.CreateMissionCheckpoints.ToArray());
		}
				
		private List<Object> GetRoadCheckpointNames()
		{
			List<int> cpIds = new List<int>();
			foreach (int i in RoadNetwork.Checkpoints.Keys)
				cpIds.Add(i);
			cpIds.Sort();

			List<Object> objs = new List<object>();
			foreach (int i in cpIds)
			{
				objs.Add(i.ToString() + " - " + RoadNetwork.Checkpoints[i].AreaSubtypeWaypointId.ToString());
			}
			
			return objs;
		}

		private List<Object> GetAreaNames()
		{			
			List<Object> objs = new List<object>();
			foreach (ArbiterSegment asg in this.RoadNetwork.ArbiterSegments.Values)
			{
				objs.Add(asg.SegmentId.ToString());
			}
			foreach (ArbiterZone az in this.RoadNetwork.ArbiterZones.Values)
			{
				objs.Add(az.ZoneId.ToString());
			}

			return objs;
		}

		private void SwapCreateMissionCheckpoint(int i, int j)
		{
			if (i < CreateMissionCheckpoints.Count && i >= 0)
			{
				if (i == 0 && j < 0)
				{
					// nada
				}
				else if (i == CreateMissionCheckpoints.Count - 1 && j >= CreateMissionCheckpoints.Count)
				{
					// nada
				}
				else
				{
					Object tmp = CreateMissionCheckpoints[i];
					CreateMissionCheckpoints[i] = CreateMissionCheckpoints[j];
					CreateMissionCheckpoints[j] = tmp;
				}
			}
		}		

		private void selectRoadNetworkCheckpointComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				int index = this.selectRoadNetworkCheckpointComboBox.SelectedIndex;
				string text = (string)this.selectRoadNetworkCheckpointComboBox.Items[index];
				string[] delimeters = new string[] { " - " };
				string[] final = text.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
				int goal = int.Parse(final[0]);
				this.roadDisplay1.Center(this.RoadNetwork.Checkpoints[goal].Position);
				this.roadDisplay1.Invalidate();
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void MoveDownMissionCheckpointButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.selectMissionCheckpointComboBox.SelectedIndex;
				if (index >= 0 && index < CreateMissionCheckpoints.Count-1)
				{
					this.SwapCreateMissionCheckpoint(index, index + 1);
					this.selectMissionCheckpointComboBox.Items.Clear();
					this.selectMissionCheckpointComboBox.Items.AddRange(this.CreateMissionCheckpoints.ToArray());
					this.selectMissionCheckpointComboBox.SelectedIndex = index + 1;
					this.selectMissionCheckpointComboBox.Invalidate();
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void MoveUpMissionCheckpointButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.selectMissionCheckpointComboBox.SelectedIndex;
				if (index > 0 && index < CreateMissionCheckpoints.Count)
				{
					this.SwapCreateMissionCheckpoint(index, index - 1);
					this.selectMissionCheckpointComboBox.Items.Clear();
					this.selectMissionCheckpointComboBox.Items.AddRange(this.CreateMissionCheckpoints.ToArray());
					this.selectMissionCheckpointComboBox.SelectedIndex = index - 1;
					this.selectMissionCheckpointComboBox.Invalidate();
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void RemoveMissionCheckpointButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.selectMissionCheckpointComboBox.SelectedIndex;
				if (index >= 0 && index < CreateMissionCheckpoints.Count)
				{
					this.CreateMissionCheckpoints.RemoveAt(index);
					this.selectMissionCheckpointComboBox.Items.Clear();
					this.selectMissionCheckpointComboBox.Items.AddRange(this.CreateMissionCheckpoints.ToArray());
					this.selectMissionCheckpointComboBox.SelectedIndex = this.CreateMissionCheckpoints.Count > 0 ? 0 : -1;
					if (this.CreateMissionCheckpoints.Count == 0)
						this.selectMissionCheckpointComboBox.Text = "";
					this.selectMissionCheckpointComboBox.Invalidate();
				}			
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void removeAllCheckpointsButton_Click(object sender, EventArgs e)
		{
			this.CreateMissionCheckpoints = new List<object>();
			this.currentMissionComboBox.Items.Clear();
			this.currentMissionComboBox.Text = "";
			this.currentMissionComboBox.Invalidate();
		}

		private void AddMissionCheckpointButton_Click(object sender, EventArgs e)
		{
			int index = this.selectRoadNetworkCheckpointComboBox.SelectedIndex;
			if(index >= 0)
				this.CreateMissionCheckpoints.Add(this.selectRoadNetworkCheckpointComboBox.Items[index]);
		}

		private void selectAreaComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			string id = (string)this.selectAreaComboBox.Items[this.selectAreaComboBox.SelectedIndex];

			if (this.CreateMissionNonDefaultSpeeds.ContainsKey(id))
			{
				this.maxAreaSpeedTextBox.Text = CreateMissionNonDefaultSpeeds[id].MaximumSpeed.ToString("F2");
				this.minAreaSpeedTextBox.Text = CreateMissionNonDefaultSpeeds[id].MinimumSpeed.ToString("F2");
				this.maxAreaSpeedTextBox.Invalidate();
				this.minAreaSpeedTextBox.Invalidate();
			}
			else
			{
				this.maxAreaSpeedTextBox.Text = "";
				this.minAreaSpeedTextBox.Text = "";
				this.maxAreaSpeedTextBox.Invalidate();
				this.minAreaSpeedTextBox.Invalidate();
			}
		}

		private void setAreaSpeedButton_Click(object sender, EventArgs e)
		{
			try
			{
				if (this.selectAreaComboBox.SelectedIndex >= 0 &&
					this.minAreaSpeedTextBox.Text != null && this.minAreaSpeedTextBox.Text != "" &&
					this.maxAreaSpeedTextBox.Text != null && this.maxAreaSpeedTextBox.Text != "")
				{
					string id = (string)this.selectAreaComboBox.Items[this.selectAreaComboBox.SelectedIndex];

					if (this.CreateMissionNonDefaultSpeeds.ContainsKey(id))
					{
						this.CreateMissionNonDefaultSpeeds[id].MaximumSpeed = double.Parse(this.maxAreaSpeedTextBox.Text);
						this.CreateMissionNonDefaultSpeeds[id].MinimumSpeed = double.Parse(this.minAreaSpeedTextBox.Text);
					}
					else
					{
						ArbiterSpeedLimit asl = new ArbiterSpeedLimit();
						asl.MaximumSpeed = double.Parse(this.maxAreaSpeedTextBox.Text);
						asl.MinimumSpeed = double.Parse(this.minAreaSpeedTextBox.Text);
						this.CreateMissionNonDefaultSpeeds.Add(id, asl);
					}
				}
			}
			catch (Exception ex) 
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void resetMissionButton_Click(object sender, EventArgs e)
		{
			this.missionNameTextBox.Text = "";
			this.roadNetworkNameTextBox.Text = "";
			this.creationDateTimePicker.ResetText();
			this.defaultMaximumSpeedTextBox.Text = "";
			this.defaultMinimumSpeedTextBox.Text = "";
			this.CreateMissionCheckpoints = new List<object>();
			this.CreateMissionNonDefaultSpeeds = new Dictionary<string, ArbiterSpeedLimit>();
			this.minAreaSpeedTextBox.Text = "";
			this.maxAreaSpeedTextBox.Text = "";
			this.currentMissionComboBox.Items.Clear();
			this.currentMissionComboBox.Text = "";
			this.selectMissionCheckpointComboBox.Items.Clear();
			this.selectMissionCheckpointComboBox.Text = "";
			this.Invalidate();
		}

		private void createMissionButton_Click_1(object sender, EventArgs e)
		{
			// create a new open file dialog
			this.saveFileDialog1 = new SaveFileDialog();

			// settings for openFileDialog
			saveFileDialog1.InitialDirectory = "Desktop\\";
			saveFileDialog1.Filter = "Mission Description File (*.mdf)|*.mdf|All files (*.*)|*.*";
			saveFileDialog1.FilterIndex = 1;
			saveFileDialog1.RestoreDirectory = true;
			saveFileDialog1.FileName = this.missionNameTextBox.Text;

			// check if everything was selected alright
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// switch over the final index
					switch (saveFileDialog1.FilterIndex)
					{
						// create an mdf
						case 1:

							// save to a file
							FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create);
							StreamWriter sw = new StreamWriter(fs);
							
							sw.WriteLine("MDF_name" + "\t" + this.missionNameTextBox.Text);
							sw.WriteLine("RNDF_name" + "\t" + this.roadNetworkNameTextBox.Text);
							sw.WriteLine("creation_date" + "\t" + this.creationDateTimePicker.Value.ToString());
							sw.WriteLine("checkpoints");
							sw.WriteLine("num_checkpoints" + "\t" + this.CreateMissionCheckpoints.Count.ToString());

							foreach (object obj in this.CreateMissionCheckpoints)
							{
								string text = (string)obj;
								string[] delimeters = new string[] { " - " };
								string[] final = text.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
								int goal = int.Parse(final[0]);
								sw.WriteLine(goal.ToString());
							}

							sw.WriteLine("end_checkpoints");
							sw.WriteLine("speed_limits");
							sw.WriteLine("num_speed_limits" + "\t" + this.GetAreaNames().Count);

							foreach(string s in this.GetAreaNames())
							{
								if(this.CreateMissionNonDefaultSpeeds.ContainsKey(s))
								{
									sw.WriteLine(s + "\t" + 
										this.CreateMissionNonDefaultSpeeds[s].MinimumSpeed.ToString("F0") + "\t" +
										this.CreateMissionNonDefaultSpeeds[s].MaximumSpeed.ToString("F0"));
								}
								else
								{
									sw.WriteLine(s + "\t" +
										this.defaultMinimumSpeedTextBox.Text + "\t" +
										this.defaultMaximumSpeedTextBox.Text);
								}
							}

							sw.WriteLine("end_speed_limits");
							sw.WriteLine("end_file");

							sw.Dispose();
							fs.Close();

							// end case
							break;
					}
				}
				catch (Exception ex)
				{
					EditorOutput.WriteLine(ex.ToString());
				}
			}
		}

		#endregion

		#region Modify Mission

		private void PopulateMissionBrowser()
		{
			try
			{
				if (this.Mission != null)
				{
					List<List<string>> browser = new List<List<string>>();

					ArbiterCheckpoint[] acs = this.Mission.MissionCheckpoints.ToArray();
					for (int i = 0; i < this.Mission.MissionCheckpoints.Count; i++)
					{
						List<string> cpString = new List<string>();
						cpString.Add(i.ToString());
						cpString.Add(acs[i].CheckpointNumber.ToString());
						cpString.Add(acs[i].WaypointId.ToString());
						browser.Add(cpString);
					}

					for (int i = 0; i < this.Mission.SpeedLimits.Count; i++)
					{
						List<string> slString = new List<string>();
						slString.Add(this.Mission.SpeedLimits[i].Area.ToString());
						slString.Add(this.Mission.SpeedLimits[i].MinimumSpeed.ToString());
						slString.Add(this.Mission.SpeedLimits[i].MaximumSpeed.ToString());

						if (browser.Count > i)
						{
							browser[i].AddRange(slString);
						}
						else
						{
							List<string> tmpString = new List<string>();
							tmpString.Add("");
							tmpString.Add("");
							tmpString.Add("");
							tmpString.AddRange(slString);
							browser.Add(tmpString);
						}
					}

					this.missionBrowser.Items.Clear();

					foreach (List<string> vals in browser)
					{
						this.missionBrowser.Items.Add(new ListViewItem(vals.ToArray()));
					}

					this.missionBrowser.Invalidate();
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void PopulateModifyTab()
		{
			try
			{
				if (this.Mission != null)
				{
					this.modifySelectAreaComboBox.Items.Clear();
					this.modifySelectAreaComboBox.Items.AddRange(this.GetAreaNames().ToArray());

					this.modifySelectCheckpointComboBox.Items.Clear();
					this.modifySelectCheckpointComboBox.Items.AddRange(this.GetRoadCheckpointNames().ToArray());

					this.modifySelectMissionCheckpointComboBox.Items.Clear();
					ArbiterCheckpoint[] acs = this.Mission.MissionCheckpoints.ToArray();
					for(int i = 0; i < acs.Length; i++)
					{
						this.modifySelectMissionCheckpointComboBox.Items.Add(i.ToString() + " - " + acs[i].CheckpointNumber.ToString() + " - " + acs[i].WaypointId.ToString());
					}

					this.PopulateMissionBrowser();
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void modifySetDefaultSpeeds_Click(object sender, EventArgs e)
		{
			try
			{
				double defMin = double.Parse(this.modifyDefaultMinimumSpeedTextBox.Text);
				double defMax = double.Parse(this.modifyDefaultMaximumSpeedTextBox.Text);

				this.Mission.SpeedLimits = new List<ArbiterSpeedLimit>();

				foreach (ArbiterSegment asg in this.RoadNetwork.ArbiterSegments.Values)
				{
					ArbiterSpeedLimit asl = new ArbiterSpeedLimit();
					asl.MinimumSpeed = defMin;
					asl.MaximumSpeed = defMax;
					asl.Area = asg.SegmentId;
					this.Mission.SpeedLimits.Add(asl);
				}

				foreach (ArbiterZone az in this.RoadNetwork.ArbiterZones.Values)
				{
					ArbiterSpeedLimit asl = new ArbiterSpeedLimit();
					asl.MinimumSpeed = defMin;
					asl.MaximumSpeed = defMax;
					asl.Area = az.ZoneId;
					this.Mission.SpeedLimits.Add(asl);
				}

				this.PopulateModifyTab();
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void modifySetAreaSpeedButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.modifySelectAreaComboBox.SelectedIndex;

				if (index >= 0)
				{
					double minSpeed = double.Parse(this.modifyMinSpeedTextBox.Text);
					double maxSpeed = double.Parse(this.modifyMaxSpeedTextBox.Text);
					int areaId = int.Parse((string)this.modifySelectAreaComboBox.Items[index]);

					foreach (ArbiterSpeedLimit asl in this.Mission.SpeedLimits)
					{
						if (asl.Area.Number.Equals(areaId))
						{
							asl.MaximumSpeed = maxSpeed;
							asl.MinimumSpeed = minSpeed;
							break;
						}
					}

					this.PopulateModifyTab();
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void modifyRandomizeButton_Click(object sender, EventArgs e)
		{
			try
			{
				int length = int.Parse(this.modifyNumberToRandomizeTextBox.Text);
				this.Mission.MissionCheckpoints = new Queue<ArbiterCheckpoint>();
				List<ArbiterCheckpoint> acs = new List<ArbiterCheckpoint>();
				foreach (KeyValuePair<int, IArbiterWaypoint> ac in this.RoadNetwork.Checkpoints)
				{
					acs.Add(new ArbiterCheckpoint(ac.Key, ac.Value.AreaSubtypeWaypointId));
				}

				Random r = new Random();
				for (int i = 0; i < length; i++)
				{
					int num = r.Next(acs.Count);
					this.Mission.MissionCheckpoints.Enqueue(acs[num]);
				}

				this.PopulateModifyTab();
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void modifyAddCheckpointButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.modifySelectCheckpointComboBox.SelectedIndex;

				if (index >= 0)
				{
					string text = (string)this.modifySelectCheckpointComboBox.Items[index];
					string[] delimeters = new string[] { " - " };
					string[] final = text.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
					int goal = int.Parse(final[0]);
					this.Mission.MissionCheckpoints.Enqueue(new ArbiterCheckpoint(goal, this.RoadNetwork.Checkpoints[goal].AreaSubtypeWaypointId));
					this.PopulateModifyTab();
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void modifyRemoveCheckpointButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.modifySelectMissionCheckpointComboBox.SelectedIndex;

				if (index >= 0)
				{
					ArbiterCheckpoint[] acs = this.Mission.MissionCheckpoints.ToArray();
					this.Mission.MissionCheckpoints = new Queue<ArbiterCheckpoint>();

					for (int i = 0; i < acs.Length; i++)
					{
						if (i != index)
						{
							this.Mission.MissionCheckpoints.Enqueue(acs[i]);
						}
					}
					this.modifySelectMissionCheckpointComboBox.Text = "";
					this.PopulateModifyTab();
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void ModifyRemoveAllButton_Click(object sender, EventArgs e)
		{
			this.Mission.MissionCheckpoints = new Queue<ArbiterCheckpoint>();
			this.PopulateModifyTab();
		}

		private void modifySaveButton_Click(object sender, EventArgs e)
		{
			// create a new open file dialog
			this.saveFileDialog1 = new SaveFileDialog();

			// settings for openFileDialog
			saveFileDialog1.InitialDirectory = "Desktop\\";
			saveFileDialog1.Filter = "Arbiter Mission Description (*.amd)|*.amd|All files (*.*)|*.*";
			saveFileDialog1.FilterIndex = 1;
			saveFileDialog1.RestoreDirectory = true;

			// check if everything was selected alright
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// switch over the final index
					switch (saveFileDialog1.FilterIndex)
					{
						// create an mdf
						case 1:

							// create file
							FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create);

							// serializer
							BinaryFormatter bf = new BinaryFormatter();

							// serialize
							bf.Serialize(fs, this.Mission);

							// release holds
							fs.Dispose();

							// end case
							break;
					}
				}
				catch (Exception ex)
				{
					EditorOutput.WriteLine(ex.ToString());
				}
			}
		}

		private void modifyMoveCheckpointDownButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.modifySelectMissionCheckpointComboBox.SelectedIndex;

				if (index >= 0 && index < this.Mission.MissionCheckpoints.Count-1)
				{
					ArbiterCheckpoint[] acs = this.Mission.MissionCheckpoints.ToArray();
					this.Mission.MissionCheckpoints = new Queue<ArbiterCheckpoint>();
					ArbiterCheckpoint tmp = acs[index];
					acs[index] = acs[index + 1];
					acs[index + 1] = tmp;

					for (int i = 0; i < acs.Length; i++)
					{
						this.Mission.MissionCheckpoints.Enqueue(acs[i]);
					}

					this.PopulateModifyTab();
					this.modifySelectMissionCheckpointComboBox.SelectedItem = this.modifySelectMissionCheckpointComboBox.Items[index - 1];
					this.modifySelectMissionCheckpointComboBox.SelectedIndex = index + 1;
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		private void modifyMoveCheckpointUpButton_Click(object sender, EventArgs e)
		{
			try
			{
				int index = this.modifySelectMissionCheckpointComboBox.SelectedIndex;

				if (index > 0 && index < this.Mission.MissionCheckpoints.Count)
				{
					ArbiterCheckpoint[] acs = this.Mission.MissionCheckpoints.ToArray();
					this.Mission.MissionCheckpoints = new Queue<ArbiterCheckpoint>();
					ArbiterCheckpoint tmp = acs[index];
					acs[index] = acs[index - 1];
					acs[index - 1] = tmp;

					for (int i = 0; i < acs.Length; i++)
					{
						this.Mission.MissionCheckpoints.Enqueue(acs[i]);
					}

					this.PopulateModifyTab();
					this.modifySelectMissionCheckpointComboBox.SelectedItem = this.modifySelectMissionCheckpointComboBox.Items[index - 1];
					this.modifySelectMissionCheckpointComboBox.SelectedIndex = index - 1;				
				}
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine(ex.ToString());
			}
		}

		#endregion
	}
}