using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RndfEditor.Tools;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Shapes;

namespace RndfEditor.Forms
{
	public partial class ZoneToolbox : Form
	{
		private ZoneToolboxMode mode;
		private ZoneTool parentTool;
		public ArbiterZone current;

		public ZoneToolbox(ZoneTool tool)
		{
			InitializeComponent();
			this.parentTool = tool;
			this.Mode = ZoneToolboxMode.None;
		}

		private void ZoneToolbox_Load(object sender, EventArgs e)
		{
			this.current = null;
		}

		private void ZoneToolboxSelectZoneButton_Click(object sender, EventArgs e)
		{
			if (this.ZoneToolboxSelectZoneButton.CheckState == CheckState.Checked)
				this.Mode = ZoneToolboxMode.Selection;
			else
			{
				this.Mode = ZoneToolboxMode.None;
				this.parentTool.Reset(true);
			}
		}

		private void ZoneToolboxStayOutPolygonButton_Click(object sender, EventArgs e)
		{
			if (this.ZoneToolboxStayOutPolygonButton.CheckState == CheckState.Checked)
			{
				this.Mode = ZoneToolboxMode.StayOut;
				this.parentTool.WrappingHelpers = new List<Coordinates>();
			}
			else
			{
				this.Mode = ZoneToolboxMode.None;
				this.parentTool.WrappingHelpers = new List<Coordinates>();
				this.parentTool.Reset(true);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			this.parentTool.ed.ZoneToolboxButton.CheckState = CheckState.Unchecked;
			base.OnClosing(e);
		}

		private void ZoneToolboxCreateNodesButton_Click(object sender, EventArgs e)
		{
			if (this.ZoneToolboxCreateNodesButton.CheckState == CheckState.Checked)
			{
				this.Mode = ZoneToolboxMode.NavNodes;
				this.parentTool.PreviousNode = null;
			}
			else
			{
				this.Mode = ZoneToolboxMode.None;
				this.parentTool.Reset(true);
			}
		}

		private ArbiterZoneNavigableNode[][] NodeMatrix(ArbiterZone az)
		{
			// get bL and tR
			Coordinates bl;
			Coordinates tr;
			this.GetBoundingPoints(az, out bl, out tr);
			int n = (int)tr.Y;
			int s = (int)bl.Y;
			int e = (int)tr.X;
			int w = (int)bl.X;

			// create matrix
			ArbiterZoneNavigableNode[][] nodeMatrix = new ArbiterZoneNavigableNode[e - w + 1][];

			// loop through coordinates
			for (int i = w; i <= e; i++)
			{
				for (int j = s; j <= n; j++)
				{
					// position
					Coordinates c = new Coordinates((double)i, (double)j);

					// check inside perimeter
					if (az.Perimeter.PerimeterPolygon.IsInside(c))
					{
						// check interacts
						bool clear = true;
						foreach (Polygon o in az.StayOutAreas)
						{
							if (o.IsInside(c))
								clear = false;
						}

						// not inside out of polys
						if (clear)
						{
							nodeMatrix[i - w][j - s] = new ArbiterZoneNavigableNode(c);
						}
					}
				}
			}

			// return
			return nodeMatrix;
		}

		private void GetBoundingPoints(ArbiterZone az, out Coordinates bl, out Coordinates tr)
		{
			// get bL and tR
			double n = Double.MinValue;
			double s = Double.MaxValue;
			double e = Double.MinValue;
			double w = Double.MaxValue;

			foreach (ArbiterPerimeterWaypoint apw in this.current.Perimeter.PerimeterPoints.Values)
			{
				if (apw.Position.X > e)
					e = apw.Position.X;

				if (apw.Position.X < w)
					w = apw.Position.X;

				if (apw.Position.Y > n)
					n = apw.Position.Y;

				if (apw.Position.Y < s)
					s = apw.Position.Y;
			}

			bl = new Coordinates(w, s);
			tr = new Coordinates(n, e);
		}

		private void ZoneToolboxResetZoneButton_Click(object sender, EventArgs e)
		{
			this.Mode = ZoneToolboxMode.None;

			if (this.current != null)
			{
				this.parentTool.ed.SaveUndoPoint();

				this.current.StayOutAreas = new List<UrbanChallenge.Common.Shapes.Polygon>();
				this.current.NavigationNodes = new List<INavigableNode>();

				foreach (NavigableEdge ne in this.current.NavigableEdges)
					ne.Start.OutgoingConnections.Remove(ne);
				this.current.NavigableEdges = new List<NavigableEdge>();

				this.parentTool.Reset(true);
				this.parentTool.rd.Invalidate();
			}
		}

		public void SelectZone(Coordinates c)
		{
			this.current = null;

			if (this.parentTool.arn != null)
			{
				foreach(ArbiterZone az in this.parentTool.arn.ArbiterZones.Values)
				{
					if (az.Perimeter.PerimeterPolygon.IsInside(c))
					{
						this.current = az;
						this.ZoneToolboxSelectedZoneTextBox.Text = az.ToString();
						this.Invalidate();
					}
				}
			}

			if (this.current == null)
			{
				this.ZoneToolboxSelectedZoneTextBox.Text = "None";
			}
		}

		public ZoneToolboxMode Mode
		{
			get { return mode; }
			set 
			{
				if (value == ZoneToolboxMode.Selection)
				{
					this.ZoneToolboxSelectZoneButton.CheckState = CheckState.Checked;
					this.ZoneToolboxStayOutPolygonButton.CheckState = CheckState.Unchecked;
					this.ZoneToolboxCreateNodesButton.CheckState = CheckState.Unchecked;
				}
				else if (value == ZoneToolboxMode.StayOut)
				{
					this.ZoneToolboxSelectZoneButton.CheckState = CheckState.Unchecked;
					this.ZoneToolboxStayOutPolygonButton.CheckState = CheckState.Checked;
					this.ZoneToolboxCreateNodesButton.CheckState = CheckState.Unchecked;
				}
				else if (value == ZoneToolboxMode.NavNodes)
				{
					this.ZoneToolboxCreateNodesButton.CheckState = CheckState.Checked;
					this.ZoneToolboxSelectZoneButton.CheckState = CheckState.Unchecked;
					this.ZoneToolboxStayOutPolygonButton.CheckState = CheckState.Unchecked;
				}
				else
				{
					this.ZoneToolboxCreateNodesButton.CheckState = CheckState.Unchecked;
					this.ZoneToolboxSelectZoneButton.CheckState = CheckState.Unchecked;
					this.ZoneToolboxStayOutPolygonButton.CheckState = CheckState.Unchecked;
				}

				this.mode = value;
				this.Invalidate();
			}
		}
	}

	public enum ZoneToolboxMode
	{
		None,
		Selection,
		NavNodes,
		StayOut
	}
}