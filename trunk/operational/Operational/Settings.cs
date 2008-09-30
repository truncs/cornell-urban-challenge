using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer {
	static class Settings {
		public const double TrackingPeriod = 1/20.0;
		public const double BehaviorPeriod = 1/10.0;
		public static readonly bool UseWheelSpeed = true;

		public static bool UsePathRoadModel = true;
		public static bool UsePosteriorPose = false;
		public static bool DoAvoidance;
		public static bool IgnoreTracks;

		private static bool testMode = false;

		public static bool TestMode {
			get { return Settings.testMode; }
			set { 
				Settings.testMode = value;
				if (testMode) {
					Console.WriteLine("*********************************************");
					Console.WriteLine("*******           TEST MODE           *******");
					Console.WriteLine("*********************************************");
					Console.WriteLine();
				}
			}
		}

		public static readonly Coordinates DefaultOrigin = new Coordinates(42.727826, -76.839341);

		static Settings() {
			DoAvoidance = Properties.Settings.Default.DoAvoidance;
			IgnoreTracks = Properties.Settings.Default.IgnoreTracks;
		}
	}
}
