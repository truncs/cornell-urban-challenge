using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace UrbanChallenge.OperationalUI.Controls {
	public class DoubleBufferedListView : ListView {
		public DoubleBufferedListView() {
			this.DoubleBuffered = true;
		}
	}
}
