using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.EarthModel {
	public static class Geocentric {
		public static Matrix3 Recef2enu(Vector3 ecef){
			double Rxy = Math.Sqrt(ecef.X*ecef.X + ecef.Y*ecef.Y);
			double gc_lon = Math.Atan2(ecef.Y, ecef.X);
			double gc_lat = Math.Atan2(ecef.Z, Rxy);
			double slon = Math.Sin(gc_lon), clon = Math.Cos(gc_lon);
			double slat = Math.Sin(gc_lat), clat = Math.Cos(gc_lat);

			return new Matrix3(
				-slon, clon, 0,
				-clon*slat, -slon*slat, clat,
				clon*clat, slon*clat, slat);
		}
	}
}
