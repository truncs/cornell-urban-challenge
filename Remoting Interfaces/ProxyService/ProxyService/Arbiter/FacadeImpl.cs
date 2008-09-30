using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.Common;
using UrbanChallenge.arbiter;

namespace UrbanChallenge.arbiterGui.ProxyService.Arbiter
{
    internal class FacadeImpl : Facade
    {
        // Singleton pattern. Static read-only instance property.
        public static Facade Instance
        {
            get
            {
                if (instance == null)
                    instance = new FacadeImpl();
                return instance;
            }
        }

        public override void GetRndf()
        {
            Console.WriteLine("returned");
        }

        // Live forever.
        public override object InitializeLifetimeService()
        {
            return null;
        }

        // The singleton instance.
        private static Facade instance = null;

        // Private constructor (singleton pattern).
        private FacadeImpl()
        {
        }
    }
}
