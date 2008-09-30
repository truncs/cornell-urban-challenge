using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Output information for forward tracking
	/// </summary>
	public struct ForwardVehicleTrackingControl
	{
		/// <summary>
		/// Identifier if the forward vehicle exists
		/// </summary>
		public bool ForwardVehicleExists;

		/// <summary>
		/// Flag if the forward vehicle is in a safety zone
		/// </summary>
		public bool ForwardVehicleInSafetyZone;

		/// <summary>
		/// Speed to follow the forward vehicle
		/// </summary>
		public double vFollowing;

		/// <summary>
		/// The separation between our vehicle's front and the target's back
		/// </summary>
		public double xSeparation;

		/// <summary>
		/// Distance to the goood distance (0 if negative)
		/// </summary>
		public double xDistanceToGood;

		/// <summary>
		/// Speed of target vehicle
		/// </summary>
		public double vTarget;

		/// <summary>
		/// Absolute minimum distance
		/// </summary>
		public double xAbsMin;

		/// <summary>
		/// Good distance to be
		/// </summary>
		public double xGood;

		/// <summary>
		/// Forward vehicle is oncoming
		/// </summary>
		public bool forwardOncoming;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="forwardVehicleExists"></param>
		/// <param name="forwardVehicleInSafetyZone"></param>
		/// <param name="vFollowing"></param>
		public ForwardVehicleTrackingControl(bool forwardVehicleExists,
			bool forwardVehicleInSafetyZone, double vFollowing, double xSeparation,
			double xDistanceToGood, double vTarget, double xAbsMin, double xGood, bool forwardOncoming)
		{
			// set the forward vehicle as existing or not
			this.ForwardVehicleExists = forwardVehicleExists;

			// set teh forward vehicle in a safety zone or not
			this.ForwardVehicleInSafetyZone = forwardVehicleInSafetyZone;

			// set the following velocity
			this.vFollowing = vFollowing;

			// set the separation
			this.xSeparation = xSeparation;

			// set distance to good
			this.xDistanceToGood = xDistanceToGood;

			// set the forward vehicle's velocity
			this.vTarget = vTarget;

			// set abs min distance
			this.xAbsMin = xAbsMin;

			// set good dist
			this.xGood = xGood;

			// set forward oncoming
			this.forwardOncoming = forwardOncoming;
		}
	}
}
