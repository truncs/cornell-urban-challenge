using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUI.Common.RunControl;
using UrbanChallenge.OperationalUI.Common.DataItem;
using UrbanChallenge.OperationalUI.Common.Map;

namespace UrbanChallenge.OperationalUI.Common {
	public static class Services {
		public static RunControlService RunControlService = new RunControlService();
		public static VehicleStateService VehicleStateService;
		public static ColorSet ColorSet;
		public static DisplayObjectCollection DisplayObjectService = new DisplayObjectCollection();
		public static IMap MapService;
	}
}
