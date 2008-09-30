using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CarBrowser.Config {
	[XmlRoot("carBrowserConfig")]
	public class CarBrowserConfig {
		[XmlArrayItem("microConfig")]
		public List<MicroConfig> microcontrollers;

		[XmlArrayItem("channelConfig")]
		public List<ChannelConfig> staticChannels;

		[XmlArrayItem("computerName")]
		public List<string> computers;

		public string nameServer;

		public bool checkActuation;

		public CarBrowserConfig() {
			microcontrollers = new List<MicroConfig>();
			staticChannels = new List<ChannelConfig>();
			computers = new List<string>();
		}
	}
}
