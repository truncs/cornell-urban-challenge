using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.OperationalService;
using UrbanChallenge.Common;
using OperationalLayer.Pose;
using UrbanChallenge.OperationalUIService.Parameters;
using UrbanChallenge.Common.Utility;

namespace OperationalLayer.Tracking.SpeedControl {
	static class SpeedUtilities {
		private const double stop_dist_tolerance  = 0.2;
		private const double stop_speed_tolerance = 0.3;
		private const double min_cmd_speed = 0.5;

		private static TunableParam profile_power_param;

		/// <summary>
		/// Computes the engine torque and brake pressure needed to achieve a specified acceleration. 
		/// </summary>
		/// <param name="ra">Requested acceleration in m/s^2</param>
		/// <param name="vs">Vehicle state</param>
		/// <param name="commandedEngineTorque"></param>
		/// <param name="commandedBrakePressure"></param>
		/// <returns>True if the vehicle state allows the acceleration to be achieved, false otherwise.</returns>
		public static bool GetCommandForAcceleration(double ra, OperationalVehicleState vs, out double commandedEngineTorque, out double commandedBrakePressure) {
			// speed, positive for forward motion (m/s)
			double v = Math.Abs(vs.speed);

			// current gear (not just shifter but also first, second, etc)
			TransmissionGear gear = vs.transGear;

			// vehicle pitch, from pose (rad)
			double phi = vs.pitch;

			if (gear == TransmissionGear.Reverse) {
				phi = -phi;
			}

			CarTimestamp lastTransChangeTimeTS = Services.Dataset.ItemAs<DateTime>("trans gear change time").CurrentTime;
			DateTime lastTransChangeTime = Services.Dataset.ItemAs<DateTime>("trans gear change time").CurrentValue;
		  
			if (gear > TransmissionGear.Fourth || gear < TransmissionGear.Reverse || (lastTransChangeTimeTS.IsValid && (HighResDateTime.Now - lastTransChangeTime) < TimeSpan.FromSeconds(1) && (gear == TransmissionGear.First || gear == TransmissionGear.Reverse))) {
				// in park or neutral or have not waited sufficient time after shift, just break out of the function
				commandedEngineTorque = 0;
				commandedBrakePressure = -TahoeParams.bc0;
				return false;
			}

			// current gear ratio
			double Nt = Math.Abs(TahoeParams.Nt[(int)gear]);

			// initialize to reasonable default values in case all else fails
			commandedEngineTorque = 0;
			commandedBrakePressure = -TahoeParams.bc0;

			// effective rotating mass
			double mr = TahoeParams.mr1 +
						TahoeParams.mr2 * Math.Pow(TahoeParams.Nf, 2) +
						TahoeParams.mr3 * Math.Pow(TahoeParams.Nf, 2) * Math.Pow(Nt, 2);

			// rolling resistance (N)
			double Rr = 2*TahoeParams.m * TahoeParams.g * (TahoeParams.rr1 + TahoeParams.rr2 * v);

			double rps2rpm = 60.0 / (2.0 * Math.PI);

			// calculate engine speed in rpm
			double rpme = vs.engineRPM;
			// calculate driveshaft rpm post torque converter
			double rpmd = (v / TahoeParams.r_wheel) * TahoeParams.Nf * Nt * rps2rpm;

			// calculate speed ratio, torque ratio, and capacity factor from lookups
			double sr = rpmd / rpme;
			// tr = interp1(srlu, trlu, sr, 'linear', 'extrap');
			double tr = TahoeParams.TorqueRatio(sr);

			//Dataset.Source.DatasetSource ds = Operational.Instance.Dataset;
			
			// calculate bleed-off torque
			double bleed_torque = TahoeParams.bleed_off_power / (rpme / rps2rpm);

			// requested acceleration in Newtons
			double ra_corr = (TahoeParams.m + mr) * ra;
			// drag force
			double drag = 0.5 * TahoeParams.rho * TahoeParams.cD * TahoeParams.cxa * Math.Pow(v, 2);
			// pitch force
			double pitch_corr = -TahoeParams.m * TahoeParams.g * Math.Sin(phi);

			// needed wheel torque (N-m)
			double wheelTorque = (ra_corr + Rr + drag + pitch_corr) * TahoeParams.r_wheel;

			// commanded engine torque (N-m) to achieve desired acceleration
			commandedEngineTorque = wheelTorque / (Nt * TahoeParams.Nf * TahoeParams.eta * tr);

			DateTime now = DateTime.Now;
			//ds.ItemAs<double>("torque multiplier").Add(tr, now);
			//ds.ItemAs<double>("speed - Rr").Add(Rr, now);
			//ds.ItemAs<double>("speed - ra").Add(ra_corr, now);
			//ds.ItemAs<double>("speed - pitch").Add(pitch_corr, now);
			//ds.ItemAs<double>("speed - drag").Add(drag, now);
			//ds.ItemAs<double>("speed - wheel torque").Add(wheelTorque, now);
			//ds.ItemAs<double>("speed - eng torque").Add(commandedEngineTorque, now);

			// retrieve the current engine torque, brake pressure
			double current_brake_pressure = vs.brakePressure;

			// decide to apply master cylinder pressure or engine torque
			// check if we want to apply less torque than is the minimum
			if (commandedEngineTorque < TahoeParams.Te_min) {
				// assume that the current engine torque is just the minimum delivered engine torque
				double current_engine_torque = TahoeParams.Te_min;
				double Btrq = Nt * TahoeParams.Nf * TahoeParams.eta / TahoeParams.r_wheel;

				// actual acceleration with no extra torque applied (m / s^2)
				double aa = -(Rr + 0.5 * TahoeParams.rho * TahoeParams.cD * TahoeParams.cxa * Math.Pow(v, 2) -
							TahoeParams.m * TahoeParams.g * Math.Sin(phi)) / (TahoeParams.m + mr) +
							(Btrq*tr*(current_engine_torque-bleed_torque)) / (TahoeParams.m + mr);

				// residual brake acceleration (m / s^2)
				double ba = ra - aa; // if ba > 0, engine braking is enough

				// desired master cylinder pressure (GM units, approx 22 to 50)
				double mcp = -TahoeParams.bc0;
				if (ba < 0) {
					// compute braking coefficent (which was empirically determined to be speed dependent)
					double bc = TahoeParams.bc1 + TahoeParams.bcs*Math.Sqrt(v);
					mcp = (-(TahoeParams.m + mr) * ba * TahoeParams.r_wheel / bc) - TahoeParams.bc0;
				}

				// set commanded engine torque to 0
				commandedEngineTorque = 0;
				// assign master cylinder pressure
				commandedBrakePressure = Math.Round(mcp, 0);
				return true;
			}
			else {
				// we want to command engine torque
				// check if the brake is off (if not, don't apply engine torque)
				if (current_brake_pressure >= 24) {
					commandedBrakePressure = -TahoeParams.bc0;
					commandedEngineTorque = 0;
					return false;
				}
				// add in the bleed off torque
				commandedEngineTorque += bleed_torque;
				// zero out the commande brake pressure
				commandedBrakePressure = -TahoeParams.bc0;
				return true;
			}
		}

		// TODO: compute minimum braking distance

		/// <summary>
		/// Computes a commanded speed given an initial velocity, final velocity, total distance of 
		/// velocity change and current distance remaining. 
		/// </summary>
		/// <param name="Vi">Initial velocity</param>
		/// <param name="Vf">Final velocity</param>
		/// <param name="D">Total distance for speed change</param>
		/// <param name="d">Remaining distance for speed change</param>
		/// <returns>Commanded speed in m/s</returns>
		public static double GetLinearProfileVelocity(double Vi, double Vf, double D, double d) {
			// Vi = initial velocity of segment
			// Vf = final velocity of segment
			// D = distance of segment
			// d = distance to the end of the segment

			d = D - d;
			if (d < 0)
				d = 0;
			else if (d > D)
				d = D;

			return d / D * (Vf - Vi) + Vi;
		}

		public static double GetPowerProfileVelocity(double Vi, double Vf, double D, double d) {
			// Vi = initial velocity of segment
			// Vf = final velocity of segment
			// D = distance of segment
			// d = distance to the end of the segment

			if (profile_power_param == null) {
				profile_power_param = Services.Params.GetParam("profile power", "speed", 1.25);
			}

			double fact = d / D;
			if (fact > 1)
				fact = 0;
			if (fact < 0)
				fact = 0;
			fact = Math.Pow(fact, 1 / profile_power_param.Value);
			return fact * (Vi - Vf) + Vf;
		}

		/// <summary>
		/// Computes a commanded torque and brake pressure to bring the vehicle to a stop given the 
    /// original speed the vehicle was travelling and the remaining distance. This will keep the 
    /// commanded speed constant until a final approach distance and then linearly ramp down the 
    /// speed.
		/// </summary>
		/// <param name="remaining">Remaining distance to stop in meters</param>
		/// <param name="origSpeed">Approach speed of stop maneuver in m/s</param>
		/// <param name="inFinalApproach">Flag indicating that the final approach is in progress. Only set to true.</param>
		/// <returns>Engine torque and brake pressure to be commanded to stop as desired</returns>
		public static bool GetStoppingSpeedCommand(double remaining, double origSpeed, OperationalVehicleState vs, ref double commandedSpeed) {
			// check if we're within the stopping tolerance
			if ((remaining <= stop_dist_tolerance && Math.Abs(vs.speed) <= stop_speed_tolerance) || remaining <= 0) {
				// return a commanded speed of 0 which flags that we should just stop
				commandedSpeed = 0;
				// return that we've stoppped
				return Math.Abs(vs.speed) <= stop_speed_tolerance;
			}
			
			// get the distance for the final approach
			double stoppingDist = 4;

			// check if the remaining distance is greater than the stopping distance or not
			if (remaining > stoppingDist) {
				// we have not reached the final approach yet, so keep commanding the original speed
				commandedSpeed = Math.Max(origSpeed, min_cmd_speed);
				
			}
			else {
				// we are within the final approach distance so linearly ramp down the speed
				commandedSpeed = GetPowerProfileVelocity(origSpeed, 0, stoppingDist, remaining);
			}

			return false;
		}
	}
}
