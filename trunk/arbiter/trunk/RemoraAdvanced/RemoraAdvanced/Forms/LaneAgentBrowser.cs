using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RemoraAdvanced.Common;
using RemoraAdvanced.Display.DisplayObjects;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;

namespace RemoraAdvanced.Forms
{
	public partial class LaneAgentBrowser : Form
	{
		public LaneAgentBrowser()
		{
			InitializeComponent();
		}

		public void UpdateInformation()
		{
			// generate the new items
			ArbiterInformationDisplay aid = RemoraCommon.aiInformation;

			if (aid != null && aid.information != null)
			{
				// check items
				if (aid.information.LAConsistent != "" || aid.information.LAInitial != "" || aid.information.LAPosteriorProbInitial != "" ||
					aid.information.LAPosteriorProbTarget != "" || aid.information.LAProbabilityCorrect != "" || aid.information.LASceneLikelyLane != "" ||
					aid.information.LATarget != "")
				{
					// clear out the list view
					this.laneAgentData.Items.Clear();

					ArbiterInformation ai = aid.information;
					ListViewItem lvf = new ListViewItem(new string[] { "Internal State", "Ai Filtered", ai.LAProbabilityCorrect });
					ListViewItem lvi = new ListViewItem(new string[] { ai.LAInitial, "Pose Initial", ai.LAPosteriorProbInitial });
					ListViewItem lvt = new ListViewItem(new string[] { ai.LATarget, "Pose Target", ai.LAPosteriorProbTarget });
					ListViewItem lvc = new ListViewItem(new string[] { "", "Pose Consistency", ai.LAConsistent });
					ListViewItem lvs = new ListViewItem(new string[] { ai.LASceneLikelyLane, "Pose Likely", "" });
					this.laneAgentData.Items.Add(lvf);
					this.laneAgentData.Items.Add(lvi);
					this.laneAgentData.Items.Add(lvt);
					this.laneAgentData.Items.Add(lvc);
					this.laneAgentData.Items.Add(lvs);
				}
				else
				{
					if (this.laneAgentData.Items.Count > 0)
						this.laneAgentData.Items.Clear();
				}
			}
		}
	}
}