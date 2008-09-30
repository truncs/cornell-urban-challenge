using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Tools
{
	/// <summary>
	/// Common tools for parameterizing speed
	/// </summary>
	public static class SpeedTools
	{
		/// <summary>
		/// Generates speed command from parameters
		/// </summary>
		/// <param name="d">distance from current position until final</param>
		/// <param name="vi">current speed</param>
		/// <param name="vf">final speed</param>
		/// <param name="vMax">maximum segment speed</param>
		/// <returns></returns>
		public static double GenerateSpeed(double d, double vf, double vMax)
		{
			// slow down early
			d = vf == 0.0 ? d : d - TahoeParams.VL;

			if (d <= 0)
				return vf;
			else if (0 < d && d <= CoreCommon.AccelerationBreakDistance)
			{
				double vBreak = Math.Sqrt(Math.Pow(vf, 2) - (2.0 * CoreCommon.DesiredNegativeAcceleration * CoreCommon.AccelerationBreakDistance));
				double vc = vf + ((d / CoreCommon.AccelerationBreakDistance) * (vBreak - vf)); //Math.Sqrt(Math.Pow(vf, 2) - (2.0 * CoreCommon.DesiredNegativeAcceleration * d));
				return Math.Min(vc, vMax);
			}
			else
			{
				double vBreak = Math.Sqrt(Math.Pow(vf, 2) - (2.0 * CoreCommon.DesiredNegativeAcceleration * CoreCommon.AccelerationBreakDistance));
				double vc = Math.Sqrt(Math.Pow(vBreak, 2) - (2.0 * CoreCommon.MaximumNegativeAcceleration * (d - CoreCommon.AccelerationBreakDistance)));
				return Math.Min(vc, vMax);
			}
		}
	}
}
