using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors.CompletionReport {
	[Serializable]
	public abstract class CompletionReport {
		private Type type;
		private string id;

		protected CompletionReport(Type behaviorType) {
			this.type = behaviorType;
			this.id = "";
		}

		protected CompletionReport(Type behaviorType, string id) {
			this.type = behaviorType;
			this.id = id;
		}

		public abstract CompletionResult Result { get; }
		public virtual Type BehaviorType { get { return type; } }
		public virtual string BehaviorId { get { return id; } set { id = value; } }
	}
}
