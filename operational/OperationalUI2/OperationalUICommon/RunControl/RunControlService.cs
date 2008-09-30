using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.RunControl {
	public class RunControlService {
		public event EventHandler RunModeChanged;
		public event EventHandler RenderCycle;
		public event EventHandler DrawCycle;
		public event EventHandler RenderPeriodChanged;

		private RunMode runMode = RunMode.Realtime;
		private TimeSpan renderPeriod = TimeSpan.FromMilliseconds(50);
		private bool doStep;

		public void Step() {
			doStep = true;
		}

		public void OnRenderCycle() {
			if (doStep) {
				RunMode = RunMode.Realtime;
			}

			if (RenderCycle != null) {
				RenderCycle(this, EventArgs.Empty);
			}

			if (DrawCycle != null) {
				DrawCycle(this, EventArgs.Empty);
			}

			if (doStep) {
				RunMode = RunMode.Paused;
				doStep = false;
			}
		}

		public RunMode RunMode {
			get { return runMode; }
			set {
				if (runMode != value) {
					runMode = value;

					if (RunModeChanged != null) {
						RunModeChanged(this, EventArgs.Empty);
					}
				}
			}
		}

		public TimeSpan RenderPeriod {
			get { return renderPeriod; }
			set {
				if (renderPeriod != value) {
					renderPeriod = value;

					if (RenderPeriodChanged != null) {
						RenderPeriodChanged(this, EventArgs.Empty);
					}
				}
			}
		}
	}
}
