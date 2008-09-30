using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.NameService;
using System.Runtime.Remoting;
using UrbanChallenge.OperationalService;
using UrbanChallenge.OperationalUIService;
using System.Reflection;
using Dataset.Client;
using System.IO;
using Dataset;
using Dataset.Source;

namespace UrbanChallenge.OperationalUI {
	static class OperationalInterface {
		public class AttachEventArgs : EventArgs {
			private string suffix;

			public AttachEventArgs(string suffix) {
				this.suffix = suffix;
			}

			public string Suffix {
				get { return suffix; }
			}
		}

		public static event EventHandler<AttachEventArgs> Attached;

		private static ObjectDirectory od;

		private static OperationalFacade operationalFacade;
		private static OperationalUIFacade operationalUIFacade;

		private static DatasetClient dataset;

		static OperationalInterface() {
			Assembly thisAssm = Assembly.GetEntryAssembly();
			dataset = new DatasetClient("operational", Path.GetDirectoryName(thisAssm.Location) + "\\net.xml");
		}

		public static void ConfigureRemoting(string server) {
			RemotingConfiguration.Configure("net.xml", false);

			if (!string.IsNullOrEmpty(server)) {
				string uri = "tcp://" + server + ":12345/ObjectDirectory";
				od = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), uri);
			}
			else {
				WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();
				// "Activate" the NameService singleton.
				od = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);
			}
		}

		public static ObjectDirectory ObjectDirectory {
			get { return od; }
		}

		public static void Attach() {
			if (!Attach(string.Empty, false)) {
				Attach("_" + Environment.MachineName, true);
			}
		}

		public static bool Attach(string suffix, bool throwException) {
			operationalFacade = null;
			operationalUIFacade = null;

			if (suffix == null) suffix = string.Empty;

			try {
				operationalFacade = (OperationalFacade)od.Resolve("OperationalService" + suffix);
				operationalUIFacade = (OperationalUIFacade)od.Resolve("OperationalUIService" + suffix);

				DatasetSourceFacade dsfacade = operationalUIFacade.DatasetFacade;
				DataItemDescriptor[] dataItems = dsfacade.GetDataItems();
				foreach (DataItemDescriptor item in dataItems) {
					if (!dataset.ContainsKey(item.Name)) {
						dataset.Add(item);
					}
				}

				dataset.AttachToSource(dsfacade);
			}
			catch (Exception) {
				operationalFacade = null;
				operationalUIFacade = null;

				if (throwException)
					throw;
				else
					return false;
			}

			if (Attached != null) {
				Attached(null, new AttachEventArgs(string.IsNullOrEmpty(suffix) ? string.Empty : suffix.Substring(1)));
			}

			return true;
		}

		public static OperationalFacade OperationalFacade {
			get { return operationalFacade; }
		}

		public static OperationalUIFacade OperationalUIFacade {
			get { return operationalUIFacade; }
		}

		public static DatasetClient Dataset {
			get { return dataset; }
		}
	}
}
