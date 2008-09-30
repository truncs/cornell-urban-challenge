using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Vehicle;
using SimOperationalService;
using UrbanChallenge.NameService;
using UrbanChallenge.MessagingService;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using Simulator.Engine;
using UrbanChallenge.Simulator.Client.World;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Utility;
using System.Threading;

namespace UrbanChallenge.Simulator.Client
{
	[Serializable]
	public class DynamicsSimVehicle : DynamicsSimFacade
	{
		private const double Torque_max = 461;
		private const double brake_max = 100;
		private SimVehicleState simVehicleState;

		// vehicle state
		private char gear;                     // gear, 'r', '1', '2', '3', '4'
		private double throttle;                 // throttle (between 0 and 1), the fraction of maximum torque commanded
		private double steering_wheel;           // steering wheel angle (rad., positive left)
		private double engine_torque;            // engine torque (N-m)
		private double master_cylinder_pressure; // master cylinder pressure (Aarons)
		private double engine_speed;             // engine speed (rad. / sec.)

		// vehicle commands
		private double commanded_steering_wheel;
		private double commanded_engine_torque;
		private double commanded_master_cylinder_pressure;

		private volatile bool acceptCommands = true;

		private volatile CarMode carMode = CarMode.Pause;

		private IChannel operationalStateChannel;

		private object lockobj = new object();

		private double? queuedSteering1;
		private double? queuedSteering2;

		public DynamicsSimVehicle(SimVehicleState vehicleState, ObjectDirectory od)
		{
			this.simVehicleState = vehicleState;

			InitializeState();

			Connect(od);
		}

		private void Connect(ObjectDirectory od)
		{
			// get the channel for the sim state
			IChannelFactory channelFactory = (IChannelFactory)od.Resolve("ChannelFactory");
			operationalStateChannel = channelFactory.GetChannel("OperationalSimState_" + SimulatorClient.MachineName, ChannelMode.Bytestream);

			// publish ourself to the object directory
			od.Rebind(this, "DynamicsSim_" + SimulatorClient.MachineName);
		}

		// initialise vehicle state and commands
		private void InitializeState()
		{
			// initialise vehicle state
			gear = '1';                     // gear, 'r', '1', '2', '3', '4'
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

		public bool AcceptCommands
		{
			get { return acceptCommands; }
			set { acceptCommands = value; }
		}

		public void Reset() {
			lock (lockobj) {
				InitializeState();
				simVehicleState.Position = new Coordinates(0, 0);
				simVehicleState.Heading = new Coordinates(1, 0);
				simVehicleState.Speed = 0;
			}
		}

		public SimVehicleState VehicleState
		{
			get
			{
				return simVehicleState;
			}
			set
			{
				simVehicleState = value;
			}
		}

		public SimVehicleId Name
		{
			get { return this.simVehicleState.VehicleID; }
		}

		public SimVehicleState Update(double dt, WorldService service)
		{
			UpdateVehicleState(dt);
			TahoeConstraints();
			CheckGear();

			// send out the vehicle state
			operationalStateChannel.PublishUnreliably(GetOperationalSimVehicleState(service));

			return simVehicleState;
		}

		public OperationalSimVehicleState GetOperationalSimVehicleState(WorldService service)
		{
			TransmissionGear g = TransmissionGear.Neutral;
			switch (gear)
			{
				case '1': g = TransmissionGear.First; break;
				case '2': g = TransmissionGear.Second; break;
				case '3': g = TransmissionGear.Third; break;
				case '4': g = TransmissionGear.Fourth; break;
				case 'r': g = TransmissionGear.Reverse; break;
			}

			double rps2rpm = 60.0 / (2.0 * Math.PI);
			double bleed_off_torque = TahoeParams.bleed_off_power / engine_speed;

			return new OperationalSimVehicleState(
				simVehicleState.Position, simVehicleState.Speed, simVehicleState.Heading.ArcTan, steering_wheel,
				g, engine_torque + bleed_off_torque, master_cylinder_pressure, engine_speed * rps2rpm, carMode, 
				0, false,	SimulatorClient.GetCurrentTimestamp);
		}

		// --------------------------------------------------------------------------------
		// updates the vehicle state using the vehicle dynamics
		private void UpdateVehicleState(double dt)
		{
			double[] k1 = new double[8];
			double[] k2 = new double[8];
			double[] k3 = new double[8];
			double[] k4 = new double[8];

			double[] xdot = new double[8];
			double[] xcur = new double[8];
			double[] xtemp = new double[8];

			xcur[0] = simVehicleState.Position.X;
			xcur[1] = simVehicleState.Position.Y;
			xcur[2] = simVehicleState.Speed;
			xcur[3] = simVehicleState.Heading.ArcTan;
			xcur[4] = steering_wheel;
			xcur[5] = engine_torque;
			xcur[6] = master_cylinder_pressure;
			xcur[7] = engine_speed;

			// do the queue on the steering commands
			lock (lockobj) {
				if (queuedSteering1.HasValue) {
					commanded_steering_wheel = queuedSteering1.Value;
				}

				queuedSteering1 = queuedSteering2;
				queuedSteering2 = null;
			}

			// make a copy to get consistent reads
			double csw = commanded_steering_wheel;
			double cet = commanded_engine_torque;
			double cmcp = commanded_master_cylinder_pressure;

			// Runge-Kutta - 1st iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xcur[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, 0);
			for (int i = 0; i < 8; i++)
			{
				k1[i] = dt * xdot[i];
				xtemp[i] = xcur[i] + k1[i] * 0.5;
			}
			//if (xtemp[4] > TahoeParams.SW_max) xtemp[4] = TahoeParams.SW_max;
			//if (xtemp[4] < -TahoeParams.SW_max) xtemp[4] = -TahoeParams.SW_max;
			if (gear == 'r')
			{
				if (xtemp[2] > 0) xtemp[2] = 0;
			}
			else
			{
				if (xtemp[2] < 0) xtemp[2] = 0;
			}

			// Runge-Kutta - 2nd iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xtemp[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, 0);
			for (int i = 0; i < 8; i++)
			{
				k2[i] = dt * xdot[i];
				xtemp[i] = xcur[i] + k2[i] * 0.5;
			}
			//if (xtemp[4] > TahoeParams.SW_max) xtemp[4] = TahoeParams.SW_max;
			//if (xtemp[4] < -TahoeParams.SW_max) xtemp[4] = -TahoeParams.SW_max;
			if (gear == 'r')
			{
				if (xtemp[2] > 0) xtemp[2] = 0;
			}
			else
			{
				if (xtemp[2] < 0) xtemp[2] = 0;
			}

			// Runge-Kutta - 3rd iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xtemp[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, 0);
			for (int i = 0; i < 8; i++)
			{
				k3[i] = dt * xdot[i];
				xtemp[i] = xcur[i] + k3[i];
			}
			//if (xtemp[4] > TahoeParams.SW_max) xtemp[4] = TahoeParams.SW_max;
			//if (xtemp[4] < -TahoeParams.SW_max) xtemp[4] = -TahoeParams.SW_max;
			if (gear == 'r')
			{
				if (xtemp[2] > 0) xtemp[2] = 0;
			}
			else
			{
				if (xtemp[2] < 0) xtemp[2] = 0;
			}

			// Runge-Kutta - 4th iteration
			for (int i = 0; i < 8; i++)
				xdot[i] = xtemp[i];
			TahoeDynamics(ref xdot, csw, cet, cmcp, 0);
			for (int i = 0; i < 8; i++)
			{
				k4[i] = dt * xdot[i];
			}

			// Runge-Kutta - Final computation
			for (int i = 0; i < 8; i++)
			{
				xdot[i] = (k1[i] / 6.0) + (k2[i] / 3.0) + (k3[i] / 3.0) + (k4[i] / 6.0);
				xcur[i] += xdot[i];
			}

			simVehicleState.Position.X = xcur[0];
			simVehicleState.Position.Y = xcur[1];
			simVehicleState.Speed = xcur[2];
			simVehicleState.Heading = new Coordinates(1, 0).Rotate(xcur[3]);
			steering_wheel = xcur[4];
			engine_torque = xcur[5];
			master_cylinder_pressure = xcur[6];
			engine_speed = xcur[7];
		}

		// --------------------------------------------------------------------------------
		// computes the state derivative of a car's drivetrain model
		public void TahoeDynamics(ref double[] xdot, double rts, double rTe, double rPmc, double phi)
		{
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
			for (int i = 0; i < 10; i++)
			{
				// precalculate repeated variables to save cycles
				//double 
				c2 = c * c;
				//double 
				Lr2 = TahoeParams.Lr * TahoeParams.Lr;
				//double 
				signc = Math.Sign(c);

				// calculate the current arguments of the arctangent
				if (Math.Abs(c) > 1e-10)
				{
					//double 
					ooc2 = 1.0 / c2;
					atarg1 = TahoeParams.L / (Math.Sqrt(ooc2 - Lr2) + 0.5 * TahoeParams.T);
					atarg2 = TahoeParams.L / (Math.Sqrt(ooc2 - Lr2) - 0.5 * TahoeParams.T);
				}
				else
				{
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

			if (double.IsNaN(c) || Math.Abs(c) > 0.2) {
				Console.WriteLine("c = {0:F4}, s = {1:F4}", c, ts);
			}

			if (Math.Abs(c) > 0.2) {
				c = Math.Sign(c)*0.2;
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
			double tr = TorqueRatio(sr);
			// Ktc = interp1(srlu, cflu, sr, 'linear', 'extrap');
			double Ktc = CapacityFactor(sr);
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
			if (gear == 'r')
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
			if (Math.Abs(xdot[4]) > 700*Math.PI/180.0) {
				xdot[4] = Math.Sign(xdot[4])*700*Math.PI/180.0;
			}

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
		public void TahoeConstraints()
		{
			// sets all state variables to their constrained values

			// speed constraints
			switch (gear)
			{
				case 'r':
					// make sure speed is negative
					if (simVehicleState.Speed > 0.0)
						simVehicleState.Speed = 0.0;
					break;

				default:
					if (simVehicleState.Speed < 0.0)
						simVehicleState.Speed = 0.0;
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
		private double GetGearRatio()
		{
			double rNt; // gear ratio, sign included

			switch (gear)
			{
				case 'r': rNt = TahoeParams.gR;
					break;
				case '1': rNt = TahoeParams.g1;
					break;
				case '2': rNt = TahoeParams.g2;
					break;
				case '3': rNt = TahoeParams.g3;
					break;
				case '4': rNt = TahoeParams.g4;
					break;
				default: rNt = 0.0;
					break;
			}

			return rNt;
		}

		// --------------------------------------------------------------------------------
		// calculate torque converter's current torque ratio on shaft speed ratio
		private double TorqueRatio(double speed_ratio)
		{
			// input:  speed_ratio - ratio of engine shaft speed to driveshaft speed (post torque converter)
			// output: rtr         - returns the torque ratio of the output torque to the input torque at the converter

			double rtr;

			// define the standard torque converter curve
			double[] srlu = {0.0,  0.50, 0.60, 0.70, 0.80, 
                             0.81, 0.82, 0.83, 0.84, 0.85, 
                             0.86, 0.87, 0.88, 0.89, 0.90, 
                             0.92, 0.94, 0.96, 0.97};
			double[] trlu = {2.2320, 1.5462, 1.4058, 1.2746, 1.1528, 
                             1.1412, 1.1296, 1.1181, 1.1067, 1.0955, 
                             1.0843, 1.0732, 1.0622, 1.0513, 1.0405, 
                             1.0192, 0.9983, 0.9983, 0.9983};

			if (speed_ratio <= srlu[0])
				rtr = trlu[0];  // return maximum torque multiplication
			else if (speed_ratio >= srlu[18])
				rtr = trlu[18]; // return minimum torque multiplication
			else
			{
				// interpolate between two points
				int i;
				for (i = 17; i >= 0; i--)
					if (speed_ratio > srlu[i])
						break;
				// i is now the index of the first speed ratio less than iSpeedRatio
				// do linear interpolation of standard torque converter curve
				rtr = (trlu[i + 1] - trlu[i]) * (speed_ratio - srlu[i]) / (srlu[i + 1] - srlu[i]) + trlu[i];
			}

			return rtr;
		}

		// --------------------------------------------------------------------------------
		// calculates the torque converter's capacity factor based on shaft speed ratio
		private double CapacityFactor(double speed_ratio)
		{
			// inputs:  speed_ratio - ratio of engine shaft speed to driveshaft speed (post torque converter)
			// outputs: rcf         - returns the capacity factor of the torque converter, spd / sqrt(torque), in (rad/sec)/sqrt(N-m)

			double rcf;

			// define the standard torque converter curve
			double[] srlu = {0.0,  0.50, 0.60, 0.70, 0.80, 
                             0.81, 0.82, 0.83, 0.84, 0.85, 
                             0.86, 0.87, 0.88, 0.89, 0.90, 
                             0.92, 0.94, 0.96, 0.97};
			double[] cflu = {12.2938, 12.8588, 13.1452, 13.6285, 14.6163, 
                             14.7747, 14.9516, 15.1502, 15.3748, 15.6309, 
                             15.9253, 16.2675, 16.6698, 17.1492, 17.7298, 
                             19.3503, 22.1046, 29.9986, 50.00};

			if (speed_ratio <= srlu[0])
				rcf = cflu[0]; // return minimum capacity factor
			else if (speed_ratio >= srlu[18])
				rcf = cflu[18]; // return maximum capacity factor
			else
			{
				// interpolate between two points
				int i;
				for (i = 17; i >= 0; i--)
					if (speed_ratio > srlu[i])
						break;
				//i is now the index of the first speed ratio less than iSpeedRatio
				//do linear interpolation of standard torque converter curve
				rcf = (cflu[i + 1] - cflu[i]) * (speed_ratio - srlu[i]) / (srlu[i + 1] - srlu[i]) + cflu[i];
			}

			return rcf;
		}

		// --------------------------------------------------------------------------------
		// checks whether the gear is consistent with the current throttle and speed setting
		// shifts gears if necessary
		private void CheckGear()
		{
			// convert speed to mph
			double spd = simVehicleState.Speed * 2.23693629;

			if (throttle < 0.125)
			{
				// gentle acceleration
				switch (gear)
				{
					case '1':
						// upshift
						if (spd > 10.0)
							gear = '2';
						// no downshift
						break;
					case '2':
						// upshift
						if (spd > 16.0)
							gear = '3';
						// downshift
						if (spd < 7.5)
							gear = '1';
						break;
					case '3':
						// upshift
						if (spd > 24.0)
							gear = '4';
						// downshift
						if (spd < 7.5)
							gear = '1';
						break;
					case '4':
						// no upshift (no 4-lock)
						// downshift
						if (spd < 20.0)
							gear = '3';
						break;
				}
			}
			else if (throttle > 0.875)
			{
				// jackass acceleration
				switch (gear)
				{
					case '1':
						// upshift
						if (spd > 32.5)
							gear = '2';
						// no downshift
						break;
					case '2':
						// upshift
						if (spd > 63.0)
							gear = '3';
						// downshift
						if (spd < 26.0)
							gear = '1';
						break;
					case '3':
						// upshift
						if (spd > 101.0)
							gear = '4';
						// downshift
						if (spd < 62.0)
							gear = '2';
						break;
					case '4':
						// no upshift (no 4-lock)
						// downshift
						if (spd < 97.0)
							gear = '3';
						break;
				}
			}
			else
			{
				// midrange acceleration
				switch (gear)
				{
					case '1':
						// upshift
						if (throttle < (0.875 - 0.125) * (spd - 10.0) / (32.5 - 10.0) + 0.125)
							gear = '2';
						// no downshift
						break;
					case '2':
						// upshift
						if (throttle < (0.875 - 0.125) * (spd - 16.0) / (63.0 - 16.0) + 0.125)
							gear = '3';
						// downshift
						if (simVehicleState.Speed < 7.5)
							gear = '1';
						break;
					case '3':
						// upshift
						if (throttle < (0.875 - 0.125) * (spd - 24.0) / (101.0 - 24.0) + 0.125)
							gear = '4';
						// downshift
						if (throttle > (0.875 - 0.375) * (spd - 7.5) / (43.0 - 7.5) + 0.375)
							gear = '2';
						else if (simVehicleState.Speed < 7.5)
							gear = '1';
						break;
					case '4':
						// no upshift (no 4-lock)
						// downshift
						if (throttle > (0.875 - 0.125) * (spd - 20.0) / (76.0 - 20.0) + 0.125)
							gear = '3';
						break; //
				}
			}
		}

		public override void SetSteeringBrakeThrottle(double? commanded_steer, double? commanded_throttle, double? commanded_brake)
		{
			if (acceptCommands)
			{
				lock (lockobj)
				{
					if (commanded_throttle.HasValue && commanded_throttle.Value > Torque_max) commanded_throttle = Torque_max;
					if (commanded_brake.HasValue && commanded_brake.Value > brake_max) commanded_brake = brake_max;

					if (commanded_steer.HasValue) {
						queuedSteering2 = commanded_steer.Value;
						//commanded_steering_wheel = commanded_steer.Value;
					}
					if (commanded_throttle.HasValue)
					{
						double bleed_off_torque = TahoeParams.bleed_off_power / engine_speed;
						commanded_engine_torque = commanded_throttle.Value - bleed_off_torque;
					}
					if (commanded_brake.HasValue) {
						Thread.VolatileWrite(ref commanded_master_cylinder_pressure, Math.Round(commanded_brake.Value));
					}

					if (commanded_engine_torque < TahoeParams.Te_min)
						commanded_engine_torque = TahoeParams.Te_min;

					//Debug.WriteLine(string.Format("received command {0},{1},{2}", commanded_steer, commanded_throttle, commanded_brake));
				}
			}
			else
			{
				Debug.WriteLine(string.Format("not accepting, but received command {0},{1},{2}", commanded_steer, commanded_throttle, commanded_brake));
			}
		}

		public override void SetTransmissionGear(TransmissionGear g)
		{
			if (g >= TransmissionGear.First && g <= TransmissionGear.Fourth)
			{
				gear = '1';
			}
			else if (g == TransmissionGear.Reverse)
			{
				gear = 'r';
			}
		}

		public void SetRunMode(CarMode mode)
		{
			this.carMode = mode;

			if (carMode == CarMode.Run) {
				acceptCommands = true;
			}
			else {
				acceptCommands = false;
			}
		}

		public void PauseVehicle()
		{
			lock (lockobj) {
				this.commanded_engine_torque = 0;
				this.commanded_master_cylinder_pressure = 35;
				this.commanded_steering_wheel = 0;
				carMode = CarMode.Pause;
				acceptCommands = false;
			}
		}

		public void RunVehicle()
		{
			carMode = CarMode.Run;
			acceptCommands = true;
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}

