using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Pose {
	
	[global::System.Serializable]
	public class TransformationNotFoundException : InvalidOperationException {
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public TransformationNotFoundException() : base("Requested transformation does not exist in the queued data") { }
		public TransformationNotFoundException(CarTimestamp ts) : base("Requested transformation (" + ts.ts.ToString("F5") + ") does not exist in the queued data") { }
		public TransformationNotFoundException(string message) : base(message) { }
		public TransformationNotFoundException(string message, Exception inner) : base(message, inner) { }
		protected TransformationNotFoundException(
		System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
