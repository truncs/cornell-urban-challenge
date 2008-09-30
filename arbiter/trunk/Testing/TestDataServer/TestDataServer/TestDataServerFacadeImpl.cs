using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common.RndfNetwork;
using UrbanChallenge.Common.Vehicle;

namespace UrbanChallenge.Arbiter.TestDataServer
{
	[Serializable]
	public class TestDataServerFacadeImpl : TestServerFacade
	{
		private RndfNetwork rndfNetwork;
		private Mdf mdf;
		private VehicleState vehicleState;

		/// <summary>
		/// Retreive the Rndf network
		/// </summary>
		public override RndfNetwork RndfNetwork
		{
			get
			{
				Console.WriteLine("Sent Rndf");
				return rndfNetwork; }
		}

		public override Mdf Mdf
		{
			get { return mdf; }
		}

		public override VehicleState VehicleState
		{
			get { return vehicleState; }
		}

		/// <summary>
		/// Singleton pattern. Static read-only instance property.
		/// </summary>
		public static TestServerFacade Instance(RndfNetwork rndfNetwork, Mdf mdf, VehicleState vehicleState)
        {
            if (instance == null)
				instance = new TestDataServerFacadeImpl(rndfNetwork, mdf, vehicleState);
            return instance;
     
        }

        /// <summary>
		/// Live forever.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
		/// The singleton instance.
        /// </summary>
        private static TestServerFacade instance = null;

        /// <summary>
		/// Private constructor (singleton pattern). (Can add fields as needed to constructor)
        /// </summary>
		/// <param name="rndfNetwork">the rndf network</param>
        private TestDataServerFacadeImpl(RndfNetwork rndfNetwork, Mdf mdf, VehicleState vehicleState)
        {
			this.rndfNetwork = rndfNetwork;
			this.mdf = mdf;
			this.vehicleState = vehicleState;
        }
	}
}
