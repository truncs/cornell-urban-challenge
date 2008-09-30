using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.Core
{
	/// <summary>
	/// Entry point for the arbiter
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Main function handling entry into the application
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			// generate core
			ArbiterCore ac = new ArbiterCore();

			// begin
			ac.BeginArbiterCore();
		}
	}
}
