using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.ArbiterRoads;
using System.IO;
using Simulator.Files;
using System.Runtime.Serialization.Formatters.Binary;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;
using Simulator.Engine;
using Simulator.Communications;
using UrbanChallenge.Simulator.Client;
using UrbanChallenge.Arbiter.ArbiterMission;

namespace Simulator
{
	/// <summary>
	/// Main gui of the simulation
	/// </summary>
	public partial class Simulation : Form
	{
		#region Private Members

		/// <summary>
		/// Stack of undo states
		/// </summary>
		private Stack<MemoryStream> undoStack = new Stack<MemoryStream>();

		/// <summary>
		/// Stack of redo states
		/// </summary>
		private Stack<MemoryStream> redoStack = new Stack<MemoryStream>();

		/// <summary>
		/// Name of the current file we are working with
		/// </summary>
		private string currentFile;

		/// <summary>
		/// Arbiter Road network we are working with
		/// </summary>
		private ArbiterRoadNetwork arbiterRoads;

		/// <summary>
		/// The default mission description
		/// </summary>
		private ArbiterMissionDescription defaultMissionDescription;

		#endregion

		#region Public Members

		/// <summary>
		/// Simulator engine
		/// </summary>
		public SimEngine simEngine;

		/// <summary>
		/// Handles commmunications
		/// </summary>
		public Communicator communicator;

		/// <summary>
		/// Client handler to itnerface with clients
		/// </summary>
		public ClientHandler clientHandler;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		public Simulation(string[] args)
		{
			// setup the editor
			InitializeComponent();

			#region Handle Events

			// change grid size
			this.gridSizeToolStripComboBox.SelectedIndexChanged += new EventHandler(gridSizeToolStripComboBox_SelectedIndexChanged);

			// properties redraw
			this.SimulatorPropertyGrid.PropertyValueChanged += new PropertyValueChangedEventHandler(SimulatorPropertyGrid_PropertyValueChanged);

			// track vehicles
			this.trackVehiclesComboBox.SelectedIndexChanged += new EventHandler(trackVehiclesComboBox_SelectedIndexChanged);

			this.simulationSpeedComboBox.SelectedIndexChanged += new EventHandler(simulationSpeedComboBox_SelectedIndexChanged);

			#endregion

			// make sure we are not in design mode
			if (!this.DesignMode)
			{
				// list of clients
				this.clientHandler = new ClientHandler();

				// create sim engine
				this.simEngine = new SimEngine(this.SimulatorPropertyGrid, this);

				// assign to the sim settings
				SimEngineSettings.simForm = this;

				// initialize comms
				this.communicator = new Communicator(this);
				this.communicator.RunMaintenance();
				
				#region File Handling

				// check to see if we started up with a file
				if (args.Length >= 1)
				{
					// get the file name
					string fileName = args[0];

					// retrieve the document extension
					string ext = Path.GetExtension(fileName);

					// check if it is an editor file
					if (ext == ".rdt")
					{
						// open the file
						OpenSimulatorFromFile(fileName);

						// notify
						SimulatorOutput.WriteLine("Opened Simulator File: " + fileName + " upon startup");

						// redraw
						this.Invalidate(true);
					}
				}

				#endregion
			}
		}

		/// <summary>
		/// upon loading the sim
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Simulation_Load(object sender, EventArgs e)
		{
			// set the editor output text box
			SimulatorOutput.SetTextBox(this.RichTextBoxSimulatorOutput);
			
			// set sim in display
			this.roadDisplay1.Simulation = this;

			// attempt connections
			this.communicator.Configure();
			this.communicator.Register();
		}

		#endregion

		#region Menu Strip

		#region File Menu

		/// <summary>
		/// Cleans and creates a new instance of the rndf editor
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// use same
			newToolStripButton_Click(sender, e);
		}

		/// <summary>
		/// Opens a saved instance of hte rndf editor or some new files
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// use same
			openToolStripButton_Click(sender, e);
		}

		/// <summary>
		/// Saves this instance of hte rndf editor
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// use same
			saveToolStripButton_Click(sender, e);
		}

		/// <summary>
		/// Saves a part or this whole instance of the rndf editor
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// create a new open file dialog
			this.saveFileDialog1 = new SaveFileDialog();

			// settings for openFileDialog
			saveFileDialog1.InitialDirectory = "Desktop\\";
			saveFileDialog1.Filter = "Simulator File (*.sim)|*.sim|All files (*.*)|*.*";
			saveFileDialog1.FilterIndex = 1;
			saveFileDialog1.RestoreDirectory = true;
			saveFileDialog1.Title = "Save As";

			// check if everything was selected alright
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				try
				{
					// switch over the final index
					switch (saveFileDialog1.FilterIndex)
					{
						// create an rndf editor file
						case 1:

							// save to a file
							SaveSimulatorToFile(saveFileDialog1.FileName);

							// notify
							SimulatorOutput.WriteLine("Saved Simulator State to File: " + saveFileDialog1.FileName);

							// end case
							break;
					}
				}
				catch (Exception ex)
				{
					SimulatorOutput.WriteLine("Error in [private void saveToolStripButton_Click(object sender, EventArgs e)]: " + ex.ToString());
				}
			}
		}

		/// <summary>
		/// Exits the application
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Initializes the variables to pass to the MessageBox.Show method.
			string message = "Exiting will delete all changes since last save, are you sure you wish to continue?";
			string caption = "Exit";
			MessageBoxButtons buttons = MessageBoxButtons.YesNo;
			DialogResult result;

			// Displays the MessageBox.
			result = MessageBox.Show(message, caption, buttons);

			if (result == System.Windows.Forms.DialogResult.Yes)
			{
				// simply call the close method
				this.Close();
			}
		}

		#endregion

		#region Tools Menu

		/// <summary>
		/// Outputs mdf speeds
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void displayMdfSpeedsToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			if (this.defaultMissionDescription != null)
			{
				foreach(ArbiterSpeedLimit asl in this.defaultMissionDescription.SpeedLimits)
				{
					SimulatorOutput.WriteLine("Area: " + asl.Area.ToString() + ", Max: " + asl.MaximumSpeed.ToString("F6") +
						", Min: " + asl.MinimumSpeed.ToString("F6"));
				}
			}
		}

		/// <summary>
		/// Changes the settings of the editor to view in the properties box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.simEngine.SetPropertyGridDefault();
		}

		/// <summary>
		/// Test something
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TestToolStripMenuItem4_Click(object sender, EventArgs e)
		{

		}

		#endregion

		#endregion

		#region Simulator Tool Strip

		/// <summary>
		/// Cleans and creates a new instance of the rndf editor
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void newToolStripButton_Click(object sender, EventArgs e)
		{
			// Initializes the variables to pass to the MessageBox.Show method.
			string message = "Creating a new simulation state will delete all changes since last save, are you sure you wish to continue?";
			string caption = "Create New Simulation State";
			MessageBoxButtons buttons = MessageBoxButtons.YesNo;
			DialogResult result;

			// Displays the MessageBox.
			result = MessageBox.Show(message, caption, buttons);

			if (result == System.Windows.Forms.DialogResult.Yes)
			{
				// redo the road display
				this.gridSizeToolStripComboBox.SelectedIndex = 3;
				this.roadDisplay1.Reset();

				// reset stacks
				this.ResetUndoRedo();

				// redraw
				this.Invalidate(true);

				// create a new open file dialog
				this.saveFileDialog1 = new SaveFileDialog();

				// title
				this.saveFileDialog1.Title = "Create New Simulation State";

				// settings for openFileDialog
				saveFileDialog1.InitialDirectory = "Desktop\\";
				saveFileDialog1.Filter = "Simulator File (*.sim)|*.sim|All files (*.*)|*.*";
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
							// create an rndf editor file
							case 1:

								// save to a file
								SaveSimulatorToFile(saveFileDialog1.FileName);

								// notify
								SimulatorOutput.WriteLine("Created New Simulator File: " + saveFileDialog1.FileName);

								// end case
								break;
						}
					}
					catch (Exception ex)
					{
						SimulatorOutput.WriteLine("Error in [private void saveToolStripButton_Click(object sender, EventArgs e)]: " + ex.ToString());
					}
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Creation of new Simulator File Aborted");
			}
		}

		/// <summary>
		/// Opens a saved instance of hte rndf editor or some new files
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void openToolStripButton_Click(object sender, EventArgs e)
		{
			// create a new open file dialog
			this.openFileDialog1 = new OpenFileDialog();

			// settings for openFileDialog
			openFileDialog1.InitialDirectory = "Desktop\\";
			openFileDialog1.Filter = "Simulator File (*.sim)|*.sim|Arbiter Road Network (*.arn)|*.arn|Arbiter Mission Description (*.amd)|*.amd|All files (*.*)|*.*";
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
						// resore from an rndf editor file
						case 1:

							// open the editor from a file
							OpenSimulatorFromFile(openFileDialog1.FileName);

							// notify
							SimulatorOutput.WriteLine("Loaded Simulator from File: " + openFileDialog1.FileName);

							// reset undo, redo
							this.ResetUndoRedo();

							// redraw
							this.Invalidate(true);

							// end case
							break;

						// open a road network
						case 2:

							// open network from a file
							OpenRoadNetworkFromFile(openFileDialog1.FileName);

							// notify
							SimulatorOutput.WriteLine("Loaded Arbiter Road Network from File: " + openFileDialog1.FileName);

							// redraw
							this.roadDisplay1.Invalidate();

							// end case
							break;

						// open mission
						case 3:

							// open mission
							OpenMissionFromFile(openFileDialog1.FileName);

							// notify
							SimulatorOutput.WriteLine("Loaded Deafault Arbiter Mission Description File: " + openFileDialog1.FileName);

							// end case
							break;

					}
				}
				catch (Exception ex)
				{
					SimulatorOutput.WriteLine("Error in [private void openToolStripButton_Click(object sender, EventArgs e)]: " + ex.ToString());
				}
			}
		}

		/// <summary>
		/// Saves this instance of hte rndf editor
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void saveToolStripButton_Click(object sender, EventArgs e)
		{
			// if no file, save as
			if (this.currentFile == null)
			{
				saveAsToolStripMenuItem_Click(sender, e);
			}
			// otherwise overwrite current
			else
			{
				SaveSimulatorToFile(currentFile);
			}

		}

		/// <summary>
		/// Undos the last main action
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void undoStripButton_Click(object sender, EventArgs e)
		{
			// undo if can
			if (this.undoStack.Count > 0)
			{
				// remove tool
				this.removeCurrentTool();

				// undo what has been done
				Undo();

				// redraw
				this.Invalidate(true);
			}
		}

		/// <summary>
		/// Redoes an undone action
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void redoToolStripButton_Click(object sender, EventArgs e)
		{
			// redo if can
			if (this.redoStack.Count > 0)
			{
				Redo();

				// redraw
				this.Invalidate(true);
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
		/// What to do when someone clicks the combo box (fill with vehicles)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void trackVehiclesComboBox_Click(object sender, EventArgs e)
		{
			if (!this.trackVehiclesComboBox.IsOnDropDown)
			{
				this.trackVehiclesComboBox.Items.Clear();
				this.trackVehiclesComboBox.Items.Add("None");
				foreach (SimVehicle sv in this.simEngine.Vehicles.Values)
				{
					this.trackVehiclesComboBox.Items.Add(sv.VehicleId.ToString());
				}
			}
		}

		void simulationSpeedComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				string s = (string)this.simulationSpeedComboBox.Items[this.simulationSpeedComboBox.SelectedIndex];
				
				if (s == "Realtime")
				{
					this.simEngine.settings.SimCycleTime = 100;
				}
				else if(s == "75%")
				{
					this.simEngine.settings.SimCycleTime = (int)(100.0 * 100.0 / 75.0);
				}
				else if (s == "50%")
				{
					this.simEngine.settings.SimCycleTime = (int)(100.0 * 100.0 / 50.0);
				}
				else if (s == "25%")
				{
					this.simEngine.settings.SimCycleTime = (int)(100.0 * 100.0 / 25.0);
				}
				else if (s == "10%")
				{
					this.simEngine.settings.SimCycleTime = (int)(100.0 * 100.0 / 10.0);
				}
				else if (s == "5%")
				{
					this.simEngine.settings.SimCycleTime = (int)(100.0 * 100.0 / 5.0);
				}
				else if (s == "1%")
				{
					this.simEngine.settings.SimCycleTime = (int)(100.0 * 100.0 / 1.0);
				}
			}
			catch (Exception ex)
			{
				SimulatorOutput.WriteLine("Error in selecting simulation speed: \n" + ex.ToString());
			}
		}

		/// <summary>
		/// What to do when we select a vehicle to track
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void trackVehiclesComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				string s = (string)this.trackVehiclesComboBox.Items[this.trackVehiclesComboBox.SelectedIndex];

				if (s != "None")
				{
					int vehicleToTrack = int.Parse(s);
					SimVehicle sv = this.simEngine.Vehicles[new SimVehicleId(vehicleToTrack)];
					if (this.roadDisplay1.tracked != null)
						this.roadDisplay1.tracked.Selected = SelectionType.NotSelected;
					this.roadDisplay1.tracked = sv;
					SimulatorOutput.WriteLine("Tracking Vehicle: " + vehicleToTrack.ToString());
				}
				else
				{
					if (this.roadDisplay1.tracked != null)
					{
						this.roadDisplay1.tracked.Selected = SelectionType.NotSelected;
						this.roadDisplay1.tracked = null;
					}
				}

				this.roadDisplay1.Invalidate();
			}
			catch (Exception ex)
			{
				SimulatorOutput.WriteLine("Error attemptimg to track vehicle: \n" + ex.ToString());
			}
		}

		#endregion

		#region Streams: Undo, Redo, Save, Restore

		/// <summary>
		/// Resets the save stacks
		/// </summary>
		private void ResetUndoRedo()
		{
			this.undoStack = new Stack<MemoryStream>();
			this.redoStack = new Stack<MemoryStream>();
			SetUndoRedoIcons();
		}

		/// <summary>
		/// Sets the undo and redo icons
		/// </summary>
		private void SetUndoRedoIcons()
		{
			// check redo for items
			if (redoStack.Count > 0)
			{
				this.redoToolStripButton.Image = global::Simulator.Properties.Resources.Redo_16_n_p;
			}
			else
			{
				this.redoToolStripButton.Image = global::Simulator.Properties.Resources.Redo_16_d_p;
			}

			// check undo for items
			if (undoStack.Count > 0)
			{
				this.undoStripButton.Image = global::Simulator.Properties.Resources.Undo_16_n_p;
			}
			else
			{
				this.undoStripButton.Image = global::Simulator.Properties.Resources.Undo_16_d_p;
			}

			// redraw the editor
			this.Invalidate();
		}

		/// <summary>
		/// Creates a save point of the editor
		/// </summary>
		/// <returns></returns>
		private MemoryStream CreateSimulatorSavePoint()
		{
			try
			{
				// save
				SimulatorSave simulatorSave = Save();

				// serialize
				MemoryStream ms = new MemoryStream();
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, simulatorSave);

				// return the stream
				return ms;
			}
			catch (Exception ex)
			{
				SimulatorOutput.WriteLine("Error in [private MemoryStream CreateSimulatorSavePoint()]: " + ex.ToString());
				return null;
			}
		}

		/// <summary>
		/// Restores the editor to its previous state from a save point
		/// </summary>
		/// <returns></returns>
		private void RestoreSimulatorFromSavePoint(MemoryStream save)
		{
			try
			{
				// restore the saved state from the stream
				BinaryFormatter bf = new BinaryFormatter();
				save.Position = 0;
				SimulatorSave es = (SimulatorSave)bf.Deserialize(save);

				// restore the edtitor
				Restore(es);
			}
			catch (Exception ex)
			{
				SimulatorOutput.WriteLine("Error in [private void RestoreSimulatorFromSavePoint(MemoryStream save)]: " + ex.ToString());
			}
		}

		/// <summary>
		/// Saves an undo point
		/// </summary>
		public void SaveUndoPoint()
		{
			// check if redo stack needs to be cleared
			if (redoStack.Count > 0)
			{
				redoStack = new Stack<MemoryStream>();
			}

			// create stream of saved state
			MemoryStream ms = CreateSimulatorSavePoint();

			// check if exists
			if (ms != null)
			{
				// save to stream
				undoStack.Push(ms);
			}

			// icons
			SetUndoRedoIcons();
		}

		/// <summary>
		/// Undo
		/// </summary>
		private void Undo()
		{
			// check if we can undo
			if (undoStack.Count > 0)
			{
				// get stream of state
				MemoryStream state = undoStack.Pop();

				// put on redo state
				redoStack.Push(CreateSimulatorSavePoint());

				// restore from undo point
				RestoreSimulatorFromSavePoint(state);

				// icons
				SetUndoRedoIcons();
			}
		}

		/// <summary>
		/// Redo
		/// </summary>
		private void Redo()
		{
			// check if we can redo
			if (redoStack.Count > 0)
			{
				// get stream of state
				MemoryStream state = redoStack.Pop();

				// put on redo state
				undoStack.Push(CreateSimulatorSavePoint());

				// restore from undo point
				RestoreSimulatorFromSavePoint(state);

				// icons
				SetUndoRedoIcons();
			}
		}

		/// <summary>
		/// Restore from a saved editor save
		/// </summary>
		/// <param name="es"></param>
		private void Restore(SimulatorSave es)
		{
			// restore the editor
			this.gridSizeToolStripComboBox.SelectedIndex = es.GridSizeIndex;
			this.arbiterRoads = es.ArbiterRoads;
			this.simEngine = es.SimEngine;
			this.simEngine.simulationMain = this;
			this.simEngine.SetPropertyGrid(this.SimulatorPropertyGrid);
			this.defaultMissionDescription = es.Mission;

			// restore the display
			this.roadDisplay1.LoadSave(es.displaySave);

			// attempt to rehash the clients
			this.clientHandler.ReBindAll(this.simEngine.Vehicles);
		}

		/// <summary>
		/// Save state of the editor
		/// </summary>
		/// <returns></returns>
		private SimulatorSave Save()
		{
			// create display save
			DisplaySave displaySave = this.roadDisplay1.Save();

			// create editor save
			SimulatorSave simulatorSave = new SimulatorSave();

			// set fields
			simulatorSave.displaySave = displaySave;
			simulatorSave.ArbiterRoads = this.arbiterRoads;
			simulatorSave.SimEngine = this.simEngine;
			simulatorSave.Mission = this.defaultMissionDescription;

			if (this.gridSizeToolStripComboBox.SelectedIndex >= 0)
				simulatorSave.GridSizeIndex = this.gridSizeToolStripComboBox.SelectedIndex;
			else
				simulatorSave.GridSizeIndex = 3;

			// return
			return simulatorSave;
		}

		#endregion

		#region File Handling

		#region Simulator File Handling

		/// <summary>
		/// Save the editor to a file
		/// </summary>
		/// <param name="fileName"></param>
		public void SaveSimulatorToFile(string fileName)
		{
			// create file
			FileStream fs = new FileStream(fileName, FileMode.Create);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// save point
			SimulatorSave es = Save();

			// set path
			currentFile = fileName;

			// set title
			this.Text = "Simulator - " + Path.GetFileNameWithoutExtension(fileName);

			// serialize
			bf.Serialize(fs, es);

			// release holds
			fs.Dispose();
		}

		/// <summary>
		/// Opens the editor saved state from a file
		/// </summary>
		/// <param name="fileName"></param>
		private void OpenSimulatorFromFile(string fileName)
		{
			// create file
			FileStream fs = new FileStream(fileName, FileMode.Open);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			SimulatorSave es = (SimulatorSave)bf.Deserialize(fs);

			// restore
			Restore(es);

			// set path
			currentFile = fileName;

			// set title
			this.Text = "Simulator - " + Path.GetFileNameWithoutExtension(fileName);

			// release holds
			fs.Dispose();
		}

		#endregion

		#region Road Network Handling
		
		/// <summary>
		/// Open a road network from a file
		/// </summary>
		/// <param name="p"></param>
		private void OpenRoadNetworkFromFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Open);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			ArbiterRoadNetwork es = (ArbiterRoadNetwork)bf.Deserialize(fs);

			// remove previous road network
			this.roadDisplay1.RemoveDisplayObjectType(this.roadDisplay1.RoadNetworkFilter);

			// set netwrok
			this.arbiterRoads = es;

			// display
			this.roadDisplay1.AddDisplayObjectRange(es.DisplayObjects);

			// release holds
			fs.Dispose();
		}

		#endregion

		#region Mdf Handling

		/// <summary>
		/// Open a mission from a file
		/// </summary>
		/// <param name="p"></param>
		private void OpenMissionFromFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Open);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			ArbiterMissionDescription amd = (ArbiterMissionDescription)bf.Deserialize(fs);

			// set default mission
			this.defaultMissionDescription = amd;

			// release holds
			fs.Dispose();
		}

		#endregion

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

		#endregion

		#region Events

		/// <summary>
		/// when the property grid changes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void SimulatorPropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			this.roadDisplay1.Invalidate();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			this.communicator.ShutDown();
		}

		#endregion

		#region Simulator Tools

		/// <summary>
		/// Removes the current tool from existing
		/// </summary>
		private void removeCurrentTool()
		{
			
		}

		/// <summary>
		/// Adds a vehicle to the sim
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddVehicleToolStripButton_Click(object sender, EventArgs e)
		{
			// vehicle created
			SimVehicle stv = this.simEngine.AddVehicle(this.roadDisplay1.WorldTransform.CenterPoint);

			// create and display vehicle
			this.roadDisplay1.AddDisplayObject(stv);

			// notfy
			SimulatorOutput.WriteLine("Created New Vehicle with Id: " + stv.VehicleState.VehicleID.ToString());

			// bind to client if can
			if (this.clientHandler.ReBind(stv.VehicleId))
			{
				this.OnClientsChanged();
			}

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Initializes the remoting configuration
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void InitializeRemotingToolStripButton_Click(object sender, EventArgs e)
		{
			this.communicator.Configure();
		}

		/// <summary>
		/// Registers remoting with the correct services
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RebindServicesToolStripButton_Click(object sender, EventArgs e)
		{
			this.communicator.Register();
		}

		/// <summary>
		/// Adds an obstacle to the world
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddObstacleToolStripButton_Click(object sender, EventArgs e)
		{
			// obstacle created
			SimObstacle so = this.simEngine.AddObstacle(this.roadDisplay1.WorldTransform.CenterPoint);

			// create and display vehicle
			this.roadDisplay1.AddDisplayObject(so);

			// notfy
			SimulatorOutput.WriteLine("Created New Obstacle with Id: " + so.ObstacleId.ToString());

			// redraw
			this.roadDisplay1.Invalidate();
		}

		/// <summary>
		/// Put car in run mode
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CarModeRunToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.roadDisplay1.selected != null && this.roadDisplay1.selected is SimVehicle)
			{
				SimVehicle sv = (SimVehicle)this.roadDisplay1.selected;

				if (this.clientHandler.VehicleToClientMap.ContainsKey(sv.VehicleId))
				{
					try
					{
						this.clientHandler.AvailableClients[this.clientHandler.VehicleToClientMap[sv.VehicleId]].SetCarMode(CarMode.Run);
						sv.CarMode = CarMode.Run;
						SimulatorOutput.WriteLine("Set car mode of Vehicle: " + sv.VehicleId.ToString() + " to Run");
					}
					catch (Exception ex)
					{
						SimulatorOutput.WriteLine("Error setting car mode of Vehicle: " + sv.VehicleId.ToString() + ": \n" + ex.ToString());
					}
				}
				else
				{
					SimulatorOutput.WriteLine("Vehicle not associated with client");
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Need to select vehicle");
			}
		}

		/// <summary>
		/// Put car into pause
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CarModePauseToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.roadDisplay1.selected != null && this.roadDisplay1.selected is SimVehicle)
			{
				SimVehicle sv = (SimVehicle)this.roadDisplay1.selected;

				if (this.clientHandler.VehicleToClientMap.ContainsKey(sv.VehicleId))
				{
					try
					{
						this.clientHandler.AvailableClients[this.clientHandler.VehicleToClientMap[sv.VehicleId]].SetCarMode(CarMode.Pause);
						sv.CarMode = CarMode.Pause;
						SimulatorOutput.WriteLine("Set car mode of Vehicle: " + sv.VehicleId.ToString() + " to Pause");
					}
					catch (Exception ex)
					{
						SimulatorOutput.WriteLine("Error setting car mode of Vehicle: " + sv.VehicleId.ToString() + ": \n" + ex.ToString());
					}
				}
				else
				{
					SimulatorOutput.WriteLine("Vehicle not associated with client");
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Need to select vehicle");
			}
		}

		/// <summary>
		/// put car in human mode
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CarModeHumanToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.roadDisplay1.selected != null && this.roadDisplay1.selected is SimVehicle)
			{
				SimVehicle sv = (SimVehicle)this.roadDisplay1.selected;

				if (this.clientHandler.VehicleToClientMap.ContainsKey(sv.VehicleId))
				{
					try
					{
						this.clientHandler.AvailableClients[this.clientHandler.VehicleToClientMap[sv.VehicleId]].SetCarMode(CarMode.Human);
						sv.CarMode = CarMode.Human;
						SimulatorOutput.WriteLine("Set car mode of Vehicle: " + sv.VehicleId.ToString() + " to Human");
					}
					catch (Exception ex)
					{
						SimulatorOutput.WriteLine("Error setting car mode of Vehicle: " + sv.VehicleId.ToString() + ": \n" + ex.ToString());
					}
				}
				else
				{
					SimulatorOutput.WriteLine("Vehicle not associated with client");
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Need to select vehicle");
			}
		}

		/// <summary>
		/// put car in estop mode
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CarModeEStopToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.roadDisplay1.selected != null && this.roadDisplay1.selected is SimVehicle)
			{
				SimVehicle sv = (SimVehicle)this.roadDisplay1.selected;

				if (this.clientHandler.VehicleToClientMap.ContainsKey(sv.VehicleId))
				{
					try
					{
						this.clientHandler.AvailableClients[this.clientHandler.VehicleToClientMap[sv.VehicleId]].SetCarMode(CarMode.EStop);
						sv.CarMode = CarMode.EStop;
						SimulatorOutput.WriteLine("Set car mode of Vehicle: " + sv.VehicleId.ToString() + " to Estop");
					}
					catch (Exception ex)
					{
						SimulatorOutput.WriteLine("Error setting car mode of Vehicle: " + sv.VehicleId.ToString() + ": \n" + ex.ToString());
					}
				}
				else
				{
					SimulatorOutput.WriteLine("Vehicle not associated with client");
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Need to select vehicle");
			}
		}

		/// <summary>
		/// put simulation into run mode
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SimulatorModeRun_Click(object sender, EventArgs e)
		{
			// just put the sim into run mode
			// check vehicles -> clients
			if (this.simEngine.SimulationState != SimulationState.Running)
			{
				if (this.clientHandler.VehicleToClientMap.Count == this.simEngine.Vehicles.Count)
				{
					if (this.arbiterRoads != null && this.defaultMissionDescription != null)
					{
						// begin sim
						this.simEngine.RunSimulation();
					}
					else
					{
						SimulatorOutput.WriteLine("Need to initialize roads and mission");
					}
				}
				else
				{
					SimulatorOutput.WriteLine("all vehicles reuired to be bound to client upon sim begin");
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Sim can't be running");
			}
		}

		/// <summary>
		/// Reinitialize the simulation
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SimulatorReinitialize_Click(object sender, EventArgs e)
		{
			// check vehicles -> clients
			if (this.simEngine.SimulationState != SimulationState.Running && this.clientHandler.VehicleToClientMap.Count == this.simEngine.Vehicles.Count)
			{
				if (this.arbiterRoads != null && this.defaultMissionDescription != null)
				{
					// begin sim
					this.simEngine.BeginSimulation(this.arbiterRoads, this.defaultMissionDescription);
				}
				else
				{
					SimulatorOutput.WriteLine("Need to initialize roads and mission");
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Sim can't be running and all vehicles reuired to be bound to client upon sim begin");
			}
		}

		/// <summary>
		/// stop simulation from running
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SimulatorModeStop_Click(object sender, EventArgs e)
		{
			// stop sim
			this.simEngine.EndSimulation();
		}

    /// <summary>
    /// Resets the dynamics vehicles in all the sim clients
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void resetSimDynamicsButton_Click(object sender, EventArgs e)
    {
			foreach (SimVehicle sv in this.simEngine.Vehicles.Values)
			{
				sv.Speed = 0.0;
			}

      foreach (KeyValuePair<string, SimulatorClientFacade> scf in this.clientHandler.AvailableClients)
      {
        try
        {
					scf.Value.ResetSim();
					SimulatorOutput.WriteLine("Reset: " + scf.Key);
        }
        catch (Exception ex)
        {
					Console.WriteLine(ex.ToString());
        }
      }
    }

		#endregion

		#region Clients

		/// <summary>
		/// What to do when the clients have changed
		/// </summary>
		public void OnClientsChanged()
		{
			if(this.InvokeRequired)
			{
				this.BeginInvoke(new MethodInvoker(delegate()
				{
					// remove previous items
					this.listView1.Items.Clear();

					// adds all new
					foreach (SimulatorClientFacade scf in this.clientHandler.AvailableClients.Values)
					{
						this.listView1.Items.Add((ListViewItem)scf.ViewableItem().Clone());
					}
				}));
			}
			else
			{
				// remove previous items
				this.listView1.Items.Clear();

				// adds all new
				foreach (SimulatorClientFacade scf in this.clientHandler.AvailableClients.Values)
				{
					this.listView1.Items.Add((ListViewItem)scf.ViewableItem().Clone());
				}
			}
		}

		#endregion

		#region List View

		/// <summary>
		/// Refresh the clients
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void refreshClientsButton_Click(object sender, EventArgs e)
		{
			this.OnClientsChanged();
		}

		/// <summary>
		/// search for clients
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ClientsButtonLookForClients_Click(object sender, EventArgs e)
		{
			SimulatorOutput.WriteLine("Searching for Clients");

			try
			{
				this.communicator.SearchForClients();
			}
			catch (Exception ex)
			{
				SimulatorOutput.WriteLine("Error Looking for Clients: " + ex.ToString());
			}
		}

		/// <summary>
		/// Remove the client selected in the list view
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RemoveClientButton_Click(object sender, EventArgs e)
		{
			try
			{
				System.Windows.Forms.ListView.SelectedListViewItemCollection slvic = this.listView1.SelectedItems;

				if (slvic != null)
				{
					foreach (ListViewItem lvi in slvic)
					{
						System.Windows.Forms.ListViewItem.ListViewSubItemCollection lvsic = lvi.SubItems;						

						if (this.clientHandler.AvailableClients.ContainsKey(lvsic[2].Text))
						{
							this.clientHandler.Remove(lvsic[2].Text);
						}
					}

					this.OnClientsChanged();
				}
			}
			catch (Exception ex)
			{
				SimulatorOutput.WriteLine("Removal of Client Failed: \n " + ex.ToString());
			}
		}

		private void InitializeClient_Click(object sender, EventArgs e)
		{
			try
			{
				System.Windows.Forms.ListView.SelectedListViewItemCollection slvic = this.listView1.SelectedItems;

				if (slvic != null)
				{
					foreach (ListViewItem lvi in slvic)
					{
						System.Windows.Forms.ListViewItem.ListViewSubItemCollection lvsic = lvi.SubItems;

						if (this.clientHandler.AvailableClients.ContainsKey(lvsic[2].Text))
						{
							string clientName = lvsic[2].Text;
							SimulatorClientFacade scf = this.clientHandler.AvailableClients[lvsic[2].Text];

							if (this.clientHandler.ClientToVehicleMap.ContainsKey(clientName))
							{
								SimVehicle sv = this.simEngine.Vehicles[this.clientHandler.ClientToVehicleMap[clientName]];
								KeyValuePair<SimVehicleId, SimVehicle> vhcs = new KeyValuePair<SimVehicleId, SimVehicle>(sv.VehicleId, sv);
								ArbiterRoadNetwork roadNetwork = this.arbiterRoads;
								ArbiterMissionDescription mission = this.defaultMissionDescription;

								// check if we need to randomize mission
								if (vhcs.Value.RandomMission)
								{
									// create random mission
									Queue<ArbiterCheckpoint> checks = new Queue<ArbiterCheckpoint>(60);
									int num = mission.MissionCheckpoints.Count - 1;
									ArbiterCheckpoint[] checkPointArray = mission.MissionCheckpoints.ToArray();
									Random r = new Random();
									for (int i = 0; i < 60; i++)
									{
										checks.Enqueue(checkPointArray[r.Next(num)]);
									}
									ArbiterMissionDescription amd = new ArbiterMissionDescription(checks, mission.SpeedLimits);

									// set road , mission
									scf.SetRoadNetworkAndMission(roadNetwork, amd);
								}
								// otherwise no random mission
								else
								{
									// set road , mission
									scf.SetRoadNetworkAndMission(roadNetwork, mission);
								}

								// startup ai
								bool b = scf.StartupVehicle();

								// check for false
								if (!b)
								{
									SimulatorOutput.WriteLine("Error starting simulation for vehicle id: " + vhcs.Key.ToString());
									return;
								}
							}
							else
							{
								SimulatorOutput.WriteLine("Client needs to be connected to vehicle");
							}
						}
					}

					this.OnClientsChanged();
				}
			}
			catch (Exception ex)
			{
				SimulatorOutput.WriteLine("Client Initialization Failed: \n " + ex.ToString());
			}
		}

		#endregion

		private void resetIndividualDynamics_Click(object sender, EventArgs e)
		{
			if (this.roadDisplay1.selected != null && this.roadDisplay1.selected is SimVehicle)
			{
				SimVehicle sv = (SimVehicle)this.roadDisplay1.selected;

				if (this.clientHandler.VehicleToClientMap.ContainsKey(sv.VehicleId))
				{
					try
					{
						sv.Speed = 0.0;
						this.clientHandler.AvailableClients[this.clientHandler.VehicleToClientMap[sv.VehicleId]].ResetSim();						
						SimulatorOutput.WriteLine("Reset: " + sv.VehicleId.ToString());
					}
					catch (Exception ex)
					{
						SimulatorOutput.WriteLine("Error resetting Vehicle: " + sv.VehicleId.ToString() + ": \n" + ex.ToString());
					}
				}
				else
				{
					SimulatorOutput.WriteLine("Vehicle not associated with client");
				}
			}
			else
			{
				SimulatorOutput.WriteLine("Need to select vehicle");
			}
		}

		private void allCarModeRun_Click(object sender, EventArgs e)
		{
			foreach (KeyValuePair<string, SimulatorClientFacade> scf in this.clientHandler.AvailableClients)
			{
				try
				{
					scf.Value.SetCarMode(CarMode.Run);
					SimulatorOutput.WriteLine("Set Car Mode Run: " + scf.Key);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}

		private void allCarModeHuman_Click(object sender, EventArgs e)
		{
			foreach (KeyValuePair<string, SimulatorClientFacade> scf in this.clientHandler.AvailableClients)
			{
				try
				{
					scf.Value.SetCarMode(CarMode.Human);
					SimulatorOutput.WriteLine("Set Car Mode Human: " + scf.Key);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
			}
		}		
	}
}