using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace UrbanChallenge.Common.Utility {
	public class MMWaitableTimer : IDisposable {
		private AutoResetEvent ev;
		private uint eventID;
		private uint periodValue = 0;

		private const uint TIME_CALLBACK_EVENT_SET = 0x0010;
		private const uint TIME_PERIODIC = 0x0001;

		[StructLayout(LayoutKind.Sequential)]
		public struct TimeCaps {
			public UInt32 wPeriodMin;
			public UInt32 wPeriodMax;
		}

		[DllImport("winmm.dll", SetLastError = true)]
		static extern UInt32 timeGetDevCaps(ref TimeCaps timeCaps,
								UInt32 sizeTimeCaps);

		[DllImport("winmm.dll", SetLastError = true)]
		static extern UInt32 timeBeginPeriod(UInt32 uPeriod);

		[DllImport("winmm.dll", SetLastError = true)]
		static extern UInt32 timeEndPeriod(UInt32 uPeriod);

		[DllImport("winmm.dll", SetLastError = true)]
		static extern UInt32 timeSetEvent(UInt32 msDelay, UInt32 msResolution,
								IntPtr evHandle, IntPtr userCtx, UInt32 eventType);

		[DllImport("winmm.dll", SetLastError=true)]
		static extern UInt32 timeKillEvent(UInt32 uTimerID);

		public MMWaitableTimer(uint period) {
			TimeCaps caps = new TimeCaps();
			uint res = timeGetDevCaps(ref caps, (uint)Marshal.SizeOf(typeof(TimeCaps)));

			if (res == 0) {
				timeBeginPeriod(caps.wPeriodMin);
			}
			else {
				Console.WriteLine("could not get timing resolution");
			}

			ev = new AutoResetEvent(false);
			eventID = timeSetEvent(period, 0, ev.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, TIME_CALLBACK_EVENT_SET | TIME_PERIODIC);
		}

		public AutoResetEvent WaitEvent {
			get { return ev; }
		}

		protected void Dispose(bool explicitDisposing) {
			if (eventID != 0) {
				timeKillEvent(eventID);
				eventID = 0;
			}

			if (periodValue != 0) {
				timeEndPeriod(periodValue);
				periodValue = 0;
			}

			if (explicitDisposing)
				ev.Close();
		}

		~MMWaitableTimer() {
			Dispose(false);
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
		}

		#endregion
	}
}
