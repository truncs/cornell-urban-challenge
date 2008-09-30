using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.WorldSimulation {

	[Serializable()]
	public struct TotalInfo {
	}

	public abstract class Facade : MarshalByRefObject {

		public abstract void InitializeWorld(TotalInfo totalInfo);

		public abstract IWorld GetWorld();

	}

}
