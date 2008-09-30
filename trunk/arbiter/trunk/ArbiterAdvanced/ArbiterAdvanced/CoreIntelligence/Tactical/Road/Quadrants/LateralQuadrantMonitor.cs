using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using System.Diagnostics;
using UrbanChallenge.Common.Sensors;
using UrbanChallenge.Arbiter.Core.Communications;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Monitors quadrant laterally to the vehicle
	/// </summary>
	public class LateralQuadrantMonitor
	{
		/// <summary>
		/// Checks time since lateral vehicle clear
		/// </summary>
		public Stopwatch LateralClearStopwatch;

		/// <summary>
		/// Side of the current side obstacle
		/// </summary>
		public SideObstacleSide VehicleSide;

		/// <summary>
		/// Current vehicle occupying the monitor
		/// </summary>
		public VehicleAgent CurrentVehicle;

		/// <summary>
		/// Constructor
		/// </summary>
		public LateralQuadrantMonitor(SideObstacleSide sos)
		{
			this.LateralClearStopwatch = new Stopwatch();
			this.VehicleSide = sos;
		}

		/// <summary>
		/// Check if a side sick blocking obstacle is detected
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		private bool SideSickObstacleDetected(ArbiterLane lane, VehicleState state)
		{
			try
			{
				// get distance from vehicle to lane opp side
				Coordinates vec = state.Front - state.Position;
				vec = this.VehicleSide == SideObstacleSide.Driver ? vec.Rotate90() : vec.RotateM90();

				SideObstacles sobs = CoreCommon.Communications.GetSideObstacles(this.VehicleSide);
				if (sobs != null && sobs.obstacles != null)
				{
					foreach (SideObstacle so in sobs.obstacles)
					{
						Coordinates cVec = state.Position + vec.Normalize(so.distance);
						if (so.height > 0.7 && lane.LanePolygon.IsInside(cVec))
							return true;
					}
				}
			}
			catch (Exception ex)
			{
				ArbiterOutput.Output("side sick obstacle exception: " + ex.ToString());
			}

			return false;
		}

		/// <summary>
		/// Given vehicles and there locations determines if the lane adjacent to us is occupied adjacent to the vehicle
		/// </summary>
		/// <param name="lane"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public bool Occupied(ArbiterLane lane, VehicleState state)
		{
			// check stopwatch time for proper elapsed
			if (this.LateralClearStopwatch.ElapsedMilliseconds / 1000.0 > 10)
				this.Reset();

			if (TacticalDirector.VehicleAreas.ContainsKey(lane))
			{
				// vehicles in the lane
				List<VehicleAgent> laneVehicles = TacticalDirector.VehicleAreas[lane];

				// position of the center of our vehicle
				Coordinates center = state.Front - state.Heading.Normalize(TahoeParams.VL / 2.0);

				// cutoff for allowing vehicles outside this range
				double dCutOff = TahoeParams.VL * 1.5;

				// loop through vehicles
				foreach (VehicleAgent va in laneVehicles)
				{
					// vehicle high level distance
					double d = Math.Abs(lane.DistanceBetween(center, va.ClosestPosition));

					// check less than distance cutoff
					if (d < dCutOff)
					{
						ArbiterOutput.Output("Forward Lateral: " + this.VehicleSide.ToString() + " filled with vehicle: " + va.ToString());
						this.CurrentVehicle = va;
						this.Reset();
						return true;
					}
				}
			}

			// now check the lateral sensor for being occupied
			if (this.SideSickObstacleDetected(lane, state))
			{
				this.CurrentVehicle = new VehicleAgent(true, true);
				this.Reset();
				ArbiterOutput.Output("Forward Lateral: " + this.VehicleSide.ToString() + " SIDE OBSTACLE DETECTED");
				return true;
			}

			// none found
			this.CurrentVehicle = null;

			// if none found, timer not running start timer
			if (!this.LateralClearStopwatch.IsRunning)
			{
				this.CurrentVehicle = new VehicleAgent(true, true);
				this.Reset();
				this.LateralClearStopwatch.Start();
				ArbiterOutput.Output("Forward Lateral: " + this.VehicleSide.ToString() + " Clear, starting cooldown");
				return true;
			}
			// check enough time
			else if (this.LateralClearStopwatch.IsRunning && this.LateralClearStopwatch.ElapsedMilliseconds / 1000.0 > 0.5)
			{
				this.CurrentVehicle = null;
				ArbiterOutput.Output("Forward Lateral: " + this.VehicleSide.ToString() + " Clear, cooldown complete");
				return false;
			}
			// not enough time
			else
			{
				this.CurrentVehicle = new VehicleAgent(true, true);
				double timer = this.LateralClearStopwatch.ElapsedMilliseconds / 1000.0;
				ArbiterOutput.Output("Forward Lateral: " + this.VehicleSide.ToString() + " Clear, cooldown timer: " + timer.ToString("F2"));
				return true;
			}
		}

		/// <summary>
		/// Resets values held over time
		/// </summary>
		public void Reset()
		{
			this.LateralClearStopwatch.Stop();
			this.LateralClearStopwatch.Reset();
		}
	}
}
