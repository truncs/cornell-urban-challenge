using System;
using System.Collections.Generic;
using System.Text;

namespace CarBrowser.Micros {
	struct MicroTimestamp {
		public ushort secs;
		public int ticks;

		public MicroTimestamp(ushort secs, int ticks) {
			this.secs = secs;
			this.ticks = ticks;
		}
	}

	enum MicroTimingMode {
		Local = 0,
		Server = 1
	}

	enum MicroMessageCodes {
		GetTimestamp = 250,
		TimingPulse = 251,
		TimingSync = 252,
		TimingMode = 253,
		TimingResync = 254,
		PowerEnable = 0,
		PowerReset = 1,
		PowerStatus = 2,
		PowerPingEnabled = 3,
		PowerSave = 4,
		PowerEnableAll = 5
	}

	enum PowerState {
		Off,
		On,
		OpenLoad,
		FuseBlown,
		Resetting1,
		Resetting2
	}
}
