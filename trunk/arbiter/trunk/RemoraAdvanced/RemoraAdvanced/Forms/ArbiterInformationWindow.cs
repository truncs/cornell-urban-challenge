using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using RemoraAdvanced.Common;
using UrbanChallenge.Common.Vehicle;

namespace RemoraAdvanced.Forms
{
	/// <summary>
	/// Displays ai information
	/// </summary>
  public partial class ArbiterInformationWindow : Form
  {
		/// <summary>
		/// Constructor
		/// </summary>
    public ArbiterInformationWindow()
    {
			InitializeComponent();
    }

		/// <summary>
		/// Updates visible information
		/// </summary>
		public void UpdateInformation()
		{
			if (RemoraCommon.aiInformation != null)
			{
				// get ai info
				ArbiterInformation ai = RemoraCommon.aiInformation.information;

				if (ai != null)
				{
					this.state.Text = ai.CurrentState;
					this.stateInfo.Text = ai.CurrentStateInfo;
					this.blockageDisplayLabel.Text = ai.Blockage;

					this.fqmWaypoint.Text = ai.FQMWaypoint;
					this.fqmSpeed.Text = ai.FQMSpeed;
					this.fqmDistance.Text = ai.FQMDistance;
					this.fqmStopType.Text = ai.FQMStopType;
					this.fqmNextState.Text = ai.FQMState;
					this.fqmNextStateInfo.Text = ai.FQMStateInfo;
					this.fqmBehavior.Text = ai.FQMBehavior;
					this.fqmBehaviorInfo.Text = ai.FQMBehaviorInfo;
					this.fqmSpeedCommand.Text = ai.FQMSpeedCommand;
					this.segmentSpeeds.Text = ai.FQMSegmentSpeedLimit;

					this.nextState.Text = ai.NextState;
					this.nextStateInfo.Text = ai.NextStateInfo;
					this.behavior.Text = ai.NextBehavior;
					this.behaviorInfo.Text = ai.NextBehaviorInfo;
					this.speedCommand.Text = ai.NextSpeedCommand;
					this.turnSignalsLabel.Text = ai.NextBehaviorTurnSignals;
					this.timestampLabel.Text = ai.NextBehaviorTimestamp;

					this.route1Waypoint.Text = ai.Route1Wp;
					this.route1Time.Text = ai.Route1Time;
					this.route2Waypoint.Text = ai.Route2Wp;
					this.route2Time.Text = ai.Route2Time;
					this.checkpoint.Text = ai.RouteCheckpoint;
					this.goalsRemainingText.Text = ai.GoalsRemaining;
					this.checkpointId.Text = ai.RouteCheckpointId;

					this.fvSpeed.Text = ai.FVTSpeed;
					this.fvSpeedCommand.Text = ai.FVTSpeedCommand;
					this.fvNextStateInfo.Text = ai.FVTStateInfo;
					this.fvNextState.Text = ai.FVTState;
					this.fvNextBehavior.Text = ai.FVTBehavior;
					this.fvDistance.Text = ai.FVTDistance;
					this.FVTxSeparationLabel.Text = ai.FVTXSeparation;

					this.FVTIgnoredLabel.Text = "";
					if (ai.FVTIgnorable != null && ai.FVTIgnorable.Length > 0)
					{
						for (int i = 0; i < ai.FVTIgnorable.Length; i++)
						{
							this.FVTIgnoredLabel.Text += ai.FVTIgnorable[i];
							this.FVTIgnoredLabel.Text += i == ai.FVTIgnorable.Length - 1 ? "" : ", ";
						}
					}

					VehicleState vs = RemoraCommon.Communicator.GetVehicleState();
					if (vs != null)
					{
						this.posteriorPoseEast.Text = vs.Position.X.ToString("F3");
						this.posteriorPoseNorth.Text = vs.Position.Y.ToString("F3");
					}

					if (RemoraCommon.Communicator.GetVehicleSpeed().HasValue)
						this.posteriorPoseSpeed.Text = RemoraCommon.Communicator.GetVehicleSpeed().Value.ToString("F3");

					this.CarModeLabel.Text = RemoraCommon.Communicator.GetCarMode().ToString();

					this.Invalidate();
				}
			}
		}
	}
}