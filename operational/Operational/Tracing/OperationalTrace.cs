using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace OperationalLayer.Tracing {
	static class OperationalTrace {
		private static TraceSource defaultSource = new TraceSource("operational");
		private static LocalDataStoreSlot traceSourceSlot = Thread.AllocateDataSlot();

		public static TraceSource ThreadTraceSource {
			get {
				object traceSourceObject = Thread.GetData(traceSourceSlot);
				if (traceSourceObject == null) {
					return defaultSource;
				}
				else {
					return (TraceSource)traceSourceObject;
				}
			}
			set {
				Thread.SetData(traceSourceSlot, value);
			}
		}

		public static void WriteEvent(TraceEventType type, string message) {
			TraceSource source = ThreadTraceSource;
			source.TraceEvent(type, 0, message);			
		}

		public static void WriteEvent(TraceEventType type, string format, params object[] args) {
			TraceSource source = ThreadTraceSource;
			source.TraceEvent(type, 0, format, args);
		}

		public static void WriteVerbose(string message) {
			WriteEvent(TraceEventType.Verbose, message);
		}

		public static void WriteVerbose(string format, params object[] args) {
			WriteEvent(TraceEventType.Verbose, format, args);
		}

		public static void WriteInformation(string message) {
			WriteEvent(TraceEventType.Information, message);
		}

		public static void WriteInformation(string format, params object[] args) {
			WriteEvent(TraceEventType.Information, format, args);
		}

		public static void WriteWarning(string message) {
			WriteEvent(TraceEventType.Warning, message);
		}

		public static void WriteWarning(string format, params object[] args) {
			WriteEvent(TraceEventType.Warning, format, args);
		}

		public static void WriteError(string message) {
			WriteEvent(TraceEventType.Error, message);
		}

		public static void WriteError(string format, params object[] args) {
			WriteEvent(TraceEventType.Error, format, args);
		}

		public static void WriteCritical(string message) {
			WriteEvent(TraceEventType.Critical, message);
		}

		public static void WriteCritical(string format, params object[] args) {
			WriteEvent(TraceEventType.Critical, format, args);
		}
	}
}