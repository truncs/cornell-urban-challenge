using System;
using System.Collections.Generic;
using System.Text;

namespace RndfEditor.Display.Utilities
{
	/// <summary>
	/// Result of trying to select a display object
	/// </summary>
	[Serializable]
	public struct HitTestResult
	{
		private IDisplayObject dispObject;
		private bool hit;
		private float dist;

		public HitTestResult(IDisplayObject dispObject, bool hit, float dist)
		{
			this.dispObject = dispObject;
			this.hit = hit;
			this.dist = dist;
		}

		public IDisplayObject DisplayObject
		{
			get { return dispObject; }
		}

		public bool Hit
		{
			get { return hit; }
		}

		public float Dist
		{
			get { return dist; }
		}
	}
}
