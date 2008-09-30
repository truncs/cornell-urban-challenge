using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using RndfEditor.Display.Utilities;
using Simulator.Display.DisplayObjects;

namespace Simulator.Files
{
	/// <summary>
	/// Save point of the Road Display
	/// </summary>
	[Serializable]
	public class DisplaySave
	{
		/// <summary>
		/// Center of display
		/// </summary>
		public Coordinates center;

		/// <summary>
		/// Scale of display
		/// </summary>
		public float scale;

		/// <summary>
		/// List of display objects
		/// </summary>
		public List<IDisplayObject> displayObjects;

		/// <summary>
		/// The display grid
		/// </summary>
		public GridDisplay DisplayGrid;
	}
}
