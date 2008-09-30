using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.ArbiterRoads;
using RndfEditor.Tools;
using RndfEditor.Forms;

namespace RndfEditor
{
	public enum InterToolboxMode
	{
		None,
		SafetyZone,
		Box,
		Helpers
	}

	public partial class IntersectionToolbox : Form
	{
		private ArbiterRoadNetwork arn;
		private RoadDisplay rd;
		private Editor ed;

		public IntersectionToolbox(ArbiterRoadNetwork arn, RoadDisplay rd, Editor ed)
		{
			this.arn = arn;
			this.rd = rd;
			this.ed = ed;
			InitializeComponent();
		}

		/// <summary>
		/// the intersection toolbox mode
		/// </summary>
		public InterToolboxMode Mode
		{
			get
			{
				if (this.setSafetyZonesIntersectionToolkitButton.CheckState == CheckState.Checked)
					return InterToolboxMode.SafetyZone;
				else if (this.boxIntersectionToolkitButton.CheckState == CheckState.Checked)
					return InterToolboxMode.Box;
				else if (this.AddIntersectionWrapHelperPoint.CheckState == CheckState.Checked)
					return InterToolboxMode.Helpers;
				else
					return InterToolboxMode.None;
			}
		}

		public void ResetIcons()
		{
			this.setSafetyZonesIntersectionToolkitButton.CheckState = CheckState.Unchecked;
			this.boxIntersectionToolkitButton.CheckState = CheckState.Unchecked;
			this.AddIntersectionWrapHelperPoint.CheckState = CheckState.Unchecked;
			this.Invalidate();
		}

		/// <summary>
		/// sets safety zones
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void setSafetyZonesIntersectionToolkitButton_Click(object sender, EventArgs e)
		{
			if (this.setSafetyZonesIntersectionToolkitButton.CheckState == CheckState.Checked)
			{
				this.boxIntersectionToolkitButton.CheckState = CheckState.Unchecked;
				this.AddIntersectionWrapHelperPoint.CheckState = CheckState.Unchecked;
				this.Invalidate();
			}
		}

		/// <summary>
		/// boxes the intersection
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void boxIntersectionToolkitButton_Click(object sender, EventArgs e)
		{
			if (this.boxIntersectionToolkitButton.CheckState == CheckState.Checked)
			{
				this.setSafetyZonesIntersectionToolkitButton.CheckState = CheckState.Unchecked;				
				this.AddIntersectionWrapHelperPoint.CheckState = CheckState.Unchecked;
				this.Invalidate();
			}
		}

		/// <summary>
		/// adds intersection helper points
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddIntersectionWrapHelperPoint_Click(object sender, EventArgs e)
		{
			if (this.AddIntersectionWrapHelperPoint.CheckState == CheckState.Checked)
			{
				this.setSafetyZonesIntersectionToolkitButton.CheckState = CheckState.Unchecked;
				this.boxIntersectionToolkitButton.CheckState = CheckState.Unchecked;				
				this.Invalidate();
			}
		}

		private void IntersectionReParseAllButton_Click(object sender, EventArgs e)
		{
			// tool
			IntersectionPulloutTool ipt = new IntersectionPulloutTool(this.arn, rd, ed, false);
			
			// save old
			ArbiterIntersection[] ais = new ArbiterIntersection[this.arn.ArbiterIntersections.Count];
			this.arn.ArbiterIntersections.Values.CopyTo(ais, 0);

			// remove all old
			/*for(int i =0; i < ais.Length; i++)
			{
				ArbiterIntersection ai = ais[i];
				
				this.rd.displayObjects.Remove(ai);
				ai.RoadNetwork.DisplayObjects.Remove(ai);
				ai.RoadNetwork.ArbiterIntersections.Remove(ai.IntersectionId);

				foreach (ITraversableWaypoint aw in ai.AllExits.Values)
					ai.RoadNetwork.IntersectionLookup.Remove(aw.AreaSubtypeWaypointId);
			}*/

			// refinalize
			for (int j = 0; j < ais.Length; j++)
			{
				ArbiterIntersection ai = ais[j];
				ipt.FinalizeIntersection(ai.IntersectionPolygon.Inflate(3.0), ai);
			}
		}
	}
}