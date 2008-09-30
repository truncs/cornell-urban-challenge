using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public class DisplayObjectEventArgs : EventArgs {
		private IRenderable displayObject;

		public DisplayObjectEventArgs(IRenderable displayObject) {
			this.displayObject = displayObject;
		}

		public IRenderable DisplayObject {
			get { return displayObject; }
		}
	}
}
