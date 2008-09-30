using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Simulator.Client;
using UrbanChallenge.Operational.Common;

namespace UrbanChallenge.OperationalUIService.Debugging {
	/// <summary>
	/// Provides facilities for advanced debugging
	/// </summary>
	[Serializable]
	public abstract class DebuggingFacade : ClientRunControlFacade {
		public abstract PlanningGrid GetPlanningGrid(PlanningGrids grid);

		public abstract AvoidanceDetails GetAvoidanceDetails();
		public abstract bool GenerateAvoidanceDetails { get; set; }

		public abstract bool StepMode { get; }
	}
}
