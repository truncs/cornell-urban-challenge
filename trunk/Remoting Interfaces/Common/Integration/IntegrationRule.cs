using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Common.Integration {
	public delegate void IntegrationRule(QuadFunction f, double a, double b, out double result, out double abserr, out double resabs, out double resasc);
}
