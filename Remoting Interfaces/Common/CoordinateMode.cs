using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common {
	/// <summary>
	/// Specifies the form of the coordinates of the path
	/// </summary>
	[Serializable]
	public enum CoordinateMode {
		/// <summary>
		/// Path is vehicle relative. i.e. the coordinates of the path are what 
		/// the vehicle would see 
		/// </summary>
		VehicleRelative,
		/// <summary>
		/// Path is absolute with respect to the projection in use on the vehicle
		/// </summary>
		AbsoluteProjected
		/* These can be implemented later if we want
		 * AbsoluteLatLon,
		 * AbsoluteSceneEst
		 */
	}
}
