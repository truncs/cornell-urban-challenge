using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.NameService;
using UrbanChallenge.MessagingService;
using System.Runtime.Remoting;

namespace OperationalLayer.Communications {
	static class CommBuilder {
		private static string machineName = null;
		private static ObjectDirectory objectDirectory;
		private static IChannelFactory channelFactory;

		public static void InitComm() {
			// set the machine name from the config
			machineName = Properties.Settings.Default.MachineName.Trim().ToUpper();

			// configure remoting 
			RemotingConfiguration.Configure("net.xml", false);

			// get the name service
			WellKnownServiceTypeEntry[] wkst = RemotingConfiguration.GetRegisteredWellKnownServiceTypes();
			objectDirectory = (ObjectDirectory)Activator.GetObject(typeof(ObjectDirectory), wkst[0].ObjectUri);

			// get the channel factory
			channelFactory = (IChannelFactory)objectDirectory.Resolve("ChannelFactory");
		}

		public static string BuildServiceName(string baseName) {
			if (OperationalBuilder.BuildMode == BuildMode.FullSim) {
				if (string.IsNullOrEmpty(machineName))
					machineName = Environment.MachineName;

				return baseName + "_" + machineName;
			}
			else {
				return baseName;
			}
		}

		public static void BindObject(string baseName, MarshalByRefObject obj) {
			// bind the object to the object directory
			objectDirectory.Rebind(obj, BuildServiceName(baseName));
		}

		public static MarshalByRefObject GetObject(string baseName) {
			return objectDirectory.Resolve(BuildServiceName(baseName));
		}

		public static IChannel GetChannel(string name) {
			return channelFactory.GetChannel(BuildServiceName(name), ChannelMode.Bytestream);
		}
	}
}
