using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using RndfEditor.Common;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common.Path;

namespace RndfEditor.Forms
{
	public partial class ShiftNetwork : Form
	{
		private ArbiterRoadNetwork roads;
		private RoadDisplay display;

		public ShiftNetwork(ArbiterRoadNetwork roads, RoadDisplay roadDisplay)
		{
			InitializeComponent();

			this.roads = roads;
			this.display = roadDisplay;
		}

		private void ShiftNetworkCancelButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ShiftNetworkOkButton_Click(object sender, EventArgs e)
		{
			try
			{
				double east = double.Parse(this.ShiftNetworkEastTextBox.Text);
				double north = double.Parse(this.ShiftNetworkNorthTextBox.Text);
				Coordinates shift = new Coordinates(east, north);

				foreach (IArbiterWaypoint iaw in roads.ArbiterWaypoints.Values)
				{
					iaw.Position = iaw.Position + shift;
				}

				// safety zone filter		
				DisplayObjectFilter f = delegate(IDisplayObject target)
				{
					// check if target is network object
					if (target is ArbiterSafetyZone)
						return true;
					else
						return false;
				};

				// remove safety zones
				display.RemoveDisplayObjectType(f);

				// new safety
				roads.ArbiterSafetyZones = new List<ArbiterSafetyZone>();

				// remove from network
				List<IDisplayObject> displayObjects = new List<IDisplayObject>();
				foreach (IDisplayObject ido in roads.DisplayObjects)
				{
					if (!f(ido))
						displayObjects.Add(ido);
				}

				// remove lane safety zones, create new partition paths
				foreach (ArbiterSegment asg in roads.ArbiterSegments.Values)
				{
					foreach (ArbiterLane al in asg.Lanes.Values)
					{
						al.SafetyZones = new List<ArbiterSafetyZone>();

						// path segments of lane
						List<IPathSegment> pathSegments = new List<IPathSegment>();

						// loop 
						foreach (ArbiterLanePartition alPar in al.Partitions)
						{
							// make new segment
							pathSegments.Add(new LinePathSegment(alPar.Initial.Position, alPar.Final.Position));
						}

						// generate lane partition path
						Path partitionPath = new Path(pathSegments);
						al.PartitionPath = partitionPath;
					}
				}

				// recreate safety zones
				foreach (IArbiterWaypoint iaw in roads.ArbiterWaypoints.Values)
				{
					if (iaw is ArbiterWaypoint)
					{
						ArbiterWaypoint aw = (ArbiterWaypoint)iaw;

						if (aw.IsStop)
						{
							ArbiterLane al = aw.Lane;

							LinePath.PointOnPath end = al.GetClosestPoint(aw.Position);
							double dist = -30;
							LinePath.PointOnPath begin = al.LanePath().AdvancePoint(end, ref dist);
							if (dist != 0)
							{
								EditorOutput.WriteLine(aw.ToString() + " safety zone too close to start of lane, setting start to start of lane");
								begin = al.LanePath().StartPoint;
							}
							ArbiterSafetyZone asz = new ArbiterSafetyZone(al, end, begin);
							asz.isExit = true;
							asz.Exit = aw;
							al.SafetyZones.Add(asz);
							roads.DisplayObjects.Add(asz);
							roads.ArbiterSafetyZones.Add(asz);
							display.AddDisplayObject(asz);
						}
					}
				}

				// redraw
				this.display.Invalidate();

				// notify
				EditorOutput.WriteLine("Shifted road network: east: " + east.ToString("F6") + ", north: " + north.ToString("F6"));
				EditorOutput.WriteLine("Recomputed position-dependent types");

				// close form
				this.Close();
			}
			catch (Exception ex)
			{
				EditorOutput.WriteLine("Shift of road network failed: \n" + ex.ToString());
			}
		}
	}
}