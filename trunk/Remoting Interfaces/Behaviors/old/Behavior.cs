using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	[Serializable]
	public abstract class Behavior {
		protected List<BehaviorDecorator> decorators;

		public virtual Behavior NextBehavior {
			get { return null; }
		}

		public List<BehaviorDecorator> Decorators {
			get { return decorators; }
			set { decorators = value; }
		}
	}
}
