using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.CarTime {
	interface ICarTimeProvider {
		CarTimestamp Now { get; }
	}
}
