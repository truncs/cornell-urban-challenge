using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using UrbanChallenge.Common;
using System.IO;
using RndfEditor.Files;
using System.Runtime.Serialization.Formatters.Binary;
using RndfEditor.Common;
using RndfToolkit;
using UrbanChallenge.Common.EarthModel;
using UrbanChallenge.Arbiter.ArbiterRoads;
using RndfEditor.Display.Utilities;
using RndfEditor.Tools;
using UrbanChallenge.Arbiter.ArbiterMission;
using UrbanChallenge.DarpaRndf;
using RndfEditor.Display.DisplayObjects;
using UrbanChallenge.Arbiter.Core.Common.Tools;
using GpcWrapper;
using System.Threading;

namespace RndfEditor.Forms
{
	public partial class Editor : Form
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
		/// Current mission description file
		/// </summary>
		private ArbiterMissionDescription missionDescription;

		/// <summary>
		/// Arbiter Road network we are working with
		/// </summary>
		private ArbiterRoadNetwork arbiterRoads;

		/// <summary>
		/// Temp points
		/// </summary>
		private TemporaryPointsDisplay temporaryPoints;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor
		/// </summary>
		public Editor(string[] args)
		{
			// Setup the editor
			InitializeComponent();

			#region Handle Events

			// change grid size
			this.gridSizeToolStripComboBox.SelectedIndexChanged += new EventHandler(gridSizeToolStripComboBox_SelectedIndexChanged);

			#endregion

			// make sure we are not in design mode
			if (!this.DesignMode)
			{
				// init temp points
				this.temporaryPoints = new TemporaryPointsDisplay();
				this.roadDisplay1.AddDisplayObject(this.temporaryPoints);

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
						OpenEditorFromFile(fileName);

						// notify
						EditorOutput.WriteLine("Opened Rndf Editor File: " + fileName + " upon startup");

						// redraw
						this.Invalidate(true);
					}
					else if (ext == ".rdf")
					{
						// create the rndf network
						arbiterRoads = RndfTools.GenerateRndfNetwork(fileName);

						// display
						this.roadDisplay1.AddDisplayObjectRange(arbiterRoads.DisplayObjects);
					}
				}

				#endregion
			}
		}

		/// <summary>
		/// Take care of some start up stuff
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Editor_Load(object sender, EventArgs e)
		{
			// set the editor output text box
			EditorOutput.SetTextBox(this.RichTextBoxEditorOutput, this);
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
			saveFileDialog1.Filter = "Rndf Editor File (*.rdt)|*.rdt|Arbiter Road Network (*.arn)|*.arn|Arbiter Mission Description Save (*.amd)|*.amd|Road Graph (*.rgp)|*.rgp|All files (*.*)|*.*";
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
							SaveEditorToFile(saveFileDialog1.FileName);

							// notify
							EditorOutput.WriteLine("Saved Rndf Editor to File: " + saveFileDialog1.FileName);

							// end case
							break;

						// create arbiter road network file
						case 2:

							// save to file
							SaveRoadNetworkToFile(saveFileDialog1.FileName);

							// notify
							EditorOutput.WriteLine("Created Arbiter Road Network File: " + saveFileDialog1.FileName);

							// end case
							break;

						case 3:

							// save to file
							this.SaveMissionToFile(saveFileDialog1.FileName);

							// notify
							EditorOutput.WriteLine("Created Arbiter Mission Description: " + saveFileDialog1.FileName);

							break;

						// create a road graph for the scene estimator
						case 4:

							// road graph generator initialization
							SceneRoadGraphGeneration srgg = new SceneRoadGraphGeneration(this.arbiterRoads);

							// create the road graph at the specified location
							srgg.GenerateRoadGraph(saveFileDialog1.FileName);

							// notify
							EditorOutput.WriteLine("Exported Rndf Editor to Road Graph File: " + saveFileDialog1.FileName);

							// end case
							break;
					}
				}
				catch (Exception ex)
				{
					EditorOutput.WriteLine("Error in [private void saveToolStripButton_Click(object sender, EventArgs e)]: " + ex.ToString());
				}
			}
		}

		/// <summary>
		/// Prints out to files for vaious information
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>Can be used to print back out waypoint coordinates, for example</remarks>
		private void PrintToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// create a new print file dialog
			this.printDialog1 = new PrintDialog();

			// settings for openFileDialog
			printDialog1.AllowPrintToFile = true;
			printDialog1.PrintToFile = true;

			this.printDialog1.ShowDialog();
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
		private void displayMdfSpeedsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.missionDescription != null)
			{
				foreach (ArbiterSpeedLimit asl in this.missionDescription.SpeedLimits)
				{
					EditorOutput.WriteLine("Area: " + asl.Area.ToString() + ", Min: " + asl.MinimumSpeed.ToString("F6") + ", Max: " + asl.MaximumSpeed.ToString("F6"));
				}
			}
		}

		/// <summary>
		/// Converts mdf speeds to m/s
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void convertMdfToMsToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// Test something
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TestToolStripMenuItem4_Click(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// output temp points to file
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void transferTempPointsToDegreesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.temporaryPoints.Points.Count > 0)
			{
				// create a new open file dialog
				this.saveFileDialog1 = new SaveFileDialog();

				// settings for openFileDialog
				saveFileDialog1.InitialDirectory = "Desktop\\";
				saveFileDialog1.Filter = "Text File (*.txt)|*.txt|All files (*.*)|*.*";
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
								this.temporaryPoints.SaveToFileAsDegrees(saveFileDialog1.FileName, this.arbiterRoads.PlanarProjection);

								// notify
								EditorOutput.WriteLine("Saved Temporary Points to File: " + saveFileDialog1.FileName);

								// end case
								break;
						}
					}
					catch (Exception ex)
					{
						EditorOutput.WriteLine("Error in [private void transferTempPointsToDegreesToolStripMenuItem_Click(object sender, EventArgs e)]: " + ex.ToString());
					}
				}
			}
			else
			{
				EditorOutput.WriteLine("No Points to output");				
			}
		}

		#endregion		

		#endregion

		#region Editor Tool Strip

		/// <summary>
		/// Cleans and creates a new instance of the rndf editor
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void newToolStripButton_Click(object sender, EventArgs e)
		{
			// Initializes the variables to pass to the MessageBox.Show method.
			string message = "Creating a new editor file will delete all changes since last save, are you sure you wish to continue?";
			string caption = "Create New Rndf Editor File";
			MessageBoxButtons buttons = MessageBoxButtons.YesNo;
			DialogResult result;

			// Displays the MessageBox.
			result = MessageBox.Show(message, caption, buttons);

			if (result == System.Windows.Forms.DialogResult.Yes)
			{
				// save state of the editor
				this.SaveUndoPoint();

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
				this.saveFileDialog1.Title = "Create New Rndf Editor File";

				// settings for openFileDialog
				saveFileDialog1.InitialDirectory = "Desktop\\";
				saveFileDialog1.Filter = "Rndf Editor File (*.rdt)|*.rdt|All files (*.*)|*.*";
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
								SaveEditorToFile(saveFileDialog1.FileName);

								// notify
								EditorOutput.WriteLine("Created New Rndf Editor File: " + saveFileDialog1.FileName);

								// end case
								break;
						}
					}
					catch (Exception ex)
					{
						EditorOutput.WriteLine("Error in [private void saveToolStripButton_Click(object sender, EventArgs e)]: " + ex.ToString());
					}
				}
			}
			else
			{
				EditorOutput.WriteLine("Creation of new Rndf Editor File Aborted");
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
			openFileDialog1.Filter = "Rndf Editor File (*.rdt)|*.rdt|Rndf (*.rdf)|*.rdf|Mdf (*.mdf)|*.mdf|Pose Log (*.pos)|*.pos|Arbiter Road Network (*.arn)|*.arn|Arbiter Mission Description (*.amd)|*.amd|Temporary Points File (*.txt)|*.txt|All files (*.*)|*.*";
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
							OpenEditorFromFile(openFileDialog1.FileName);

							// notify
							EditorOutput.WriteLine("Loaded Rndf Editor from File: " + openFileDialog1.FileName);

							// reset undo, redo
							this.ResetUndoRedo();

							// redraw
							this.Invalidate(true);

							// end case
							break;

						case 2:

							// create display object filter to remove old network objects
							DisplayObjectFilter dof = delegate(IDisplayObject target)
							{
								// check if target is network object
								if (target is INetworkObject)
									return true;
								else
									return false;
							};

							// remove old road network types
							this.roadDisplay1.RemoveDisplayObjectType(dof);

							// notify
							EditorOutput.WriteLine("Starting Initial Rdf Parse from File: " + openFileDialog1.FileName);

							// create the rndf network
							ArbiterRoadNetwork arnTmp = RndfTools.GenerateRndfNetwork(openFileDialog1.FileName);

							// notify
							EditorOutput.WriteLine("Parsed Base Arbiter Road Network Graph from Rdf: " + openFileDialog1.FileName);

							// get key current
							KeyStateInfo shiftKey = KeyboardInfo.GetKeyState(Keys.ShiftKey);				

							if (shiftKey.IsPressed)
							{
								EditorOutput.WriteLine("Shift key pressed on load, not generating lane polygons");
								this.arbiterRoads = arnTmp;
							}
							else
							{
								EditorOutput.WriteLine("Shift key not pressed on load, generating lane polygons");
								this.arbiterRoads = arnTmp;
								this.GenerateLanePolygonsButton_Click(sender, e);
							}						
														
							// display
							this.roadDisplay1.AddDisplayObjectRange(arbiterRoads.DisplayObjects);

							// reset undo, redo
							this.ResetUndoRedo();

							// Notify
							EditorOutput.WriteLine("Opened Rdf From File: " + openFileDialog1.FileName);

							// redraw
							this.Invalidate(true);

							// end case
							break;

						case 3:

							if (this.arbiterRoads != null)
							{
								// create the mission file
								this.missionDescription = this.OpenMissionFromFile(openFileDialog1.FileName);
								this.arbiterRoads.SetSpeedLimits(this.missionDescription.SpeedLimits);

								// Notify
								EditorOutput.WriteLine("Opened Mdf From File: " + openFileDialog1.FileName);
								EditorOutput.WriteLine("Set Road Network speeds");								
							}
							else
							{
								throw new Exception("Need to load rdf before mdf");
							}

							break;

						case 4:
							break;

						// open arbiter road network
						case 5:

							// get network
							ArbiterRoadNetwork arn = this.OpenRoadNetworkFromFile(this.openFileDialog1.FileName);

							// Notify
							EditorOutput.WriteLine("Opened Arbiter Road Network From File: " + openFileDialog1.FileName);

							// set network
							this.arbiterRoads = arn;

							// filter			
							DisplayObjectFilter filter = delegate(IDisplayObject target)
							{
								// check if target is network object
								if (target is INetworkObject)
									return true;
								else
									return false;
							};

							// remove road network
							this.roadDisplay1.RemoveDisplayObjectType(filter);

							// display
							this.roadDisplay1.AddDisplayObjectRange(arn.DisplayObjects);

							break;

						// open arbiter mission
						case 6:

							if (this.arbiterRoads != null)
							{
								// create the mission file
								this.missionDescription = this.OpenArbiterMissionFromFile(openFileDialog1.FileName);
								this.arbiterRoads.SetSpeedLimits(this.missionDescription.SpeedLimits);

								// Notify
								EditorOutput.WriteLine("Opened Arbiter Mission From File: " + openFileDialog1.FileName);
								EditorOutput.WriteLine("Set Road Network speeds");
							}
							else
							{
								throw new Exception("Need to load rdf before mdf");
							}

							break;
						// temp points
						case 7:

							if (this.arbiterRoads != null)
							{
								this.temporaryPoints.ReadArcMinutesFromFile(openFileDialog1.FileName, this.arbiterRoads.PlanarProjection);

								// filter			
								DisplayObjectFilter f = delegate(IDisplayObject target)
								{
									// check if target is network object
									if (target is TemporaryPointsDisplay)
										return true;
									else
										return false;
								};

								this.roadDisplay1.RemoveDisplayObjectType(f);
								this.roadDisplay1.AddDisplayObject(this.temporaryPoints);
								this.roadDisplay1.Invalidate();

								EditorOutput.WriteLine("Loaded temporary points (Blue Dots)");
							}
							else
								EditorOutput.WriteLine("Need to open road network before loading temporary points");

							break;
					}
				}
				catch (Exception ex)
				{
					EditorOutput.WriteLine("Error in [private void openToolStripButton_Click(object sender, EventArgs e)]: " + ex.ToString());
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
				SaveEditorToFile(currentFile);
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
				this.redoToolStripButton.Image = global::RndfEditor.Properties.Resources.Redo_16_n_p;
			}
			else
			{
				this.redoToolStripButton.Image = global::RndfEditor.Properties.Resources.Redo_16_d_p;
			}

			// check undo for items
			if (undoStack.Count > 0)
			{
				this.undoStripButton.Image = global::RndfEditor.Properties.Resources.Undo_16_n_p;
			}
			else
			{
				this.undoStripButton.Image = global::RndfEditor.Properties.Resources.Undo_16_d_p;
			}

			// redraw the editor
			this.Invalidate();
		}

		/// <summary>
		/// Creates a save point of the editor
		/// </summary>
		/// <returns></returns>
		private MemoryStream CreateEditorSavePoint()
		{
			try
			{
				// save
				EditorSave editorSave = Save();

				// serialize
				MemoryStream ms = new MemoryStream();
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, editorSave);

				// return the stream
				return ms;
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine("Error in [private MemoryStream CreateEditorSavePoint()]: " + ex.ToString());
				return null;
			}
		}

		/// <summary>
		/// Restores the editor to its previous state from a save point
		/// </summary>
		/// <returns></returns>
		private void RestoreEditorFromSavePoint(MemoryStream save)
		{
			try
			{
				// restore the saved state from the stream
				BinaryFormatter bf = new BinaryFormatter();
				save.Position = 0;
				EditorSave es = (EditorSave)bf.Deserialize(save);

				// restore the edtitor
				Restore(es);
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine("Error in [private void RestoreEditorFromSavePoint(MemoryStream save)]: " + ex.ToString());
			}
		}

		private void RestoreTool(IEditorTool iet)
		{
			if (iet != null && iet is ZoneTool)
			{
				ZoneTool zt = (ZoneTool)iet;

				if (zt.zt.current != null)
				{
					zt.zt.current = this.arbiterRoads.ArbiterZones[zt.zt.current.ZoneId];
				}
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
			MemoryStream ms = CreateEditorSavePoint();

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
		public void Undo()
		{
			// check if we can undo
			if (undoStack.Count > 0)
			{
				// get stream of state
				MemoryStream state = undoStack.Pop();

				// put on redo state
				redoStack.Push(CreateEditorSavePoint());

				// restore from undo point
				RestoreEditorFromSavePoint(state);

				// icons
				SetUndoRedoIcons();
			}
		}

		/// <summary>
		/// Redo
		/// </summary>
		public void Redo()
		{
			// check if we can redo
			if (redoStack.Count > 0)
			{
				// get stream of state
				MemoryStream state = redoStack.Pop();

				// put on redo state
				undoStack.Push(CreateEditorSavePoint());

				// restore from undo point
				RestoreEditorFromSavePoint(state);				

				// icons
				SetUndoRedoIcons();
			}
		}

		/// <summary>
		/// Restore from a saved editor save
		/// </summary>
		/// <param name="es"></param>
		private void Restore(EditorSave es)
		{
			// restore the editor
			this.gridSizeToolStripComboBox.SelectedIndex = es.GridSizeIndex;
			this.arbiterRoads = es.ArbiterRoads;
			this.missionDescription = es.Mission;

			// restore the display
			this.roadDisplay1.LoadSave(es.displaySave);
			
			// restore tool
			this.RestoreTool(this.roadDisplay1.CurrentEditorTool);
		}

		/// <summary>
		/// Save state of the editor
		/// </summary>
		/// <returns></returns>
		private EditorSave Save()
		{
			// create display save
			DisplaySave displaySave = this.roadDisplay1.Save();

			// create editor save
			EditorSave editorSave = new EditorSave();

			// set fields
			editorSave.displaySave = displaySave;
			editorSave.ArbiterRoads = this.arbiterRoads;
			editorSave.Mission = this.missionDescription;

			if (this.gridSizeToolStripComboBox.SelectedIndex >= 0)
				editorSave.GridSizeIndex = this.gridSizeToolStripComboBox.SelectedIndex;
			else
				editorSave.GridSizeIndex = 3;

			// return
			return editorSave;
		}

		#endregion

		#region File Handling

		#region Editor File Handling

		/// <summary>
		/// Save the editor to a file
		/// </summary>
		/// <param name="fileName"></param>
		public void SaveEditorToFile(string fileName)
		{
			// create file
			FileStream fs = new FileStream(fileName, FileMode.Create);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// save point
			EditorSave es = Save();

			// set path
			currentFile = fileName;

			// set title
			this.Text = "Rndf Editor - " + Path.GetFileNameWithoutExtension(fileName);

			// serialize
			bf.Serialize(fs, es);

			// release holds
			fs.Dispose();
		}

		/// <summary>
		/// Opens the editor saved state from a file
		/// </summary>
		/// <param name="fileName"></param>
		private void OpenEditorFromFile(string fileName)
		{
			// create file
			FileStream fs = new FileStream(fileName, FileMode.Open);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			EditorSave es = (EditorSave)bf.Deserialize(fs);

			// restore
			Restore(es);

			// set path
			currentFile = fileName;

			// set title
			this.Text = "Rndf Editor - " + Path.GetFileNameWithoutExtension(fileName);

			// release holds
			fs.Dispose();
		}

		#endregion
		
		#region Mdf Handling

		/// <summary>
		/// Open a road network from a file
		/// </summary>
		/// <param name="p"></param>
		private ArbiterMissionDescription OpenMissionFromFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Open);
			
			// generator
			MissionGenerator mg = new MissionGenerator();

			// mdf
			Parser.RndfParser rp = new Parser.RndfParser();
			IMdf mdf = rp.createMdf(fs);

			// release holds
			fs.Dispose();

			// create
			return mg.GenerateMission(mdf, this.arbiterRoads);
		}

		/// <summary>
		/// Open a road network from a file
		/// </summary>
		/// <param name="p"></param>
		private void SaveMissionToFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Create);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			bf.Serialize(fs, this.missionDescription);

			// release holds
			fs.Dispose();
		}

		/// <summary>
		/// reads serialized arbiter mission from file
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private ArbiterMissionDescription OpenArbiterMissionFromFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Open);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// deserialize
			ArbiterMissionDescription es = (ArbiterMissionDescription)bf.Deserialize(fs);

			// release holds
			fs.Dispose();

			// return
			return es;
		}

		#endregion

		#region Pose Log Handling

		#endregion

		#region Road Network Handling

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

			// deserialize
			ArbiterRoadNetwork es = (ArbiterRoadNetwork)bf.Deserialize(fs);

			// release holds
			fs.Dispose();

			// return
			return es;
		}

		/// <summary>
		/// Open a road network from a file
		/// </summary>
		/// <param name="p"></param>
		private void SaveRoadNetworkToFile(string p)
		{
			// create file
			FileStream fs = new FileStream(p, FileMode.Create);

			// serializer
			BinaryFormatter bf = new BinaryFormatter();

			// serialize
			bf.Serialize(fs, this.arbiterRoads);

			// release holds
			fs.Dispose();
		}

		#endregion

		#endregion

		#region Editor Tools

		/// <summary>
		/// Removes the current tool from existing
		/// </summary>
		private void removeCurrentTool()
		{
			if (this.roadDisplay1.CurrentEditorTool is WaypointAdjustmentTool)
			{
				this.WaypointAdjustmentToolStripButton.CheckState = CheckState.Unchecked;

				// remove the current tool
				this.roadDisplay1.CurrentEditorTool = null;
			}
			else if (this.roadDisplay1.CurrentEditorTool is ZoneTool)
			{
				ZoneTool zt = (ZoneTool)this.roadDisplay1.CurrentEditorTool;
				zt.PreviousNode = null;
			}
		}

		/// <summary>
		/// Point analysis tool
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PointAnalysisToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.PointAnalysisToolStripButton.Checked)
			{
				if (this.arbiterRoads != null)
				{
					this.roadDisplay1.SecondaryEditorTool = new PointAnalysisTool(this.arbiterRoads.PlanarProjection,
						this.PointAnalysisSnapToWaypoints.CheckState == CheckState.Checked, this.arbiterRoads, this.roadDisplay1.WorldTransform);
					this.roadDisplay1.Cursor = Cursors.Cross;
				}
			}
			else
			{
				this.roadDisplay1.Cursor = Cursors.Default;

				if (this.roadDisplay1.SecondaryEditorTool is PointAnalysisTool)
				{
					this.roadDisplay1.SecondaryEditorTool = null;
					this.roadDisplay1.Invalidate();
				}
			}
		}

		/// <summary>
		/// Ruler tool
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RulerToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.RulerToolStripButton.Checked)
			{
				this.roadDisplay1.CurrentEditorTool = new RulerTool(
					this.RulerSnapToWaypointsEditorTool.CheckState == CheckState.Checked, this.arbiterRoads, this.roadDisplay1.WorldTransform);
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool is RulerTool)
				{
					this.roadDisplay1.CurrentEditorTool = null;

					this.roadDisplay1.Invalidate();
				}

				if (this.roadDisplay1.SecondaryEditorTool != null && this.roadDisplay1.SecondaryEditorTool is PointAnalysisTool)
				{
					((PointAnalysisTool)this.roadDisplay1.SecondaryEditorTool).Save = null;
				}
			}
		}

		/// <summary>
		/// Measures an angle defines by the user
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AngleMeasureToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.AngleMeasureToolStripButton.CheckState == CheckState.Checked)
			{
				this.roadDisplay1.CurrentEditorTool = new AngleMeasureTool();
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool is AngleMeasureTool)
				{
					((AngleMeasureTool)this.roadDisplay1.CurrentEditorTool).Reset(this.roadDisplay1.SecondaryEditorTool);
					this.roadDisplay1.CurrentEditorTool = null;
					this.roadDisplay1.Invalidate();
				}
			}
		}

		/// <summary>
		/// Adjusts a waypoints location
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WaypointAdjustmentToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.WaypointAdjustmentToolStripButton.CheckState == CheckState.Checked)
			{
				this.roadDisplay1.CurrentEditorTool = new WaypointAdjustmentTool(this.roadDisplay1.WorldTransform);
				this.SaveUndoPoint();
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool is WaypointAdjustmentTool)
				{
					WaypointAdjustmentTool wat = (WaypointAdjustmentTool)this.roadDisplay1.CurrentEditorTool;

					if (wat.CheckInMove)
						wat.CancelMove();

					this.roadDisplay1.CurrentEditorTool = null;
				}
			}
		}

		/// <summary>
		/// Pulls out any complex intersection
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void IntersectionPulloutToolStripButton_Click(object sender, EventArgs e)
		{
			if (this.IntersectionPulloutToolStripButton.CheckState == CheckState.Checked)
			{
				this.SaveUndoPoint();
				this.roadDisplay1.CurrentEditorTool = new IntersectionPulloutTool(arbiterRoads, this.roadDisplay1, this, true);
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool is IntersectionPulloutTool)
				{
					((IntersectionPulloutTool)this.roadDisplay1.CurrentEditorTool).ShutDown();
					this.roadDisplay1.CurrentEditorTool = null;
					this.roadDisplay1.Invalidate();
				}
			}
		}

		/// <summary>
		/// Shifts the network by a bias
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void shiftNetworkByBias_Click(object sender, EventArgs e)
		{
			if (this.arbiterRoads != null)
			{
				// Initializes the variables to pass to the MessageBox.Show method.
				string message = "Shifting the network must be done prior to any \nother modification to avoid critical errors in the network. \nAre you sure you wish to continue?";
				string caption = "Shift Network";
				MessageBoxButtons buttons = MessageBoxButtons.YesNo;
				DialogResult result;

				// Displays the MessageBox.
				result = MessageBox.Show(message, caption, buttons);

				if (result == System.Windows.Forms.DialogResult.Yes)
				{
					ShiftNetwork sn = new ShiftNetwork(this.arbiterRoads, this.roadDisplay1);
					sn.Show();
				}
			}
			else
			{
				EditorOutput.WriteLine("Need to load road network before shift");
			}
		}

		/// <summary>
		/// Tool generates recommended speed for all interconnects
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GenerateInterconnectSpeedsTool_Click(object sender, EventArgs e)
		{
			if (this.arbiterRoads != null && this.missionDescription != null)
			{
				foreach (ArbiterInterconnect ai in this.arbiterRoads.ArbiterInterconnects.Values)
				{
					double speed = RndfTools.MaximumInterconnectSpeed(ai);
					ai.MaximumDefaultSpeed = speed;
					Console.WriteLine(ai.ToString() + ": " + speed.ToString("F6")); 
				}

				// notify
				EditorOutput.WriteLine("Set Interconnect Default Speeds");
			}
			else
			{
				// notify
				EditorOutput.WriteLine("Arbiter Road Network and Mdf need to be set");
			}
		}

		/// <summary>
		/// Opens the mission toolbox
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void missionToolboxButton_Click(object sender, EventArgs e)
		{
			if (this.arbiterRoads != null)
			{
				DrawingUtility.DisplayArbiterWaypointCheckpointId = true;
				DrawingUtility.DisplayArbiterWaypointId = false;
				DrawingUtility.DrawArbiterWaypoint = true;
				MissionToolbox mt = new MissionToolbox(this.arbiterRoads, this.missionDescription, this.roadDisplay1.WorldTransform.CenterPoint);
				mt.Show();
			}
			else
			{
				EditorOutput.WriteLine("Road network needs to be set before mission toolbox can be initialized");
			}
		}

		/// <summary>
		/// Opens the zone toolbox
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ZoneToolboxButton_Click(object sender, EventArgs e)
		{
			if (this.ZoneToolboxButton.CheckState == CheckState.Checked)
			{
				this.ZoneMapCheckbox.CheckState = CheckState.Checked;
				DrawingUtility.DrawArbiterZoneMap = true;
				this.SaveUndoPoint();
				this.roadDisplay1.CurrentEditorTool = new ZoneTool(arbiterRoads, this.roadDisplay1, this);
				this.lanePathCheckBox.CheckState = CheckState.Checked;
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool is ZoneTool)				
				{
					this.ZoneMapCheckbox.CheckState = CheckState.Unchecked;
					((ZoneTool)this.roadDisplay1.CurrentEditorTool).ShutDown();
					this.roadDisplay1.CurrentEditorTool = null;
					this.roadDisplay1.Invalidate();
					DrawingUtility.DrawArbiterZoneMap = false;
					this.lanePathCheckBox.CheckState = CheckState.Unchecked;
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
		/// Whether to drtaw temp point id's
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DrawTemporaryPointsIdCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (DrawTemporaryPointsIdCheckBox.CheckState == CheckState.Checked)
				DrawingUtility.DrawRndfEditorTemporaryPointId = true;
			else
				DrawingUtility.DrawRndfEditorTemporaryPointId = false;

			// redraw
			this.roadDisplay1.Invalidate();
		}

		#endregion							

		#region Other

		/// <summary>
		/// Whether to view temp points
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void viewTemporaryPoints_CheckedChanged(object sender, EventArgs e)
		{
			if (viewTemporaryPoints.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawRndfEditorTemporaryPoint = true;
			}
			else
				DrawingUtility.DrawRndfEditorTemporaryPoint = false;

			// redraw
			this.roadDisplay1.Invalidate();
		}

		private void ZoneMapCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.ZoneMapCheckbox.CheckState == CheckState.Checked)
				DrawingUtility.DrawArbiterZoneMap = true;
			else
				DrawingUtility.DrawArbiterZoneMap = false;

			this.roadDisplay1.Invalidate();
		}

		#endregion

		#endregion

		#region Other Options

		/// <summary>
		/// Whether the ruler should snap to a waypoint
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RulerSnapToWaypointsEditorTool_CheckedChanged(object sender, EventArgs e)
		{
			if (this.RulerSnapToWaypointsEditorTool.CheckState == CheckState.Checked)
			{
				if (this.roadDisplay1.CurrentEditorTool is RulerTool)
				{
					RulerTool rt = (RulerTool)this.roadDisplay1.CurrentEditorTool;
					rt.snapToWaypoints = true;
					rt.roadNetwork = this.arbiterRoads;
					this.roadDisplay1.Invalidate();
				}
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool is RulerTool)
				{
					RulerTool rt = (RulerTool)this.roadDisplay1.CurrentEditorTool;
					rt.snapToWaypoints = false;					
					this.roadDisplay1.Invalidate();
				}
			}
		}

		/// <summary>
		/// Whether the point analysis tool should snap to a waypoint
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PointAnalysisSnapToWaypoints_CheckedChanged(object sender, EventArgs e)
		{
			if (this.PointAnalysisSnapToWaypoints.CheckState == CheckState.Checked)
			{
				if (this.roadDisplay1.SecondaryEditorTool is PointAnalysisTool)
				{
					PointAnalysisTool pat = (PointAnalysisTool)this.roadDisplay1.SecondaryEditorTool;
					pat.snapToWaypoints = true;
					pat.roadNetwork = this.arbiterRoads;
					this.roadDisplay1.Invalidate();
				}
			}
			else
			{
				if (this.roadDisplay1.SecondaryEditorTool is PointAnalysisTool)
				{
					PointAnalysisTool pat = (PointAnalysisTool)this.roadDisplay1.SecondaryEditorTool;
					pat.snapToWaypoints = false;
					this.roadDisplay1.Invalidate();
				}
			}
		}

		#endregion

		private void checkBox7_CheckedChanged(object sender, EventArgs e)
		{
			if (this.lanePathCheckBox.CheckState == CheckState.Checked)
				DrawingUtility.DisplayArbiterLanePath = true;
			else
				DrawingUtility.DisplayArbiterLanePath = false;

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


		private bool errors = false;
		private void GenerateLanePolygonsButton_Click(object sender, EventArgs e)
		{
			if (this.arbiterRoads != null)
			{
				EditorOutput.WriteLine("Generating Lane Polygons");
				errors = false;
				EditorOutput.WriteLine("Saving restore point");
				this.SaveUndoPoint();

				ArbiterSegment[] asgs = new ArbiterSegment[this.arbiterRoads.ArbiterSegments.Count];
				this.arbiterRoads.ArbiterSegments.Values.CopyTo(asgs, 0);

				for(int i = 0; i < asgs.Length; i++)
				{
					ArbiterSegment asg = asgs[i];
					double percent = ((double)i)/((double)asgs.Length) * 100.0;
					EditorOutput.WriteLine(percent.ToString("F1") + "% Complete, on Segment: " + asg.ToString());
					foreach(ArbiterLane al in asg.Lanes.Values)
					{
						try
						{
							this.evaluationLane = null;
							this.evaluationLane = al;

							// create evaluation thread
							Thread lanePolygonThread = new Thread(GenerateLanePolygonThread);
							lanePolygonThread.IsBackground = true;
							lanePolygonThread.Priority = ThreadPriority.AboveNormal;

							// polygon timer, start thread
							Stopwatch timer = new Stopwatch();
							timer.Start();
							lanePolygonThread.Start();

							// check thread
							while (lanePolygonThread.IsAlive)
							{
								// check timing
								if (timer.ElapsedMilliseconds > 300000)
								{
									// kill thread
									try
									{
										EditorOutput.WriteLine("Lane polygon generation for lane: " + al.ToString() + " took longer than 5 minutes, aborting setting to default");
										errors = true;
										lanePolygonThread.Abort();										
									}
									catch (Exception) { }

									// set default poly
									al.LanePolygon = LaneTools.DefaultLanePolygon(al);

									// get out of the loop
									break;
								}

								// chilax
								Thread.Sleep(1000);
							}

							// check lane polygon area
							double area = al.LanePolygon.GetArea();
							if (al.LanePolygon.GetArea() > 300000)
							{
								EditorOutput.WriteLine("Lane polygon generation for lane: " + al.ToString() + " produced a polygon with area: " + area.ToString("f2") + " > 300000, setting to default");
								// set default poly
								al.LanePolygon = LaneTools.DefaultLanePolygon(al);
							}
						}
						catch (Exception ex)
						{
							EditorOutput.WriteLine("Error in editor upper level lane poly generation: " + ex.ToString());
							errors = true;

							try
							{
								al.LanePolygon = LaneTools.DefaultLanePolygon(al);
							}
							catch (Exception ex2)
							{
								EditorOutput.WriteLine(ex2.ToString());
								EditorOutput.WriteLine("Error in default lane polygon generation: " + al.ToString());
							}
						}

						this.evaluationLane = null;
					}	
				}

				if(!errors)
					EditorOutput.WriteLine("Lane polygon generation successful");
				else
					EditorOutput.WriteLine("Lane polygon generation completed with errors");
			}
			else
			{
				EditorOutput.WriteLine("Generating lane polygons unsuccessful as road network null");
			}
		}

		private ArbiterLane evaluationLane = null;
		private void GenerateLanePolygonThread()
		{
			if (evaluationLane != null)
			{
				ArbiterLane al = evaluationLane;

				try
				{
					al.LanePolygon = LaneTools.LanePolygon(al);

					if (al.LanePolygon.IsComplex)
					{
						throw new Exception("Lane Polygon Complex, lane: " + al.ToString());
					}
				}
				catch (Exception ex)
				{
					errors = true;
					EditorOutput.WriteLine(ex.ToString());
					EditorOutput.WriteLine("Error in lane polygon generation, segment: " + al.Way.Segment.ToString() + ", lane: " + al.ToString() + " set to default polygon");

					try
					{
						al.LanePolygon = LaneTools.DefaultLanePolygon(al);
					}
					catch (Exception ex2)
					{
						EditorOutput.WriteLine(ex2.ToString());
						EditorOutput.WriteLine("Error in default lane polygon generation");
					}
				}
			}
			else
			{
				EditorOutput.WriteLine("Error in lane polygon generation: evaluation lane does not exist");
			}
		}

		private void interconnectTurnDirectionRedo_Click(object sender, EventArgs e)
		{
			if (this.arbiterRoads != null)
			{
				InterconnectGeneration ig = new InterconnectGeneration(null);
				foreach (ArbiterInterconnect ai in this.arbiterRoads.ArbiterInterconnects.Values)
				{
					ig.SetTurnDirection(ai);
				}
				EditorOutput.WriteLine("reset all interconnect turn directions");
			}
			else
			{
				EditorOutput.WriteLine("road network cannot be null");
			}
		}

		private void partitionToolButton_Click(object sender, EventArgs e)
		{
			if (partitionToolButton.CheckState == CheckState.Checked)
			{
				this.SaveUndoPoint();
				this.roadDisplay1.CurrentEditorTool = new PartitionTools();
			}
			else if (this.roadDisplay1.CurrentEditorTool != null && this.roadDisplay1.CurrentEditorTool is PartitionTools)
			{
				this.roadDisplay1.CurrentEditorTool = null;
			}
		}

		private void displayUserWaypointsCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (this.displayUserWaypointsCheckBox.CheckState == CheckState.Checked)
			{
				DrawingUtility.DrawArbiterUserWaypoint = true;
			}
			else
			{
				DrawingUtility.DrawArbiterUserWaypoint = false;
			}

			this.roadDisplay1.Invalidate();
		}

		private void removeUserWaypointsButton_Click(object sender, EventArgs e)
		{
			if (removeUserWaypointsButton.CheckState == CheckState.Checked)
			{
				this.SaveUndoPoint();
				if (this.roadDisplay1.CurrentEditorTool is PartitionTools)
					((PartitionTools)this.roadDisplay1.CurrentEditorTool).RemoveUserWaypointMode = true;
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool is PartitionTools)
					((PartitionTools)this.roadDisplay1.CurrentEditorTool).RemoveUserWaypointMode = true;
			}
		}

		private void SparseToolboxButton_Click(object sender, EventArgs e)
		{
			if (this.SparseToolboxButton.CheckState == CheckState.Checked)
			{
				this.SaveUndoPoint();
				SparsePartitionToolbox spt = new SparsePartitionToolbox(this, this.roadDisplay1);
				this.roadDisplay1.CurrentEditorTool = new SparseTool(spt);
				spt.Show();
			}
			else
			{
				if (this.roadDisplay1.CurrentEditorTool != null &&
					this.roadDisplay1.CurrentEditorTool is SparseTool)
					((SparseTool)this.roadDisplay1.CurrentEditorTool).ShutDown();
			}
		}
	}
}