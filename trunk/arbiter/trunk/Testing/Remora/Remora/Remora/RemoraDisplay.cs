using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using UrbanChallenge.Common.RndfNetwork;
using Remora.Communications;
using Remora.Display;
using UrbanChallenge.Common.Sensors;
using System.Threading;
using System.Diagnostics;
using UrbanChallenge.Common;
using Remora.Display.Forms;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.ArbiterCommon;
using System.IO;
using UrbanChallenge.Common.Sensors.Vehicle;


namespace Remora
{
	/// <summary>
	/// The main display class of the arbiter
	/// </summary>
	public partial class RemoraDisplay : Form
	{
		private Communicator communicator;
    private Thread outputThread;
    private bool runOutput = true;
		private PosteriorPoseTracks poseLog = null;
		private string rndfPathSave;
		private string mdfPathSave;


		/// <summary>
		/// Constructor
		/// </summary>
		public RemoraDisplay()
		{
			// construct
			InitializeComponent();

      // don't run code in design mode
      if (!this.DesignMode)
      {
          // output
          RemoraOutput.SetTextBox(this.richTextBox1);
      }
		}

		#region Display Directives

		/// <summary>
    /// Standard zoom in
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
		private void ZoomIn_Click(object sender, EventArgs e)
		{
			this.roadDisplay1.Zoom = 18.0f;
		}

		/// <summary>
		/// Reset the zoom
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ZoomStandard_Click(object sender, EventArgs e)
		{
			this.roadDisplay1.Zoom = 6.0f;
		}

		/// <summary>
		/// Standard zoom out
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ZoomOut_Click(object sender, EventArgs e)
		{
			this.roadDisplay1.Zoom = 2.0f;
		}

    /// <summary>
    /// What to do when the track vehicle button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
		private void TrackVehicle_Click(object sender, EventArgs e)
		{
			if (this.TrackVehicle.CheckState == CheckState.Checked)
				Globals.TrackVehicle = true;
			else
				Globals.TrackVehicle = false;
		}

		#endregion

		#region Arbiter Directives

		/// <summary>
		/// Stop the vehicle
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Stop_Click(object sender, EventArgs e)
		{
			try
			{
				this.communicator.StopArbiter();
				RemoraOutput.WriteLine("Stopped Arbiter");
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString());
			} 
		}

		/// <summary>
		/// Set the display's rndf
		/// </summary>
		/// <param name="rndf"></param>
		public void SetRndf(RndfNetwork rndf)
		{
			if (rndf != null)
				this.roadDisplay1.SetRndf(rndf);
			else
				RemoraOutput.WriteLine("Received Null Rndf");
		}

		/// <summary>
		/// set the display's mdf
		/// </summary>
		/// <param name="mdf"></param>
		public void SetMdf(Mdf mdf)
		{
			if (mdf != null)
				this.roadDisplay1.mdf = mdf;
			else
				RemoraOutput.WriteLine("Received Null Rndf");
		}

		#endregion

		#region Arbiter Information

		

		#endregion

		#region Arbiter Connection

		/// <summary>
		/// Binds the arbiter as an ArbiterRemote facade to provide remote control
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConnectArbiter_Click(object sender, EventArgs e)
		{
			//check if we have initialized remoting
			if(this.communicator.RemotingInitialized)
			{
				try
				{
					this.communicator.ConnectToArbiter();
				}
				catch (Exception ex)
				{
					RemoraOutput.WriteLine(ex.ToString());
				}            
			}
			else
			{
				MessageBox.Show("Need to Successfully Initialize Remoting Communication");
			}
		}

    /// <summary>
    /// Try to Listen for Available Stream Data
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
		private void BeginDataStream_Click(object sender, EventArgs e)
		{
			if (BeginDataStream.CheckState == CheckState.Checked)
			{
				//check if we have initialized remoting
				if (this.communicator.RemotingInitialized)
				{
					outputThread = new Thread(OutputThread);
					runOutput = true;
					outputThread.Name = "DataStreamThread";
					outputThread.IsBackground = true;
					outputThread.Start();
				}
				else
				{
					MessageBox.Show("Need to Successfully Initialize Remoting Communication");
				}
			}
			else
			{
				this.StopDataStreamButton_Click(sender, e);
			}
		}

		/// <summary>
		/// Restarts the arbiter
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RestartArbiterButton_Click(object sender, EventArgs e)
		{
			try
			{
				if (this.roadDisplay1.rndf != null && this.roadDisplay1.mdf != null)
				{
					this.communicator.RestartArbiter(this.roadDisplay1.rndf, this.roadDisplay1.mdf);
				}
				else
				{
					RemoraOutput.WriteLine("Cannot start arbiter with rndf or mdf null");
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString());
			}
		}

		#endregion

		#region File Handling

		/// <summary>
		/// load rndf from a file
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void loadRndfToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// create new openFileDialog
			openFileDialog1 = new OpenFileDialog();

			// settings for openFileDialog
			openFileDialog1.InitialDirectory = "Desktop\\";
			openFileDialog1.Filter = "Rndf Network Files (*.rnet)|*.rnet|All files (*.*)|*.*";
			openFileDialog1.FilterIndex = 1;
			openFileDialog1.RestoreDirectory = true;

			// check if everything was selected alright
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// Get Rndf Filename
					string fileName = openFileDialog1.FileName;
					this.rndfPathSave = fileName;

					// create a new network handler
					RndfNetworkHandler rndfNetworkHandler = new RndfNetworkHandler();

					// deserialize
					this.roadDisplay1.SetRndf(rndfNetworkHandler.Load(fileName));
					this.roadDisplay1.Invalidate();

					// notify
					RemoraOutput.WriteLine("Rndf Network Loaded: " + fileName);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}

		/// <summary>
		/// load mdf from a file
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void loadMdfToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// create new openFileDialog
			openFileDialog1 = new OpenFileDialog();

			// settings for openFileDialog
			openFileDialog1.InitialDirectory = "Desktop\\";
			openFileDialog1.Filter = "Mdf Network Files (*.mnet)|*.mnet|All files (*.*)|*.*";
			openFileDialog1.FilterIndex = 1;
			openFileDialog1.RestoreDirectory = true;

			// check if everything was selected alright
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// Get Rndf Filename
					string fileName = openFileDialog1.FileName;
					this.mdfPathSave = fileName;

					// create a new network handler
					MdfHandler mdfHandler = new MdfHandler();

					// deserialize
					this.roadDisplay1.SetMdf(mdfHandler.Load(fileName));

					// notify
					RemoraOutput.WriteLine("Mdf Network Loaded: " + fileName);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}

		#endregion

		#region Main Block

		/// <summary>
		/// Monitors output of the vehicle
		/// </summary>
		private void OutputThread()
		{
			// create a new stopwatch
			Stopwatch stopwatch = new Stopwatch();
			
			// while supposed to listen for data
			while (runOutput)
			{
				try
				{
					// start teh timer
					stopwatch.Reset();
					stopwatch.Start();

					// reset the display, adding grid and rndf and pose log
					List<IDisplayObject> newDisplayObjects = new List<IDisplayObject>();
					newDisplayObjects.Add(new DisplayGrid());
					newDisplayObjects.Add(new RndfDisplay(this.roadDisplay1.rndf));
					newDisplayObjects.Add(this.poseLog);

					// vehicle information fields
					string positionX = "";
					string positionY = "";
					string degreesHeading = "";
					string speed = "";
					string carMode = "";

					// check for fake vehicles
					if (this.communicator.channelListener.FakeVehicles != null && this.communicator.channelListener.FakeVehicles.Value.Vehicles != null &&
						this.communicator.channelListener.FakeVehicles.Value.Vehicles.Length > 0)
					{
						// add fakes
						foreach (ObservedVehicle ov in this.communicator.channelListener.FakeVehicles.Value.Vehicles)
						{
							newDisplayObjects.Add(new ObservedVehicleDisplay(ov, Color.Orange));
						}
					}

					// make sure the vehicle exists
					if (this.communicator.channelListener.Vehicle != null)
					{
						// draw static obstacles
						if (this.communicator.channelListener.ObservedObstacles != null)
						{
							newDisplayObjects.Add(new StaticObstaclesDisplay(this.communicator.channelListener.ObservedObstacles.Value, 
								this.communicator.channelListener.Vehicle));
						}

						if (this.communicator.channelListener.ObservedVehicles != null)
						{
							foreach (ObservedVehicle ov in this.communicator.channelListener.ObservedVehicles.Value.Vehicles)
							{
								newDisplayObjects.Add(new ObservedVehicleDisplay(ov));
							}
						}

						// vehicle information fields
						positionX = this.communicator.channelListener.Vehicle.xyPosition.X.ToString();
						positionY = this.communicator.channelListener.Vehicle.xyPosition.Y.ToString();
						degreesHeading = this.communicator.channelListener.Vehicle.heading.ToDegrees().ToString();
						speed = this.communicator.channelListener.Vehicle.speed.ToString();

						// car mode						
						carMode = this.communicator.CarMode.ToString();

						// add to log
						if (DrawingUtility.LogPose)
						{
							this.poseLog.Update(this.communicator.channelListener.Vehicle.xyPosition);
						}

						// add the car to the display objects
						newDisplayObjects.Add(new CarDisplay(this.communicator.channelListener.Vehicle.xyPosition, this.communicator.channelListener.Vehicle.heading, true));

						// vehicle tracking
						if (Globals.TrackVehicle)
						{
							// Get the offset.
							Point point = new Point(this.roadDisplay1.ClientRectangle.Width / 2, this.roadDisplay1.ClientRectangle.Height / 2);

							// get screen po of vehicle
							PointF screenCarPos = this.roadDisplay1.transform.GetScreenPoint(this.communicator.channelListener.Vehicle.xyPosition);

							// Calculate change in Position
							double deltaX = ((double)screenCarPos.X) - point.X;
							double deltaY = ((double)screenCarPos.Y) - point.Y;

							// Update the world	
							Coordinates tempCenter = this.roadDisplay1.transform.CenterPoint;
							tempCenter.X += deltaX / this.roadDisplay1.transform.Scale;
							tempCenter.Y -= deltaY / this.roadDisplay1.transform.Scale;

							this.roadDisplay1.transform.CenterPoint = tempCenter;
						}
					}

					// arbiter information fields
					string routeTime = "";
					string arbiterState = "";
					string currentGoal = "";
					string goalsLeft = "";
					string routeDistance = "";
					string arbiterCarMode = "";

					// make sure the arbiter state update exists
					if (this.communicator.channelListener.ArbiterInformation != null)
					{
						// get the arbiter information class
						ArbiterInformation arbiterInfo = this.communicator.channelListener.ArbiterInformation;

						// route
						if (arbiterInfo.FullRoute != null)
							newDisplayObjects.Add(new RouteDisplay(this.roadDisplay1.rndf, this.communicator.channelListener.ArbiterInformation.FullRoute));

						// route info
						routeTime = arbiterInfo.RouteTime.ToString();

						// goal
						if (arbiterInfo.CurrentGoal != null && this.roadDisplay1.rndf != null)
							newDisplayObjects.Add(new GoalsDisplay(this.roadDisplay1.rndf, arbiterInfo.CurrentGoal, null));

						// goals left
						if (arbiterInfo.Goals != null)
							goalsLeft = arbiterInfo.Goals.Count.ToString();

						// route distance
						if (arbiterInfo.FullRoute != null)
						{
							double distance = 0;
							for (int i = 0; i < arbiterInfo.FullRoute.RouteNodes.Count - 1; i++)
							{
								distance += this.roadDisplay1.rndf.Waypoints[arbiterInfo.FullRoute.RouteNodes[i]].Position.DistanceTo(
									this.roadDisplay1.rndf.Waypoints[arbiterInfo.FullRoute.RouteNodes[i + 1]].Position);
							}
							routeDistance = distance.ToString();
						}

						// arbiter state
						arbiterState = this.communicator.channelListener.ArbiterInformation.CurrentArbiterState;

						// uturn polygon
						if (arbiterInfo.Behavior != null && arbiterInfo.Behavior is UTurnBehavior)
							newDisplayObjects.Add(new PolygonDisplay(((UTurnBehavior)arbiterInfo.Behavior).BoundingPolygon));

						// current goal
						if (this.communicator.channelListener.ArbiterInformation.CurrentGoal != null)
							currentGoal = this.communicator.channelListener.ArbiterInformation.CurrentGoal.LaneID.ToString()
								+ "." + this.communicator.channelListener.ArbiterInformation.CurrentGoal.WaypointNumber.ToString();

						// car mode
						arbiterCarMode = arbiterInfo.InternalCarMode.ToString();
					}

					// set new display objects
					this.roadDisplay1.displayObjects = newDisplayObjects;

					// redraw
					this.roadDisplay1.Invalidate();
					this.OnResize(new EventArgs());

					// set display of vehicle information
					this.BeginInvoke(new MethodInvoker(delegate()
					{
						// Position
						if (positionX.Length >= 12 && positionY.Length >= 12)
							this.GeneralTabPosition.Text = positionX.Substring(0, 12) + ", " + positionY.Substring(0, 12);
						else
							this.GeneralTabPosition.Text = positionX + ", " + positionY;

						// heading
						if (degreesHeading.Length >= 8)
							this.GeneralTabHeading.Text = degreesHeading.Substring(0, 8) + " degrees";
						else
							this.GeneralTabHeading.Text = degreesHeading + " degrees";

						// speed
						if (speed.Length >= 8)
							this.GeneralTabSpeed.Text = speed.Substring(0, 8) + " m/s";
						else
							this.GeneralTabSpeed.Text = speed + " m/s";

						/*// lane estimates
						if (this.communicator.channelListener.Vehicle.laneEstimate != null)
						{
							this.GeneralTabInitialLane.Text = this.communicator.channelListener.Vehicle.laneEstimate.InitialLane.ToString();
							this.GeneralTabFinalLane.Text = this.communicator.channelListener.Vehicle.laneEstimate.TargetLane.ToString();
							this.GeneralTabLaneEstimateConfidence.Text = this.communicator.channelListener.Vehicle.laneEstimate.Confidence.ToString();
						}*/

						// set route time
						this.GeneralTabRouteTimeText.Text = routeTime;

						// current goal
						this.GeneralTabCurrentGoalText.Text = currentGoal;

						// arbiter state
						this.ArbiterStateTextGeneralTab.Text = arbiterState;

						// route distance
						this.GeneralTabArbiterRouteDistanceText.Text = routeDistance;

						// goals left
						this.GeneralTabGoalsLeftLabelText.Text = goalsLeft;

						// internal car mode
						this.GeneralTabArbiterCarModeText.Text = arbiterCarMode;

						// set external car mode
						this.CarModeLabelTextGeneralTab.Text = carMode;
					}));

					// stop the stopwatch
					stopwatch.Stop();

					// check if we have reached the elapsed time yet
					if (stopwatch.ElapsedMilliseconds < 50)
					{
						// sleep so we can get 50ms cycles
						Thread.Sleep(50 - (int)stopwatch.ElapsedMilliseconds);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString() + "\n\n" + ex.StackTrace);
				}
			}
		}

		#endregion

		/// <summary>
    /// Exit the application
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    /// <summary>
    /// Stops the data stream thread if running
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void StopDataStreamButton_Click(object sender, EventArgs e)
    {
      this.runOutput = false;
			this.BeginDataStream.CheckState = CheckState.Unchecked;
    }

        /// <summary>
        /// Try to reinitialize the remoting communications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitializeCommunicationsButton_Click(object sender, EventArgs e)
        {
            // call communciation initialization
            this.communicator.InitializeRemotingCommunications();
        }

        /// <summary>
        /// Open up the readme in the display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void readmeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // initialize and display the readme
            Readme readme = new Readme();
            readme.Show();
        }

        /// <summary>
        /// Open up the about window in the display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // initialize and display the about page
            About about = new About();
            about.Show();
        }

        /// <summary>
        /// Set that we should display the rndf waypoint ids
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayRndfWaypointIdCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (DisplayRndfWaypointIdCheckBox.CheckState == CheckState.Checked)
                DrawingUtility.DrawRndfWaypointText = true;
            else
                DrawingUtility.DrawRndfWaypointText = false;

            // redraw
            this.roadDisplay1.Invalidate();
        }

        /// <summary>
        /// What to do on startup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoraDisplay_Load(object sender, EventArgs e)
        {
            // make sure not in design mode
            if (!this.DesignMode)
            {
              // communications
              this.communicator = new Communicator(this);

							// pose log
							this.poseLog = new PosteriorPoseTracks(600);
            }
        }

		/// <summary>
		/// Cleas up when closing
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosing(CancelEventArgs e)
		{
			// stop data stream
			runOutput = false;

			// shut down communications
			this.communicator.ShutDown();
			
			// path to saved recent
			string relativePath = "Recent.txt";

			try
			{
				using (StreamWriter sw = new StreamWriter(relativePath))
				{
					// set rndf save
					sw.WriteLine(rndfPathSave);

					// set mdf save
					sw.WriteLine(mdfPathSave);

					// close the file
					sw.Close();
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString());
			}

			// run usual stuff
			base.OnClosing(e);
		}

        /// <summary>
        /// Whether or not to display interconnections
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayInterconnectsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.DisplayInterconnectsCheckBox.CheckState == CheckState.Checked)
            {
                DrawingUtility.DrawInterconnects = true;
            }
            else
            {
                DrawingUtility.DrawInterconnects = false;
            }

            // redraw
            this.roadDisplay1.Invalidate();
        }

        /// <summary>
        /// Whether or not to draw the rndf
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayRndfCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.DisplayRndfCheckBox.CheckState == CheckState.Checked)
            {
                DrawingUtility.DrawRndf = true;
            }
            else
            {
                DrawingUtility.DrawRndf = false;
            }

            // redraw
            this.roadDisplay1.Invalidate();
        }

        /// <summary>
        /// Whether or not to draw the user partitions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayUserPartitionsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.DisplayUserPartitionsCheckBox.CheckState == CheckState.Checked)
            {
                DrawingUtility.DrawUserPartitions = true;
            }
            else
            {
                DrawingUtility.DrawUserPartitions = false;
            }

            // redraw
            this.roadDisplay1.Invalidate();
        }

        /// <summary>
        /// Whether or not to draw lane partitions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayLanePartitionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.DisplayLanePartitionCheckBox.CheckState == CheckState.Checked)
            {
                DrawingUtility.DrawLanePartitions = true;
            }
            else
            {
                DrawingUtility.DrawLanePartitions = false;
            }

            // redraw
            this.roadDisplay1.Invalidate();
        }

        /// <summary>
        /// Whether or not to draw rndf waypoints
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayRndfWaypointsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.DisplayRndfWaypointsCheckBox.CheckState == CheckState.Checked)
            {
                DrawingUtility.DrawRndfWaypoints = true;
            }
            else
            {
                DrawingUtility.DrawRndfWaypoints = false;
            }

            // redraw
            this.roadDisplay1.Invalidate();
        }

		/// <summary>
		/// Whether or not to display the current goals
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DisplayGoalsCheckBoxOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (this.DisplayGoalsCheckBoxOptionsTab.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayRndfGoals = true;
			}
			else
			{
				DrawingUtility.DisplayRndfGoals = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Whether or not to display lane splines
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DisplayLaneSplinesCheckBoxOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (this.DisplayLaneSplinesCheckBoxOptionsTab.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayLaneSplines = true;
			}
			else
			{
				DrawingUtility.DisplayLaneSplines = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Whether or not to display interconnect splines
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DisplayInterconnectSplinesCheckBoxOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (this.DisplayInterconnectSplinesCheckBoxOptionsTab.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayIntersectionSplines = true;
			}
			else
			{
				DrawingUtility.DisplayIntersectionSplines = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Whether or not to display the intersection bounds
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void IntersectionBoundsCheckBoxOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (this.IntersectionBoundsCheckBoxOptionsTab.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayIntersectionBounds = true;
			}
			else
			{
				DrawingUtility.DisplayIntersectionBounds = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Sends mdf to arbiter
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void updateMdfToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(this.roadDisplay1.mdf == null)
			{
				MessageBox.Show("Need to load mdf before sending mdf to arbiter");
			}
			else
			{
				if (!communicator.UpdateMdf(this.roadDisplay1.mdf))
				{
					RemoraOutput.WriteLine("Error setting mdf");
				}
			}			
		}

		/// <summary>
		/// get the rndf from the arbiter
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void retrieveRndfToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.SetRndf(communicator.RetrieveRndf());
		}

		/// <summary>
		/// get the mdf from the arbiter
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void retrieveMdfToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.SetMdf(communicator.RetrieveMdf());
		}

		/// <summary>
		/// pings the arbiter
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PingArbiterToolStripButton_Click(object sender, EventArgs e)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Reset();
			stopwatch.Start();

			if (this.communicator.PingArbiter())
			{
				stopwatch.Stop();
				RemoraOutput.WriteLine("Ping Response Received: " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
			}
			else
			{
				RemoraOutput.WriteLine("Ping Response Failed!");
				stopwatch.Stop();
			}
		}

		/// <summary>
		/// gets teh rndf and mdf from the arbiter
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SyncNetworkDataToolStripButton_Click(object sender, EventArgs e)
		{
			this.communicator.RetrieveNetworkData();
		}

		private void ArbiterLanePathDisplayCheckBoxOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (ArbiterLanePathDisplayCheckBoxOptionsTab.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterLanePath = true;
			}
			else
			{
				DrawingUtility.DrawArbiterLanePath = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		private void OperationalLanePathDisplayCheckBoxOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (OperationalLanePathDisplayCheckBoxOptionsTab.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawOperationalLanePath = true;
			}
			else
			{
				DrawingUtility.DrawOperationalLanePath = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		private void FullRouteDisplayCheckBoxOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (FullRouteDisplayCheckBoxOptionsTab.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayFullRoute = true;
			}
			else
			{
				DrawingUtility.DisplayFullRoute = false;
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// button to log the pose
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LogPoseToolStripButton_Click(object sender, EventArgs e)
		{
			if (LogPoseToolStripButton.CheckState == CheckState.Checked)
				DrawingUtility.LogPose = true;
			else
				DrawingUtility.LogPose = false;
		}

		/// <summary>
		/// restarts logging the pose
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RestartPoseLog_Click(object sender, EventArgs e)
		{
			this.poseLog.Restart(this.poseLog.size);

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Display option for the pose log
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PoseLogDisplayOptionsTab_CheckedChanged(object sender, EventArgs e)
		{
			if (this.PoseLogDisplayOptionsTab.CheckState == CheckState.Checked)
				DrawingUtility.DisplayPoseLog = true;
			else
				DrawingUtility.DisplayPoseLog = false;

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Loads a recent rndf and mdf
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LoadRecentToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// path to saved recent
			string relativePath = "Recent.txt";

			try
			{
				using (StreamReader sr = new StreamReader(relativePath))
				{
					// get rndf
					string rndfPath = sr.ReadLine();
					RemoraOutput.WriteLine("Loading Rndf: " + rndfPath);

					// get mdf
					string mdfPath = sr.ReadLine();
					RemoraOutput.WriteLine("Loading Mdf: " + mdfPath);
					
					sr.Dispose();
					sr.Close();
				}
			}
			catch (Exception ex)
			{
				RemoraOutput.WriteLine(ex.ToString());
			}
		}


		/// <summary>
		/// Whether or not to display deleted vehicles
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DisplayDeletedVehiclesBoxOptions_CheckedChanged(object sender, EventArgs e)
		{
			if (this.DisplayDeletedVehiclesBoxOptions.CheckState == CheckState.Checked)
			{
				DrawingUtility.DisplayDeletedVehicles = true;
			}
			else
			{
				DrawingUtility.DisplayDeletedVehicles = false;
			}

			this.roadDisplay1.Invalidate();
		}
	}
}