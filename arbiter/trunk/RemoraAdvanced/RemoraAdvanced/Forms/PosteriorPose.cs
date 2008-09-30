using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RemoraAdvanced.Display.DisplayObjects;

namespace RemoraAdvanced.Forms
{
	public partial class PosteriorPose : Form
	{
		private AiVehicle vehicle;

		public PosteriorPose(AiVehicle vehicle)
		{
			InitializeComponent();

			this.vehicle = vehicle;
		}

		public void UpdatePose(double speed)
		{
			if (this.vehicle != null && this.vehicle.State != null)
			{
				this.xPosition.Text = this.vehicle.State.Position.X.ToString("F6");
				this.yPosition.Text = this.vehicle.State.Position.Y.ToString("F6");
			}

			this.speed.Text = speed.ToString("F6");
			this.Invalidate();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
		}
	}
}