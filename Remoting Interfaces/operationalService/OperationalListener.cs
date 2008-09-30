using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors.CompletionReport;

namespace UrbanChallenge.OperationalService {
	[Serializable]
	public abstract class OperationalListener : MarshalByRefObject {
		public abstract void OnCompletionReport(CompletionReport report);
	}
}
