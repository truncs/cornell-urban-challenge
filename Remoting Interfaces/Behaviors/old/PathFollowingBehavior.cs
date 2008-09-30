using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.Path;
using System.Diagnostics;
using UrbanChallenge.Common;

namespace UrbanChallenge.Behaviors {
	/// <summary>
	/// Behavior specifying that the vehicle should follow the specified path
	/// </summary>
	[Serializable]
	public class PathFollowingBehavior : Behavior {
		private IPath path;

		private SpeedCommand speedCommand;

		/// <summary>
		/// Constructs the path following with the specified path and with the specified speed command.
		/// </summary>
		/// <param name="path">Path to follow.</param>
		/// <param name="coordMode">Form of the coordinates in path.</param>
		/// <param name="speedCommand">Speed command associated with the command.</param>
		public PathFollowingBehavior(IPath path, SpeedCommand speedCommand) {
			if (speedCommand == null)
				throw new ArgumentNullException("speedCommand");
			if (path == null)
				throw new ArgumentNullException("path");
			if (path.Count == 0)
				throw new ArgumentException("Path must have at least one segment", "path");

			this.path = path;
			this.speedCommand = speedCommand;
		}

		/// <summary>
		/// Path to follow.
		/// </summary>
		public IPath Path {
			get { return path; }			
		}

		/// <summary>
		/// Speed command to execute.
		/// </summary>
		public SpeedCommand SpeedCommand {
			get { return speedCommand; }
		}

		public override bool Equals(object obj) {
			PathFollowingBehavior pb = obj as PathFollowingBehavior;
			if (pb != null) {
				return object.Equals(speedCommand, pb.speedCommand) && object.Equals(path, pb.path);
			}
			else {
				return false;
			}
		}

		public override int GetHashCode() {
			return speedCommand.GetHashCode() ^ path.GetHashCode();
		}

		public override string ToString() {
			return "Behavior: PathFollowing";
		}
	}
}
