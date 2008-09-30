using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;


namespace UrbanChallenge.Behaviors
{
	[Serializable]
	public class UTurnBehavior : Behavior {
		private PathFollowingBehavior exitPath;
		private PathFollowingBehavior entryPath;
		private Polygon polygon;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exitPath"></param>
		/// <param name="entryPath"></param>
		/// <param name="polygon"></param>
		public UTurnBehavior(PathFollowingBehavior exitPath, PathFollowingBehavior entryPath, Polygon polygon)
		{
			this.exitPath = exitPath;
			this.entryPath = entryPath;
			this.polygon = polygon;
		}

		public PathFollowingBehavior ExitPath {
			get { return exitPath; }
		}

		public PathFollowingBehavior EntryPath {
			get { return entryPath; }
		}

		public Polygon BoundingPolygon {
			get { return polygon; }
		}

		public override Behavior NextBehavior {
			get {
				// this should never really happen
				return exitPath;
			}
		}
		public override string ToString()
		{
			return "Behavior: UTurn";
		}
	}
}
