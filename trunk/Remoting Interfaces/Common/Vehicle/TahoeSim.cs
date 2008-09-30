using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Vehicle {
	public class TahoeSim {
		private const double Torque_max = 461;
		private const double brake_max = 60;

		// vehicle state
		private double x;                        // x-position (m)
		private double y;                        // y-position (m)
		private double speed;                    // speed (m/s, positive forward)
		private double heading;                  // heading (rad., CCW East)
		private TransmissionGear gear;			     // gear, 'r', '1', '2', '3', '4'
		private double throttle;                 // throttle (between 0 and 1), the fraction of maximum torque commanded
		private double steering_wheel;           // steering wheel angle (rad., positive left)
		private double engine_torque;            // engine torque (N-m)
		private double master_cylinder_pressure; // master cylinder pressure (Aarons)
		private double engine_speed;             // engine speed (rad. / sec.)

		private double phi = 0;									 // ground pitch

		// vehicle commands
		private double commanded_steering_wheel;
		private double commanded_engine_torque;
		private double commanded_master_cylinder_pressure;

		private bool acceptCommands = true;

		private CarMode carMode = CarMode.Pause;

		public TahoeSim(Coordinates loc, double heading) {
			InitializeState();

			this.x = loc.X;
			this.y = loc.Y;
			this.heading = heading;
		}

		public TahoeSim(Coordinates loc, double heading, double speed, TransmissionGear gear, double throttle, double steering_wheel, double engine_torque, double master_cylinder_pressure, double engine_speed) {
			InitializeState();

			this.x = loc.X;
			this.y = loc.Y;
			this.heading = heading;
			this.speed = speed;
			this.gear = gear;
			this.throttle = throttle;
			this.steering_wheel = steering_wheel;
			this.engine_torque = engine_torque;
			this.master_cylinder_pressure = master_cylinder_pressure;
			this.engine_speed = engine_speed;

			TahoeConstraints();
		}

		// initialise vehicle state and commands
		private void InitializeState() {
			// initialise vehicle state
			this.x = 0;
			this.y = 0;
			this.heading = 0;
			speed = 0;
			gear = TransmissionGear.First;  // gear, 'r', '1', '2', '3', '4'
			throttle = 0.0;                 // throttle (between 0 and 1), the fraction of maximum torque commanded
			steering_wheel = 0.0;           // steering wheel angle (rad., positive left)
			engine_torque = 0.0;            // engine torque (N-m)
			master_cylinder_pressure = 0.0; // master cylinder pressure (Aarons)
			engine_speed = 0.0;             // engine speed (rad. / sec.)

			// initialise commands
			commanded_steering_wheel = 0.0;
			commanded_engine_torque = 0.0;
			commanded_master_cylinder_pressure = 35; // initialize to something so we don't start moving around

			// make sure initial state is valid
			TahoeConstraints();
		}

		#region Accessors 

		public bool AcceptCommands {
			get { return acceptCommands; }
			set { acceptCommands = value; }
		}

		public Coordinates Position {
			get { return new Coordinates(x, y); }
			set {
				x = value.X;
				y = value.Y;
			}
		}

		public double Heading {
			get { return heading; }
			set { heading = value; }
		}

		public double Speed {
			get { return speed; }
			set { speed = value; }
		}

		public TransmissionGear Gear {
			get { return gear; }
		}

		public double SteeringWheelAngle {
			get { return steering_wheel; }
		}

		public double Throttle {
			get { return throttle; }
		}

		public double EngineTorque {
			get { return engine_torque; }
		}

		public double MasterCylinderPressure {
			get { return master_cylinder_pressure; }
		}

		public double EngineSpeed {
			get { return engine_speed; }
		}

		public double GroundPitch {
			get { return phi; }
			set { phi = value; }
		}

		public CarMode CarMode {
			get { return carMode; }
			set { carMode = value; }
		}

		#endregion

		#region Command Functions

		public void SetSteeringBrakeThrottle(double? commanded_steer, double? commanded_throttle, double? commanded_brake) {
			if (acceptCommands) {
				if (commanded_throttle.HasValue && commanded_throttle.Value > Torque_max) commanded_throttle = Torque_max;
				if (commanded_brake.HasValue && commanded_brake.Value > brake_max) commanded_brake = brake_max;

				if (commanded_steer.HasValue) {
					commanded_steering_wheel = commanded_steer.Value;
				}

				if (commanded_throttle.HasValue) {
					double bleed_off_torque = TahoeParams.bleed_off_power / engine_speed;
					commanded_engine_torque = commanded_throttle.Value - bleed_off_torque;
				}

				if (commanded_brake.HasValue) {
					commanded_master_cylinder_pressure = Math.Round(commanded_brake.Value);
				}

				if (commanded_engine_torque < TahoeParams.Te_min) {
					commanded_engine_torque = TahoeParams.Te_min;
				}
			}
		}

		public void SetTransmissionGear(TransmissionGear g) {
			if (g >= TransmissionGear.First && g <= TransmissionGear.Fourth) {
				gear = TransmissionGear.First;
			}
			else if (g == TransmissionGear.Reverse) {
				gear = TransmissionGear.Reverse;
			}
		}

		public void PauseVehicle() {
			this.commanded_engine_torque = 0;
			this.commanded_master_cylinder_pressure = 40;
			this.commanded_steering_wheel = 0;
			carMode = CarMode.Pause;
			acceptCommands = false;
		}

		public void RunVehicle() {
			carMode = CarMode.Run;
			acceptCommands = true;
		}

		#endregion

		#region Update functions

		public void Update(double dt) {
			UpdateVehicleState(dt);
			TahoeConstraints();
			CheckGear();
		}

		// --------------------------------------------------------------------------------
		// updates the vehicle state using the vehicle dynamics
		private void UpdateVehicleState(double dt) {
			double[] k1 = new double[8];
			double[] k2 = new double[8];
			double[] k3 = new double[8];
			double[] k4 = new double[8];

			double[] xdot = new double[8];
			double[] xcur = new double[8];
			double[] xtemp = new double[8];

			xcur[0] = x;
			xcur[1] = y;
			xcur[2] = speed;
			xcur[3] = heading;
			xcur[4] = steering_wheel;
			xcur[5] = engine_torque;
			xcur[6] = master_cylinder_pressure;
			xcur[7] = engine_speed;

			// make a copy to get consistent reads
			double csw = commanded_steering_wheel;
			double cet = commanded_engine_torque;
			double cmcp = commanded_master_cylinder_pressure;

			// Runge-Kutta - 1st iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xcur[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, phi);
			for (int i = 0; i < 8; i++) {
				k1[i] = dt * xdot[i];
				xtemp[i] = xcur[i] + k1[i] * 0.5;
			}

			if (gear == TransmissionGear.Reverse) {
				if (xtemp[2] > 0) xtemp[2] = 0;
			}
			else {
				if (xtemp[2] < 0) xtemp[2] = 0;
			}

			// Runge-Kutta - 2nd iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xtemp[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, phi);
			for (int i = 0; i < 8; i++) {
				k2[i] = dt * xdot[i];
				xtemp[i] = xcur[i] + k2[i] * 0.5;
			}

			if (gear == TransmissionGear.Reverse) {
				if (xtemp[2] > 0) xtemp[2] = 0;
			}
			else {
				if (xtemp[2] < 0) xtemp[2] = 0;
			}

			// Runge-Kutta - 3rd iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xtemp[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, phi);
			for (int i = 0; i < 8; i++) {
				k3[i] = dt * xdot[i];
				xtemp[i] = xcur[i] + k3[i];
			}

			if (gear == TransmissionGear.Reverse) {
				if (xtemp[2] > 0) xtemp[2] = 0;
			}
			else {
				if (xtemp[2] < 0) xtemp[2] = 0;
			}

			// Runge-Kutta - 4th iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xtemp[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, phi);
			for (int i = 0; i < 8; i++) {
				k4[i] = dt * xdot[i];
			}

			// Runge-Kutta - Final computation
			for (int i = 0; i < 8; i++) {
				xdot[i] = (k1[i] / 6.0) + (k2[i] / 3.0) + (k3[i] / 3.0) + (k4[i] / 6.0);
				xcur[i] += xdot[i];
			}

			x = xcur[0];
			y = xcur[1];
			speed = xcur[2];
			heading = xcur[3];
			steering_wheel = xcur[4];
			engine_torque = xcur[5];
			master_cylinder_pressure = xcur[6];
			engine_speed = xcur[7];
		}

		// --------------------------------------------------------------------------------
		// computes the state derivative of a car's drivetrain model
		private void TahoeDynamics(ref double[] xdot, double rts, double rTe, double rPmc, double phi) {
			/* 
			inputs:
			xdot - vehicle state vector [x, y, s, h, ts, Te, Pmc, we]:
				x   - vehicle x-position (m)
				y   - vehicle y-position (m)
				s   - vehicle speed (m/s)
				h   - vehicle heading (rad., CCW-East)
				ts  - angle of the steering wheel
				Te  - current torque provided by engine (N-m)
				Pmc - current pressure on the master cylinder (Aarons)
				we  - current engine speed (rad. / sec.)
			rts  - requested steering wheel angle (rad.)
			rTe  - requested engine torque (N-m)
			rPmc - requested master cylinder pressure (Aarons)
			phi  - current road grade (deg. - downhill is positive)

		outputs:
			xdot - derivative of vehicle state vector at the current time.

		Centa, G.       "Motor Vehicle Dynamics: Modeling and Simulation."  
									Singapore: World Scientific, 1997.
		Gillespie, T.   "Fundamentals of Vehicle Dynamics."  Warrendale, PA: 
									Society of Automotive Engineers, Inc., 1992.
		Wong, J.        "Theory of Ground Vehicles, Third Edition."  New York, NY: John
									Wiley & Sons, Inc., 2001.

		see for engine curves:
		http://media.gm.com/us/powertrain/en/product_services/2007/07truck.htm
		*/

			// extract components from the state vector
			double s = xdot[2]; // speed
			double h = xdot[3]; // heading
			double ts = xdot[4]; // steering_wheel
			double Te = xdot[5]; // engine_torque
			double Pmc = xdot[6]; // master_cylinder_pressure
			double we = xdot[7]; // engine_speed

			for (int i = 0; i < 8; i++)
				xdot[i] = 0.0; // initialize the output

			// use newton raphson to calculate current curvature from steering wheel
			double c = 0.0;
			double atarg1, atarg2;
			double s2 = s * s;
			double c2, Lr2, signc, ooc2, d, sa;
			double somc2Lr2, datarg1dc, datarg2dc, daadc, dsadc, dc;
			for (int i = 0; i < 10; i++) {
				// precalculate repeated variables to save cycles
				//double 
				c2 = c * c;
				//double 
				Lr2 = TahoeParams.Lr * TahoeParams.Lr;
				//double 
				signc = Math.Sign(c);

				// calculate the current arguments of the arctangent
				if (Math.Abs(c) > 1e-10) {
					//double 
					ooc2 = 1.0 / c2;
					atarg1 = TahoeParams.L / (Math.Sqrt(ooc2 - Lr2) + 0.5 * TahoeParams.T);
					atarg2 = TahoeParams.L / (Math.Sqrt(ooc2 - Lr2) - 0.5 * TahoeParams.T);
				}
				else {
					atarg1 = 0.0;
					atarg2 = 0.0;
				}

				// calculate steering angle as a function of current curvature guess
				//double 
				d = signc * (0.5 * Math.Atan(atarg1) + 0.5 * Math.Atan(atarg2)) + TahoeParams.cs0 * c * s2;
				//double 
				sa = TahoeParams.s0 + TahoeParams.s1 * d + TahoeParams.s3 * Math.Pow(d, 3);

				// calculate partial derivatives
				//double 
				somc2Lr2 = Math.Sqrt(1.0 - c2 * Lr2);
				//double 
				datarg1dc = TahoeParams.L / ((somc2Lr2 + 0.5 * c * TahoeParams.T) * (somc2Lr2 + 0.5 * Math.Abs(c) * TahoeParams.T) * somc2Lr2);
				//double 
				datarg2dc = TahoeParams.L / ((somc2Lr2 - 0.5 * c * TahoeParams.T) * (somc2Lr2 - 0.5 * Math.Abs(c) * TahoeParams.T) * somc2Lr2);

				// partial of ackerman angle wrt curvature
				//double 
				daadc = 0.5 / (1.0 + atarg1 * atarg1) * datarg1dc + 0.5 / (1.0 + atarg2 * atarg2) * datarg2dc + TahoeParams.cs0 * s2;

				// partial of steering angle function wrt curvature
				//double 
				dsadc = TahoeParams.s1 * daadc + 3 * TahoeParams.s3 * d * d * daadc;

				// calculate updated curvature using Newton's method
				//double 
				dc = -(sa - ts) / dsadc;
				c = c + dc;
				if (Math.Abs(dc) < 1.0e-8)
					break;
			}

			double rps2rpm = 60.0 / (2.0 * Math.PI);
			double Nt = GetGearRatio();

			// calculate engine speed in rpm
			double rpme = we * rps2rpm;
			// calculate driveshaft rpm post torque converter
			double rpmd = (s / TahoeParams.r_wheel) * TahoeParams.Nf * Nt * rps2rpm;
			// calculate speed ratio, torque ratio, and capacity factor from lookups
			double sr = rpmd / rpme;
			// tr = interp1(srlu, trlu, sr, 'linear', 'extrap');
			double tr = TahoeParams.TorqueRatio(sr);
			// Ktc = interp1(srlu, cflu, sr, 'linear', 'extrap');
			double Ktc = TahoeParams.CapacityFactor(sr);
			// calculate applied torque post torque converter
			double Ttc = tr * Te;
			// calculate torque converter backdrive torque (N-m) using capacity factor
			double Ttcb = (we / Ktc);
			Ttcb *= Ttcb;

			// calculate equivalent rotating mass
			double mr = TahoeParams.mr1 + TahoeParams.mr2 * TahoeParams.Nf * TahoeParams.Nf +
									TahoeParams.mr3 * TahoeParams.Nf * TahoeParams.Nf * Nt * Nt;

			// calculate rolling resistance
			double Rr = TahoeParams.m * TahoeParams.g * (TahoeParams.rr1 + TahoeParams.rr2 * s);
			// calculate aerodynamic resistance
			double Ra = 0.5 * TahoeParams.rho * TahoeParams.cD * TahoeParams.cxa * s2;
			// calculate gravity resistance (negative due to pitch convention)
			double Rg = -TahoeParams.m * TahoeParams.g * Math.Sin(phi);
			// input mapping coefficient from engine torque to force
			double Btrq = Nt * TahoeParams.Nf * TahoeParams.eta / TahoeParams.r_wheel;
			// input mapping coefficient from master cylinder pressure to force
			double Bbrk = TahoeParams.bc1 * (Pmc + TahoeParams.bc0) / TahoeParams.r_wheel;
			if (gear == TransmissionGear.Reverse)
				Bbrk = -Bbrk;

			//--------------------------------------------------
			// state derivatives

			// position derivatives are based on velocity
			xdot[0] = s * Math.Cos(h);
			xdot[1] = s * Math.Sin(h);
			// calculate speed derivative = drag forces + control forces
			xdot[2] = (-(Rr + Ra + Rg) + (Btrq * Ttc - Bbrk)) / (TahoeParams.m + mr);
			// calculate heading derivative = speed * curvature
			xdot[3] = s * c;
			// calculate steering wheel angle derivative
			xdot[4] = -TahoeParams.bs * (ts - rts);

			// calculate engine torque derivative
			xdot[5] = -TahoeParams.be * (Te - rTe);
			// calculate master cylinder pressure derivative
			xdot[6] = -TahoeParams.bb * (Pmc - rPmc);
			// calculate engine speed derivative
			xdot[7] = (Te - Ttcb) / TahoeParams.Je;
		}

		// --------------------------------------------------------------------------------
		// applies physical constraints to the vehicle state, including minimum and maximum speeds, 
		// engine rpm constraints, engine torque constraints, steering constraints, etc
		private void TahoeConstraints() {
			// sets all state variables to their constrained values

			// speed constraints
			switch (gear) {
				case TransmissionGear.Reverse:
					// make sure speed is negative
					if (speed > 0.0)
						speed = 0.0;
					break;

				case TransmissionGear.Park:
					speed = 0;
					break;

				default:
					if (speed < 0.0)
						speed = 0.0;
					break;
			}

			// steering wheel constraints
			if (steering_wheel < -TahoeParams.SW_max)
				steering_wheel = -TahoeParams.SW_max;
			else if (steering_wheel > TahoeParams.SW_max)
				steering_wheel = TahoeParams.SW_max;

			// engine rpm constraints
			double rps2rpm = 60.0 / (2.0 * Math.PI);
			double rpme = engine_speed * rps2rpm;
			// do not allow engine to stall
			if (rpme < TahoeParams.rpm_idle)
				rpme = TahoeParams.rpm_idle;
			//do not allow engine to red-line
			if (rpme > TahoeParams.rpm_max)
				rpme = TahoeParams.rpm_max;
			//convert back into rad. / sec.
			engine_speed = rpme / rps2rpm;

			//engine torque constraints
			double rpme2 = rpme * rpme;
			double Te_max = TahoeParams.e0 +
							TahoeParams.e1 * rpme +
							TahoeParams.e2 * rpme2 +
							TahoeParams.e3 * rpme2 * rpme +
							TahoeParams.e4 * rpme2 * rpme2;
			//do not allow engine to provide too much torque
			if (engine_torque > Te_max)
				engine_torque = Te_max;
			//do not allow engine to provide negative torque
			if (engine_torque < TahoeParams.Te_min)
				engine_torque = TahoeParams.Te_min;

			//brake master cylinder constraints
			//do not allow negative pressure on the master cylinder
			if (master_cylinder_pressure < -TahoeParams.bc0)
				master_cylinder_pressure = -TahoeParams.bc0;

			//calculate throttle from current and maximum torque
			throttle = (engine_torque - TahoeParams.Te_min) / (Te_max - TahoeParams.Te_min);
		}

		// --------------------------------------------------------------------------------
		// returns the gear ratio of the Tahoe depending on what gear the car is in
		private double GetGearRatio() {
			double rNt; // gear ratio, sign included

			switch (gear) {
				case TransmissionGear.Reverse: rNt = TahoeParams.gR;
					break;
				case TransmissionGear.First: rNt = TahoeParams.g1;
					break;
				case TransmissionGear.Second: rNt = TahoeParams.g2;
					break;
				case TransmissionGear.Third: rNt = TahoeParams.g3;
					break;
				case TransmissionGear.Fourth: rNt = TahoeParams.g4;
					break;
				default: rNt = 0.0;
					break;
			}

			return rNt;
		}

		// --------------------------------------------------------------------------------
		// checks whether the gear is consistent with the current throttle and speed setting
		// shifts gears if necessary
		private void CheckGear() {
			// convert speed to mph
			double spd = speed * 2.23693629;

			if (throttle < 0.125) {
				// gentle acceleration
				switch (gear) {
					case TransmissionGear.First:
						// upshift
						if (spd > 10.0)
							gear = TransmissionGear.Second;
						// no downshift
						break;
					case TransmissionGear.Second:
						// upshift
						if (spd > 16.0)
							gear = TransmissionGear.Third;
						// downshift
						if (spd < 7.5)
							gear = TransmissionGear.First;
						break;
					case TransmissionGear.Third:
						// upshift
						if (spd > 24.0)
							gear = TransmissionGear.Fourth;
						// downshift
						if (spd < 7.5)
							gear = TransmissionGear.Second;
						break;
					case TransmissionGear.Fourth:
						// no upshift (no 4-lock)
						// downshift
						if (spd < 20.0)
							gear = TransmissionGear.Third;
						break;
				}
			}
			else if (throttle > 0.875) {
				// jackass acceleration
				switch (gear) {
					case TransmissionGear.First:
						// upshift
						if (spd > 32.5)
							gear = TransmissionGear.Second;
						// no downshift
						break;
					case TransmissionGear.Second:
						// upshift
						if (spd > 63.0)
							gear = TransmissionGear.Third;
						// downshift
						if (spd < 26.0)
							gear = TransmissionGear.First;
						break;
					case TransmissionGear.Third:
						// upshift
						if (spd > 101.0)
							gear = TransmissionGear.Fourth;
						// downshift
						if (spd < 62.0)
							gear = TransmissionGear.Second;
						break;
					case TransmissionGear.Fourth:
						// no upshift (no 4-lock)
						// downshift
						if (spd < 97.0)
							gear = TransmissionGear.Third;
						break;
				}
			}
			else {
				// midrange acceleration
				switch (gear) {
					case TransmissionGear.First:
						// upshift
						if (throttle < (0.875 - 0.125) * (spd - 10.0) / (32.5 - 10.0) + 0.125)
							gear = TransmissionGear.Second;
						// no downshift
						break;
					case TransmissionGear.Second:
						// upshift
						if (throttle < (0.875 - 0.125) * (spd - 16.0) / (63.0 - 16.0) + 0.125)
							gear = TransmissionGear.Third;
						// downshift
						if (speed < 7.5)
							gear = TransmissionGear.First;
						break;
					case TransmissionGear.Third:
						// upshift
						if (throttle < (0.875 - 0.125) * (spd - 24.0) / (101.0 - 24.0) + 0.125)
							gear = TransmissionGear.Fourth;
						// downshift
						if (throttle > (0.875 - 0.375) * (spd - 7.5) / (43.0 - 7.5) + 0.375)
							gear = TransmissionGear.Second;
						else if (speed < 7.5)
							gear = TransmissionGear.First;
						break;
					case TransmissionGear.Fourth:
						// no upshift (no 4-lock)
						// downshift
						if (throttle > (0.875 - 0.125) * (spd - 20.0) / (76.0 - 20.0) + 0.125)
							gear = TransmissionGear.Third;
						break; //
				}
			}
		}

		#endregion

	}
}
