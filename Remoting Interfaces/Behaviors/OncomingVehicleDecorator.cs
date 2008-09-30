using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Behaviors
{
	[Serializable]
	public class OncomingVehicleDecorator : BehaviorDecorator
	{
		private ScalarSpeedCommand secondarySpeed;
		private double targetDistance;
		private double targetSpeed;

		public OncomingVehicleDecorator(ScalarSpeedCommand secondarySpeed, double targetDistance, double targetSpeed)
		{
			this.secondarySpeed = secondarySpeed;
			this.targetDistance = targetDistance;
			this.targetSpeed = targetSpeed;
		}

		public ScalarSpeedCommand SecondarySpeed
		{
			get { return secondarySpeed; }
		}

		public double TargetDistance
		{
			get { return targetDistance; }
		}

		public double TargetSpeed
		{
			get { return targetSpeed; }
		}
	}
}
