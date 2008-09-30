using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public static class DisplayObjectService {
		public static string BuildName(string name, params string[] groups) {
			string fullName = "";
			if (groups != null) {
				for (int i = 0; i < groups.Length; i++) {
					fullName += groups[i] + "/";
				}
			}

			fullName += name;

			return fullName;
		}

		public static string[] GetGroups(IRenderable obj) {
			string name = obj.Name;
			string[] parts = name.Split('/');

			if (parts.Length == 1) {
				return new string[0];
			}
			else {
				string[] ret = new string[parts.Length-1];
				for (int i = 0; i < parts.Length-1; i++) {
					ret[i] = parts[i];
				}

				return ret;
			}
		}

		public static string GetDisplayName(IRenderable obj) {
			string name = obj.Name;
			string[] parts = name.Split('/');

			return parts[parts.Length-1];
		}
	}
}
