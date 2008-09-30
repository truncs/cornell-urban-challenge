using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Net;

namespace CarBrowser.Config {
	public class MicroConfig {
		[XmlAttribute]
		public string name;
		[XmlAttribute]
		public string address;
		[XmlAttribute]
		public bool supportsTiming;
		[XmlAttribute]
		public int powerPort;

		public MicroConfig() {
		}

		public MicroConfig(string name, string address, bool supportsTiming, int powerPort) {
			this.name = name;
			this.address = address;
			this.supportsTiming = supportsTiming;
			this.powerPort = powerPort;
		}

		[XmlIgnore]
		public IPAddress Address {
			get {
				IPAddress ipAddress;
				if (IPAddress.TryParse(address, out ipAddress)) {
					return ipAddress;
				}
				else {
					return null;
				}
			}
		}
	}
}
