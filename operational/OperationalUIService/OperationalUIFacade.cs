using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUIService.Parameters;
using Dataset.Source;
using UrbanChallenge.OperationalUIService.Debugging;

namespace UrbanChallenge.OperationalUIService {
	[Serializable]
	public abstract class OperationalUIFacade : MarshalByRefObject {
		public const string ServiceName = "OperationalUIService";

		/// <summary>
		/// Retrieves the current dataset.
		/// </summary>
		/// <remarks>UI ONLY</remarks>
		public abstract DatasetSourceFacade DatasetFacade { get; }

		/// <summary>
		/// Retrieves the current tunable parameter table interface
		/// </summary>
		/// <remarks>UI ONLY</remarks>
		public abstract TunableParameterFacade TunableParamFacade { get; }

		public abstract void Ping();

		public abstract DebuggingFacade DebuggingFacade { get; }
	}
}
