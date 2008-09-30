using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.EarthModel {
	/// <summary>
	/// Transforms coordinates between ECEF (Earth Centered Earth Fixed) and 
	/// LLA (Latitude Longitude Altitude) with respect to the WGS-84 ellipsoid
	/// </summary>
	public static class WGS84 {
		public const double ae = 6378137;
		public const double be = 6356752.31424518;
		public static readonly double e = Math.Sqrt((ae * ae - be * be) / (ae * ae));
		public static readonly double ep = Math.Sqrt((ae * ae - be * be) / (be * be));

		public static Vector3 LLAtoECEF(LLACoord lla) {
			double sin_lat = Math.Sin(lla.lat);
			double cos_lat = Math.Cos(lla.lat);
			double N = ae / Math.Sqrt(1 - e * e * sin_lat * sin_lat);

			return new Vector3(
				(N + lla.alt) * cos_lat * Math.Cos(lla.lon),
				(N + lla.alt) * cos_lat * Math.Sin(lla.lon),
				(be * be / (ae * ae) * N + lla.alt) * sin_lat);
		}

		public static LLACoord ECEFtoLLA(Vector3 ecef) {
			double lon = Math.Atan2(ecef.Y, ecef.X);

			double p = Math.Sqrt(ecef.X * ecef.X + ecef.Y * ecef.Y);
			double theta = Math.Atan2(ecef.Z * ae, p * be);
			double st = Math.Sin(theta);
			double ct = Math.Cos(theta);
			double lat = Math.Atan2(ecef.Z + ep * ep * be * st * st * st, p - e * e * ae * ct * ct * ct);

			double sin_lat = Math.Sin(lat);
			double N = ae / Math.Sqrt(1 - e * e * sin_lat * sin_lat);
			double alt = p / Math.Cos(lat) - N;

			return new LLACoord(lat, lon, alt);
		}
	}
}
