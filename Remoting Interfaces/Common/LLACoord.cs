using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common {
	/// <summary>
	/// Represents a latitude, longitude, altitude coordinate.
	/// </summary>
	[Serializable]
	public struct LLACoord {
		/// <summary>
		/// Lattitude in radians
		/// </summary>
		public double lat;
		/// <summary>
		/// Longitude in radians
		/// </summary>
		public double lon;
		/// <summary>
		/// Altitude in meters
		/// </summary>
		/// <remarks>
		/// The precise meaning of the altitude is the heigh above some reference. The reference
		/// is defined by the earth model in use (i.e. geocentric, WGS84, etc.)
		/// </remarks>
		public double alt;

		/// <summary>
		/// Populates the fields of the LLA coordinate
		/// </summary>
		/// <param name="lat">Latitude in radians</param>
		/// <param name="lon">Longitude in radians</param>
		/// <param name="alt">Altitude in radians</param>
		public LLACoord(double lat, double lon, double alt) {
			this.lat = lat;
			this.lon = lon;
			this.alt = alt;
		}
	}
}
