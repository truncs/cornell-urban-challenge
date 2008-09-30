using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Roots {
	/// <summary>
	/// Delegate used for calculating the roots (i.e. zero cross) of a scalar function with single parameter t
	/// </summary>
	/// <param name="t">Parameter value</param>
	/// <returns>Function evaluated at parameter t</returns>
	public delegate double RootFunction(double t);
}
