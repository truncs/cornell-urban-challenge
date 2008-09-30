using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Remoting;
using UrbanChallenge.NameService;

namespace UrbanChallenge.arbiterGui.ProxyService
{
    public class ProxyService : MarshalByRefObject
    {
        static void Main(string[] args)
        {
            // Read the configuration file.
            RemotingConfiguration.Configure("..\\..\\ProxyService.exe.config", false);
            WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();            

            // "Activate" the NameService singleton.
            ObjectDirectory od = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

            // Bind the facades of components we implement.
            od.Rebind(Arbiter.FacadeImpl.Instance, "Arbiter");

            Console.WriteLine("Waiting");

            // Enter the main event loop...
            Console.ReadLine();
        }
    }
}
