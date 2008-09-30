using System;
using System.Collections.Generic;
using System.Text;
using CarBrowser.Channels;
using System.Xml.Serialization;

namespace CarBrowser.Config {
	public class ChannelConfig {
		[XmlAttribute]
		public string name;
		[XmlAttribute]
		public string address;
		[XmlAttribute]
		public int port;
		[XmlAttribute]
		public ChannelType channelType;
	}
}
