using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Vehicle
{
	public static class TahoeParams // class to store the Chevy Tahoe Parameters and dynamics
	{
		// external Parameters
		/// <summary>
		/// gravity (m/s^2)
		/// </summary>
		public const double g = 9.8; 
		/// <summary>
		/// density of air (kg/m^3), at ambient pressure and about 70 deg. F
		/// </summary>
		public const double rho = 1.2; 
		/// <summary>
		/// // road surface coefficient (1.0 = smooth concrete, 1.2 = worn concrete or brick, 1.5 = hot blacktop)
		/// </summary>
		public const double rsc = 1.2; 

		// general Parameters
		/// <summary>
		/// vehicle mass (kg)
		/// </summary>
		public const double m = 2721.6;          
		/// <summary>
		/// speed independent rolling resistance coefficient (unitless)
		/// </summary>
		public const double rr1 = 0;	            
		/// <summary>
		/// speed dependent rolling resistance coefficient (sec/m)
		/// </summary>
		public const double rr2 = 0.0013;          
		// note: conversion from m/s to mph is included
		/// <summary>
		/// aerodynamic drag coefficient
		/// </summary>
		public const double cD = 0.36;			 
		/// <summary>
		/// vehicle cross sectional area (m^2)
		/// </summary>
		public const double cxa = 1.9304 * 2.0066; 
		/// <summary>
		/// wheel radius (m)
		/// </summary>
		public const double r_wheel = 0.381;	 
		/// <summary>
		/// wheel base (m), distance between two axles
		/// </summary>
		public const double L = 2.9464;		 
		/// <summary>
		/// track (m), distance between two wheels
		/// </summary>
		public const double T = 2.0066;
		/// <summary>
		/// imu offset (m), distance from rear axles to imu (verified-ish)
		/// </summary>
		public const double IL = 0.4;
		/// <summary>
		/// rear offset (m), distance from rear axles to rear bumper (verified-ish)
		/// </summary>
		public const double RL = 1.12;	
		/// <summary>
		/// front offset (m), distance from rear axles to front bumper (verified-ish) 
		/// </summary>
		public const double FL = 3.9624;
		/// <summary>
		/// vehicle length (m), distance from front bumper to rear bumper (verified-ish)
		/// </summary>
		public const double VL = RL + FL;

		// steering Parameters
		/// <summary>
		/// steering wheel characteristic settle time (~90%, sec.)
		/// </summary>
		public const double bs = 2/0.1;  
		/// <summary>
		/// cornering stiffness coefficient (rad. sec^2 / m)
		/// </summary>
		public const double cs0 = 0.00567;
		/// <summary>
		/// distance from back axle to center of rotation (m)
		/// </summary>
		public const double Lr = 0.0;              
		// steering angle (rad.) = s0 + s1*aa + s3*aa^3, aa = ackerman angle (rad.)
		/// <summary>
		/// 0th order steering coefficient
		/// </summary>
		public const double s0 = 0.0402;             
		/// <summary>
		/// 1st order steering coefficient
		/// </summary>
		public const double s1 = -17.546;           
		/// <summary>
		/// 3rd order steering coefficient
		/// </summary>
		public const double s3 = -5.9398;            
		/// <summary>
		/// maximum steering wheel angle (rad.)
		/// </summary>
		public const double SW_max = 9.826203688728;//10.471975511966;

		// drivetrain Parameters
		// inertia coefficients (kg):
		// rotating mass mr = [Iw + Id*Nf^2 + (Ie + It)*Nt^2*Nf^2] / r^2
		// Iw = moment of inertia of wheels, 
		// Id = moment of inertia of driveshaft
		// Ie = moment of inertia of engine, and 
		// It = inertia of transmission
		// mr = [mr1 + mr2*Nf^2 + mr3*Nf^2*Nt^2]
		/// <summary>
		/// rotating inertia of wheels (kg)
		/// </summary>
		public const double mr1 = 141.5;                             
		/// <summary>
		/// rotating inertia of drivetrain (kg)
		/// </summary>
		public const double mr2 = 0.05;                              
		/// <summary>
		/// rotating inertia of engine (kg)
		/// </summary>
		public const double mr3 = 5.7;                               
		/// <summary>
		/// engine moment of inertia (kg*m^2)
		/// </summary>
		public const double Je = mr3 * r_wheel*r_wheel;          
		/// <summary>
		/// engine efficiency (unitless)
		/// </summary>
		public const double eta = 0.8;                               
		/// <summary>
		/// transmission gear ratios
		/// </summary>
		public static readonly double[] Nt = { -2.29, 3.06, 1.63, 1.00, 0.70 };
		public static readonly double gR = Nt[0];
		public static readonly double g1 = Nt[1];
		public static readonly double g2 = Nt[2];
		public static readonly double g3 = Nt[3];
		public static readonly double g4 = Nt[4];
		/// <summary>
		/// final drive gear ratio
		/// </summary>
		public const double Nf = 3.73; 

		// engine torque Parameters (ft-lb, rpm)
		// rpmlu = [1000, 2000, 3000, 4000, 4200, 5000, 5600];
		// trqlu = [262.5, 300, 320, 337.5, 340, 330, 293.75];
		// engine max torque curve coefficients:
		// Tmax (N-m) = e0 + e1*rpm + e2*rpm^2 + e3*rpm^3 + e4*rpm^4
		public const double e0 = 162.391051569354 * 1.35581795;
		public const double e1 = 0.155482011069862 * 1.35581795;
		public const double e2 = -7.00414931361564e-005 * 1.35581795;
		public const double e3 = 1.61128735510829e-008 * 1.35581795;
		public const double e4 = -1.39577967829927e-012 * 1.35581795;
		

		//(valid from rpm = 500 to 6000 or so)
		/// <summary>
		/// engine torque characteristic settle time (~90%, sec.)
		/// </summary>
		public const double be = 2.0 / 0.5; 
		/// <summary>
		/// engine idle speed (rpm)
		/// </summary>
		public const double rpm_idle = 600.0;    
		/// <summary>
		/// maximum engine speed (rpm)
		/// </summary>
		public const double rpm_max = 6000.0; 
		/// <summary>
		/// minimum engine torque (N-m)
		/// </summary>
		public const double Te_min = 35;      

		// brake Parameters
		/// <summary>
		/// constant coefficient mapping master cylinder pressure (Aarons) to torque
		/// </summary>
		public const double bc0 = -22.0;     
		/// <summary>
		/// slope coefficient mapping master cylinder pressure (Aarons) to torque
		/// </summary>
		public const double bc1 = 181.4568; // 181.4568
		/// <summary>
		/// sqrt(speed) dependent brake parameter 
		/// </summary>
		public const double bcs = -27.8483; // -27.8483
		/// <summary>
		/// brake characteristic settle time (~90%, sec.)
		/// </summary>
		public const double bb = 2.0 / 0.5;
		/// <summary>
		/// proportional control parameter
		/// </summary>
		public const double K = 0.2;

		/// <summary>
		/// power difference from what the engine is reporting to what it is delivering (watts)
		/// </summary>
		public const double bleed_off_power = 2750; // 3330

		/// <summary>
		/// constant brake pressure to hold the tahoe to a stop
		/// </summary>
		public const double brake_hold = 33;
		public const double brake_hard = 45;

		/// <summary>
		/// delay in the actuation
		/// </summary>
		public const double actuation_delay = 0.21;

		public static Coordinates RearLeftCorner { get { return new Coordinates(-RL, T/2.0); } }
		public static Coordinates RearRightCorner { get { return new Coordinates(-RL, -T/2.0); } }
		public static Coordinates FrontLeftCorner { get { return new Coordinates(FL, T/2.0); } }
		public static Coordinates FrontRightCorner { get { return new Coordinates(FL, -T/2.0); } }
		public static Coordinates RearLeftWheel { get { return new Coordinates(0, T/2.0); } }
		public static Coordinates RearRightWheel { get { return new Coordinates(0, -T/2.0); } }
		public static Coordinates FrontLeftWheel { get { return new Coordinates(L, T/2.0); } }
		public static Coordinates FrontRightWheel { get { return new Coordinates(L, -T/2.0); } }

		// --------------------------------------------------------------------------------
		// calculate torque converter's current torque ratio on shaft speed ratio
		public static double TorqueRatio(double speed_ratio) {
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
			else {
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
		public static double CapacityFactor(double speed_ratio) {
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
			else {
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

		public static double CalculateCurvature(double steeringWheelAngle, double v) {
			double ts = steeringWheelAngle;

			// use newton raphson to calculate current curvature from steering wheel
			double c = 0.0;
			double atarg1, atarg2;
			double s2 = v * v;
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
				d = signc * (0.5 * Math.Atan(atarg1) + 0.5 * Math.Atan(atarg2)) + TahoeParams.cs0 * c * s2;
				sa = TahoeParams.s0 + TahoeParams.s1 * d + TahoeParams.s3 * Math.Pow(d, 3);

				// calculate partial derivatives
				somc2Lr2 = Math.Sqrt(1.0 - c2 * Lr2);
				datarg1dc = TahoeParams.L / ((somc2Lr2 + 0.5 * c * TahoeParams.T) * (somc2Lr2 + 0.5 * Math.Abs(c) * TahoeParams.T) * somc2Lr2);
				datarg2dc = TahoeParams.L / ((somc2Lr2 - 0.5 * c * TahoeParams.T) * (somc2Lr2 - 0.5 * Math.Abs(c) * TahoeParams.T) * somc2Lr2);

				// partial of ackerman angle wrt curvature
				daadc = 0.5 / (1.0 + atarg1 * atarg1) * datarg1dc + 0.5 / (1.0 + atarg2 * atarg2) * datarg2dc + TahoeParams.cs0 * s2;

				// partial of steering angle function wrt curvature
				dsadc = TahoeParams.s1 * daadc + 3 * TahoeParams.s3 * d * d * daadc;

				// calculate updated curvature using Newton's method
				dc = -(sa - ts) / dsadc;
				c = c + dc;
				if (Math.Abs(dc) < 1.0e-8)
					break;
			}

			return c;
		}
	}
}
