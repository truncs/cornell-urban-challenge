using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using System.Windows.Forms;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public class MouseEvent {
		private Coordinates location;
		private int clicks;
		private MouseButtons buttons;

		public MouseEvent(Coordinates location, int clicks, MouseButtons buttons) {
			this.location = location;
			this.clicks = clicks;
			this.buttons = buttons;
		}

		public Coordinates Location {
			get { return location; }
		}

		public int Clicks {
			get { return clicks; }
		}

		public MouseButtons Buttons {
			get { return buttons; }
		}
	}
}
