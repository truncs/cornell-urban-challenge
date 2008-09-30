using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors.CompletionReport {
	[Serializable]
	public class SuccessCompletionReport : CompletionReport {
		public SuccessCompletionReport(Type behaviorType)
			: base(behaviorType) {
		}

		public SuccessCompletionReport(Type behaviorType, string behaviorId)
			: base(behaviorType, behaviorId)
		{
		}

		public override CompletionResult Result {
			get { return CompletionResult.Success; }
		}

		public override string ToString() {
			return string.Format("SuccessCompletionReport ({0})", this.BehaviorType);
		}
	}
}
