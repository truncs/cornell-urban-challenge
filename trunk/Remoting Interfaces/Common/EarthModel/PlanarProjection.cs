using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Mapack;

namespace UrbanChallenge.Common.EarthModel {
	[Serializable]
	public class PlanarProjection {
		private Vector3 originECEF;
		private Matrix R_enu_to_ecef;
		private Matrix R_ecef_to_enu;

		public PlanarProjection(double originLat, double originLon) {
			LLACoord originLLA = new LLACoord(originLat, originLon, 0);
			originECEF = WGS84.LLAtoECEF(originLLA);

			double slon = Math.Sin(originLon);
			double clon = Math.Cos(originLon);
			double slat = Math.Sin(originLat);
			double clat = Math.Cos(originLat);

			R_enu_to_ecef = new Matrix(3, 3);
			R_enu_to_ecef[0, 0] = -slon;
			R_enu_to_ecef[0, 1] = -clon * slat;
			R_enu_to_ecef[0, 2] = clon * clat;
			R_enu_to_ecef[1, 0] = clon;
			R_enu_to_ecef[1, 1] = -slon * slat;
			R_enu_to_ecef[1, 2] = slon * clat;
			R_enu_to_ecef[2, 0] = 0;
			R_enu_to_ecef[2, 1] = clat;
			R_enu_to_ecef[2, 2] = slat;

			R_ecef_to_enu = R_enu_to_ecef.Transpose();
		}

		public Coordinates ECEFtoXY(Vector3 ecef) {
			// convert to zero altitude lla
			LLACoord lla = WGS84.ECEFtoLLA(ecef);
			lla.alt = 0;
			ecef = WGS84.LLAtoECEF(lla);
			Vector3 enu = R_ecef_to_enu * (ecef - originECEF);

			return new Coordinates(enu.X, enu.Y);
		}

		public Vector3 XYtoECEF(Coordinates xy) {
			return R_enu_to_ecef * new Vector3(xy.X, xy.Y, 0) + originECEF;
		}
	}
}
