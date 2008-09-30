using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;
using UrbanChallenge.Common.Utility;
using System.Threading;
using System.Drawing.Drawing2D;
using UrbanChallenge.Pose;
using System.Net;

namespace VehicleSeparationDisplay
{
	public partial class VehicleSeparationDisplay : Form
	{
		public ArbiterInformation Information;
		public Communicator comms;
		private Color defaultBackColor = Color.DarkSeaGreen;
		private double lastCarTime = double.NaN;

		private PoseClient client;

		public VehicleSeparationDisplay()
		{
			InitializeComponent();
			this.Information = null;
			this.comms = new Communicator(this);

			// set our style
			base.SetStyle(ControlStyles.UserPaint, true);
			base.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			base.SetStyle(ControlStyles.Opaque, true);
			base.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			base.SetStyle(ControlStyles.ResizeRedraw, true);
			base.SetStyle(ControlStyles.Selectable, true);

			Thread t = new Thread(ForceRedraw);
			t.IsBackground = true;
			t.Start();

			client = new PoseClient(IPAddress.Parse("239.132.1.33"), 4839);
			client.PoseAbsReceived += new EventHandler<PoseAbsReceivedEventArgs>(client_PoseAbsReceived);
			client.Start();
		}

		void client_PoseAbsReceived(object sender, PoseAbsReceivedEventArgs e) {
			lastCarTime = e.PoseAbsData.timestamp.ts;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			// clear the background
			e.Graphics.Clear(BackColor);

			// save the graphics state
			GraphicsState gs = e.Graphics.Save();

			if (this.Information != null)
			{
				string Separation = Information.FVTXSeparation;

				if (Separation != "NaN")
				{
					this.BackColor = Color.DarkRed;
					this.label1.Text = Separation;
				}
				else
				{
					this.BackColor = this.defaultBackColor;
					this.label1.Text = "";
				}
			}
			else
			{
				this.BackColor = this.defaultBackColor;
				this.label1.Text = "";
			}

			if (!double.IsNaN(lastCarTime)) {
				this.label2.Text = lastCarTime.ToString("F4");
			}
			
			e.Graphics.Restore(gs);
			base.OnPaint(e);
		}

		public void ForceRedraw()
		{
			MMWaitableTimer timer = new MMWaitableTimer(100);

			while (true)
			{
				try
				{
					// timer wait
					timer.WaitEvent.WaitOne();

					// invoke
					if (!this.IsDisposed)
					{
						this.BeginInvoke(new MethodInvoker(delegate()
						{
							this.Invalidate();
						}));
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
				}
			}			
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (this.defaultBackColor == Color.DarkSeaGreen)
			{
				this.defaultBackColor = Color.Black;
				this.button1.Text = "Day Mode";
				this.button1.BackColor = Color.Black;
				this.button1.ForeColor = Color.DarkSeaGreen;
			}
			else
			{
				this.defaultBackColor = Color.DarkSeaGreen;
				this.button1.Text = "Night Mode";
				this.button1.BackColor = Color.DarkSeaGreen;
				this.button1.ForeColor = Color.Black;
			}
		}
	}
}