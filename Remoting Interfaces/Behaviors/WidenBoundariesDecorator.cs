using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors
{
	public class WidenBoundariesDecorator : BehaviorDecorator
	{
		private double extraWidth;

		public WidenBoundariesDecorator(double extra) {
			this.extraWidth = extra;
		}

		public double ExtraWidth {
			get { return extraWidth; }
		}
	}
}
