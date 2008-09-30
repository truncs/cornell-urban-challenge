using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public class TurnSignalDecorator : BehaviorDecorator {
		private TurnSignal signal;

		public TurnSignalDecorator(TurnSignal signal) {
			this.signal = signal;
		}

		public TurnSignal Signal {
			get { return signal; }
		}
	}
}
