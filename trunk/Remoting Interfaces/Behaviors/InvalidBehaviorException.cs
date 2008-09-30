using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors {
	
	[Serializable]
	public class InvalidBehaviorException : Exception {
		public InvalidBehaviorException() : base("The requested behavior is invalid for the current behavior state") { }
		public InvalidBehaviorException(string message) : base(message) { }
		public InvalidBehaviorException(string message, Exception inner) : base(message, inner) { }
		protected InvalidBehaviorException(
		System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
