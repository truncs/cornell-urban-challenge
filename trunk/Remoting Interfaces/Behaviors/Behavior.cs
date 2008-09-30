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

		public double TimeStamp;

    public abstract string ToShortString();

    public abstract string ShortBehaviorInformation();

		public abstract string SpeedCommandString();

		public virtual string UniqueId()
		{
			return this.ShortBehaviorInformation();
		}
	}
}
