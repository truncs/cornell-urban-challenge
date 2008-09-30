using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.ArbiterRoads;
using RemoraAdvanced.Common;
using UrbanChallenge.Arbiter.ArbiterMission;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UrbanChallenge.Common;
using RemoraAdvanced.Communications;
using UrbanChallenge.Common.Utility;
using System.Threading;
using RndfEditor.Display.Utilities;
using RemoraAdvanced.Forms;
using RemoraAdvanced.Display.DisplayObjects;
using RemoraAdvanced.Tools;

namespace RemoraAdvanced
{
	/// <summary>
	/// Main program of remora
	/// </summary>
	public partial class Remora : Form
	{
		#region Private members

		private Thread updateThread;
		private bool updateThreadRun = true;
		private PosteriorPose posteriorPoseDisplay;
		private ArbiterInformationWindow aiInformationWindow;
		private LaneAgentBrowser laneBrowser;

		#endregion

		#region Public Members
		
		/// <summary>
		/// Handles ai clients
		/// </summary>
		public ClientHandler clientHandler;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		public Remora()
		{
			InitializeComponent();

			if (!this.DesignMode)
			{
				this.gridSizeToolStripComboBox.SelectedIndexChanged += new EventHandler(gridSizeToolStripComboBox_SelectedIndexChanged);
				this.posteriorPoseDisplay = new PosteriorPose(this.roadDisplay1.aiVehicle);
				RemoraCommon.aiInformation = new ArbiterInformationDisplay();
			}
		}

		/// <summary>
		/// What to do on load
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Remora_Load(object sender, EventArgs e)
		{
			if (!this.DesignMode)
			{
				// set output
				RemoraOutput.SetTextBox(this.RemoraTextBox);
				RemoraOutput.RemoraMain = this;

				// comms
				RemoraCommon.Communicator = new Communicator(this);
				RemoraCommon.Communicator.BeginCommunications();

				// update!
				updateThread = new Thread(UpdateThread);
				updateThread.IsBackground = true;
				updateThread.Priority = ThreadPriority.Normal;
				updateThread.Start();

				// client
				this.clientHandler = new ClientHandler();

				// event for machine name updates
				RemoraCommon.Communicator.ManchineNameUpdate += new Communicator.MachineEventHandler(Communicator_ManchineNameUpdate);
			}
		}

		#endregion

		#region Tool Strip

		/// <summary>
		/// Opens files
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			// create a new open file dialog
			this.openFileDialog1 = new OpenFileDialog();

			// settings for openFileDialog
			openFileDialog1.InitialDirectory = "Desktop\\";
			openFileDialog1.Filter = "Arbiter Road Network (*.arn)|*.arn|Arbiter Mission Description (*.amd)|*.amd|All files (*.*)|*.*";
			openFileDialog1.FilterIndex = 1;
			openFileDialog1.RestoreDirectory = true;

			// check if everything was selected alright
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// switch over the final index
					switch (openFileDialog1.FilterIndex)
					{
						// open a road network
						case 1:

							// open network from a file
							this.SetRoadNetwork(this.OpenRoadNetworkFromFile(openFileDialog1.FileName));

							// end case
							break;

						// open mission
						case 2:

							// open mission
							this.SetMission(OpenMissionFromFile(openFileDialog1.FileName));

							// end case
							break;
					}
				}
				catch (Exception ex)
				{
					RemoraOutput.WriteLine("Error in [private void openToolStripButton_Click(object sender, EventArgs e)]: " + ex.ToString(), OutputType.Remora);
				}
			}
		}

		/// <summary>
		/// Return to the origin of the graph
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void homeToolStripButton_Click(object sender, EventArgs e)
		{
			// set center as 0,0
			this.roadDisplay1.Center(new Coordinates(0, 0));

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Zoom out by some amount
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void zoomOutToolStripButton_Click(object sender, EventArgs e)
		{
			// zoom out relative to the default zoom
			this.roadDisplay1.Zoom = Math.Max(this.roadDisplay1.Zoom - this.roadDisplay1.Zoom / 6.0f, 0);
		}

		/// <summary>
		/// Return to the standard zoom
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void zoomStandardToolStripButton_Click(object sender, EventArgs e)
		{
			// set standard zoom
			this.roadDisplay1.Zoom = 6.0f;
		}

		/// <summary>
		/// Zoom in by some amount
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ZoomInToolStripButton_Click(object sender, EventArgs e)
		{
			// zoom out relative to the default zoom
			this.roadDisplay1.Zoom = this.roadDisplay1.Zoom + this.roadDisplay1.Zoom / 6.0f;
		}

		/// <summary>
		/// Change the grid size
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void gridSizeToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// switch over the different grids
			switch (this.gridSizeToolStripComboBox.SelectedIndex)
			{
				case 0:
					this.roadDisplay1.DisplayGrid.ShowGrid = false;
					break;
				case 1:
					this.roadDisplay1.DisplayGrid.Spacing = 0.5F;
					break;
				case 2:
					this.roadDisplay1.DisplayGrid.Spacing = 1.0F;
					break;
				case 3:
					this.roadDisplay1.DisplayGrid.Spacing = 5.0F;
					break;
				case 4:
					this.roadDisplay1.DisplayGrid.Spacing = 10.0F;
					break;
				case 5:
					this.roadDisplay1.DisplayGrid.Spacing = 20.0F;
					break;
			}

			// check to draw
			if (this.gridSizeToolStripComboBox.SelectedIndex != 0 && !this.roadDisplay1.DisplayGrid.ShowGrid)
				this.roadDisplay1.DisplayGrid.ShowGrid = true;

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Track the ai vehicle
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TrackAiVehicleButton_Click(object sender, EventArgs e)
		{
			if (this.roadDisplay1.aiVehicle.State != null)
			{
				if (this.TrackAiVehicleButton.CheckState == CheckState.Checked)
				{
					this.roadDisplay1.tracked = this.roadDisplay1.aiVehicle;
					RemoraOutput.WriteLine("Tracking Vehicle", OutputType.Remora);
				}
				else
				{
					this.roadDisplay1.tracked = null;
					RemoraOutput.WriteLine("Stopped Tracking Vehicle", OutputType.Remora);
				}
			}
			else
			{
				RemoraOutput.WriteLine("Cannot track vehicle, as we have no vehicle state update", OutputType.Remora);
				this.TrackAiVehicleButton.CheckState = CheckState.Unchecked;
			}
		}

		/// <summary>
		/// Sync road and mission with ai
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SyncWithAi_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					this.SetRoadNetwork(RemoraCommon.Communicator.ArbiterRemote.GetRoadNetwork());
					this.SetMission(RemoraCommon.Communicator.ArbiterRemote.GetMissionDescription());
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine("Error in road network, mission sync: \n" + ex.ToString(), OutputType.Remora);
			}
		}

		/// <summary>
		/// Prints speeds of the current mdf
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void printMdfSpeeds_Click(object sender, EventArgs e)
		{
			if (RemoraCommon.Mission != null)
			{
				foreach (ArbiterSpeedLimit asl in RemoraCommon.Mission.SpeedLimits)
				{
					RemoraOutput.WriteLine("SpeedLimit: " + asl.Area.ToString() + ", Max: " + asl.MaximumSpeed.ToString() + ", Min: " + asl.MinimumSpeed.ToString(), OutputType.Remora);
				}

				ArbiterCheckpoint[] acs = RemoraCommon.Mission.MissionCheckpoints.ToArray();
				for (int i = 0; i < acs.Length; i++)
				{
					RemoraOutput.WriteLine(i.ToString() + ": Checkpoint Number: " + acs[i].CheckpointNumber.ToString() + ", Waypoint: " + acs[i].WaypointId.ToString(), OutputType.Remora);
				}
			}
			else
			{
				RemoraOutput.WriteLine("Mission cannot be null to print speeds", OutputType.Remora);
			}
		}

		/// <summary>
		/// Test statement to debug
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void testButton_Click(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// Force watchdog in comms to reconnect to all
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RemoraReconnectButton_Click(object sender, EventArgs e)
		{
			RemoraCommon.Communicator.CommunicationsReady = false;
			RemoraOutput.WriteLine("Set Communications Ready to False", OutputType.Remora);
		}

		#endregion

		#region File Handling

		/// <summary>
		/// Sets the road network
		/// </summary>
		/// <param name="roads"></param>
		private void SetRoadNetwork(ArbiterRoadNetwork roads)
		{
			// set network
			RemoraCommon.RoadNetwork = roads;			

			// clean out display
			this.roadDisplay1.RemoveDisplayObjectType(this.roadDisplay1.RoadNetworkFilter);

			// add to display
			this.roadDisplay1.AddDisplayObjectRange(roads.DisplayObjects);

			// notify
			RemoraOutput.WriteLine("Set Road Network", OutputType.Remora);

			// test mission
			if (RemoraCommon.Mission != null)
			{
				RemoraOutput.WriteLine("Testing Previously Loaded Mission aginst new Road Network, Removing and Reloading", OutputType.Remora);
				ArbiterMissionDescription tmp = RemoraCommon.Mission;
				RemoraCommon.Mission = null;
				this.SetMission(tmp);
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Sets mission
		/// </summary>
		/// <param name="mission"></param>
		private void SetMission(ArbiterMissionDescription mission)
		{
			// set mission
			RemoraCommon.Mission = mission;

			// set speeds if can
			if (RemoraCommon.RoadNetwork != null)
			{
				try
				{
					RemoraCommon.RoadNetwork.SetSpeedLimits(mission.SpeedLimits);
				}
				catch (Exception e)
				{
					RemoraOutput.WriteLine("Error setting road network speed limits from inputted mission: " + e.ToString(), OutputType.Remora);
				}

				RemoraOutput.WriteLine("Successfully loaded mission, passed speed limit set check", OutputType.Remora);

				int numAreas = RemoraCommon.RoadNetwork.ArbiterSegments.Count + RemoraCommon.RoadNetwork.ArbiterZones.Count;
				int numSpeeds = RemoraCommon.Mission.SpeedLimits.Count;

				if (numAreas != numSpeeds)
				{
					RemoraOutput.WriteLine("Warning: Number of Speed Limits: " + numSpeeds.ToString() + " not equal to number of network Areas: " + numAreas.ToString(), OutputType.Remora);
				}
				else
				{
					RemoraOutput.WriteLine("Number of Speed Limits: " + numSpeeds.ToString() + " equal to number of network Areas: " + numAreas.ToString(), OutputType.Remora);
				}
			}
			else
			{
				RemoraOutput.WriteLine("Cannot load Mission before Road Network", OutputType.Remora);
			}
		}

		/// <summary>
		/// Open a road network from a file
		/// </summary>
		/// <param name="p"></param>
		private ArbiterRoadNetwork OpenRoadNetworkFromFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Open);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			ArbiterRoadNetwork es = (ArbiterRoadNetwork)bf.Deserialize(fs);

			// release holds
			fs.Dispose();

			return es;
		}

		/// <summary>
		/// Open a mission from a file
		/// </summary>
		/// <param name="p"></param>
		private ArbiterMissionDescription OpenMissionFromFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Open);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			ArbiterMissionDescription amd = (ArbiterMissionDescription)bf.Deserialize(fs);

			// release holds
			fs.Dispose();

			return amd;
		}

		#endregion

		#region Functions

		/// <summary>
		/// Update thread
		/// </summary>
		private void UpdateThread()
		{
			MMWaitableTimer timer = new MMWaitableTimer(100);

			while (updateThreadRun)
			{
				try
				{
					// timer wait
					timer.WaitEvent.WaitOne();

					double speed = RemoraCommon.Communicator.GetVehicleSpeed() != null ? RemoraCommon.Communicator.GetVehicleSpeed().Value : 0;

					// invoke
					if (!this.IsDisposed)
					{
						this.BeginInvoke(new MethodInvoker(delegate()
						{
							if (this.posteriorPoseDisplay != null && !this.posteriorPoseDisplay.IsDisposed)
								this.posteriorPoseDisplay.UpdatePose(speed);

							if (this.aiInformationWindow != null && !this.aiInformationWindow.IsDisposed)
								this.aiInformationWindow.UpdateInformation();

							if (this.laneBrowser != null && !this.laneBrowser.IsDisposed)
								this.laneBrowser.UpdateInformation();

							this.roadDisplay1.Invalidate();
						}));						
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}
		}

		/// <summary>
		/// What to do when machine names updated
		/// </summary>
		/// <param name="source"></param>
		/// <param name="machineNames"></param>
		public void Communicator_ManchineNameUpdate(object source, Dictionary<string, int> machineNames)
		{
			// check if same			
			Dictionary<string, int> current = this.clientHandler.AvailableClients;
			bool same = machineNames.Count == current.Count ? true : false;
			foreach(KeyValuePair<string, int> mn in machineNames)
			{
				if (!current.ContainsKey(mn.Key) || (current[mn.Key] != mn.Value))
					same = false;
			}

			if (!same)
			{
				if (!this.IsDisposed && this.InvokeRequired)
				{
					// invoke
					this.BeginInvoke(new MethodInvoker(delegate()
					{
						this.clientHandler.AvailableClients = machineNames;
						if (this.clientHandler.Current != "" && !machineNames.ContainsKey(this.clientHandler.Current))
							this.clientHandler.Remove(this.clientHandler.Current);

						// remove previous items
						this.ClientListView.Items.Clear();

						// adds all new
						foreach (ListViewItem lvi in clientHandler.ViewableClients)
						{
							this.ClientListView.Items.Add(lvi);
						}
					}));
				}
			}
		}

		#endregion

		#region View Options

		#region Rndf

		/// <summary>
		/// Display the interconnects
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewInterconnectsRndfCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewInterconnectsRndfCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterInterconnects = true;
			}
			else
			{
				DrawingUtility.DrawArbiterInterconnects = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Display the partitions
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewPartitionsRndfCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewPartitionsRndfCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterLanePartition = true;
			}
			else
			{
				DrawingUtility.DrawArbiterLanePartition = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Display waypoints
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewWaypointsRndfEditorCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewWaypointsRndfEditorCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterWaypoint = true;
				DrawingUtility.DrawArbiterPerimeterWaypoint = true;
				DrawingUtility.DrawArbiterParkingSpotWaypoint = true;
			}
			else
			{
				DrawingUtility.DrawArbiterWaypoint = false;
				DrawingUtility.DrawArbiterPerimeterWaypoint = false;
				DrawingUtility.DrawArbiterParkingSpotWaypoint = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// draw parking spots
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewParkingSpotsRndfCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewParkingSpotsRndfCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterParkingSpot = true;
			}
			else
			{
				DrawingUtility.DrawArbiterParkingSpot = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// draw perimeters of zones
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewPerimeterRndfCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewPerimeterRndfCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterPerimeter = true;
			}
			else
			{
				DrawingUtility.DrawArbiterPerimeter = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Draw safety zones
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewSafetyZonesRndfCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewSafetyZonesRndfCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterSafetyZones = true;
			}
			else
			{
				DrawingUtility.DrawArbiterSafetyZones = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Draw intersections
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewIntersectionsRndfEditorCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewIntersectionsRndfEditorCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterIntersections = true;
			}
			else
			{
				DrawingUtility.DrawArbiterIntersections = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		#endregion

		#region Id Info

		/// <summary>
		/// display waypoint id's
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewWaypointIdInfoCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewWaypointIdInfoCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayArbiterWaypointId = true;
				DrawingUtility.DisplayArbiterPerimeterWaypointId = true;
				DrawingUtility.DisplayArbiterParkingSpotWaypointId = true;
			}
			else
			{
				DrawingUtility.DisplayArbiterWaypointId = false;
				DrawingUtility.DisplayArbiterPerimeterWaypointId = false;
				DrawingUtility.DisplayArbiterParkingSpotWaypointId = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// display checkpoint id's
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewCheckpointIdInfoCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewCheckpointIdInfoCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayArbiterWaypointCheckpointId = true;
			}
			else
			{
				DrawingUtility.DisplayArbiterWaypointCheckpointId = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Display way colors of partitions
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewWaysIdInfoCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewWaysIdInfoCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterLanePartitionWays = true;
			}
			else
			{
				DrawingUtility.DrawArbiterLanePartitionWays = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Vehicle id draw
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewVehicleIdCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ViewVehicleIdCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawSimCarId = true;
			}
			else
			{
				DrawingUtility.DrawSimCarId = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		#endregion

		#region Other		

		private void DisplayVehiclesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (DisplayVehiclesCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawSimCars = true;
			}
			else
			{
				DrawingUtility.DrawSimCars = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		private void DisplayObstaclesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (DisplayVehiclesCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawSimObstacles = true;
			}
			else
			{
				DrawingUtility.DrawSimObstacles = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		#endregion

		#endregion

		#region List View

		/// <summary>
		/// Connect to a client
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConnectClientButton_Click(object sender, EventArgs e)
		{
			try
			{
				System.Windows.Forms.ListView.SelectedListViewItemCollection slvic = this.ClientListView.SelectedItems;

				if (slvic != null && slvic.Count == 1)
				{
					ListViewItem lvi = slvic[0];

					System.Windows.Forms.ListViewItem.ListViewSubItemCollection lvsic = lvi.SubItems;

					this.clientHandler.SetMachine(lvsic[1].Text);

					RemoraCommon.Communicator.Shutdown();
					RemoraCommon.Communicator.ResgisterWithClient();
					RemoraOutput.WriteLine("Connecting to Client: " + lvsic[1].Text, OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine("Connection to Client Failed: \n " + ex.ToString(), OutputType.Remora);
			}
		}

		/// <summary>
		/// Remove the client selected in the list view
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RemoveClientButtons_Click(object sender, EventArgs e)
		{
			try
			{
				System.Windows.Forms.ListView.SelectedListViewItemCollection slvic = this.ClientListView.SelectedItems;
				ListViewItem toRemove = null;

				if (slvic != null)
				{
					foreach (ListViewItem lvi in slvic)
					{
						System.Windows.Forms.ListViewItem.ListViewSubItemCollection lvsic = lvi.SubItems;						

						if (this.clientHandler.AvailableClients.ContainsKey(lvsic[1].Text))
						{
							this.clientHandler.Remove(lvsic[1].Text);
							toRemove = lvi;
						}
					}
				}

				if (toRemove != null)
					this.ClientListView.Items.Remove(toRemove);
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine("Removal of Client Failed: \n " + ex.ToString(), OutputType.Remora);
			}
		}

		#endregion

		#region Data

		/// <summary>
		/// View posterior pose
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DataPosteriorPoseInformation_Click_1(object sender, EventArgs e)
		{
			this.posteriorPoseDisplay = new PosteriorPose(this.roadDisplay1.aiVehicle);
			this.posteriorPoseDisplay.Show();
		}

		/// <summary>
		/// View arbiter information
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DataArbiterStateInformation_Click_1(object sender, EventArgs e)
		{
			this.aiInformationWindow = new ArbiterInformationWindow();
			this.aiInformationWindow.Show();
		}

		/// <summary>
		/// Show the lane agent browser
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DataLaneAgent_Click_1(object sender, EventArgs e)
		{
			this.laneBrowser = new LaneAgentBrowser();
			this.laneBrowser.Show();
		}

		#endregion

		#region Tools

		private void jumpstartAiTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					if (RemoraCommon.RoadNetwork != null && RemoraCommon.Mission != null)
						RemoraCommon.Communicator.ArbiterRemote.JumpstartArbiter(RemoraCommon.RoadNetwork, RemoraCommon.Mission);
					else
						RemoraOutput.WriteLine("Road and Mission need to have value for jumpstart", OutputType.Remora);
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void updateMissionTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					if (RemoraCommon.RoadNetwork != null && RemoraCommon.Mission != null)
					{
						bool b = RemoraCommon.Communicator.ArbiterRemote.UpdateMission(RemoraCommon.Mission);
						string s = b ? "Mission set successfully" : "Mission Update Failed";
						RemoraOutput.WriteLine("s", OutputType.Remora);
					}
					else
						RemoraOutput.WriteLine("Mission needs to have value for update", OutputType.Remora);
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void runAiTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					RemoraCommon.Communicator.ArbiterRemote.SetAiMode(UrbanChallenge.Arbiter.Core.Common.ArbiterMode.Run);
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void pauseAiTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					RemoraCommon.Communicator.ArbiterRemote.SetAiMode(UrbanChallenge.Arbiter.Core.Common.ArbiterMode.Pause);
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void restartAiTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					RemoraCommon.Communicator.ArbiterRemote.Reset();
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void shutdownAiTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					RemoraCommon.Communicator.ArbiterRemote.SetAiMode(UrbanChallenge.Arbiter.Core.Common.ArbiterMode.Stop);
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void beginNewLogTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					RemoraCommon.Communicator.ArbiterRemote.BeginNewLog();
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void pauseVehicleFromAi_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					RemoraCommon.Communicator.ArbiterRemote.PauseFromAi();
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void emergencyStopTool_Click(object sender, EventArgs e)
		{
			try
			{
				if (RemoraCommon.Communicator.ArbiterRemote != null)
				{
					RemoraCommon.Communicator.ArbiterRemote.EmergencyStop();
				}
				else
				{
					RemoraOutput.WriteLine("Arbiter Remote needs to be initialized", OutputType.Remora);
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString(), OutputType.Remora);
			}
		}

		private void pointAnalysisTool_Click(object sender, EventArgs e)
		{
			if (this.pointAnalysisTool.CheckState == CheckState.Checked)
			{
				if (RemoraCommon.RoadNetwork != null)
				{
					this.roadDisplay1.SecondaryEditorTool = new PointAnalysisTool(RemoraCommon.RoadNetwork.PlanarProjection, false, RemoraCommon.RoadNetwork, this.roadDisplay1.WorldTransform);
				}
				else
				{
					RemoraOutput.WriteLine("Road network cannot be null for point analysis tool", OutputType.Remora);
				}
			}
			else
			{
				if (this.roadDisplay1.SecondaryEditorTool != null && this.roadDisplay1.SecondaryEditorTool is PointAnalysisTool)
				{
					this.roadDisplay1.SecondaryEditorTool = null;
				}
			}

			this.roadDisplay1.Invalidate();
		}

		private void rulerTool_Click(object sender, EventArgs e)
		{
			if (this.rulerTool.CheckState == CheckState.Checked)
			{
				if (RemoraCommon.RoadNetwork != null)
				{
					this.roadDisplay1.CurrentEditorTool = new RulerTool(false, RemoraCommon.RoadNetwork, this.roadDisplay1.WorldTransform);
				}
				else
				{
					RemoraOutput.WriteLine("Road network cannot be null for ruler tool", OutputType.Remora);
				}
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool != null && this.roadDisplay1.CurrentEditorTool is RulerTool)
				{
					this.roadDisplay1.CurrentEditorTool = null;
				}
			}

			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Forces the ai to reconnect to the shits
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ForceReconnectTool_Click(object sender, EventArgs e)
		{
			if (RemoraCommon.Communicator.ArbiterRemote != null)
			{
				RemoraOutput.WriteLine("Forcing ai to reconnect to the shits", OutputType.Remora);

				try
				{
					RemoraCommon.Communicator.ArbiterRemote.Reconnect();
				}
				catch (Exception ex)
				{
					RemoraOutput.WriteLine("Error reconnecting: \n" + ex.ToString(), OutputType.Remora);
					RemoraCommon.Communicator.CommunicationsReady = false;
				}
			}
			else
			{
				RemoraOutput.WriteLine("Remora not connected to ai", OutputType.Remora);
			}
		}

		#endregion

		private void checkBox7_CheckedChanged(object sender, EventArgs e)
		{
			if (this.checkBox7.CheckState == CheckState.Checked)
				DrawingUtility.DrawArbiterZoneMap = true;
			else
				DrawingUtility.DrawArbiterZoneMap = false;

			this.roadDisplay1.Invalidate();
		}

		private void DisplayLanesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (DisplayLanesCheckBox.CheckState == CheckState.Checked)
				DrawingUtility.DisplayArbiterLanes = true;
			else
				DrawingUtility.DisplayArbiterLanes = false;

			this.roadDisplay1.Invalidate();
		}

		private void viewLane1Polygon_CheckedChanged(object sender, EventArgs e)
		{
			if (this.viewLane1Polygon.CheckState == CheckState.Checked)
				DrawingUtility.DisplayArbiterLanePolygon1 = true;
			else
				DrawingUtility.DisplayArbiterLanePolygon1 = false;

			this.roadDisplay1.Invalidate();
		}

		private void viewLane2Polygon_CheckedChanged(object sender, EventArgs e)
		{
			if (this.viewLane2Polygon.CheckState == CheckState.Checked)
				DrawingUtility.DisplayArbiterLanePolygon2 = true;
			else
				DrawingUtility.DisplayArbiterLanePolygon2 = false;

			this.roadDisplay1.Invalidate();
		}

		private void viewLane3Polygon_CheckedChanged(object sender, EventArgs e)
		{
			if (this.viewLane3Polygon.CheckState == CheckState.Checked)
				DrawingUtility.DisplayArbiterLanePolygon3 = true;
			else
				DrawingUtility.DisplayArbiterLanePolygon3 = false;

			this.roadDisplay1.Invalidate();
		}

		private void viewLane4Polygon_CheckedChanged(object sender, EventArgs e)
		{
			if (this.viewLane4Polygon.CheckState == CheckState.Checked)
				DrawingUtility.DisplayArbiterLanePolygon4 = true;
			else
				DrawingUtility.DisplayArbiterLanePolygon4 = false;

			this.roadDisplay1.Invalidate();
		}

		private void removeNextCheckpoint_Click(object sender, EventArgs e)
		{
			if (RemoraCommon.Communicator.ArbiterRemote != null)
			{
				RemoraCommon.Communicator.ArbiterRemote.RemoveNextCheckpoint();
			}
		}
	}
}