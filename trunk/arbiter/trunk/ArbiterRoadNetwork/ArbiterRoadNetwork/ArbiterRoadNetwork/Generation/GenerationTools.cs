using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Tools to help with generation of a network
	/// </summary>
	public static class GenerationTools
	{
		/// <summary>
		/// Parses an id for its integer components
		/// </summary>
		/// <param name="stringId"></param>
		/// <returns></returns>
		public static int[] GetId(string stringId)
		{
			char[] delimters = { '.' };
			string[] splitIds = stringId.Split(delimters);

			int[] id = new int[splitIds.Length];

			for (int i = 0; i < splitIds.Length; i++)
			{
				id[i] = int.Parse(splitIds[i]);
			}

			return id;
		}
	}
}
